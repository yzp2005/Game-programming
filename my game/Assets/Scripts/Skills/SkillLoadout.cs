using System;
using UnityEngine;

/// <summary>备战阶段选中的技能 id，按槽位顺序存放；空槽为 null。</summary>
public static class SkillLoadout
{
    static string[] slots = Array.Empty<string>();

    public static event Action Changed;

    public static int SlotCount => slots.Length;

    public static string[] SelectedSkillIds
    {
        get
        {
            if (slots.Length == 0)
                return Array.Empty<string>();

            string[] copy = new string[slots.Length];
            Array.Copy(slots, copy, slots.Length);
            return copy;
        }
    }

    public static bool IsFull
    {
        get
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (string.IsNullOrEmpty(slots[i]))
                    return false;
            }

            return slots.Length > 0;
        }
    }

    public static int FilledSlotCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!string.IsNullOrEmpty(slots[i]))
                    count++;
            }

            return count;
        }
    }

    public static void Configure(int slotCount)
    {
        slotCount = Mathf.Max(1, slotCount);

        if (slots.Length == slotCount)
        {
            Clear();
            return;
        }

        slots = new string[slotCount];
        Changed?.Invoke();
    }

    public static string GetSlot(int index)
    {
        if (index < 0 || index >= slots.Length)
            return null;

        return slots[index];
    }

    public static SkillDefinition GetDefinition(SkillDatabase database, int index)
    {
        string skillId = GetSlot(index);
        return database != null ? database.GetById(skillId) : null;
    }

    public static SkillDefinition[] ResolveAll(SkillDatabase database)
    {
        SkillDefinition[] result = new SkillDefinition[slots.Length];
        if (database == null)
            return result;

        for (int i = 0; i < slots.Length; i++)
            result[i] = database.GetById(slots[i]);

        return result;
    }

    public static string ToDebugString()
    {
        if (slots.Length == 0)
            return "[] (未 Configure)";

        var parts = new string[slots.Length];
        for (int i = 0; i < slots.Length; i++)
            parts[i] = slots[i] ?? "null";

        return $"[{string.Join(", ", parts)}]";
    }

    public static bool Contains(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == skillId)
                return true;
        }

        return false;
    }

    public static bool TryAdd(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId) || Contains(skillId))
            return false;

        skillId = skillId.Trim();

        for (int i = 0; i < slots.Length; i++)
        {
            if (!string.IsNullOrEmpty(slots[i]))
                continue;

            slots[i] = skillId;
            Changed?.Invoke();
            return true;
        }

        return false;
    }

    public static void Clear()
    {
        if (slots.Length == 0)
            return;

        bool hadEntry = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!string.IsNullOrEmpty(slots[i]))
                hadEntry = true;

            slots[i] = null;
        }

        if (hadEntry)
            Changed?.Invoke();
    }
}
