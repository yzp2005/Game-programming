using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 监听 GameEventManager 标签，出现时打开备战面板。
/// 面板显示期间锁定玩法输入，但保留鼠标用于 UI 点击。
/// </summary>
[DisallowMultipleComponent]
public class FlagSkillPrepTrigger : MonoBehaviour
{
    [Header("触发")]
    [Tooltip("GameEventManager.Set 此 flag 时弹出备战面板")]
    [SerializeField] string whenFlag;
    [SerializeField] bool onlyOnce = true;
    [Tooltip("进场景时若已有该 flag，也弹出（例如跨场景保留 flag）")]
    [SerializeField] bool checkFlagOnStart;

    [Header("面板")]
    [SerializeField] SkillPrepPanel skillPrepPanel;

    [Header("关闭后")]
    [SerializeField] string[] flagsToAddOnClose;
    [SerializeField] string[] flagsToRemoveOnClose;

    readonly HashSet<string> triggeredFlags = new HashSet<string>();
    bool inputLockedByPanel;

    void Awake()
    {
        GameEventManager.FlagAdded += OnFlagAdded;

        if (skillPrepPanel != null)
        {
            skillPrepPanel.PanelClosed += OnPanelClosed;
            skillPrepPanel.EnsureHidden();
        }
    }

    void Start()
    {
        if (!checkFlagOnStart || string.IsNullOrWhiteSpace(whenFlag))
            return;

        string flag = whenFlag.Trim();
        if (GameEventManager.Has(flag))
            TryOpen(flag);
    }

    void OnDestroy()
    {
        GameEventManager.FlagAdded -= OnFlagAdded;

        if (skillPrepPanel != null)
            skillPrepPanel.PanelClosed -= OnPanelClosed;

        ReleaseInputLock();
    }

    void OnFlagAdded(string flag) => TryOpen(flag);

    void TryOpen(string flag)
    {
        if (!FlagMatches(whenFlag, flag))
            return;

        if (onlyOnce && triggeredFlags.Contains(flag))
            return;

        if (skillPrepPanel == null)
        {
            Debug.LogWarning($"{name}: 未指定 SkillPrepPanel。", this);
            return;
        }

        triggeredFlags.Add(flag);
        skillPrepPanel.OpenPanel();
        PlayerInputLock.SetLocked(true, dialogueCursor: true);
        inputLockedByPanel = true;
    }

    void OnPanelClosed()
    {
        ReleaseInputLock();
        FlagEventActions.Apply(flagsToAddOnClose, flagsToRemoveOnClose);
    }

    void ReleaseInputLock()
    {
        if (!inputLockedByPanel)
            return;

        PlayerInputLock.SetLocked(false);
        inputLockedByPanel = false;
    }

    static bool FlagMatches(string expected, string actual)
    {
        return !string.IsNullOrWhiteSpace(expected)
            && string.Equals(expected.Trim(), actual, StringComparison.Ordinal);
    }
}
