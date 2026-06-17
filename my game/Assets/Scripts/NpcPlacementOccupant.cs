using UnityEngine;

/// <summary>运行时挂在已放置的 NPC 上，记录其占用的摆放区域。</summary>
[DisallowMultipleComponent]
public class NpcPlacementOccupant : MonoBehaviour
{
    NpcPlacementZone zone;

    public NpcPlacementZone Zone => zone;

    public void Bind(NpcPlacementZone placementZone)
    {
        zone = placementZone;
    }

    /// <summary>移除已放置 NPC：释放占位、减少人数，不返还巧克力。</summary>
    public static bool TryRemove(GameObject npc)
    {
        if (npc == null)
            return false;

        string npcName = npc.name;
        GameStatsUI.Instance?.UnregisterPlacedNpc();
        GameMessageFeed.Post($"Removed {npcName}", GameMessageCategory.Placement);
        Destroy(npc);
        NpcPlacementZone.RefreshPlacementModeVisuals(NpcPlacementController.IsActive);
        return true;
    }

    void OnDestroy()
    {
        zone?.ReleaseFrom(gameObject);
    }
}
