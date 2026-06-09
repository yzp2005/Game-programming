using System;
using UnityEngine;

/// <summary>
/// 按展示编号绑定：展示模型 ↔ 放置 NPC ↔ 摆放预览 Renderer。
/// </summary>
[DisallowMultipleComponent]
public class NpcCharacterRegistry : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public string displayName;
        [Min(0)] public int chocolateCost;
        public GameObject displayPrefab;
        public GameObject placementPrefab;
        [Tooltip("从 displayPrefab 资产拖 Renderer")]
        public Renderer[] previewRenderers;
    }

    [SerializeField] Slot[] slots;

    public int SlotCount => slots != null ? slots.Length : 0;

    public GameObject GetDisplayPrefab(int index) => GetSlot(index)?.displayPrefab;

    public GameObject GetPlacementPrefab(int index)
    {
        Slot slot = GetSlot(index);
        if (slot == null)
            return null;

        return slot.placementPrefab != null ? slot.placementPrefab : slot.displayPrefab;
    }

    public Renderer[] GetPreviewRenderers(int index) => GetSlot(index)?.previewRenderers;

    public string GetDisplayName(int index)
    {
        Slot slot = GetSlot(index);
        if (slot == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(slot.displayName))
            return slot.displayName;

        return slot.displayPrefab != null ? slot.displayPrefab.name : $"Slot {index}";
    }

    public int GetChocolateCost(int index) => GetSlot(index)?.chocolateCost ?? 0;

    public bool CanPlace(int index) => GetSlot(index) != null && GetPlacementPrefab(index) != null;

    Slot GetSlot(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length)
            return null;

        return slots[index];
    }
}
