using UnityEngine;

/// <summary>新场景加载后，若 SceneTransition 有出生点 Id，把玩家挪到对应 PlayerSpawnPoint。</summary>
[DefaultExecutionOrder(100)]
public class PlayerSpawnOnLoad : MonoBehaviour
{
    void Start() => ApplyIfNeeded();

    public static void ApplyIfNeeded()
    {
        string spawnId = SceneTransition.NextSpawnPointId;
        if (string.IsNullOrEmpty(spawnId))
            return;

        SceneTransition.NextSpawnPointId = null;

        PlayerSpawnPoint spawn = FindSpawnPoint(spawnId);
        if (spawn == null)
        {
            Debug.LogWarning($"[PlayerSpawnOnLoad] 未找到 Spawn Id: \"{spawnId}\"。请检查目标场景 PlayerSpawnPoint 配置。");
            return;
        }

        Transform player = FindPlayerTransform();
        if (player == null)
        {
            Debug.LogWarning("[PlayerSpawnOnLoad] 未找到玩家。请在 Riko 上挂 PlayerCrossScene，使切场景时保留玩家。");
            return;
        }

        PlacePlayer(player, spawn.transform);
        RebindCameras(player);
        Debug.Log($"[PlayerSpawnOnLoad] 玩家已移动到 \"{spawnId}\"。", spawn);
    }

    static PlayerSpawnPoint FindSpawnPoint(string spawnId)
    {
        spawnId = spawnId.Trim();

        foreach (PlayerSpawnPoint point in FindObjectsOfType<PlayerSpawnPoint>())
        {
            if (point.SpawnId != null && point.SpawnId.Trim() == spawnId)
                return point;
        }

        return null;
    }

    static Transform FindPlayerTransform()
    {
        if (PlayerCrossScene.TryGetPersistedPlayer(out GameObject persisted))
            return persisted.transform;

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
            return tagged.transform;

        CharactorController controller = FindObjectOfType<CharactorController>();
        return controller != null ? controller.transform : null;
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

    static void RebindCameras(Transform player)
    {
        CameraController[] cameras = FindObjectsOfType<CameraController>();
        for (int i = 0; i < cameras.Length; i++)
            cameras[i].SetTarget(player);
    }
}
