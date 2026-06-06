using System;
using RengeGames.HealthBars;
using UnityEngine;

/// <summary>
/// 塔防核心 / 目标点血量：无动画，同步 RadialSegmentedHealthBar。
/// 挂 Destination 或核心物体上。
/// </summary>
[DisallowMultipleComponent]
public class CoreHealth : MonoBehaviour
{
    [Header("生命值")]
    [SerializeField] float maxHealth = 500f;
    [SerializeField] float currentHealth = 500f;

    [Header("UI")]
    [SerializeField] RadialSegmentedHealthBar healthBar;
    [SerializeField] bool hideHealthBarOnDestroyed = true;

    public event Action<float, float> OnHealthChanged;
    public event Action<float> OnDamaged;
    public event Action OnDestroyed;

    public bool IsDestroyed { get; private set; }
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth > 0f ? currentHealth / maxHealth : 0f;

    void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        IsDestroyed = currentHealth <= 0f;
        SyncHealthBar();
    }

    public void TakeDamage(float amount)
    {
        if (IsDestroyed || amount <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnDamaged?.Invoke(amount);
        NotifyHealthChanged();

        if (currentHealth <= 0f)
            HandleDestroyed();
    }

    public void Heal(float amount)
    {
        if (IsDestroyed || amount <= 0f || currentHealth >= maxHealth)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        NotifyHealthChanged();
    }

    void HandleDestroyed()
    {
        if (IsDestroyed)
            return;

        IsDestroyed = true;
        currentHealth = 0f;
        NotifyHealthChanged();

        if (hideHealthBarOnDestroyed && healthBar != null)
            healthBar.gameObject.SetActive(false);

        OnDestroyed?.Invoke();
    }

    void NotifyHealthChanged()
    {
        SyncHealthBar();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void SyncHealthBar()
    {
        if (healthBar != null)
            healthBar.SetPercent(HealthPercent);
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }
}
