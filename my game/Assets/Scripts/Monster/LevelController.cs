using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单 Scene 多关卡：激活 WaveEvent + WaveManager。
/// 与 GameEventManager / SetGameFlag 联动：每关可配置 startWhenFlag，对话 SetFlag 后自动开波。
/// </summary>
[DisallowMultipleComponent]
public class LevelController : MonoBehaviour
{
    [Serializable]
    public class LevelEntry
    {
        [Tooltip("可选 Id，供 StartLevel(string) / UnityEvent 使用")]
        public string levelId;
        [Tooltip("该关 WaveEvent 根物体（挂 WaveManager）")]
        public GameObject waveEventRoot;

        [Header("由 Flag 触发（SetGameFlag → GameEventManager）")]
        [Tooltip("出现此 flag 时开启本关；留空则仅代码/UnityEvent 可开")]
        public string startWhenFlag;
        [Tooltip("由 flag 触发时是否只开一次")]
        public bool startWhenFlagOnlyOnce = true;

        [Header("关卡发出 Flag（可选）")]
        [Tooltip("本关开始时 Set 的 flag")]
        public string gameFlagOnStart;
        [Tooltip("本关停止/切换时 Set 的 flag")]
        public string gameFlagOnStop;
    }

    [SerializeField] LevelEntry[] levels;
    [SerializeField] int startLevelIndex;
    [Tooltip("Play 后自动开 Start Level Index；等对话/flag 触发时请取消")]
    [SerializeField] bool autoStartOnPlay;
    [SerializeField] bool beginWavesOnActivate = true;
    [Tooltip("启用时若 startWhenFlag 已存在，立刻尝试开波（读档/晚挂载时用）")]
    [SerializeField] bool checkExistingFlagsOnEnable = true;

    int currentLevelIndex = -1;
    readonly HashSet<int> startedLevelIndices = new HashSet<int>();

    public int CurrentLevelIndex => currentLevelIndex;
    public bool HasActiveLevel => currentLevelIndex >= 0 && levels != null && currentLevelIndex < levels.Length;
    public event Action<int> OnLevelStarted;
    public event Action<int> OnLevelStopped;

    void Awake()
    {
        DeactivateAllLevels();
    }

    void OnEnable()
    {
        GameEventManager.FlagAdded += OnGameFlagAdded;

        if (checkExistingFlagsOnEnable)
            CheckExistingStartFlags();
    }

    void OnDisable()
    {
        GameEventManager.FlagAdded -= OnGameFlagAdded;
    }

    void Start()
    {
        if (autoStartOnPlay && levels != null && levels.Length > 0)
            StartLevel(startLevelIndex);
    }

    void OnGameFlagAdded(string flag)
    {
        if (levels == null || string.IsNullOrEmpty(flag))
            return;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].startWhenFlag == flag)
                StartLevelFromFlagConfig(i);
        }
    }

    void CheckExistingStartFlags()
    {
        if (levels == null)
            return;

        for (int i = 0; i < levels.Length; i++)
        {
            string flag = levels[i].startWhenFlag;
            if (!string.IsNullOrEmpty(flag) && GameEventManager.Has(flag))
                StartLevelFromFlagConfig(i);
        }
    }

    void StartLevelFromFlagConfig(int levelIndex)
    {
        if (levels[levelIndex].startWhenFlagOnlyOnce)
            TryStartLevelOnce(levelIndex);
        else
            StartLevel(levelIndex);
    }

    public void StartLevel(int levelIndex)
    {
        if (levels == null || levels.Length == 0)
        {
            Debug.LogWarning($"{name}: 未配置任何关卡。", this);
            return;
        }

        if (levelIndex < 0 || levelIndex >= levels.Length)
        {
            Debug.LogWarning($"{name}: 关卡索引 {levelIndex} 超出范围 (0~{levels.Length - 1})。", this);
            return;
        }

        if (currentLevelIndex == levelIndex && IsLevelRootActive(levelIndex))
            return;

        StopCurrentLevelInternal();

        LevelEntry entry = levels[levelIndex];
        if (entry.waveEventRoot == null)
        {
            Debug.LogWarning($"{name}: 关卡 {levelIndex} 的 WaveEvent 未指定。", this);
            return;
        }

        entry.waveEventRoot.SetActive(true);

        if (beginWavesOnActivate && entry.waveEventRoot.TryGetComponent(out WaveManager waveManager))
            waveManager.BeginWaves();

        currentLevelIndex = levelIndex;
        startedLevelIndices.Add(levelIndex);
        OnLevelStarted?.Invoke(currentLevelIndex);
        SetLevelFlag(entry.gameFlagOnStart);
    }

    public bool TryStartLevelOnce(int levelIndex)
    {
        if (HasLevelStarted(levelIndex))
            return false;

        StartLevel(levelIndex);
        return true;
    }

    public bool TryStartLevelOnce(string levelId)
    {
        if (string.IsNullOrEmpty(levelId) || levels == null)
            return false;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].levelId != levelId)
                continue;

            return TryStartLevelOnce(i);
        }

        Debug.LogWarning($"{name}: 找不到关卡 Id「{levelId}」。", this);
        return false;
    }

    public bool HasLevelStarted(int levelIndex) => startedLevelIndices.Contains(levelIndex);

    public bool HasLevelStarted(string levelId)
    {
        if (string.IsNullOrEmpty(levelId) || levels == null)
            return false;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].levelId == levelId)
                return HasLevelStarted(i);
        }

        return false;
    }

    public void ResetStartedLevelHistory() => startedLevelIndices.Clear();

    public bool StartLevel(string levelId)
    {
        if (string.IsNullOrEmpty(levelId) || levels == null)
            return false;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].levelId == levelId)
            {
                StartLevel(i);
                return true;
            }
        }

        Debug.LogWarning($"{name}: 找不到关卡 Id「{levelId}」。", this);
        return false;
    }

    public void StartNextLevel()
    {
        if (levels == null || levels.Length == 0)
            return;

        int next = currentLevelIndex + 1;
        if (next >= levels.Length)
        {
            Debug.Log($"{name}: 已是最后一关。", this);
            return;
        }

        StartLevel(next);
    }

    public void StopCurrentLevel()
    {
        StopCurrentLevelInternal();
        currentLevelIndex = -1;
    }

    public void RestartCurrentLevel()
    {
        if (!HasActiveLevel)
            return;

        StartLevel(currentLevelIndex);
    }

    public void DeactivateAllLevels()
    {
        if (levels == null)
            return;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].waveEventRoot == null)
                continue;

            if (levels[i].waveEventRoot.TryGetComponent(out WaveManager waveManager))
                waveManager.StopWaves();

            levels[i].waveEventRoot.SetActive(false);
        }

        currentLevelIndex = -1;
    }

    void StopCurrentLevelInternal()
    {
        if (!HasActiveLevel)
            return;

        LevelEntry entry = levels[currentLevelIndex];
        if (entry.waveEventRoot != null)
        {
            if (entry.waveEventRoot.TryGetComponent(out WaveManager waveManager))
                waveManager.StopWaves();

            entry.waveEventRoot.SetActive(false);
        }

        SetLevelFlag(entry.gameFlagOnStop);
        OnLevelStopped?.Invoke(currentLevelIndex);
    }

    static void SetLevelFlag(string flag)
    {
        if (!string.IsNullOrEmpty(flag))
            GameEventManager.Set(flag);
    }

    bool IsLevelRootActive(int levelIndex)
    {
        return levels[levelIndex].waveEventRoot != null && levels[levelIndex].waveEventRoot.activeSelf;
    }

    public GameObject GetWaveEventRoot(int levelIndex)
    {
        if (levels == null || levelIndex < 0 || levelIndex >= levels.Length)
            return null;

        return levels[levelIndex].waveEventRoot;
    }
}
