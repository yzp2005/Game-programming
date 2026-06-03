using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>全局事件标签。场景放一个空物体挂此脚本即可。</summary>
public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance { get; private set; }

    readonly HashSet<string> _flags = new HashSet<string>();

    public event Action<string> FlagAdded;

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
            Instance.FlagAdded?.Invoke(tag);
    }
}
