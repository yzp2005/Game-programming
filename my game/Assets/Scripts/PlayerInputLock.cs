using System;
using UnityEngine;

/// <summary>
/// 全局玩家输入锁。对话、过场等调用 SetLocked(true/false)，
/// 各玩法脚本在 Update 开头判断 IsLocked。
/// 无需挂在场景物体上；对话脚本直接调用静态方法即可。
/// </summary>
public static class PlayerInputLock
{
    public static bool IsLocked { get; private set; }

    /// <summary>锁定状态变化时触发，参数为当前是否锁定。</summary>
    public static event Action<bool> OnLockChanged;

    /// <param name="locked">true = 禁止移动/攻击/镜头操作等玩法输入。</param>
    /// <param name="dialogueCursor">锁定时是否显示鼠标（给对话 UI 点击用）。</param>
    public static void SetLocked(bool locked, bool dialogueCursor = true)
    {
        bool changed = IsLocked != locked;
        IsLocked = locked;

        if (locked)
        {
            if (dialogueCursor)
                ApplyDialogueCursor();
        }
        else
        {
            ApplyGameplayCursor();
        }

        if (changed)
            OnLockChanged?.Invoke(IsLocked);
    }

    public static void ApplyDialogueCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public static void ApplyGameplayCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
