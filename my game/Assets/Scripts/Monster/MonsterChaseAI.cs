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

    [Header("攻击目标")]
    [Tooltip("拖核心物体（如 Dawncore）；留空则从 Path 的 Destination 自动查找 CoreHealth")]
    [SerializeField] GameObject attackTargetObject;
    [SerializeField] float attackDamage = 10f;
    [Tooltip("两次扣血最小间隔（攻击动画循环时防连打）")]
    [SerializeField] float attackHitCooldown = 0.8f;

    Transform[] waypoints;
    Transform destination;
    float destinationRadius;
    int waypointIndex;
    bool attacking;

    Animator animator;
    int animHash;
    int currentAnim = -1;
    float nextAttackHitTime;
    CoreHealth attackTarget;

    void Awake()
    {
        animator = GetComponent<Animator>();
        animHash = Animator.StringToHash(animParameter);
    }

    public void Configure(MonsterPath path)
    {
        if (path == null) return;

        waypoints = path.Waypoints;
        destination = path.Destination;
        destinationRadius = path.DestinationRadius;
        ResolveAttackTarget();
        waypointIndex = 0;
        attacking = false;
        currentAnim = -1;
    }

    void Update()
    {
        if (TryGetComponent(out MonsterHealth health) && health.IsDead)
            return;

        if (attacking)
        {
            if (ShouldLeaveAttack())
            {
                attacking = false;
            }
            else
            {
                if (destination != null)
                    FaceFlat(destination.position);
                SetAnim(attackAnimValue);
                return;
            }
        }

        Transform target = GetMoveTarget();
        if (target == null)
        {
            SetAnim(idleAnimValue);
            return;
        }

        Vector3 flat = FlatOnGround(target.position);
        if (IsHeadingToDestination())
        {
            if (InDestinationZone())
            {
                attacking = true;
                SetAnim(attackAnimValue);
                return;
            }
        }
        else if (Reached(flat))
        {
            waypointIndex++;
            return;
        }

        FaceFlat(flat);
        MoveFlat(flat);
        SetAnim(runAnimValue);
    }

    Transform GetMoveTarget()
    {
        if (waypoints != null && waypointIndex < waypoints.Length)
            return waypoints[waypointIndex];
        return destination;
    }

    bool IsHeadingToDestination()
    {
        return destination != null && (waypoints == null || waypointIndex >= waypoints.Length);
    }

    bool ShouldLeaveAttack()
    {
        if (destination == null)
            return true;

        return !InDestinationZone();
    }

    bool InDestinationZone()
    {
        if (destination == null)
            return false;

        return FlatDistance(transform.position, destination.position) <= destinationRadius;
    }

    bool Reached(Vector3 flatTarget)
    {
        return FlatDistance(transform.position, flatTarget) <= arriveDistance;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y;
        return Vector3.Distance(a, b);
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

    void ResolveAttackTarget()
    {
        attackTarget = null;

        if (attackTargetObject != null)
            attackTarget = attackTargetObject.GetComponent<CoreHealth>();

        if (attackTarget == null && destination != null)
            attackTarget = destination.GetComponent<CoreHealth>();
    }

    /// <summary>
    /// 挂在 Animator 同一物体的攻击动画上：Add Event → 调用此函数（出手帧）。
    /// </summary>
    public void OnAttackHit()
    {
        if (!attacking || attackTarget == null || attackTarget.IsDestroyed)
            return;

        if (Time.time < nextAttackHitTime)
            return;

        nextAttackHitTime = Time.time + attackHitCooldown;
        attackTarget.TakeDamage(attackDamage);
    }
}
