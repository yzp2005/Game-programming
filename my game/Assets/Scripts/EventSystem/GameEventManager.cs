using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>全局事件标签。场景放一个空物体挂此脚本即可。</summary>
public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance { get; private set; }

    readonly HashSet<string> _flags = new HashSet<string>();

    /// <summary>首次 Set 某 flag 时触发。与 Set / Has 一样通过类名直接订阅。</summary>
    public static event Action<string> FlagAdded;

    /// <summary>Remove 成功移除某 flag 时触发。</summary>
    public static event Action<string> FlagRemoved;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static bool Has(string tag) =>
        Instance != null && !string.IsNullOrEmpty(tag) && Instance._flags.Contains(tag);

    public static void Set(string tag)
    {
        if (Instance == null || string.IsNullOrEmpty(tag))
            return;

        if (Instance._flags.Add(tag))
            FlagAdded?.Invoke(tag);
    }

    public static bool Remove(string tag)
    {
        if (Instance == null || string.IsNullOrEmpty(tag))
            return false;

        if (!Instance._flags.Remove(tag))
            return false;

        FlagRemoved?.Invoke(tag);
        return true;
    }

    /// <summary>将已有 flag 重命名。新名已存在或旧名不存在时返回 false。</summary>
    public static bool Rename(string oldTag, string newTag)
    {
        if (Instance == null || string.IsNullOrEmpty(oldTag) || string.IsNullOrEmpty(newTag))
            return false;

        newTag = newTag.Trim();
        if (string.IsNullOrEmpty(newTag))
            return false;

        if (oldTag == newTag)
            return true;

        if (!Instance._flags.Contains(oldTag))
            return false;

        if (Instance._flags.Contains(newTag))
            return false;

        Instance._flags.Remove(oldTag);
        Instance._flags.Add(newTag);
        FlagRemoved?.Invoke(oldTag);
        FlagAdded?.Invoke(newTag);
        return true;
    }

    public static int FlagCount => Instance != null ? Instance._flags.Count : 0;

    public static IReadOnlyList<string> GetAllFlagsSorted()
    {
        if (Instance == null || Instance._flags.Count == 0)
            return Array.Empty<string>();

        return Instance._flags.OrderBy(flag => flag, StringComparer.Ordinal).ToArray();
    }
}
