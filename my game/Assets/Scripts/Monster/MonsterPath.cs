using UnityEngine;

/// <summary>
/// 场景中的固定路径：子物体为路点，Destination 为终点。
/// </summary>
[DisallowMultipleComponent]
public class MonsterPath : MonoBehaviour
{
    [SerializeField] Transform[] waypoints;
    [SerializeField] Transform destination;
    [SerializeField] float destinationRadius = 2f;

    public Transform[] Waypoints => waypoints;
    public Transform Destination => destination;
    public float DestinationRadius => destinationRadius;

    void OnValidate()
    {
        if (transform.childCount == 0) return;

        waypoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            waypoints[i] = transform.GetChild(i);
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

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

        if (destination == null || waypoints.Length == 0 || waypoints[waypoints.Length - 1] == null)
            return;

        Gizmos.color = Color.red;
        Vector3 last = waypoints[waypoints.Length - 1].position;
        Vector3 dest = destination.position;
        last.y = y;
        dest.y = y;
        Gizmos.DrawLine(last, dest);
        Gizmos.DrawWireSphere(dest, destinationRadius);
    }
}
