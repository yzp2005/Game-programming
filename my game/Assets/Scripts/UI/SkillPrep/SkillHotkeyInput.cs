using System;
using UnityEngine;

/// <summary>
/// 战斗技能热键：数字键 1–4 对应 loadout 槽位。仅在战时 UI 显示且未全局锁输入时生效。
/// </summary>
[DisallowMultipleComponent]
public class SkillHotkeyInput : MonoBehaviour
{
    [SerializeField] SkillDatabase database;

    public event Action<int, SkillDefinition> SlotPressed;

    void Update()
    {
        if (!FightLevelInputGate.TryGetSkillSlotKeyDown(out int slotIndex))
            return;

        SkillDefinition skill = SkillLoadout.GetDefinition(database, slotIndex);
        if (skill == null)
            return;

        SlotPressed?.Invoke(slotIndex, skill);
    }
}
