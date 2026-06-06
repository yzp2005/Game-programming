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
    [SerializeField] bool autoStartOnPlay = true;
    [SerializeField] bool loopWaves;
    [Min(0f)] [SerializeField] float delayBetweenWaves = 3f;

    void Start()
    {
        if (autoStartOnPlay)
            StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        if (waves == null || waves.Length == 0)
            yield break;

        do
        {
            for (int w = 0; w < waves.Length; w++)
            {
                Wave wave = waves[w];

                if (wave.delayBeforeWave > 0f)
                    yield return new WaitForSeconds(wave.delayBeforeWave);

                if (wave.spawners != null)
                {
                    foreach (SpawnerWaveEntry entry in wave.spawners)
                    {
                        if (entry.spawner == null || entry.count <= 0)
                            continue;

                        for (int i = 0; i < entry.count; i++)
                        {
                            entry.spawner.SpawnOne();
                            if (i < entry.count - 1 && entry.interval > 0f)
                                yield return new WaitForSeconds(entry.interval);
                        }
                    }
                }

                if (w < waves.Length - 1 && delayBetweenWaves > 0f)
                    yield return new WaitForSeconds(delayBetweenWaves);
            }
        } while (loopWaves);
    }
}
