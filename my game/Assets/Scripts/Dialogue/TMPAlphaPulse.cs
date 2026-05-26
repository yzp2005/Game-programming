using TMPro;
using UnityEngine;

/// <summary>
/// 让 TextMeshPro 文字透明度在最小/最大值之间周期性变化。
/// 挂在带 TMP 组件的物体上即可。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TMPAlphaPulse : MonoBehaviour
{
    [Header("透明度范围")]
    [SerializeField] [Range(0f, 1f)] private float minAlpha = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float maxAlpha = 1f;

    [Header("速度")]
    [Tooltip("数值越大，忽明忽暗越快。")]
    [SerializeField] private float speed = 2f;

    [Header("可选")]
    [Tooltip("勾选后游戏开始时随机偏移相位，多个文字不会完全同步。")]
    [SerializeField] private bool randomPhaseOnStart;

    private TMP_Text tmp;
    private Color baseColor;
    private float phase;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        baseColor = tmp.color;
        phase = randomPhaseOnStart ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    void OnEnable()
    {
        if (tmp != null)
            baseColor = tmp.color;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed + phase) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        Color c = baseColor;
        c.a = alpha;
        tmp.color = c;
    }
}
