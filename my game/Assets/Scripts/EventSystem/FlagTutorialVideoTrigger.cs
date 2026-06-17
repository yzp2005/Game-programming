using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 跨场景保留：监听 GameEventManager 标签，按 Entry 配置打开本场景的 TutorialVideoPanel 并播放对应视频。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class FlagTutorialVideoTrigger : MonoBehaviour
{
    public static FlagTutorialVideoTrigger Instance { get; private set; }

    [Serializable]
    public class Entry
    {
        [Tooltip("Set 此 flag 时弹出视频")]
        public string whenFlag;

        public TutorialVideoPanel.VideoStep[] steps;

        public bool onlyOnce = true;

        [Header("关闭后")]
        public string[] flagsToAddOnClose;
        public string[] flagsToRemoveOnClose;
    }

    [SerializeField] Entry[] entries;

    [Header("进场景")]
    [Tooltip("加载场景后，若已有 Entry 对应的 flag，也尝试弹出")]
    [SerializeField] bool checkFlagOnStart = true;

    readonly HashSet<string> triggeredFlags = new HashSet<string>();

    TutorialVideoPanel boundPanel;
    Entry activeEntry;
    bool inputLockedByPanel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);

        GameEventManager.FlagAdded += OnFlagAdded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        BindScenePanel();

        if (checkFlagOnStart)
            CheckExistingFlags();
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        GameEventManager.FlagAdded -= OnFlagAdded;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindPanel();
        ReleaseInputLock();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (inputLockedByPanel)
        {
            ReleaseInputLock();
            activeEntry = null;
        }

        BindScenePanel();

        if (checkFlagOnStart)
            CheckExistingFlags();
    }

    void OnFlagAdded(string flag)
    {
        if (entries == null || string.IsNullOrEmpty(flag))
            return;

        for (int i = 0; i < entries.Length; i++)
            TryOpen(entries[i], flag);
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
                TryOpen(entry, flag);
        }
    }

    void TryOpen(Entry entry, string flag)
    {
        if (entry == null || !FlagMatches(entry.whenFlag, flag))
            return;

        string key = entry.whenFlag.Trim();
        if (entry.onlyOnce && triggeredFlags.Contains(key))
            return;

        TutorialVideoPanel panel = ResolvePanel();
        if (panel == null)
        {
            Debug.LogWarning($"{name}: 当前场景未找到 TutorialVideoPanel（flag: {flag}）。", this);
            return;
        }

        if (panel.IsOpen)
            return;

        if ((entry.steps == null || entry.steps.Length == 0) && !PanelHasDefaultSteps(panel))
        {
            Debug.LogWarning($"{name}: flag「{flag}」未配置视频步骤。", this);
            return;
        }

        triggeredFlags.Add(key);
        activeEntry = entry;
        BindPanel(panel);
        panel.OpenPanel(entry.steps);
        PlayerInputLock.SetLocked(true, dialogueCursor: true);
        inputLockedByPanel = true;
    }

    void OnPanelClosed()
    {
        ReleaseInputLock();

        if (activeEntry != null)
            FlagEventActions.Apply(activeEntry.flagsToAddOnClose, activeEntry.flagsToRemoveOnClose);

        activeEntry = null;
    }

    void BindScenePanel()
    {
        UnbindPanel();
        TutorialVideoPanel panel = ResolvePanel();
        if (panel == null)
            return;

        BindPanel(panel);
        panel.EnsureHidden();
    }

    void BindPanel(TutorialVideoPanel panel)
    {
        if (panel == null)
            return;

        if (boundPanel == panel)
            return;

        UnbindPanel();
        boundPanel = panel;
        boundPanel.PanelClosed += OnPanelClosed;
    }

    void UnbindPanel()
    {
        if (boundPanel == null)
            return;

        boundPanel.PanelClosed -= OnPanelClosed;
        boundPanel = null;
    }

    TutorialVideoPanel ResolvePanel()
    {
        if (boundPanel != null)
            return boundPanel;

        return FindObjectOfType<TutorialVideoPanel>(true);
    }

    void ReleaseInputLock()
    {
        if (!inputLockedByPanel)
            return;

        PlayerInputLock.SetLocked(false);
        inputLockedByPanel = false;
    }

    static bool PanelHasDefaultSteps(TutorialVideoPanel panel)
    {
        return panel != null && panel.HasConfiguredSteps;
    }

    static bool FlagMatches(string expected, string actual)
    {
        return !string.IsNullOrWhiteSpace(expected)
            && string.Equals(expected.Trim(), actual, StringComparison.Ordinal);
    }
}
