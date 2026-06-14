using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 从中心向外放大，同时 Image 透明度 1 → 0。
/// 建议挂在 Panel 下的子物体 QuestUpdateFx 上（单独 Image 光晕），不要挂在任务 Panel 本体的背景 Image 上。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class UIPanelExpandFadeEffect : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Image image;

    [Header("动画")]
    [SerializeField] private float startScale = 1f;
    [SerializeField] private float endScale = 1.6f;
    [SerializeField] private float duration = 0.55f;
    [SerializeField] private bool useUnscaledTime = true;

    Color baseColor;
    Coroutine playRoutine;

    void Awake()
    {
        if (target == null)
            target = transform as RectTransform;

        if (image == null)
            image = GetComponent<Image>();

        if (image != null)
        {
            baseColor = image.color;
            image.raycastTarget = false;
        }

        ResetHidden();
    }

    public void Play()
    {
        if (target == null || image == null)
            return;

        if (!isActiveAndEnabled)
            return;

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRoutine());
    }

    public void Stop()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        ResetHidden();
    }

    IEnumerator PlayRoutine()
    {
        ShowVisual();
        target.localScale = Vector3.one * startScale;
        SetImageAlpha(1f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            target.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
            SetImageAlpha(Mathf.Lerp(1f, 0f, t));

            yield return null;
        }

        ResetHidden();
        playRoutine = null;
    }

    void ShowVisual()
    {
        // 脚本挂在 Panel 上时不能 SetActive(false) Panel，否则下次无法 StartCoroutine
        if (CanDeactivateTarget() && !target.gameObject.activeSelf)
            target.gameObject.SetActive(true);
    }

    void SetImageAlpha(float alpha)
    {
        Color c = baseColor;
        c.a = alpha;
        image.color = c;
    }

    void ResetHidden()
    {
        if (target != null)
            target.localScale = Vector3.one * startScale;

        if (image != null)
            SetImageAlpha(0f);

        if (CanDeactivateTarget())
            target.gameObject.SetActive(false);
    }

    bool CanDeactivateTarget()
    {
        return target != null && target != transform;
    }
}
