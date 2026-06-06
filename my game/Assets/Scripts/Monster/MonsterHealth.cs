using System;
using UnityEngine;

/// <summary>
/// 怪物生命值：扣血、死亡动画、可选回血。挂怪物根物体。
/// </summary>
[DisallowMultipleComponent]
public class MonsterHealth : MonoBehaviour
{
    [Header("生命值")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float currentHealth = 100f;

    [Header("回血")]
    [SerializeField] bool enableRegen = true;
    [SerializeField] float regenPerSecond = 4f;
    [SerializeField] float regenDelayAfterDamage = 3f;

    [Header("死亡")]
    [SerializeField] string animParameter = "eanimation";
    [SerializeField] int deathAnimValue = 3;
    [SerializeField] bool destroyOnDeath = true;
    [SerializeField] float destroyDelay = 5f;
    [SerializeField] bool disableCollidersOnDeath = true;

    public event Action<float, float> OnHealthChanged;
    public event Action<float> OnDamaged;
    public event Action OnDeath;

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
        if (IsDead)
            HandleDeath();
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

        OnDeath?.Invoke();
        NotifyHealthChanged();

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }

    void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        regenPerSecond = Mathf.Max(0f, regenPerSecond);
        regenDelayAfterDamage = Mathf.Max(0f, regenDelayAfterDamage);
    }
}
