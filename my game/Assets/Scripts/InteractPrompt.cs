using TMPro;
using UnityEngine;

/// <summary>
/// 挂在 NPC 上：进入 Trigger 显示提示，按 F 调用场景里的 DialogueReader 播放对话 JSON。
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteractPrompt : MonoBehaviour
{
    [Header("条件（可选）")]
    [SerializeField] private string requiredFlag;

    [Header("提示 UI")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string promptMessage = "Talk [F]";

    [Header("对话（拖场景里已有的 DialogueReader + JSON）")]
    [SerializeField] private DialogueReader dialogueReader;
    [SerializeField] private TextAsset dialogue;

    [Header("对话结束后 Flag（可选）")]
    [Tooltip("对话正常播完后 GameEventManager.Set")]
    [SerializeField] private string[] flagsToAddOnDialogueFinish;
    [Tooltip("对话正常播完后 GameEventManager.Remove")]
    [SerializeField] private string[] flagsToRemoveOnDialogueFinish;

    [Header("对话结束后任务 UI（留空不更新）")]
    [SerializeField] private QuestDisplay questDisplay;
    [SerializeField] private string questNameAfter;
    [SerializeField] private string questContentAfter;

    bool _playerInside;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (dialogueReader == null)
            dialogueReader = FindObjectOfType<DialogueReader>();
        if (questDisplay == null)
            questDisplay = FindObjectOfType<QuestDisplay>();
        SetPrompt(false);
        MinimapTrackable.EnsureOn(gameObject, MinimapTrackable.BlipKind.Friendly);
    }

    void OnDestroy()
    {
        if (dialogueReader != null)
            dialogueReader.ReadingFinished -= OnDialogueFinished;
    }

    void Update()
    {
        if (dialogueReader != null && dialogueReader.IsPlaying)
        {
            SetPrompt(false);
            return;
        }

        if (!_playerInside || PlayerInputLock.IsLocked || !CanInteract())
        {
            SetPrompt(false);
            return;
        }

        SetPrompt(true);

        if (Input.GetKeyDown(KeyCode.F) && dialogue != null && dialogueReader != null)
        {
            dialogueReader.StartReading(dialogue);
            if (NeedsDialogueFinishedHandler() && dialogueReader.IsPlaying)
                dialogueReader.ReadingFinished += OnDialogueFinished;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
            _playerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            _playerInside = false;
            SetPrompt(false);
        }
    }

    bool CanInteract()
    {
        return string.IsNullOrEmpty(requiredFlag) || GameEventManager.Has(requiredFlag);
    }

    bool HasQuestAfterDialogue()
    {
        return questDisplay != null
            && (!string.IsNullOrWhiteSpace(questNameAfter) || !string.IsNullOrWhiteSpace(questContentAfter));
    }

    bool NeedsDialogueFinishedHandler()
    {
        return HasQuestAfterDialogue() || HasFlagChangesOnDialogueFinish();
    }

    bool HasFlagChangesOnDialogueFinish()
    {
        return HasNonEmptyEntry(flagsToAddOnDialogueFinish)
            || HasNonEmptyEntry(flagsToRemoveOnDialogueFinish);
    }

    static bool HasNonEmptyEntry(string[] entries)
    {
        if (entries == null)
            return false;

        foreach (string entry in entries)
        {
            if (!string.IsNullOrWhiteSpace(entry))
                return true;
        }

        return false;
    }

    void OnDialogueFinished()
    {
        dialogueReader.ReadingFinished -= OnDialogueFinished;
        ApplyDialogueFinishFlags();

        if (HasQuestAfterDialogue())
            questDisplay.SetCurrentQuest(questNameAfter, questContentAfter);
    }

    void ApplyDialogueFinishFlags()
    {
        if (flagsToAddOnDialogueFinish != null)
        {
            foreach (string flag in flagsToAddOnDialogueFinish)
            {
                if (!string.IsNullOrWhiteSpace(flag))
                    GameEventManager.Set(flag.Trim());
            }
        }

        if (flagsToRemoveOnDialogueFinish != null)
        {
            foreach (string flag in flagsToRemoveOnDialogueFinish)
            {
                if (!string.IsNullOrWhiteSpace(flag))
                    GameEventManager.Remove(flag.Trim());
            }
        }
    }

    void SetPrompt(bool show)
    {
        if (show)
            EventHintUI.Show(this, promptRoot, promptText, promptMessage);
        else
            EventHintUI.Hide(this, promptRoot);
    }

    static bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponent<CharacterController>() != null;
    }
}
