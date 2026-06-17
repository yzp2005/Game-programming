using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 怪物生命值：扣血、死亡、受伤后延迟缓慢回血。
/// 挂到怪物根物体（与 MonsterChaseAI、Animator 同级）。
/// </summary>
[DisallowMultipleComponent]
public class MonsterHealth : MonoBehaviour
{
    [Header("生命值")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float currentHealth = 100f;

    [Header("回血")]
    [SerializeField] bool enableRegen = true;
    [Tooltip("每秒恢复血量")]
    [SerializeField] float regenPerSecond = 4f;
    [Tooltip("受伤后等待多久才开始回血")]
    [SerializeField] float regenDelayAfterDamage = 3f;

    [Header("死亡")]
    [SerializeField] string animParameter = "eanimation";
    [SerializeField] int deathAnimValue = 3;
    [SerializeField] bool destroyOnDeath = true;
    [SerializeField] float destroyDelay = 5f;
    [SerializeField] bool disableCollidersOnDeath = true;

    [Header("受击碰撞")]
    [SerializeField] bool autoFitHitCollider = true;
    [Tooltip("相对 Renderer 包围盒的缩放，Dragon 等大型怪可略大于 1")]
    [SerializeField] float hitColliderPadding = 1.05f;

    public event Action<float, float> OnHealthChanged;
    public event Action<float> OnDamaged;
    public event Action OnDeath;
    public static event Action<MonsterHealth> OnAnyDeath;

    public bool IsDead { get; private set; }
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;

    float lastDamageTime = -999f;
    Animator animator;
    int animParamHash;

    void Awake()
    {
        animator = GetComponent<Animator>();
        animParamHash = Animator.StringToHash(animParameter);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        IsDead = currentHealth <= 0f;
        EnsureSolidHitCollider();
        if (IsDead)
            HandleDeath();
    }

    void Start()
    {
        if (autoFitHitCollider)
            StartCoroutine(RefitHitColliderNextFrame());
    }

    IEnumerator RefitHitColliderNextFrame()
    {
        yield return null;
        RefitRootHitColliderFromBounds();
    }

    void Update()
    {
        if (IsDead || !enableRegen || currentHealth >= maxHealth)
            return;

        if (Time.time < lastDamageTime + regenDelayAfterDamage)
            return;

        float before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + regenPerSecond * Time.deltaTime);
        if (currentHealth != before)
            NotifyHealthChanged();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        lastDamageTime = Time.time;

        OnDamaged?.Invoke(amount);
        NotifyHealthChanged();

        if (currentHealth <= 0f)
            HandleDeath();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f || currentHealth >= maxHealth)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        NotifyHealthChanged();
    }

    void HandleDeath()
    {
        if (IsDead)
            return;

        IsDead = true;
        currentHealth = 0f;

        if (TryGetComponent(out MonsterChaseAI chaseAI))
            chaseAI.enabled = false;

        if (animator != null)
            animator.SetInteger(animParamHash, deathAnimValue);

        if (disableCollidersOnDeath)
        {
            foreach (Collider col in GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        if (TryGetComponent(out CharacterController controller))
            controller.enabled = false;

        if (TryGetComponent(out MinimapTrackable minimapTrackable))
            minimapTrackable.enabled = false;
        else
        {
            MinimapTrackable childTrackable = GetComponentInChildren<MinimapTrackable>();
            if (childTrackable != null)
                childTrackable.enabled = false;
        }

        OnDeath?.Invoke();
        OnAnyDeath?.Invoke(this);
        NotifyHealthChanged();

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }

    void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void EnsureSolidHitCollider()
    {
        if (HasSolidHitCollider())
            return;

        CapsuleCollider body = GetComponent<CapsuleCollider>();
        if (body == null)
        {
            body = gameObject.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, 1f, 0f);
            body.height = 2f;
            body.radius = 0.5f;
        }

        body.isTrigger = false;
        body.enabled = true;
    }

    bool HasSolidHitCollider()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (col == null || !col.enabled || col.isTrigger)
                continue;

            if (col is MeshCollider meshCol && !meshCol.convex)
                continue;

            return true;
        }

        return false;
    }

    void RefitRootHitColliderFromBounds()
    {
        if (!TryGetBodyBounds(out Bounds bounds))
            return;

        CapsuleCollider body = GetComponent<CapsuleCollider>();
        if (body == null)
            body = gameObject.AddComponent<CapsuleCollider>();

        Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
        body.center = localCenter;
        body.direction = 1;
        body.height = Mathf.Max(bounds.size.y, bounds.size.x, bounds.size.z) * hitColliderPadding;
        body.radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * hitColliderPadding;
        body.isTrigger = false;
        body.enabled = true;
    }

    bool TryGetBodyBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer == null || renderer is ParticleSystemRenderer)
                continue;

            if (renderer.GetComponentInParent<Canvas>() != null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
                bounds.Encapsulate(renderer.bounds);
        }

        return hasBounds;
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        regenPerSecond = Mathf.Max(0f, regenPerSecond);
        regenDelayAfterDamage = Mathf.Max(0f, regenDelayAfterDamage);
    }
}
