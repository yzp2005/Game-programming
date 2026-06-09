using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂到 NPC 上，配合 Trigger 碰撞体检测 Enemy 层物体。
/// npcanimation：0=待机，1=射击。多个敌人时只攻击最先进入范围的那个，直到其离开。
/// 射击时自动瞄准当前目标并发射子弹（与主角相同的弹道预制体 + ProjectileDamage）。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class NPCShootController : MonoBehaviour
{
    [Header("动画")]
    [SerializeField] Animator animator;
    [SerializeField] string animParameter = "npcanimation";
    [SerializeField] int idleAnimValue = 0;
    [SerializeField] int shootAnimValue = 1;
    [SerializeField] string shootStateName = "ShootSingleshot_RF02_Anim";
    [Tooltip("射击动画播放到该进度时自动开火（0~1）。也可在动画 Clip 里加 Animation Event 调用 OnShootFire。")]
    [SerializeField] float fireNormalizedTime = 0.35f;
    [SerializeField] bool autoFireOnAnim = true;

    [Header("射击")]
    [SerializeField] Transform firePoint;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] float projectileDamage = 15f;
    [SerializeField] float aimHeightOffset = 1.2f;
    [Tooltip("开启后子弹追踪锁定目标，必定命中并扣血，同时播放命中特效。")]
    [SerializeField] bool useLockedHit = true;
    [SerializeField] float homingHitDistance = 0.35f;

    GameObject hitVfxPrefab;
    GameObject flashVfxPrefab;
    float projectileSpeed = 22f;

    [Header("瞄准线")]
    [SerializeField] LineRenderer aimLine;
    [SerializeField] Color aimLineColor = Color.red;
    [SerializeField] float aimLineWidth = 0.04f;

    [Header("检测")]
    [SerializeField] float rotationSpeed = 8f;
    [SerializeField] LayerMask enemyLayerMask;

    [Header("运行时状态（Play 模式下查看）")]
    [SerializeField] int npcAnimationCurrent;
    [SerializeField] bool isShooting;
    [SerializeField] string currentTargetName;

    int animHash;
    int currentAnim = -1;
    bool firedThisShootCycle;

    Transform currentTarget;
    readonly List<Transform> enteredEnemies = new List<Transform>();

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        animHash = Animator.StringToHash(animParameter);

        if (enemyLayerMask == 0)
            enemyLayerMask = LayerMask.GetMask("Enemy");

        EnsureTriggerSetup();
        EnsureAimLine();
        MinimapTrackable.EnsureOn(gameObject, MinimapTrackable.BlipKind.Friendly);
    }

    void Start()
    {
        if (animator == null)
        {
            Debug.LogError($"[{name}] 未找到 Animator，请拖到 Inspector 或挂在同一物体/子物体上。", this);
            enabled = false;
            return;
        }

        if (!HasAnimParameter(animator, animHash))
        {
            Debug.LogError(
                $"[{name}] Animator Controller 里没有 Int 参数 \"{animParameter}\"，请在 Animator 窗口 Parameters 里添加。",
                this);
            enabled = false;
            return;
        }

        SetAnim(idleAnimValue);
        SetAimLineVisible(false);
        CacheVfxFromProjectilePrefab();
    }

    void Update()
    {
        if (currentTarget == null)
        {
            SetAnim(idleAnimValue);
            firedThisShootCycle = false;
            SetAimLineVisible(false);
            RefreshDebugState();
            return;
        }

        if (!IsValidTarget(currentTarget))
        {
            RemoveEnemy(currentTarget);
            RefreshDebugState();
            return;
        }

        FaceTarget(currentTarget);
        SetAnim(shootAnimValue);

        if (autoFireOnAnim)
            TryAutoFireFromAnimation();

        RefreshDebugState();
    }

    void LateUpdate()
    {
        UpdateAimLine();
    }

    /// <summary>
    /// 在射击动画 Clip 的合适帧添加 Animation Event，Function 填此方法。
    /// </summary>
    public void OnShootFire()
    {
        FireProjectile();
    }

    void TryAutoFireFromAnimation()
    {
        if (animator == null || currentTarget == null)
            return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!state.IsName(shootStateName))
        {
            firedThisShootCycle = false;
            return;
        }

        if (!firedThisShootCycle && state.normalizedTime >= fireNormalizedTime)
        {
            firedThisShootCycle = true;
            FireProjectile();
        }

        if (state.normalizedTime < 0.05f)
            firedThisShootCycle = false;
    }

    void FireProjectile()
    {
        if (currentTarget == null || !IsValidTarget(currentTarget))
            return;

        if (firePoint == null)
        {
            Debug.LogWarning($"[{name}] 未设置 Fire Point，无法发射子弹。", this);
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[{name}] 未设置 Projectile Prefab，请拖入与主角相同的弹道预制体。", this);
            return;
        }

        if (useLockedHit)
            FireLockedProjectile();
        else
            FirePhysicsProjectile();
    }

    void FireLockedProjectile()
    {
        Vector3 aimPoint = GetAimPoint(currentTarget);
        Vector3 direction = aimPoint - firePoint.position;
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        NPCHomingProjectile.SpawnMuzzleFlash(flashVfxPrefab, firePoint.position, direction.normalized);

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction.normalized));
        DisablePhysicsProjectile(projectile);

        NPCHomingProjectile homing = projectile.AddComponent<NPCHomingProjectile>();
        homing.Configure(
            currentTarget,
            projectileDamage,
            projectileSpeed,
            hitVfxPrefab,
            aimHeightOffset,
            homingHitDistance);
    }

    void FirePhysicsProjectile()
    {
        Vector3 targetPoint = GetAimPoint(currentTarget);
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        if (projectile.TryGetComponent(out ProjectileMover mover))
            mover.targetPoint = targetPoint;

        if (projectile.TryGetComponent(out ProjectileDamage damage))
            damage.SetDamage(projectileDamage);
    }

    void DisablePhysicsProjectile(GameObject projectile)
    {
        if (projectile.TryGetComponent(out ProjectileDamage damage))
            damage.enabled = false;

        if (projectile.TryGetComponent(out ProjectileMover mover))
            mover.enabled = false;

        if (projectile.TryGetComponent(out Rigidbody rb))
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        foreach (Collider col in projectile.GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    void CacheVfxFromProjectilePrefab()
    {
        if (projectilePrefab == null || !projectilePrefab.TryGetComponent(out ProjectileMover mover))
            return;

        hitVfxPrefab = mover.hit;
        flashVfxPrefab = mover.flash;
        projectileSpeed = mover.speed;
    }

    Vector3 GetAimPoint(Transform target)
    {
        if (target.TryGetComponent(out Collider col))
            return col.bounds.center;

        return target.position + Vector3.up * aimHeightOffset;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsEnemy(other))
            return;

        Transform enemy = GetEnemyRoot(other);
        if (enteredEnemies.Contains(enemy))
            return;

        enteredEnemies.Add(enemy);
        if (currentTarget == null)
            currentTarget = enemy;
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsEnemy(other))
            return;

        RemoveEnemy(GetEnemyRoot(other));
    }

    void RemoveEnemy(Transform enemy)
    {
        enteredEnemies.Remove(enemy);

        if (currentTarget != enemy)
            return;

        currentTarget = enteredEnemies.Count > 0 ? enteredEnemies[0] : null;
        if (currentTarget == null)
        {
            SetAnim(idleAnimValue);
            SetAimLineVisible(false);
        }
    }

    void EnsureTriggerSetup()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (col == null || col is CharacterController)
                continue;

            col.isTrigger = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void EnsureAimLine()
    {
        if (aimLine != null)
            return;

        var lineObject = new GameObject("AimLine");
        lineObject.transform.SetParent(transform, false);
        aimLine = lineObject.AddComponent<LineRenderer>();
        aimLine.useWorldSpace = true;
        aimLine.positionCount = 2;
        aimLine.startWidth = aimLineWidth;
        aimLine.endWidth = aimLineWidth;
        aimLine.numCapVertices = 4;
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor = aimLineColor;
        aimLine.endColor = aimLineColor;
        aimLine.enabled = false;
    }

    void UpdateAimLine()
    {
        if (aimLine == null)
            return;

        if (currentTarget == null || !IsValidTarget(currentTarget))
        {
            SetAimLineVisible(false);
            return;
        }

        Vector3 start = firePoint != null ? firePoint.position : transform.position + Vector3.up * aimHeightOffset;
        Vector3 end = GetAimPoint(currentTarget);

        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
        aimLine.startColor = aimLineColor;
        aimLine.endColor = aimLineColor;
        aimLine.startWidth = aimLineWidth;
        aimLine.endWidth = aimLineWidth;
        SetAimLineVisible(true);
    }

    void SetAimLineVisible(bool visible)
    {
        if (aimLine != null)
            aimLine.enabled = visible;
    }

    bool IsValidTarget(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        if (target.TryGetComponent(out MonsterHealth health) && health.IsDead)
            return false;

        return true;
    }

    bool IsEnemy(Collider other)
    {
        return (enemyLayerMask.value & (1 << other.gameObject.layer)) != 0;
    }

    Transform GetEnemyRoot(Collider other)
    {
        MonsterHealth health = other.GetComponentInParent<MonsterHealth>();
        if (health != null)
            return health.transform;

        return other.transform.root;
    }

    void FaceTarget(Transform target)
    {
        Vector3 dir = GetAimPoint(target) - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime);
    }

    void SetAnim(int value)
    {
        if (animator == null)
            return;

        currentAnim = value;
        npcAnimationCurrent = value;
        animator.SetInteger(animHash, value);
    }

    void RefreshDebugState()
    {
        isShooting = currentTarget != null;
        currentTargetName = currentTarget != null ? currentTarget.name : "(无)";
    }

    static bool HasAnimParameter(Animator anim, int hash)
    {
        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.nameHash == hash && p.type == AnimatorControllerParameterType.Int)
                return true;
        }

        return false;
    }
}
