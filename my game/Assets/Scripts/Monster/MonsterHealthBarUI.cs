using UnityEngine;

/// <summary>
/// 用 UI Image 宽度表示血量，受伤时宽度平滑减少（非瞬间跳变）。
/// 监听同物体上的 MonsterHealth（或旧版 Health）。
/// </summary>
public class MonsterHealthBarUI : MonoBehaviour
{
    [SerializeField] RectTransform fillBar;
    [SerializeField] float fullWidth = 0f;
    [SerializeField] bool hideOnDeath = true;

    [Header("平滑")]
    [Tooltip("数值越大，血条跟得越快")]
    [SerializeField] float smoothSpeed = 12f;

    MonsterHealth monsterHealth;
    Health legacyHealth;
    float maxBarWidth;
    float targetPercent = 1f;
    float displayPercent = 1f;

    void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
        legacyHealth = monsterHealth == null ? GetComponent<Health>() : null;

        if (monsterHealth == null && legacyHealth == null)
        {
            Debug.LogWarning($"{name}: MonsterHealthBarUI 需要 MonsterHealth 或 Health 组件。", this);
            return;
        }

        if (fillBar == null)
        {
            Debug.LogWarning($"{name}: MonsterHealthBarUI 未指定 Fill Bar。", this);
            return;
        }

        maxBarWidth = fullWidth > 0f ? fullWidth : fillBar.sizeDelta.x;

        if (monsterHealth != null)
        {
            monsterHealth.OnHealthChanged += OnHealthChanged;
            monsterHealth.OnDeath += OnDeath;
            targetPercent = monsterHealth.HealthPercent;
        }
        else
        {
            legacyHealth.OnHealthChanged += OnHealthChanged;
            legacyHealth.OnDeath += OnDeath;
            targetPercent = legacyHealth.HealthPercent;
        }

        displayPercent = targetPercent;
        ApplyWidth(displayPercent);
    }

    void OnDestroy()
    {
        if (monsterHealth != null)
        {
            monsterHealth.OnHealthChanged -= OnHealthChanged;
            monsterHealth.OnDeath -= OnDeath;
        }

        if (legacyHealth != null)
        {
            legacyHealth.OnHealthChanged -= OnHealthChanged;
            legacyHealth.OnDeath -= OnDeath;
        }
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
        targetPercent = 0f;

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
