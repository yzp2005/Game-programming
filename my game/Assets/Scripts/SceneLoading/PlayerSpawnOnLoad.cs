using UnityEngine;

/// <summary>新场景加载后，若 SceneTransition 有出生点 Id，把玩家挪到对应 PlayerSpawnPoint。</summary>
public class PlayerSpawnOnLoad : MonoBehaviour
{
    void Start()
    {
        string spawnId = SceneTransition.NextSpawnPointId;
        if (string.IsNullOrEmpty(spawnId))
            return;

        SceneTransition.NextSpawnPointId = null;

        PlayerSpawnPoint spawn = FindSpawnPoint(spawnId);
        if (spawn == null)
        {
            Debug.LogWarning($"[PlayerSpawnOnLoad] 未找到 Spawn Id: {spawnId}", this);
            return;
        }

        Transform player = FindPlayerTransform();
        if (player == null)
        {
            Debug.LogWarning("[PlayerSpawnOnLoad] 场景中未找到玩家。", this);
            return;
        }

        PlacePlayer(player, spawn.transform);
    }

    static PlayerSpawnPoint FindSpawnPoint(string spawnId)
    {
        foreach (PlayerSpawnPoint point in FindObjectsOfType<PlayerSpawnPoint>())
        {
            if (point.SpawnId == spawnId)
                return point;
        }

        return null;
    }

    static Transform FindPlayerTransform()
    {
        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
            return tagged.transform;

        CharactorController cc = FindObjectOfType<CharactorController>();
        return cc != null ? cc.transform : null;
    }

    static void PlacePlayer(Transform player, Transform spawn)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        player.SetPositionAndRotation(spawn.position, spawn.rotation);

        if (controller != null)
            controller.enabled = true;
    }
}
