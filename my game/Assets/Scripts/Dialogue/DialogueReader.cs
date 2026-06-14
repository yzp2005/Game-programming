using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 读取 Dialogue JSON，左键下一句。仅通过 StartReading(TextAsset) 播放。
/// </summary>
public class DialogueReader : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image portraitImage;

    [Header("头像")]
    [SerializeField] private DialoguePortraitDatabase portraitDatabase;

    [Header("流程")]
    [SerializeField] private bool lockPlayerInput = true;

    private DialogueData data;
    private int lineIndex;
    private bool lockedByThisReader;

    public bool IsPlaying => data != null;

    /// <summary>对话正常播完（最后一句点过）时触发。</summary>
    public event Action ReadingFinished;

    void Awake()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        EnsurePortraitRefs();
        HidePortrait();
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

        if (!DialogueJsonParser.TryParse(jsonAsset.text, out DialogueData parsed))
            return;

        EnsurePortraitRefs();

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

        HidePortrait();
        UnlockPlayer();
        ReadingFinished?.Invoke();
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

        ApplyPortrait(line);
    }

    void EnsurePortraitRefs()
    {
        if (portraitImage == null)
            portraitImage = FindPortraitImage();

        if (portraitDatabase == null)
            portraitDatabase = Resources.Load<DialoguePortraitDatabase>("DialoguePortraitDatabase");

#if UNITY_EDITOR
        if (portraitDatabase == null)
        {
            portraitDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<DialoguePortraitDatabase>(
                "Assets/Emotion/DialoguePortraitDatabase.asset");
        }
#endif
    }

    Image FindPortraitImage()
    {
        if (portraitImage != null)
            return portraitImage;

        Transform root = dialoguePanel != null ? dialoguePanel.transform : transform;
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].gameObject.name.IndexOf("portrait", StringComparison.OrdinalIgnoreCase) >= 0)
                return images[i];
        }

        return null;
    }

    void ApplyPortrait(DialogueLine line)
    {
        EnsurePortraitRefs();

        if (portraitImage == null || portraitDatabase == null || line.emo <= 0)
        {
            HidePortrait();
            return;
        }

        if (!portraitDatabase.TryGetSprite(line.speakerName, line.emo, out Sprite sprite))
        {
            HidePortrait();
            return;
        }

        portraitImage.sprite = sprite;
        portraitImage.enabled = true;

        if (!portraitImage.gameObject.activeSelf)
            portraitImage.gameObject.SetActive(true);
    }

    void HidePortrait()
    {
        if (portraitImage == null)
            return;

        portraitImage.enabled = false;
    }
}
