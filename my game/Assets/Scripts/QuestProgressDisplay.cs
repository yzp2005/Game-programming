using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局任务展示：在此配置所有 flag → 任务文案；flag 增删或切场景时自动更新当前场景的 QuestDisplay。
/// 与 GameEventManager 同场景放一个即可（DontDestroyOnLoad）。
/// </summary>
[DisallowMultipleComponent]
public class QuestProgressDisplay : MonoBehaviour
{
    public static QuestProgressDisplay Instance { get; private set; }

    const string ResourcesPrefabPath = "QuestProgressDisplay";

    [Serializable]
    public class QuestEntry
    {
        [Tooltip("需全部存在才匹配")]
        public string[] requiredFlags;

        public string questName;

        [TextArea(2, 5)]
        public string questContent;

        [Tooltip("多条同时匹配时，数值越大越优先")]
        public int priority;

        [Tooltip("留空=任意场景；填 Build Settings 里的场景名则仅在该场景参与匹配")]
        public string sceneNameFilter;
    }

    [Header("任务表（全项目统一在此维护）")]
    [SerializeField] QuestEntry[] entries;

    [Header("无匹配时")]
    [SerializeField] bool hideWhenNoMatch = true;
    [SerializeField] QuestEntry fallbackEntry;

    [Header("刷新")]
    [Tooltip("切场景或 flag 变化后延迟刷新（秒），可等 QuestDisplay 与加载淡入完成")]
    [SerializeField] float refreshDelay;

    Coroutine refreshRoutine;
    QuestEntry lastAppliedEntry;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapEnsureInstance()
    {
        EnsureInstance();
    }

    /// <summary>保证 DDOL 单例存在（主菜单继续游戏时不会经过 Suntail Village 场景）。</summary>
    public static void EnsureInstance()
    {
        if (Instance != null)
            return;

        if (FindObjectOfType<QuestProgressDisplay>() != null)
            return;

        GameObject prefab = Resources.Load<GameObject>(ResourcesPrefabPath);
        if (prefab == null)
            return;

        Instantiate(prefab);
    }

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

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        GameEventManager.FlagAdded += OnFlagChanged;
        GameEventManager.FlagRemoved += OnFlagChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        ScheduleRefresh();
    }

    void OnDisable()
    {
        GameEventManager.FlagAdded -= OnFlagChanged;
        GameEventManager.FlagRemoved -= OnFlagChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        lastAppliedEntry = null;
        ScheduleRefresh();
    }

    void OnFlagChanged(string flag)
    {
        if (string.IsNullOrEmpty(flag))
            return;

        ScheduleRefresh();
    }

    public void RefreshNow()
    {
        ApplyEntry(ResolveBestEntry());
    }

    public void ScheduleRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);

        if (refreshDelay > 0f)
            refreshRoutine = StartCoroutine(RefreshAfterDelay(refreshDelay));
        else
            RefreshNow();
    }

    IEnumerator RefreshAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        refreshRoutine = null;
        RefreshNow();
    }

    QuestEntry ResolveBestEntry()
    {
        QuestEntry best = null;
        string activeScene = SceneManager.GetActiveScene().name;

        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                QuestEntry entry = entries[i];
                if (!EntryMatches(entry, activeScene))
                    continue;

                if (best == null || entry.priority > best.priority)
                    best = entry;
            }
        }

        if (best != null)
            return best;

        if (!hideWhenNoMatch && fallbackEntry != null && HasQuestText(fallbackEntry))
            return fallbackEntry;

        return null;
    }

    void ApplyEntry(QuestEntry entry)
    {
        if (entry != null && entry == lastAppliedEntry)
            return;

        lastAppliedEntry = entry;

        QuestDisplay display = FindObjectOfType<QuestDisplay>(true);
        if (display == null)
            return;

        if (entry == null)
        {
            display.SetCurrentQuest("", "");
            return;
        }

        display.SetCurrentQuest(entry.questName, entry.questContent);
    }

    static bool EntryMatches(QuestEntry entry, string activeSceneName)
    {
        if (entry == null || entry.requiredFlags == null || entry.requiredFlags.Length == 0)
            return false;

        if (!string.IsNullOrWhiteSpace(entry.sceneNameFilter)
            && entry.sceneNameFilter != activeSceneName)
            return false;

        bool hasValidFlag = false;
        foreach (string flag in entry.requiredFlags)
        {
            if (string.IsNullOrWhiteSpace(flag))
                continue;

            hasValidFlag = true;
            if (!GameEventManager.Has(flag.Trim()))
                return false;
        }

        return hasValidFlag;
    }

    static bool HasQuestText(QuestEntry entry)
    {
        return !string.IsNullOrWhiteSpace(entry.questName)
            || !string.IsNullOrWhiteSpace(entry.questContent);
    }
}
