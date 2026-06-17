using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>监听 GameEventManager 标签，出现时播放对话 JSON；进场景时若已有对应 flag 也会尝试播放。</summary>
[DisallowMultipleComponent]
public class FlagDialogueTrigger : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("存在此 flag 时播放（Set 瞬间或进场景时已存在）")]
        public string whenFlag;
        public DialogueReader dialogueReader;
        public TextAsset dialogue;
        public bool onlyOnce = true;
        public string[] flagsToAddOnFinish;
        public string[] flagsToRemoveOnFinish;
    }

    [SerializeField] Entry[] entries;

    [Header("进场景")]
    [Tooltip("加载场景后，若已有 Entry 对应的 flag，也尝试播放（读档、跨场景保留 flag）")]
    [SerializeField] bool checkFlagOnStart = true;

    readonly HashSet<string> triggeredFlags = new HashSet<string>();
    DialogueReader activeReader;
    Entry activeEntry;

    void Awake() => GameEventManager.FlagAdded += OnFlagAdded;

    void Start()
    {
        if (checkFlagOnStart)
            CheckExistingFlags();
    }

    void OnDestroy()
    {
        GameEventManager.FlagAdded -= OnFlagAdded;
        UnsubscribeDialogue();
    }

    void OnFlagAdded(string flag)
    {
        if (entries == null || string.IsNullOrEmpty(flag))
            return;

        for (int i = 0; i < entries.Length; i++)
            TryPlay(entries[i], flag);
    }

    void CheckExistingFlags()
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.whenFlag))
                continue;

            string flag = entry.whenFlag.Trim();
            if (GameEventManager.Has(flag))
                TryPlay(entry, flag);
        }
    }

    void TryPlay(Entry entry, string flag)
    {
        if (entry == null || !FlagMatches(entry.whenFlag, flag))
            return;

        string key = entry.whenFlag.Trim();
        if (entry.onlyOnce && triggeredFlags.Contains(key))
            return;

        if (entry.dialogue == null)
        {
            Debug.LogWarning($"{name}: flag「{flag}」未指定 dialogue。", this);
            return;
        }

        DialogueReader reader = entry.dialogueReader != null
            ? entry.dialogueReader
            : FindObjectOfType<DialogueReader>();

        if (reader == null)
        {
            Debug.LogWarning($"{name}: 场景中找不到 DialogueReader。", this);
            return;
        }

        if (reader.IsPlaying)
            return;

        triggeredFlags.Add(key);
        activeEntry = entry;
        activeReader = reader;
        reader.ReadingFinished -= OnDialogueFinished;
        reader.ReadingFinished += OnDialogueFinished;
        reader.StartReading(entry.dialogue);
    }

    void OnDialogueFinished()
    {
        UnsubscribeDialogue();

        if (activeEntry != null)
            FlagEventActions.Apply(activeEntry.flagsToAddOnFinish, activeEntry.flagsToRemoveOnFinish);

        activeEntry = null;
    }

    void UnsubscribeDialogue()
    {
        if (activeReader == null)
            return;

        activeReader.ReadingFinished -= OnDialogueFinished;
        activeReader = null;
    }

    static bool FlagMatches(string expected, string actual)
    {
        return !string.IsNullOrWhiteSpace(expected)
            && string.Equals(expected.Trim(), actual, StringComparison.Ordinal);
    }
}
