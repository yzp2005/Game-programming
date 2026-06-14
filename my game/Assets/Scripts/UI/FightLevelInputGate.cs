using System;
using UnityEngine;

/// <summary>
/// 战时输入门控：存在 fight_level{n} 标签时允许 E、Q、数字键 1–4，否则失效（仍受 PlayerInputLock 约束）。
/// 开场跳过等场景专用脚本请继续直接用 Input，不要经过此门控。
/// </summary>
public static class FightLevelInputGate
{
    static bool subscribed;

    public static bool IsInFightLevel { get; private set; }

    public static bool CanUseFightLevelInputs =>
        IsInFightLevel && !PlayerInputLock.IsLocked;

    /// <summary>与 CanUseFightLevelInputs 相同，兼容旧调用。</summary>
    public static bool CanUseHotkeys => CanUseFightLevelInputs;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        subscribed = false;
        IsInFightLevel = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        EnsureSubscribed();
        RefreshFromFlags();
    }

    public static void EnsureSubscribed()
    {
        if (subscribed)
            return;

        subscribed = true;
        GameEventManager.FlagAdded += OnFlagsChanged;
        GameEventManager.FlagRemoved += OnFlagsChanged;
    }

    static void OnFlagsChanged(string _) => RefreshFromFlags();

    public static void RefreshFromFlags()
    {
        IsInFightLevel = false;

        if (GameEventManager.Instance == null)
            return;

        foreach (string flag in GameEventManager.GetAllFlagsSorted())
        {
            if (TryParseFightLevelNumber(flag, out _))
            {
                IsInFightLevel = true;
                return;
            }
        }
    }

    public static bool IsManagedHotkey(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.E:
            case KeyCode.Q:
            case KeyCode.Alpha1:
            case KeyCode.Alpha2:
            case KeyCode.Alpha3:
            case KeyCode.Alpha4:
                return true;
            default:
                return false;
        }
    }

    public static bool ShouldSuppress(KeyCode key)
    {
        return IsManagedHotkey(key) && !CanUseFightLevelInputs;
    }

    public static bool GetKeyDown(KeyCode key)
    {
        if (ShouldSuppress(key))
            return false;

        return Input.GetKeyDown(key);
    }

    public static bool TryGetSkillSlotKeyDown(out int slotIndex)
    {
        slotIndex = -1;

        if (!CanUseFightLevelInputs)
            return false;

        if (Input.GetKeyDown(KeyCode.Alpha1)) { slotIndex = 0; return true; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { slotIndex = 1; return true; }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { slotIndex = 2; return true; }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { slotIndex = 3; return true; }

        return false;
    }

    public static bool TryParseFightLevelNumber(string flag, out int levelNumber)
    {
        levelNumber = 0;

        if (string.IsNullOrEmpty(flag) || !flag.StartsWith("fight_level", StringComparison.Ordinal))
            return false;

        string suffix = flag.Substring("fight_level".Length);
        return suffix.Length > 0
            && int.TryParse(suffix, out levelNumber)
            && levelNumber > 0;
    }
}
