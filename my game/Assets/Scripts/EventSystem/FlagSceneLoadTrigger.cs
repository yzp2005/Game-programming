using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>监听 GameEventManager 标签，出现时切换场景（走 SceneLoadRunner）。</summary>
[DisallowMultipleComponent]
public class FlagSceneLoadTrigger : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("首次 Set 此 flag 时加载场景")]
        public string whenFlag;
        [Tooltip("Build Settings 里 Scenes In Build 的序号，从 0 开始")]
        public int targetSceneBuildIndex;
        [Tooltip("与目标场景 PlayerSpawnPoint 的 Spawn Id 一致")]
        public string targetSpawnId;
        public bool onlyOnce = true;
        [Tooltip("新场景淡入完成后写入")]
        public string[] flagsToAddOnFinish;
        public string[] flagsToRemoveOnFinish;
    }

    [SerializeField] Entry[] entries;

    readonly HashSet<string> triggeredFlags = new HashSet<string>();

    void Awake() => GameEventManager.FlagAdded += OnFlagAdded;

    void OnDestroy() => GameEventManager.FlagAdded -= OnFlagAdded;

    void OnFlagAdded(string flag)
    {
        if (entries == null || string.IsNullOrEmpty(flag))
            return;

        for (int i = 0; i < entries.Length; i++)
            TryLoad(entries[i], flag);
    }

    void TryLoad(Entry entry, string flag)
    {
        if (entry == null || !FlagMatches(entry.whenFlag, flag))
            return;

        string key = entry.whenFlag.Trim();
        if (entry.onlyOnce && triggeredFlags.Contains(key))
            return;

        if (string.IsNullOrEmpty(entry.targetSpawnId))
        {
            Debug.LogWarning($"{name}: flag「{flag}」未指定 targetSpawnId。", this);
            return;
        }

        if (entry.onlyOnce)
            triggeredFlags.Add(key);

        SceneTransition.QueueLoadCompleteFlags(entry.flagsToAddOnFinish, entry.flagsToRemoveOnFinish);
        SceneLoadRunner.LoadScene(entry.targetSceneBuildIndex, entry.targetSpawnId);
    }

    static bool FlagMatches(string expected, string actual)
    {
        return !string.IsNullOrWhiteSpace(expected)
            && string.Equals(expected.Trim(), actual, StringComparison.Ordinal);
    }
}
