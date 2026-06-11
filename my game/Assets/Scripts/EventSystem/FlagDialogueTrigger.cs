using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>监听 GameEventManager 标签，出现时播放对话 JSON。</summary>
[DisallowMultipleComponent]
public class FlagDialogueTrigger : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("首次 Set 此 flag 时播放")]
        public string whenFlag;
        public DialogueReader dialogueReader;
        public TextAsset dialogue;
        public bool onlyOnce = true;
        public string[] flagsToAddOnFinish;
        public string[] flagsToRemoveOnFinish;
    }

    [SerializeField] Entry[] entries;

    readonly HashSet<string> triggeredFlags = new HashSet<string>();
    DialogueReader activeReader;
    Entry activeEntry;

    void Awake() => GameEventManager.FlagAdded += OnFlagAdded;

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

    void TryPlay(Entry entry, string flag)
    {
        if (entry == null || !FlagMatches(entry.whenFlag, flag))
            return;

        if (entry.onlyOnce && triggeredFlags.Contains(flag))
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

        triggeredFlags.Add(flag);
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
