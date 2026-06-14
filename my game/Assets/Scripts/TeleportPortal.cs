using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TeleportPortal : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("全部存在时使用本条目；留空表示默认条目（兜底）")]
        public string[] requiredFlags;
        public string promptMessage = "Teleport [F]";
        [Tooltip("条件未满足时显示；留空则不显示")]
        public string lockedPromptMessage;
        [Tooltip("Build Settings 里 Scenes In Build 序号，从 0 开始")]
        public int targetSceneBuildIndex = 1;
        [Tooltip("与目标场景 PlayerSpawnPoint 的 Spawn Id 一致")]
        public string targetSpawnId;
        public string[] flagsToAddOnTeleport;
        public string[] flagsToRemoveOnTeleport;
    }

    [SerializeField] Entry[] entries;
    [SerializeField] GameObject promptRoot;
    [SerializeField] TMP_Text promptText;

    [Header("Debug（运行时只读）")]
    [SerializeField] string activeEntryFlags;
    [SerializeField] bool _inside;
    [SerializeField] bool playerInputLocked;

    Entry activeEntry;
    Transform _player;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (promptRoot != null)
            promptRoot.SetActive(false);

        GameEventManager.FlagAdded += OnFlagsChanged;
        GameEventManager.FlagRemoved += OnFlagsChanged;
        RefreshActiveEntry();
    }

    void OnDestroy()
    {
        GameEventManager.FlagAdded -= OnFlagsChanged;
        GameEventManager.FlagRemoved -= OnFlagsChanged;
    }

    void OnFlagsChanged(string _) => RefreshActiveEntry();

    void Update()
    {
        playerInputLocked = PlayerInputLock.IsLocked;

        if (!_inside || playerInputLocked || activeEntry == null)
        {
            HidePrompt();
            return;
        }

        if (!CanTeleport(activeEntry))
        {
            if (string.IsNullOrWhiteSpace(activeEntry.lockedPromptMessage))
                HidePrompt();
            else
                EventHintUI.Show(this, promptRoot, promptText, activeEntry.lockedPromptMessage);
            return;
        }

        EventHintUI.Show(this, promptRoot, promptText, activeEntry.promptMessage);

        if (!Input.GetKeyDown(KeyCode.F) || _player == null)
            return;

        TryTeleport();
    }

    void TryTeleport()
    {
        if (activeEntry == null)
            return;



        int buildIndex = activeEntry.targetSceneBuildIndex;
        string spawnId = activeEntry.targetSpawnId.Trim();

        SceneTransition.QueueLoadCompleteFlags(
            activeEntry.flagsToAddOnTeleport,
            activeEntry.flagsToRemoveOnTeleport);

        SceneLoadRunner.LoadScene(buildIndex, spawnId);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && other.GetComponent<CharacterController>() == null)
            return;

        _inside = true;
        _player = other.transform;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") && other.GetComponent<CharacterController>() == null)
            return;

        _inside = false;
        _player = null;
        HidePrompt();
    }

    void RefreshActiveEntry()
    {
        activeEntry = ResolveActiveEntry();
        activeEntryFlags = activeEntry != null ? FormatFlags(activeEntry.requiredFlags) : string.Empty;
    }

    Entry ResolveActiveEntry()
    {
        if (entries == null || entries.Length == 0)
            return null;

        Entry fallback = null;
        Entry matched = null;

        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            if (entry == null || !HasAllFlags(entry.requiredFlags))
                continue;

            if (IsFallbackEntry(entry))
                fallback = entry;
            else
                matched = entry;
        }

        return matched ?? fallback;
    }

    static bool CanTeleport(Entry entry) =>
        entry != null
        && !string.IsNullOrEmpty(entry.targetSpawnId)
        && HasAllFlags(entry.requiredFlags);

    static bool IsFallbackEntry(Entry entry)
    {
        if (entry.requiredFlags == null || entry.requiredFlags.Length == 0)
            return true;

        foreach (string flag in entry.requiredFlags)
        {
            if (!string.IsNullOrWhiteSpace(flag))
                return false;
        }

        return true;
    }

    static bool HasAllFlags(string[] flags)
    {
        if (flags == null || flags.Length == 0)
            return true;

        foreach (string flag in flags)
        {
            if (string.IsNullOrWhiteSpace(flag))
                continue;

            if (!GameEventManager.Has(flag.Trim()))
                return false;
        }

        return true;
    }

    static string FormatFlags(string[] flags)
    {
        if (flags == null || flags.Length == 0)
            return "(default)";

        return string.Join(", ", flags);
    }

    void HidePrompt()
    {
        EventHintUI.Hide(this, promptRoot);
    }
}
