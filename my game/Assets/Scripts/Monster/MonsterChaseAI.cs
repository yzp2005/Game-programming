using UnityEngine;

/// <summary>
/// eanimation：0=待机，1=移动，2=攻击。路径由 MonsterSpawner 通过 Configure 注入。
/// </summary>
[DisallowMultipleComponent]
public class MonsterChaseAI : MonoBehaviour
{
    [SerializeField] float arriveDistance = 0.4f;
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float rotationSpeed = 8f;
    [SerializeField] string animParameter = "eanimation";
    [SerializeField] int idleAnimValue = 0;
    [SerializeField] int runAnimValue = 1;
    [SerializeField] int attackAnimValue = 2;

    [Header("攻击核心")]
    [SerializeField] float coreAttackDamage = 25f;
    [SerializeField] float coreAttackInterval = 1f;

    Transform[] waypoints;
    Transform destination;
    MonsterPath path;
    CoreHealth coreHealth;
    int waypointIndex;
    float nextCoreAttackTime;

    Animator animator;
    int animHash;
    int currentAnim = -1;

    void Awake()
    {
        animator = GetComponent<Animator>();
        animHash = Animator.StringToHash(animParameter);
    }

    public void Configure(MonsterPath monsterPath)
    {
        if (monsterPath == null) return;

        path = monsterPath;
        waypoints = monsterPath.Waypoints;
        destination = monsterPath.Destination;
        coreHealth = destination != null ? destination.GetComponent<CoreHealth>() : null;
        waypointIndex = 0;
        currentAnim = -1;
        nextCoreAttackTime = 0f;
    }

    void Update()
    {
        if (TryGetComponent(out MonsterHealth health) && health.IsDead)
            return;

        if (path != null && path.IsInDestinationRange(transform.position))
        {
            if (destination != null)
                FaceFlat(destination.position);

            TryAttackCore();
            SetAnim(attackAnimValue);
            return;
        }

        Transform target = GetMoveTarget();
        if (target == null)
        {
            SetAnim(idleAnimValue);
            return;
        }

        Vector3 flat = FlatOnGround(target.position);
        if (Reached(flat))
        {
            if (HasMoreWaypoints())
                waypointIndex++;
            return;
        }

        FaceFlat(flat);
        MoveFlat(flat);
        SetAnim(runAnimValue);
    }

    Transform GetMoveTarget()
    {
        if (HasMoreWaypoints())
            return waypoints[waypointIndex];

        // 路点走完后只朝范围中心靠近；进入 MonsterPath 球形范围即攻击，不必贴中心点
        return destination;
    }

    bool HasMoreWaypoints()
    {
        return waypoints != null && waypointIndex < waypoints.Length;
    }

    bool Reached(Vector3 flatTarget)
    {
        Vector3 pos = transform.position;
        pos.y = flatTarget.y;
        return Vector3.Distance(pos, flatTarget) <= arriveDistance;
    }

    Vector3 FlatOnGround(Vector3 world)
    {
        world.y = transform.position.y;
        return world;
    }

    void FaceFlat(Vector3 world)
    {
        Vector3 dir = world - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime);
    }

    void MoveFlat(Vector3 world)
    {
        Vector3 dir = world - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.position += dir.normalized * (moveSpeed * Time.deltaTime);
    }

    void SetAnim(int value)
    {
        if (animator == null || currentAnim == value) return;
        currentAnim = value;
        animator.SetInteger(animHash, value);
    }

    void TryAttackCore()
    {
        if (coreHealth == null || coreHealth.IsDestroyed)
            return;

        if (Time.time < nextCoreAttackTime)
            return;

        nextCoreAttackTime = Time.time + coreAttackInterval;
        coreHealth.TakeDamage(coreAttackDamage);
    }
}
