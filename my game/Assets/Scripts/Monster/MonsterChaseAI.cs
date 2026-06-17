using UnityEngine;

/// <summary>
/// eanimation：0=待机，1=移动，2=攻击。路径由 MonsterSpawner 通过 Configure 注入。
/// 进入/离开攻击以 MonsterPath.DestinationRadius 为准；路点仍用 arriveDistance。
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

    [Header("核心伤害")]
    [SerializeField] float coreAttackDamage = 10f;
    [Range(0f, 1f)] [SerializeField] float attackHitNormalizedTime = 0.45f;

    MonsterPath monsterPath;
    Transform[] waypoints;
    Transform destination;
    int waypointIndex;
    bool attacking;

    Animator animator;
    int animHash;
    int currentAnim = -1;
    CoreHealth coreHealth;
    bool damageDealtThisSwing;
    bool attackEndHandled;

    void Awake()
    {
        animator = GetComponent<Animator>();
        animHash = Animator.StringToHash(animParameter);
    }

    public void Configure(MonsterPath path)
    {
        if (path == null)
            return;

        monsterPath = path;
        waypoints = path.Waypoints;
        destination = path.Destination;
        waypointIndex = 0;
        attacking = false;
        currentAnim = -1;
        ResetAttackSwingState();
        ResolveCoreHealth();
    }

    void ResetAttackSwingState()
    {
        damageDealtThisSwing = false;
        attackEndHandled = false;
    }

    void Update()
    {
        if (IsDead())
            return;

        if (attacking)
        {
            if (ShouldLeaveAttack())
            {
                attacking = false;
                ResetAttackSwingState();
            }
            else
            {
                if (destination != null)
                    FaceFlat(destination.position);
                SetAnim(attackAnimValue);
                UpdateCoreAttackDamage();
                TryRepeatAttackAnimation();
                return;
            }
        }

        Transform target = GetMoveTarget();
        if (target == null)
        {
            SetAnim(idleAnimValue);
            return;
        }

        if (IsHeadingToDestination() && IsInAttackRange())
        {
            attacking = true;
            ResetAttackSwingState();
            SetAnim(attackAnimValue);
            return;
        }

        Vector3 flat = FlatOnGround(target.position);
        if (!IsHeadingToDestination() && Reached(flat))
        {
            waypointIndex++;
            return;
        }

        FaceFlat(flat);
        MoveFlat(flat);
        SetAnim(runAnimValue);
    }

    bool IsDead()
    {
        if (TryGetComponent(out MonsterHealth monsterHealth) && monsterHealth.IsDead)
            return true;

        if (TryGetComponent(out Health health) && health.IsDead)
            return true;

        return false;
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

    bool IsInAttackRange()
    {
        if (monsterPath != null)
            return monsterPath.IsInDestinationRange(transform.position);

        if (destination == null)
            return false;

        Vector3 pos = transform.position;
        Vector3 center = destination.position;
        pos.y = center.y;
        return Vector3.Distance(pos, center) <= arriveDistance;
    }

    bool ShouldLeaveAttack()
    {
        return !IsInAttackRange();
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
        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime);
    }

    void MoveFlat(Vector3 world)
    {
        Vector3 dir = world - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        transform.position += dir.normalized * (moveSpeed * Time.deltaTime);
    }

    void SetAnim(int value)
    {
        if (animator == null || currentAnim == value)
            return;

        currentAnim = value;
        animator.SetInteger(animHash, value);
    }

    void ResolveCoreHealth()
    {
        coreHealth = null;
        if (destination == null)
            return;

        if (destination.TryGetComponent(out CoreHealth health))
            coreHealth = health;
        else
            coreHealth = destination.GetComponentInParent<CoreHealth>();
    }

    void UpdateCoreAttackDamage()
    {
        if (animator == null || coreAttackDamage <= 0f)
            return;

        if (coreHealth == null)
            ResolveCoreHealth();

        if (coreHealth == null || coreHealth.IsDestroyed)
            return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!IsAttackAnimatorState(state))
            return;

        if (damageDealtThisSwing)
            return;

        if (state.normalizedTime % 1f < attackHitNormalizedTime)
            return;

        damageDealtThisSwing = true;
        coreHealth.TakeDamage(coreAttackDamage);
    }

    void TryRepeatAttackAnimation()
    {
        if (animator == null)
            return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!IsAttackAnimatorState(state))
        {
            attackEndHandled = false;
            return;
        }

        float phase = state.normalizedTime % 1f;
        if (phase < 0.95f)
        {
            attackEndHandled = false;
            return;
        }

        if (attackEndHandled)
            return;

        attackEndHandled = true;
        damageDealtThisSwing = false;
        currentAnim = -1;
        animator.SetInteger(animHash, idleAnimValue);
        currentAnim = -1;
        SetAnim(attackAnimValue);
    }

    static bool IsAttackAnimatorState(AnimatorStateInfo state)
    {
        return state.IsName("Attack01") || state.IsName("Attack02");
    }
}
