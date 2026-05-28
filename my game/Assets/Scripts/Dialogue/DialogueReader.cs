using TMPro;
using UnityEngine;

/// <summary>
/// 读取 Dialogue JSON，左键下一句。仅通过 StartReading(TextAsset) 播放。
/// </summary>
public class DialogueReader : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;

    [Header("流程")]
    [SerializeField] private bool lockPlayerInput = true;

    private DialogueData data;
    private int lineIndex;
    private bool lockedByThisReader;

    public bool IsPlaying => data != null;

    void Awake()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    void OnDisable()
    {
        if (lockedByThisReader)
            UnlockPlayer();
    }

    void Update()
    {
        if (!IsPlaying)
            return;

        if (Input.GetMouseButtonDown(0))
            AdvanceLine();
    }

    public void StartReading(TextAsset jsonAsset)
    {
        if (jsonAsset == null)
        {
            Debug.LogError("[DialogueReader] TextAsset 为空。", this);
            return;
        }

        if (!TryParse(jsonAsset.text, out DialogueData parsed))
            return;

        data = parsed;
        lineIndex = 0;

        LockPlayer();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowLine(lineIndex);
    }

    public void AdvanceLine()
    {
        if (!IsPlaying)
            return;

        lineIndex++;
        if (lineIndex >= data.lines.Length)
        {
            StopReading();
            return;
        }

        ShowLine(lineIndex);
    }

    public void StopReading()
    {
        if (!IsPlaying)
            return;

        data = null;
        lineIndex = 0;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        UnlockPlayer();
    }

    void LockPlayer()
    {
        if (!lockPlayerInput)
            return;

        PlayerInputLock.SetLocked(true);
        lockedByThisReader = true;
    }

    void UnlockPlayer()
    {
        if (!lockedByThisReader)
            return;

        PlayerInputLock.SetLocked(false);
        lockedByThisReader = false;
    }

    void ShowLine(int index)
    {
        DialogueLine line = data.lines[index];
        if (speakerText != null)
            speakerText.text = line.speakerName ?? "";
        if (bodyText != null)
            bodyText.text = line.text ?? "";
    }

    static bool TryParse(string json, out DialogueData result)
    {
        result = JsonUtility.FromJson<DialogueData>(json);

        if (result?.lines == null || result.lines.Length == 0)
        {
            Debug.LogError("[DialogueReader] JSON 解析失败或 lines 为空。");
            result = null;
            return false;
        }

        return true;
    }
}
