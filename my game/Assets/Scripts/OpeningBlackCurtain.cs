using System.Collections;
using UnityEngine;

/// <summary>
/// 开场黑幕：全黑 →（可选逐句旁白）→ 渐变透明 → 进入游戏 / 对话。
/// </summary>
[DefaultExecutionOrder(-250)]
[RequireComponent(typeof(CanvasGroup))]
public class OpeningBlackCurtain : MonoBehaviour
{
    [Header("时间")]
    [Tooltip("黑幕出现后、旁白开始前的等待（秒）")]
    [SerializeField] private float holdBeforeNarration = 0.5f;
    [Tooltip("最后一句旁白结束后、黑幕淡出前的额外等待（秒）")]
    [SerializeField] private float holdAfterNarration = 2f;
    [Tooltip("从黑到完全透明的渐变时长（秒）")]
    [SerializeField] private float fadeDuration = 1f;
    [Tooltip("点击跳过后，黑幕淡出的时长（秒）")]
    [SerializeField] private float skipFadeDuration = 0.35f;

    [Header("流程")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool lockPlayerInput = true;
    [Tooltip("勾选后黑幕结束再调用 DialogueReader；请将 DialogueReader 的 Play On Start 关掉")]
    [SerializeField] private bool startDialogueAfterFade;
    [SerializeField] private DialogueReader dialogueReader;

    [Header("逐句旁白（可选）")]
    [SerializeField] private IntroNarrationLines narration;
    [Tooltip("勾选后等旁白全部播完再淡出黑幕")]
    [SerializeField] private bool waitForNarrationBeforeFade = true;

    [Header("跳过")]
    [SerializeField] private GameObject skipButtonRoot;

    private CanvasGroup canvasGroup;
    private Coroutine sequenceCoroutine;
    private bool isPlaying;
    private bool skipRequested;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;
        gameObject.SetActive(true);

        if (skipButtonRoot != null)
            skipButtonRoot.SetActive(true);

        if (lockPlayerInput)
            PlayerInputLock.SetLocked(true);
    }

    void Start()
    {
        if (playOnStart)
            Play();
    }

    public void Play()
    {
        if (isPlaying)
            return;

        skipRequested = false;
        sequenceCoroutine = StartCoroutine(PlaySequence());
    }

    /// <summary>跳过黑幕与旁白，直接进入淡出 / 对话。</summary>
    public void SkipIntro()
    {
        if (!isPlaying || skipRequested)
            return;

        skipRequested = true;
        narration?.StopImmediately();
    }

    IEnumerator PlaySequence()
    {
        isPlaying = true;
        canvasGroup.alpha = 1f;
        gameObject.SetActive(true);

        if (lockPlayerInput)
            PlayerInputLock.SetLocked(true);

        if (!skipRequested)
            yield return WaitSeconds(holdBeforeNarration);

        if (!skipRequested && narration != null)
        {
            narration.Begin();

            if (waitForNarrationBeforeFade)
            {
                while (!narration.IsFinished && !skipRequested)
                    yield return null;
            }
        }

        if (skipRequested)
            narration?.StopImmediately();

        if (!skipRequested)
            yield return WaitSeconds(holdAfterNarration);

        float fadeTime = skipRequested ? skipFadeDuration : fadeDuration;
        if (fadeTime > 0f)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / fadeTime));
                yield return null;
            }
        }

        FinishIntro();
    }

    IEnumerator WaitSeconds(float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration && !skipRequested)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void FinishIntro()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);

        if (skipButtonRoot != null)
            skipButtonRoot.SetActive(false);

        if (startDialogueAfterFade && dialogueReader != null)
            dialogueReader.StartReading();
        else if (lockPlayerInput)
            PlayerInputLock.SetLocked(false);

        isPlaying = false;
        skipRequested = false;
        sequenceCoroutine = null;
    }
}
