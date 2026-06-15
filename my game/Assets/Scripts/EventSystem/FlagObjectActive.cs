using System;
using UnityEngine;

/// <summary>
/// 监听 GameEventManager 标签，控制物体 SetActive。
/// 同一 Target 配多条 Entry 时：任意一条满足即显示（OR）。
/// 建议把本脚本挂在始终激活的管理物体上，Target 拖要显隐的子物体。
/// </summary>
[DisallowMultipleComponent]
public class FlagObjectActive : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("监听此 flag；满足条件时尝试显示 Target")]
        public string whenFlag;
        [Tooltip("要显隐的物体（必填）")]
        public GameObject target;
        [Tooltip("勾选：有 flag 时隐藏，无 flag 时显示")]
        public bool invert;
    }

    [SerializeField] Entry[] entries;
    [SerializeField] bool checkFlagsOnStart = true;

    void OnEnable()
    {
        GameEventManager.FlagAdded += OnFlagsChanged;
        GameEventManager.FlagRemoved += OnFlagsChanged;

        if (checkFlagsOnStart)
            RefreshAll();
    }

    void OnDisable()
    {
        GameEventManager.FlagAdded -= OnFlagsChanged;
        GameEventManager.FlagRemoved -= OnFlagsChanged;
    }

    void OnFlagsChanged(string _) => RefreshAll();

    void RefreshAll()
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            if (entry == null || entry.target == null)
                continue;

            if (WasTargetProcessed(i, entry.target))
                continue;

            entry.target.SetActive(ShouldTargetBeActive(entry.target));
        }
    }

    bool WasTargetProcessed(int startIndex, GameObject target)
    {
        for (int i = 0; i < startIndex; i++)
        {
            if (entries[i] != null && entries[i].target == target)
                return true;
        }

        return false;
    }

    bool ShouldTargetBeActive(GameObject target)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            if (entry == null || entry.target != target)
                continue;

            if (EntryWantsActive(entry))
                return true;
        }

        return false;
    }

    static bool EntryWantsActive(Entry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.whenFlag))
            return false;

        bool hasFlag = GameEventManager.Has(entry.whenFlag.Trim());
        return entry.invert ? !hasFlag : hasFlag;
    }
}
