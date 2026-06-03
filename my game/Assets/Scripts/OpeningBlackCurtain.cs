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
    [Header("流程")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool lockPlayerInput = true;
    [SerializeField] private bool startDialogueAfterFade = true;
    [SerializeField] private DialogueReader dialogueReader;
    [SerializeField] private TextAsset dialogueAfterFade;

    [Header("逐句旁白（可选）")]
    [SerializeField] private IntroNarrationLines narration;
    [Tooltip("勾选后等旁白全部播完再淡出黑幕")]
    [SerializeField] private bool waitForNarrationBeforeFade = true;

    [Header("跳过")]
    [SerializeField] private GameObject skipButtonRoot;

    [Header("对话结束后任务")]
    [SerializeField] private QuestDisplay questDisplay;

    private const string IntroQuestName = "Find the villager";
    private const string IntroQuestContent = "Find a villager and talk with him";

    private CanvasGroup canvasGroup;
    private Coroutine sequenceCoroutine;
    private bool isPlaying;
    private bool skipToLastLineRequested;

    public bool IsIntroPlaying => isPlaying;

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

        skipToLastLineRequested = false;
        sequenceCoroutine = StartCoroutine(PlaySequence());
    }

    /// <summary>平滑跳到黑幕旁白最后一句，之后仍按正常节奏淡出黑幕。</summary>
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
        canvasGroup.alpha = 1f;
        gameObject.SetActive(true);

        if (lockPlayerInput)
            PlayerInputLock.SetLocked(true);

        if (!skipToLastLineRequested)
            yield return WaitSeconds(holdBeforeNarration);

        if (narration != null)
        {
            // 黑幕前按 E 时，Begin 会通过 pendingSkipToLastLine 直接进入末句流程
            narration.Begin();

            if (waitForNarrationBeforeFade)
            {
                while (!narration.IsFinished)
                    yield return null;
            }
        }

        yield return WaitSeconds(holdAfterNarration);

        float fadeTime = fadeDuration;
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
        while (elapsed < duration && !skipToLastLineRequested)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void FinishIntro()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        if (skipButtonRoot != null)
            skipButtonRoot.SetActive(false);

        if (startDialogueAfterFade && dialogueReader != null && dialogueAfterFade != null)
        {
            dialogueReader.ReadingFinished += OnDialogueDone;
            dialogueReader.StartReading(dialogueAfterFade);
        }
        else
            OnDialogueDone();

        gameObject.SetActive(false);

        if (!startDialogueAfterFade && lockPlayerInput)
            PlayerInputLock.SetLocked(false);

        isPlaying = false;
        skipToLastLineRequested = false;
        sequenceCoroutine = null;
    }

    void OnDialogueDone()
    {
        if (dialogueReader != null)
            dialogueReader.ReadingFinished -= OnDialogueDone;

        if (questDisplay == null)
            questDisplay = FindObjectOfType<QuestDisplay>(true);

        if (questDisplay == null)
        {
            Debug.LogWarning("[OpeningBlackCurtain] 未指定 Quest Display。", this);
            return;
        }

        questDisplay.SetCurrentQuest(IntroQuestName, IntroQuestContent);
    }
}
