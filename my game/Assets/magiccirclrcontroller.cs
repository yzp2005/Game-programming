using UnityEngine;

public class MagicCircleFade : MonoBehaviour
{
    [Header("淡入淡出速度")]
    public float fadeSpeed = 8f;

    private Renderer _renderer;
    private Material _mat;
    private Color _originalColor;

    void Start()
    {
        // 获取渲染组件
        _renderer = GetComponent<Renderer>();
        // 获取材质
        _mat = _renderer.material;
        // 保存原本颜色
        _originalColor = _mat.color;

        // 一开始完全透明（隐藏）
        _mat.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0);
    }

    void Update()
    {
        float targetAlpha;

        // 按住右键 → 目标透明度 1（完全显示）
        if (Input.GetMouseButton(1))
        {
            targetAlpha = 1f;
        }
        // 松开右键 → 目标透明度 0（完全消失）
        else
        {
            targetAlpha = 0f;
        }

        // 平滑渐变透明度
        float currentAlpha = Mathf.Lerp(_mat.color.a, targetAlpha, fadeSpeed * Time.deltaTime);
        _mat.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, currentAlpha);
    }
}