using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 黑幕旁白：一句句依次淡入、停留、淡出。文案写在 Lines 或 Content（按行拆分）。
/// 挂在居中 TMP 物体上即可，不需要 ScrollViewport。
/// </summary>
public class IntroNarrationLines : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private TMP_Text subtitleText;

    [Header("文案")]
    [Tooltip("一行一句，优先级高于 Content")]
    [SerializeField] private string[] lines;
    [TextArea(6, 16)]
    [SerializeField] private string content;

    [Header("每句时间（秒）")]
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private float fadeOutDuration = 0.6f;
    [SerializeField] private float gapBetweenLines = 0.2f;

    [Header("最后一句")]
    [Tooltip("播到最后一句时显示的图标（Logo 等），拖 UI Image 物体")]
    [SerializeField] private GameObject lastLineIcon;
    [SerializeField] private float lastLineIconFadeInDuration = 0.5f;
    [SerializeField] private float lastLineIconFadeOutDuration = 0.5f;

    [Header("流程")]
    [SerializeField] private bool playOnStart;

    private Coroutine playRoutine;
    private bool isPlaying;
    private Color textBaseColor;
    private CanvasGroup lastLineIconCanvasGroup;

    public bool IsFinished { get; private set; }

    void Awake()
    {
        if (subtitleText == null)
            subtitleText = GetComponent<TMP_Text>();

        textBaseColor = subtitleText.color;
        SetTextAlpha(0f);
        subtitleText.text = string.Empty;

        if (lastLineIcon != null)
        {
            lastLineIconCanvasGroup = lastLineIcon.GetComponent<CanvasGroup>();
            lastLineIcon.SetActive(false);
        }
    }

    void Start()
    {
        if (playOnStart)
            Begin();
    }

    public void Begin()
    {
        if (isPlaying)
            return;

        playRoutine = StartCoroutine(PlayRoutine());
    }

    /// <summary>立刻停止旁白并清理 UI（跳过开场时调用）。</summary>
    public void StopImmediately()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        Cleanup();
    }

    public IEnumerator PlayAndWait()
    {
        Begin();
        while (!IsFinished)
            yield return null;
    }

    IEnumerator PlayRoutine()
    {
        isPlaying = true;
        IsFinished = false;

        string[] narrationLines = GetLines();
        if (narrationLines == null || narrationLines.Length == 0)
        {
            Debug.LogWarning("[IntroNarrationLines] 没有旁白句子。", this);
            Cleanup();
            yield break;
        }

        subtitleText.gameObject.SetActive(true);

        for (int i = 0; i < narrationLines.Length; i++)
        {
            bool isLastLine = i == narrationLines.Length - 1;

            if (isLastLine)
                yield return ShowLastLineIcon();

            subtitleText.text = narrationLines[i];

            yield return FadeAlpha(0f, 1f, fadeInDuration);

            if (displayDuration > 0f)
                yield return new WaitForSeconds(displayDuration);

            yield return FadeAlpha(1f, 0f, fadeOutDuration);

            if (isLastLine)
                yield return HideLastLineIcon();

            if (gapBetweenLines > 0f && !isLastLine)
                yield return new WaitForSeconds(gapBetweenLines);
        }

        Cleanup();
    }

    void Cleanup()
    {
        subtitleText.text = string.Empty;
        SetTextAlpha(0f);

        if (lastLineIcon != null)
        {
            if (lastLineIconCanvasGroup != null)
                lastLineIconCanvasGroup.alpha = 0f;
            lastLineIcon.SetActive(false);
        }

        IsFinished = true;
        isPlaying = false;
        playRoutine = null;
    }

    string[] GetLines()
    {
        if (lines != null && lines.Length > 0)
            return lines;

        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<string>();

        string[] split = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < split.Length; i++)
            split[i] = split[i].Trim();

        return split;
    }

    IEnumerator FadeAlpha(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetTextAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetTextAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetTextAlpha(to);
    }

    void SetTextAlpha(float alpha)
    {
        Color c = textBaseColor;
        c.a = alpha;
        subtitleText.color = c;
    }

    IEnumerator ShowLastLineIcon()
    {
        if (lastLineIcon == null)
            yield break;

        lastLineIcon.SetActive(true);
        yield return FadeIconAlpha(0f, 1f, lastLineIconFadeInDuration);
    }

    IEnumerator HideLastLineIcon()
    {
        if (lastLineIcon == null)
            yield break;

        yield return FadeIconAlpha(1f, 0f, lastLineIconFadeOutDuration);
        lastLineIcon.SetActive(false);
    }

    IEnumerator FadeIconAlpha(float from, float to, float duration)
    {
        if (lastLineIconCanvasGroup != null)
        {
            if (duration <= 0f)
            {
                lastLineIconCanvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                lastLineIconCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            lastLineIconCanvasGroup.alpha = to;
            yield return null;
        }

        lastLineIcon.SetActive(to > 0.5f);
    }
}
