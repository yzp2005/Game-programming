using UnityEngine;

/// <summary>
/// 用 UI Image 宽度表示血量，受伤时宽度平滑减少（非瞬间跳变）。
/// </summary>
[RequireComponent(typeof(Health))]
public class MonsterHealthBarUI : MonoBehaviour
{
    [SerializeField] RectTransform fillBar;
    [SerializeField] float fullWidth = 0f;
    [SerializeField] bool hideOnDeath = true;

    [Header("平滑")]
    [Tooltip("数值越大，血条跟得越快")]
    [SerializeField] float smoothSpeed = 12f;

    Health health;
    float maxBarWidth;
    float targetPercent = 1f;
    float displayPercent = 1f;

    void Awake()
    {
        health = GetComponent<Health>();

        if (fillBar == null)
        {
            Debug.LogWarning($"{name}: MonsterHealthBarUI 未指定 Fill Bar。", this);
            return;
        }

        maxBarWidth = fullWidth > 0f ? fullWidth : fillBar.sizeDelta.x;
        health.OnHealthChanged += OnHealthChanged;
        health.OnDeath += OnDeath;

        targetPercent = health.HealthPercent;
        displayPercent = targetPercent;
        ApplyWidth(displayPercent);
    }

    void OnDestroy()
    {
        if (health == null) return;
        health.OnHealthChanged -= OnHealthChanged;
        health.OnDeath -= OnDeath;
    }

    void Update()
    {
        if (fillBar == null)
            return;

        if (Mathf.Approximately(displayPercent, targetPercent))
            return;

        displayPercent = Mathf.Lerp(displayPercent, targetPercent, smoothSpeed * Time.deltaTime);

        if (Mathf.Abs(displayPercent - targetPercent) < 0.001f)
            displayPercent = targetPercent;

        ApplyWidth(displayPercent);
    }

    void OnHealthChanged(float current, float max)
    {
        if (max <= 0f)
            return;

        targetPercent = Mathf.Clamp01(current / max);
    }

    void OnDeath()
    {
        if (hideOnDeath && fillBar != null)
            fillBar.gameObject.SetActive(false);
    }

    void ApplyWidth(float percent)
    {
        Vector2 size = fillBar.sizeDelta;
        size.x = maxBarWidth * percent;
        fillBar.sizeDelta = size;
    }
}
