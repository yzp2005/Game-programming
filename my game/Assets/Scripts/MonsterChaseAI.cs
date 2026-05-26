using UnityEngine;

/// <summary>
/// eanimation：0=待机，1=移动/追逐，2=攻击。
/// 玩家超出检测范围后，怪物返回生成时的位置（原点）。
/// </summary>
[DisallowMultipleComponent]
public class MonsterChaseAI : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] Transform player;

    [Header("原点（生成时位置）")]
    [Tooltip("回到原点此距离内视为到达")]
    [SerializeField] float arriveHomeDistance = 0.4f;

    [Header("检测（球心 = 怪物位置）")]
    [SerializeField] float detectRange = 12f;

    [Tooltip("进入此距离：停止移动，eanimation=2（攻击）")]
    [SerializeField] float attackDistance = 1.8f;

    [Tooltip("攻击后玩家需拉开超过「攻击距离 + 此值」才重新 eanimation=1")]
    [SerializeField] float chaseResumeBuffer = 0.8f;

    [Header("移动")]
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float rotationSpeed = 8f;

    [Header("动画 eanimation")]
    [SerializeField] string animParameter = "eanimation";
    [SerializeField] int idleAnimValue = 0;
    [SerializeField] int runAnimValue = 1;
    [SerializeField] int attackAnimValue = 2;

    Animator animator;
    int animParamHash;
    bool isInAttackRange;
    bool isReturningHome;
    int currentAnimValue = -1;
    Vector3 homePosition;

    float ResumeChaseDistance => attackDistance + chaseResumeBuffer;

    void Awake()
    {
        animator = GetComponent<Animator>();
        animParamHash = Animator.StringToHash(animParameter);
        homePosition = transform.position;

        if (player == null)
            player = FindPlayer();
    }

    void Update()
    {
        Health health = GetComponent<Health>();
        if (health != null && health.IsDead)
            return;

        if (player == null)
        {
            player = FindPlayer();
            if (player == null)
            {
                isInAttackRange = false;
                UpdateReturnHomeOnly();
                return;
            }
        }

        if (isReturningHome)
        {
            UpdateReturnHome();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool inDetectRange = distanceToPlayer <= detectRange;

        if (!inDetectRange)
        {
            if (!IsAtHome())
                isReturningHome = true;
            else
                SetEanimation(idleAnimValue);

            if (isReturningHome)
                UpdateReturnHome();

            return;
        }

        UpdateAttackRangeState(distanceToPlayer);

        int animValue = GetAnimValue(true);
        bool shouldMove = !isInAttackRange;

        FaceTarget(player.position);

        if (shouldMove)
            MoveToward(player.position);

        SetEanimation(animValue);
    }

    void UpdateReturnHomeOnly()
    {
        if (!IsAtHome())
        {
            isReturningHome = true;
            UpdateReturnHome();
        }
        else
        {
            isReturningHome = false;
            SetEanimation(idleAnimValue);
        }
    }

    void UpdateReturnHome()
    {
        isInAttackRange = false;

        if (IsAtHome())
        {
            isReturningHome = false;
            SetEanimation(idleAnimValue);
            return;
        }

        FaceTarget(homePosition);
        MoveToward(homePosition);
        SetEanimation(runAnimValue);
    }

    bool IsAtHome()
    {
        Vector3 pos = transform.position;
        Vector3 home = homePosition;
        pos.y = 0f;
        home.y = 0f;
        return Vector3.Distance(pos, home) <= arriveHomeDistance;
    }

    int GetAnimValue(bool inDetectRange)
    {
        if (!inDetectRange)
            return idleAnimValue;

        if (isInAttackRange)
            return attackAnimValue;

        return runAnimValue;
    }

    void UpdateAttackRangeState(float distance)
    {
        if (!isInAttackRange)
        {
            if (distance <= attackDistance)
                isInAttackRange = true;
        }
        else if (distance > ResumeChaseDistance)
        {
            isInAttackRange = false;
        }
    }

    Transform FindPlayer()
    {
        GameObject tagged = GameObject.FindWithTag("Player");
        if (tagged != null)
            return tagged.transform;

        CharactorController controller = FindObjectOfType<CharactorController>();
        return controller != null ? controller.transform : null;
    }

    void FaceTarget(Vector3 worldPosition)
    {
        Vector3 dir = worldPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void MoveToward(Vector3 worldPosition)
    {
        Vector3 dir = worldPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.position += dir.normalized * (moveSpeed * Time.deltaTime);
    }

    void SetEanimation(int value)
    {
        if (animator == null || currentAnimValue == value) return;

        currentAnimValue = value;
        animator.SetInteger(animParamHash, value);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 home = Application.isPlaying ? homePosition : transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(home, 0.35f);
        Gizmos.DrawLine(transform.position, home);

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, ResumeChaseDistance);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireSphere(home, arriveHomeDistance);
    }
}
