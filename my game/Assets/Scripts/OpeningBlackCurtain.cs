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
    [Tooltip("勾选后黑幕结束再调用 DialogueReader；请将 DialogueReader 的 Play On Start 关掉")]
    [SerializeField] private bool startDialogueAfterFade;
    [SerializeField] private DialogueReader dialogueReader;

    [Header("逐句旁白（可选）")]
    [SerializeField] private IntroNarrationLines narration;
    [Tooltip("勾选后等旁白全部播完再淡出黑幕")]
    [SerializeField] private bool waitForNarrationBeforeFade = true;

    private CanvasGroup canvasGroup;
    private bool isPlaying;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;
        gameObject.SetActive(true);

        if (lockPlayerInput)
            PlayerInputLock.SetLocked(true);
    }

    void Start()
    {
        if (playOnStart)
            StartCoroutine(PlaySequence());
    }

    public void Play()
    {
        if (!isPlaying)
            StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        if (isPlaying)
            yield break;

        isPlaying = true;
        canvasGroup.alpha = 1f;
        gameObject.SetActive(true);

        if (lockPlayerInput)
            PlayerInputLock.SetLocked(true);

        if (holdBeforeNarration > 0f)
            yield return new WaitForSeconds(holdBeforeNarration);

        if (narration != null)
        {
            narration.Begin();

            if (waitForNarrationBeforeFade)
            {
                while (!narration.IsFinished)
                    yield return null;
            }
        }

        if (holdAfterNarration > 0f)
            yield return new WaitForSeconds(holdAfterNarration);

        if (fadeDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);

        if (startDialogueAfterFade && dialogueReader != null)
            dialogueReader.StartReading();
        else if (lockPlayerInput)
            PlayerInputLock.SetLocked(false);

        isPlaying = false;
    }
}
