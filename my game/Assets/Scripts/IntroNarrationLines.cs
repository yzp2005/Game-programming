using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 黑幕旁白。优先使用 bg_audio.json + 一条完整配音（按 segments 时间轴显示字幕）；
/// 未配置时回退到 Lines / Content 手动逐句淡入淡出。
/// </summary>
public class IntroNarrationLines : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private TMP_Text subtitleText;

    [Header("配音 + 时间轴（bg_audio.json）")]
    [Tooltip("例如 Assets/Dialogue/bg_audio/bg_audio.json")]
    [SerializeField] private TextAsset timedNarrationJson;
    [SerializeField] private AudioClip narrationAudio;
    [SerializeField] private AudioSource audioSource;

    [Header("文案（无 JSON 时使用）")]
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
    [Tooltip("已有此 flag 时不自动播放（与 OpeningBlackCurtain 的 Intro Complete Flag 一致）")]
    [SerializeField] private string skipIfHasFlag;

    [Header("跳到末句（按 E）")]
    [SerializeField] private float skipCrossfadeOutDuration = 0.35f;
    [SerializeField] private float skipCrossfadeInDuration = 0.5f;

    private Coroutine playRoutine;
    private bool isPlaying;
    private bool pendingSkipToLastLine;
    private Color textBaseColor;
    private CanvasGroup lastLineIconCanvasGroup;
    private BgAudioData timedData;

    public bool IsFinished { get; private set; }
    public bool IsPlaying => isPlaying;

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

        EnsureAudioSource();
    }

    void Start()
    {
        if (ShouldSkip())
            return;

        if (playOnStart)
            Begin();
    }

    bool ShouldSkip()
    {
        return !string.IsNullOrWhiteSpace(skipIfHasFlag)
            && GameEventManager.Has(skipIfHasFlag.Trim());
    }

    public void Begin()
    {
        if (isPlaying)
            return;

        playRoutine = StartCoroutine(pendingSkipToLastLine ? SkipToLastLineRoutine() : PlayRoutine());
        pendingSkipToLastLine = false;
    }

    /// <summary>平滑过渡到最后一句话（不直接结束旁白）。</summary>
    public void SkipToLastLineSmooth()
    {
        if (IsFinished)
            return;

        pendingSkipToLastLine = true;

        if (!isPlaying)
            return;

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(SkipToLastLineRoutine());
        pendingSkipToLastLine = false;
    }

    /// <summary>立刻停止旁白并清理 UI（紧急中止时用）。</summary>
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
        if (TryLoadTimedData(out BgAudioData data))
        {
            yield return PlayTimedRoutine(data);
            yield break;
        }

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

    IEnumerator SkipToLastLineRoutine()
    {
        if (TryLoadTimedData(out BgAudioData data))
        {
            yield return SkipToLastLineTimedRoutine(data);
            yield break;
        }

        isPlaying = true;
        IsFinished = false;
        subtitleText.gameObject.SetActive(true);

        string[] narrationLines = GetLines();
        if (narrationLines == null || narrationLines.Length == 0)
        {
            Debug.LogWarning("[IntroNarrationLines] 没有旁白句子。", this);
            Cleanup();
            yield break;
        }

        float currentAlpha = subtitleText.color.a;
        if (currentAlpha > 0.01f)
            yield return FadeAlpha(currentAlpha, 0f, skipCrossfadeOutDuration);
        else
            SetTextAlpha(0f);

        if (lastLineIcon != null && lastLineIcon.activeSelf)
            yield return HideLastLineIcon();

        string lastLine = narrationLines[narrationLines.Length - 1];
        yield return ShowLastLineIcon();

        subtitleText.text = lastLine;
        yield return FadeAlpha(0f, 1f, skipCrossfadeInDuration);

        if (displayDuration > 0f)
            yield return new WaitForSeconds(displayDuration);

        yield return FadeAlpha(1f, 0f, fadeOutDuration);
        yield return HideLastLineIcon();

        Cleanup();
    }

    bool TryLoadTimedData(out BgAudioData data)
    {
        if (timedData != null)
        {
            data = timedData;
            return true;
        }

        data = null;
        if (timedNarrationJson == null || narrationAudio == null)
            return false;

        data = JsonUtility.FromJson<BgAudioData>(timedNarrationJson.text);
        if (data?.segments == null || data.segments.Length == 0)
        {
            Debug.LogWarning("[IntroNarrationLines] timedNarrationJson 解析失败或 segments 为空。", this);
            data = null;
            return false;
        }

        timedData = data;
        return true;
    }

    IEnumerator PlayTimedRoutine(BgAudioData data)
    {
        isPlaying = true;
        IsFinished = false;
        subtitleText.gameObject.SetActive(true);
        SetTextAlpha(0f);

        BgAudioSegment[] segments = data.segments;
        float endTime = segments[segments.Length - 1].end;

        EnsureAudioSource();
        audioSource.clip = narrationAudio;
        audioSource.time = 0f;
        audioSource.Play();

        int currentSegment = -1;
        bool lastIconShown = false;
        bool subtitleVisible = false;

        while (isPlaying)
        {
            float t = audioSource.time;
            if (!audioSource.isPlaying && t >= endTime - 0.02f)
                break;

            int segIdx = FindSegmentIndex(segments, t);
            if (segIdx != currentSegment)
            {
                if (subtitleVisible)
                {
                    yield return FadeAlpha(subtitleText.color.a, 0f, fadeOutDuration);
                    subtitleVisible = false;
                }

                if (segIdx >= 0)
                {
                    bool isLast = segIdx == segments.Length - 1;
                    if (isLast && !lastIconShown)
                    {
                        yield return ShowLastLineIcon();
                        lastIconShown = true;
                    }

                    subtitleText.text = segments[segIdx].text;
                    yield return FadeAlpha(0f, 1f, fadeInDuration);
                    subtitleVisible = true;
                }

                currentSegment = segIdx;
            }

            if (t >= endTime)
                break;

            yield return null;
        }

        if (subtitleVisible)
            yield return FadeAlpha(subtitleText.color.a, 0f, fadeOutDuration);

        if (lastIconShown)
            yield return HideLastLineIcon();

        Cleanup();
    }

    IEnumerator SkipToLastLineTimedRoutine(BgAudioData data)
    {
        isPlaying = true;
        IsFinished = false;
        subtitleText.gameObject.SetActive(true);

        BgAudioSegment[] segments = data.segments;
        BgAudioSegment last = segments[segments.Length - 1];

        EnsureAudioSource();
        if (audioSource.clip != narrationAudio)
            audioSource.clip = narrationAudio;

        float currentAlpha = subtitleText.color.a;
        if (currentAlpha > 0.01f)
            yield return FadeAlpha(currentAlpha, 0f, skipCrossfadeOutDuration);
        else
            SetTextAlpha(0f);

        if (lastLineIcon != null && lastLineIcon.activeSelf)
            yield return HideLastLineIcon();

        audioSource.time = last.start;
        if (!audioSource.isPlaying)
            audioSource.Play();

        yield return ShowLastLineIcon();
        subtitleText.text = last.text;
        yield return FadeAlpha(0f, 1f, skipCrossfadeInDuration);

        while (audioSource.isPlaying && audioSource.time < last.end)
            yield return null;

        yield return FadeAlpha(1f, 0f, fadeOutDuration);
        yield return HideLastLineIcon();
        Cleanup();
    }

    static int FindSegmentIndex(BgAudioSegment[] segments, float time)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            if (time >= segments[i].start && time < segments[i].end)
                return i;
        }

        if (segments.Length > 0 && time >= segments[segments.Length - 1].start)
            return segments.Length - 1;

        return -1;
    }

    void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Cleanup()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

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
