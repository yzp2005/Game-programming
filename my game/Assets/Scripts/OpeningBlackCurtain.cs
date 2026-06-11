using System.Collections;
using UnityEngine;

/// <summary>开场黑幕：全黑 →（可选旁白）→ 淡出 → 可选对话。</summary>
[DefaultExecutionOrder(-250)]
[RequireComponent(typeof(CanvasGroup))]
public class OpeningBlackCurtain : MonoBehaviour
{
    [Header("时间")]
    [SerializeField] float holdBeforeNarration = 0.5f;
    [SerializeField] float holdAfterNarration = 2f;
    [SerializeField] float fadeDuration = 1f;

    [Header("流程")]
    [SerializeField] bool playOnStart = true;
    [SerializeField] bool lockPlayerInput = true;
    [SerializeField] bool startDialogueAfterFade = true;
    [SerializeField] DialogueReader dialogueReader;
    [SerializeField] TextAsset dialogueAfterFade;

    [Header("旁白（可选）")]
    [SerializeField] IntroNarrationLines narration;
    [SerializeField] bool waitForNarrationBeforeFade = true;

    [Header("跳过")]
    [SerializeField] GameObject skipButtonRoot;

    [Header("对话结束后")]
    [SerializeField] string questFlagOnDialogueDone = "intro_quest";

    [Header("只播一次（跨场景保留）")]
    [Tooltip("开场全流程结束后 Set；再次进场景时若已有则跳过黑幕与对话")]
    [SerializeField] string introCompleteFlag = "intro_played";

    CanvasGroup canvasGroup;
    bool isPlaying;
    bool skipToLastLineRequested;

    public bool IsIntroPlaying => isPlaying;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        gameObject.SetActive(true);

        if (ShouldSkipIntro())
        {
            ApplySkipIntroState();
            return;
        }

        SetCurtainOpaque(true);

        if (skipButtonRoot != null)
            skipButtonRoot.SetActive(true);
    }

    void Start()
    {
        if (ShouldSkipIntro() || !playOnStart)
            return;

        Play();
    }

    bool ShouldSkipIntro()
    {
        return !string.IsNullOrWhiteSpace(introCompleteFlag)
            && GameEventManager.Has(introCompleteFlag.Trim());
    }

    void ApplySkipIntroState()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (skipButtonRoot != null)
            skipButtonRoot.SetActive(false);

        narration?.StopImmediately();
        gameObject.SetActive(false);
    }

    public void Play()
    {
        if (isPlaying)
            return;

        skipToLastLineRequested = false;
        if (lockPlayerInput)
            PlayerInputLock.SetLocked(true);

        StartCoroutine(PlaySequence());
    }

    public void SkipIntro()
    {
        if (!isPlaying || skipToLastLineRequested)
            return;

        skipToLastLineRequested = true;
        narration?.SkipToLastLineSmooth();
    }

    IEnumerator PlaySequence()
    {
        isPlaying = true;
        SetCurtainOpaque(true);

        if (!skipToLastLineRequested)
            yield return WaitSkippable(holdBeforeNarration);

        if (narration != null)
        {
            narration.Begin();
            if (waitForNarrationBeforeFade)
            {
                while (!narration.IsFinished)
                    yield return null;
            }
        }

        yield return WaitSkippable(holdAfterNarration);
        yield return FadeOut(fadeDuration);
        FinishIntro();
    }

    IEnumerator WaitSkippable(float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration && !skipToLastLineRequested)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator FadeOut(float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = 0f;
            yield break;
        }

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    void FinishIntro()
    {
        canvasGroup.blocksRaycasts = false;

        if (skipButtonRoot != null)
            skipButtonRoot.SetActive(false);

        if (startDialogueAfterFade && dialogueReader != null && dialogueAfterFade != null)
        {
            dialogueReader.ReadingFinished += OnDialogueDone;
            dialogueReader.StartReading(dialogueAfterFade);
        }
        else
        {
            if (lockPlayerInput)
                PlayerInputLock.SetLocked(false);
            MarkIntroComplete();
            SetQuestFlag();
        }

        gameObject.SetActive(false);
        isPlaying = false;
        skipToLastLineRequested = false;
    }

    void OnDialogueDone()
    {
        if (dialogueReader != null)
            dialogueReader.ReadingFinished -= OnDialogueDone;

        MarkIntroComplete();
        SetQuestFlag();
    }

    void MarkIntroComplete()
    {
        if (!string.IsNullOrWhiteSpace(introCompleteFlag))
            GameEventManager.Set(introCompleteFlag.Trim());
    }

    void SetQuestFlag()
    {
        if (!string.IsNullOrWhiteSpace(questFlagOnDialogueDone))
            GameEventManager.Set(questFlagOnDialogueDone.Trim());
    }

    void SetCurtainOpaque(bool opaque)
    {
        canvasGroup.alpha = opaque ? 1f : 0f;
        canvasGroup.blocksRaycasts = opaque;
        canvasGroup.interactable = false;
    }
}
