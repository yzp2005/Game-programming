using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 塔防核心 / 目标点血量。挂 Dawncore 等核心物体上，同步 UI Slider（0~1）。
/// </summary>
[DisallowMultipleComponent]
public class CoreHealth : MonoBehaviour
{
    [Header("生命值")]
    [SerializeField] float maxHealth = 500f;
    [SerializeField] float currentHealth = 500f;

    [Header("UI")]
    [FormerlySerializedAs("healthBar")]
    [SerializeField] Slider healthSlider;
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
        SetupHealthSlider();
        SyncHealthBar();

        if (healthSlider == null)
            Debug.LogWarning($"{name}: Health Slider 未指定，血条不会显示。", this);
    }

    /// <summary>
    /// 把 UI Slider 和 CoreHealth 连起来。可在 Inspector 里拖引用后调用，或在代码里传入 Slider。
    /// </summary>
    public void BindHealthSlider(Slider slider)
    {
        healthSlider = slider;
        SetupHealthSlider();
        SyncHealthBar();
    }

    void SetupHealthSlider()
    {
        if (healthSlider == null)
            return;

        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.interactable = false;
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

        if (hideHealthBarOnDestroyed && healthSlider != null)
            healthSlider.gameObject.SetActive(false);

        OnDestroyed?.Invoke();
    }

    void NotifyHealthChanged()
    {
        SyncHealthBar();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void SyncHealthBar()
    {
        if (healthSlider != null)
            healthSlider.value = HealthPercent;
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }
}
