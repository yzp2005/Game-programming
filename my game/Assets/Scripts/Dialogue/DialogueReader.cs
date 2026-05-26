using TMPro;
using UnityEngine;

/// <summary>
/// 读取 Dialogue JSON（TextAsset），逐行显示到 TMP，左键切换下一句。
/// </summary>
public class DialogueReader : MonoBehaviour
{
    [Header("数据")]
    [SerializeField] private TextAsset dialogueJson;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;

    [Header("流程")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool lockPlayerInput = true;

    private DialogueData data;
    private int lineIndex;

    public bool IsPlaying => data != null;
    public DialogueData CurrentData => data;
    public int CurrentLineIndex => lineIndex;

    void Awake()
    {
        if (lockPlayerInput && playOnStart)
            PlayerInputLock.SetLocked(true);
    }

    void Start()
    {
        if (playOnStart)
            StartReading();
    }

    /// <summary>从 Inspector 指定的 JSON 开始播放。</summary>
    public void StartReading()
    {
        if (dialogueJson == null)
        {
            Debug.LogError("[DialogueReader] 请在 Inspector 指定 Dialogue Json。", this);
            return;
        }

        StartReading(dialogueJson);
    }

    /// <summary>从指定 TextAsset 开始播放。</summary>
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

        if (lockPlayerInput)
            PlayerInputLock.SetLocked(true);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowLine(lineIndex);
    }

    void Update()
    {
        if (!IsPlaying)
            return;

        // 左键在屏幕任意位置按下即可跳下一句（不依赖点到 UI）
        if (Input.GetMouseButtonDown(0))
            AdvanceLine();
    }

    /// <summary>显示下一句；若已是最后一句则结束。</summary>
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
        data = null;
        lineIndex = 0;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (lockPlayerInput)
            PlayerInputLock.SetLocked(false);
    }

    void ShowLine(int index)
    {
        DialogueLine line = data.lines[index];
        SetText(speakerText, line.speakerName);
        SetText(bodyText, line.text);
    }

    public static bool TryParse(string json, out DialogueData result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[DialogueReader] JSON 内容为空。");
            return false;
        }

        result = JsonUtility.FromJson<DialogueData>(json);

        if (result?.lines == null || result.lines.Length == 0)
        {
            Debug.LogError("[DialogueReader] JSON 解析失败或 lines 为空。");
            result = null;
            return false;
        }

        return true;
    }

    static void SetText(TMP_Text tmp, string value)
    {
        if (tmp != null)
            tmp.text = value ?? string.Empty;
    }
}
