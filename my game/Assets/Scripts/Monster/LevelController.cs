using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 监听 GameEventManager.FlagAdded 开波；WaveManager 刷怪结束后增删 Flag。
/// </summary>
[DisallowMultipleComponent]
public class LevelController : MonoBehaviour
{
    [Serializable]
    public class WaveEntry
    {
        [Tooltip("首次出现此 flag 时开启本波（GameEventManager.Set）")]
        public string startWhenFlag;

        [Tooltip("同一波是否只触发一次")]
        public bool startOnlyOnce = true;

        [Tooltip("挂 WaveManager 的根物体")]
        public GameObject waveEventRoot;

        [Header("波次结束后 Flag")]
        [Tooltip("刷怪流程正常结束后 GameEventManager.Set")]
        public string[] flagsToAddOnWaveFinish;

        [Tooltip("刷怪流程正常结束后 GameEventManager.Remove")]
        public string[] flagsToRemoveOnWaveFinish;
    }

    [SerializeField] WaveEntry[] waves;

    int activeWaveIndex = -1;
    WaveManager activeWaveManager;
    readonly HashSet<int> startedWaveIndices = new HashSet<int>();

    void Awake()
    {
        DeactivateAllWaveRoots();
    }

    void OnEnable()
    {
        GameEventManager.FlagAdded += OnGameFlagAdded;
    }

    void OnDisable()
    {
        GameEventManager.FlagAdded -= OnGameFlagAdded;
        UnsubscribeActiveWaveFinished();
    }

    void OnGameFlagAdded(string flag)
    {
        if (waves == null || string.IsNullOrEmpty(flag))
            return;

        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i].startWhenFlag != flag)
                continue;

            if (waves[i].startOnlyOnce && startedWaveIndices.Contains(i))
                continue;

            StartWave(i);
        }
    }

    void StartWave(int waveIndex)
    {
        if (waves == null || waveIndex < 0 || waveIndex >= waves.Length)
            return;

        WaveEntry entry = waves[waveIndex];
        if (entry.waveEventRoot == null)
        {
            Debug.LogWarning($"{name}: 波次 {waveIndex} 未指定 Wave Event Root。", this);
            return;
        }

        if (activeWaveIndex == waveIndex && entry.waveEventRoot.activeSelf)
            return;

        StopActiveWave();

        if (!entry.waveEventRoot.TryGetComponent(out WaveManager waveManager))
        {
            Debug.LogWarning($"{name}: 波次 {waveIndex} 的 Wave Event Root 上缺少 WaveManager。", this);
            return;
        }

        entry.waveEventRoot.SetActive(true);
        activeWaveIndex = waveIndex;
        activeWaveManager = waveManager;
        startedWaveIndices.Add(waveIndex);

        activeWaveManager.WavesFinished += OnActiveWaveFinished;
        activeWaveManager.BeginWaves();
    }

    void OnActiveWaveFinished()
    {
        if (activeWaveIndex < 0 || waves == null || activeWaveIndex >= waves.Length)
            return;

        ApplyWaveFinishFlags(waves[activeWaveIndex]);
        StopActiveWave();
    }

    void ApplyWaveFinishFlags(WaveEntry entry)
    {
        if (entry.flagsToAddOnWaveFinish != null)
        {
            foreach (string flag in entry.flagsToAddOnWaveFinish)
            {
                if (!string.IsNullOrWhiteSpace(flag))
                    GameEventManager.Set(flag.Trim());
            }
        }

        if (entry.flagsToRemoveOnWaveFinish != null)
        {
            foreach (string flag in entry.flagsToRemoveOnWaveFinish)
            {
                if (!string.IsNullOrWhiteSpace(flag))
                    GameEventManager.Remove(flag.Trim());
            }
        }
    }

    void StopActiveWave()
    {
        UnsubscribeActiveWaveFinished();

        if (activeWaveIndex < 0 || waves == null || activeWaveIndex >= waves.Length)
        {
            activeWaveIndex = -1;
            activeWaveManager = null;
            return;
        }

        WaveEntry entry = waves[activeWaveIndex];
        if (entry.waveEventRoot != null)
        {
            if (entry.waveEventRoot.TryGetComponent(out WaveManager waveManager))
                waveManager.StopWaves();

            entry.waveEventRoot.SetActive(false);
        }

        activeWaveIndex = -1;
        activeWaveManager = null;
    }

    void UnsubscribeActiveWaveFinished()
    {
        if (activeWaveManager == null)
            return;

        activeWaveManager.WavesFinished -= OnActiveWaveFinished;
    }

    void DeactivateAllWaveRoots()
    {
        if (waves == null)
            return;

        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i].waveEventRoot == null)
                continue;

            if (waves[i].waveEventRoot.TryGetComponent(out WaveManager waveManager))
                waveManager.StopWaves();

            waves[i].waveEventRoot.SetActive(false);
        }

        activeWaveIndex = -1;
        activeWaveManager = null;
    }
}
