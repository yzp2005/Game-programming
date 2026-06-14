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

    void OnDestroy()
    {
        zone?.ReleaseFrom(gameObject);
    }
}
