using UnityEngine;

/// <summary>
/// 监听 GameEventManager 标签：存在 fight_level{n}（n 为正整数）时循环播放 BGM；
/// 标签移除后停止。进场景时若标签已存在也会播放（读档、再次进入场景）。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class FlagFightLevelMusicPlayer : MonoBehaviour
{
    [SerializeField] AudioClip fightBgm;
    [SerializeField] AudioSource audioSource;
    [SerializeField] bool checkFlagOnStart = true;
    [SerializeField] bool stopWhenFightFlagRemoved = true;

    void Awake()
    {
        EnsureAudioSource();
        StopMusic();

        GameEventManager.FlagAdded += OnFlagAdded;
        GameEventManager.FlagRemoved += OnFlagRemoved;

        if (checkFlagOnStart && HasActiveFightLevel())
            PlayMusic();
    }

    void OnDestroy()
    {
        GameEventManager.FlagAdded -= OnFlagAdded;
        GameEventManager.FlagRemoved -= OnFlagRemoved;
    }

    void OnFlagAdded(string flag)
    {
        if (!TryParseFightLevelNumber(flag, out _))
            return;

        PlayMusic();
    }

    void OnFlagRemoved(string flag)
    {
        if (!stopWhenFightFlagRemoved || !TryParseFightLevelNumber(flag, out _))
            return;

        if (HasActiveFightLevel())
            PlayMusic();
        else
            StopMusic();
    }

    void EnsureAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    void PlayMusic()
    {
        if (fightBgm == null)
        {
            Debug.LogWarning($"{name}: 未配置 Fight Bgm。", this);
            StopMusic();
            return;
        }

        if (audioSource.clip == fightBgm && audioSource.isPlaying)
            return;

        audioSource.clip = fightBgm;
        audioSource.Play();
    }

    void StopMusic()
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
        audioSource.clip = null;
    }

    static bool HasActiveFightLevel()
    {
        if (GameEventManager.Instance == null)
            return false;

        foreach (string flag in GameEventManager.GetAllFlagsSorted())
        {
            if (TryParseFightLevelNumber(flag, out _))
                return true;
        }

        return false;
    }

    static bool TryParseFightLevelNumber(string flag, out int levelNumber) =>
        FightLevelInputGate.TryParseFightLevelNumber(flag, out levelNumber);
}
