using UnityEngine;

/// <summary>
/// 场景路径：子物体为路点；Destination 只表示攻击范围中心（不是必须走到的点）。
/// </summary>
[DisallowMultipleComponent]
public class MonsterPath : MonoBehaviour
{
    const float DefaultRadius = 2.5f;

    [SerializeField] Transform[] waypoints;
    [SerializeField] Transform destination;
    [SerializeField] float destinationRadius = DefaultRadius;

    public Transform[] Waypoints => waypoints;
    public Transform Destination => destination;
    public float DestinationRadius => destinationRadius > 0f ? destinationRadius : DefaultRadius;

    public bool IsInDestinationRange(Vector3 world)
    {
        if (destination == null)
            return false;

        Vector3 pos = world;
        Vector3 center = destination.position;
        pos.y = center.y;
        return Vector3.Distance(pos, center) <= DestinationRadius;
    }

    void Awake()
    {
        if (destinationRadius <= 0f)
            destinationRadius = DefaultRadius;
    }

    void OnValidate()
    {
        if (destinationRadius <= 0f)
            destinationRadius = DefaultRadius;
    }

    void OnDrawGizmos()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            float y = transform.position.y;
            Gizmos.color = Color.green;

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;

                Vector3 a = waypoints[i].position;
                a.y = y;
                Gizmos.DrawWireSphere(a, 0.35f);

                if (i + 1 < waypoints.Length && waypoints[i + 1] != null)
                {
                    Vector3 b = waypoints[i + 1].position;
                    b.y = y;
                    Gizmos.DrawLine(a, b);
                }
            }
        }

        if (destination == null)
            return;

        Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(destination.position, DestinationRadius);
    }
}
