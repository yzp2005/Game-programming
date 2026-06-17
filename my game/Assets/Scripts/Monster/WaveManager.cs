using System;
using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class SpawnerWaveEntry
    {
        public MonsterSpawner spawner;
        [Min(0)] public int count = 5;
        [Min(0f)] public float interval = 1f;
    }

    [System.Serializable]
    public class Wave
    {
        [Min(0f)] public float delayBeforeWave = 2f;
        public SpawnerWaveEntry[] spawners;
    }

    [SerializeField] Wave[] waves;
    [SerializeField] bool autoStartOnPlay;
    [SerializeField] bool loopWaves;
    [Min(0f)] [SerializeField] float delayBetweenWaves = 3f;
    [Tooltip("勾选：本段怪物全部刷出后，等它们都死亡才算结束；不勾选：刷完即结束")]
    [SerializeField] bool waitUntilAllSpawnedDead = true;

    Coroutine waveRoutine;
    int aliveSpawnedCount;

    public bool IsRunning => waveRoutine != null;

    /// <summary>非 Loop 模式下全部波次结束且（若启用）本段刷出的怪物均已死亡后触发；被 StopWaves 打断时不触发。</summary>
    public event Action WavesFinished;

    void Start()
    {
        if (autoStartOnPlay)
            BeginWaves();
    }

    public void BeginWaves()
    {
        StopWaves();
        waveRoutine = StartCoroutine(RunWaves());
    }

    public void StopWaves()
    {
        if (waveRoutine == null)
            return;

        StopCoroutine(waveRoutine);
        waveRoutine = null;
        aliveSpawnedCount = 0;
        GameMessageFeed.ClearBanner();
    }

    IEnumerator RunWaves()
    {
        if (waves == null || waves.Length == 0)
        {
            waveRoutine = null;
            yield break;
        }

        do
        {
            for (int w = 0; w < waves.Length; w++)
            {
                Wave wave = waves[w];

                if (wave.delayBeforeWave > 0f)
                    yield return RunWaveCountdown(w, wave.delayBeforeWave);

                GameMessageFeed.Post($"Wave {w + 1} started.", GameMessageCategory.Combat);

                if (wave.spawners != null)
                {
                    foreach (SpawnerWaveEntry entry in wave.spawners)
                    {
                        if (entry.spawner == null || entry.count <= 0)
                            continue;

                        for (int i = 0; i < entry.count; i++)
                        {
                            RegisterSpawn(entry.spawner?.SpawnOne());
                            if (i < entry.count - 1 && entry.interval > 0f)
                                yield return new WaitForSeconds(entry.interval);
                        }
                    }
                }

                if (waitUntilAllSpawnedDead)
                    yield return WaitUntilSpawnedDead();

                if (w < waves.Length - 1 && delayBetweenWaves > 0f)
                    yield return new WaitForSeconds(delayBetweenWaves);
            }
        } while (loopWaves);

        waveRoutine = null;
        aliveSpawnedCount = 0;
        WavesFinished?.Invoke();
    }

    void RegisterSpawn(GameObject monster)
    {
        if (!waitUntilAllSpawnedDead || monster == null)
            return;

        if (!monster.TryGetComponent(out MonsterHealth health) || health.IsDead)
            return;

        aliveSpawnedCount++;
        health.OnDeath += OnSpawnedMonsterDeath;
    }

    void OnSpawnedMonsterDeath()
    {
        aliveSpawnedCount = Mathf.Max(0, aliveSpawnedCount - 1);
    }

    IEnumerator WaitUntilSpawnedDead()
    {
        while (aliveSpawnedCount > 0)
            yield return null;
    }

    IEnumerator RunWaveCountdown(int waveIndex, float duration)
    {
        int waveNumber = waveIndex + 1;
        float remaining = duration;
        int lastShownSeconds = -1;

        while (remaining > 0f)
        {
            int seconds = Mathf.CeilToInt(remaining);
            if (seconds != lastShownSeconds)
            {
                string message = $"Wave {waveNumber} starts in {seconds}s";
                lastShownSeconds = seconds;

                if (GameMessageFeed.HasBanner)
                    GameMessageFeed.SetBanner(message);
                else
                    GameMessageFeed.Post(message, GameMessageCategory.Countdown);
            }

            yield return null;
            remaining -= Time.deltaTime;
        }

        GameMessageFeed.ClearBanner();
    }
}
