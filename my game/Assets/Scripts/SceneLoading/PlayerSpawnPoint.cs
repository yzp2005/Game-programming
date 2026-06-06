using UnityEngine;

/// <summary>新场景里的出生点。挂空物体上，Spawn Id 与传送门配置一致。</summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId;

    public string SpawnId => spawnId;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
#endif
}
