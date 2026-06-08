using UnityEngine;

[DisallowMultipleComponent]
public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] GameObject monsterPrefab;
    [SerializeField] MonsterPath path;
    [SerializeField] Transform spawnPoint;
    [SerializeField] float spawnHeightOffset = 0.5f;

    public GameObject SpawnOne()
    {
        if (monsterPrefab == null || path == null)
            return null;

        Transform point = spawnPoint != null ? spawnPoint : transform;
        Vector3 pos = point.position;
        pos.y += spawnHeightOffset;
        GameObject monster = Instantiate(monsterPrefab, pos, point.rotation);

        if (monster.TryGetComponent(out MonsterChaseAI ai))
            ai.Configure(path);

        if (!monster.TryGetComponent(out MinimapTrackable _))
            monster.AddComponent<MinimapTrackable>();

        return monster;
    }

    void OnDrawGizmos()
    {
        Transform point = spawnPoint != null ? spawnPoint : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(point.position, 0.5f);
    }
}
