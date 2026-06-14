using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 标记一块可摆放 NPC 的区域。挂 Collider（Is Trigger），每区同时只能占一个 NPC。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class NpcPlacementZone : MonoBehaviour
{
    static readonly List<NpcPlacementZone> registeredZones = new List<NpcPlacementZone>();

    public static IReadOnlyList<NpcPlacementZone> RegisteredZones => registeredZones;

    [SerializeField] string zoneId;
    [Tooltip("勾选后，左键放置时使用本物体 Transform 的位置与朝向")]
    [SerializeField] bool snapToZoneTransform;

    [Header("场景显示")]
    [Tooltip("摆放模式下才显示；留空则控制本物体及子物体上的 Renderer（Collider 始终保留）")]
    [SerializeField] GameObject sceneVisual;

    [Header("小地图（独立 Blip，不用 MinimapTrackable）")]
    [SerializeField] bool showMinimapBlip = true;
    [Tooltip("留空则用 MinimapController 上的 Available Zone Blip")]
    [SerializeField] GameObject availableBlipPrefabOverride;
    [Tooltip("留空则用 MinimapController 上的 Occupied Zone Blip")]
    [SerializeField] GameObject occupiedBlipPrefabOverride;

    Collider zoneCollider;
    GameObject occupiedNpc;
    Renderer[] sceneRenderers;

    public bool IsAvailable => occupiedNpc == null;
    public string ZoneId => zoneId;
    public bool ShowMinimapBlip => showMinimapBlip;
    public Vector3 MinimapWorldPosition => transform.position;

    public GameObject ResolveAvailableBlipPrefab(GameObject fallback) =>
        availableBlipPrefabOverride != null ? availableBlipPrefabOverride : fallback;

    public GameObject ResolveOccupiedBlipPrefab(GameObject fallback) =>
        occupiedBlipPrefabOverride != null ? occupiedBlipPrefabOverride : fallback;

    void OnEnable()
    {
        if (!registeredZones.Contains(this))
            registeredZones.Add(this);
    }

    void OnDisable()
    {
        registeredZones.Remove(this);
    }

    void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
        CacheSceneVisuals();
        SetSceneVisualVisible(false);
    }

    void Start()
    {
        SetSceneVisualVisible(false);
    }

    void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void Update()
    {
        if (occupiedNpc == null)
            return;

        if (!occupiedNpc)
            occupiedNpc = null;
    }

    public bool ContainsPoint(Vector3 worldPoint)
    {
        if (zoneCollider == null)
            return false;

        Vector3 closest = zoneCollider.ClosestPoint(worldPoint);
        return (closest - worldPoint).sqrMagnitude <= 0.0001f;
    }

    public bool TryGetPlacementPose(Vector3 fallbackPosition, Quaternion fallbackRotation, out Vector3 position, out Quaternion rotation)
    {
        if (snapToZoneTransform)
        {
            position = transform.position;
            rotation = transform.rotation;
            return true;
        }

        position = fallbackPosition;
        rotation = fallbackRotation;
        return true;
    }

    public bool TryOccupy(GameObject npc)
    {
        if (npc == null || !IsAvailable)
            return false;

        occupiedNpc = npc;
        NpcPlacementOccupant occupant = npc.GetComponent<NpcPlacementOccupant>();
        if (occupant == null)
            occupant = npc.AddComponent<NpcPlacementOccupant>();

        occupant.Bind(this);
        SetSceneVisualVisible(false);
        return true;
    }

    internal void ReleaseFrom(GameObject npc)
    {
        if (occupiedNpc != npc)
            return;

        occupiedNpc = null;

        if (NpcPlacementController.IsActive && IsAvailable)
            SetSceneVisualVisible(true);
    }

    public static void RefreshPlacementModeVisuals(bool placementModeActive)
    {
        for (int i = 0; i < registeredZones.Count; i++)
        {
            NpcPlacementZone zone = registeredZones[i];
            if (zone == null)
                continue;

            zone.SetSceneVisualVisible(placementModeActive && zone.IsAvailable);
        }
    }

    void CacheSceneVisuals()
    {
        if (sceneVisual != null)
            return;

        sceneRenderers = GetComponentsInChildren<Renderer>(true);
    }

    public void SetSceneVisualVisible(bool visible)
    {
        if (sceneVisual != null)
        {
            sceneVisual.SetActive(visible);
            return;
        }

        if (sceneRenderers == null || sceneRenderers.Length == 0)
            CacheSceneVisuals();

        if (sceneRenderers == null)
            return;

        for (int i = 0; i < sceneRenderers.Length; i++)
        {
            if (sceneRenderers[i] != null)
                sceneRenderers[i].enabled = visible;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Collider col = zoneCollider != null ? zoneCollider : GetComponent<Collider>();
        if (col == null)
            return;

        Gizmos.color = IsAvailable ? new Color(0.2f, 0.9f, 0.3f, 0.35f) : new Color(0.9f, 0.3f, 0.2f, 0.35f);
        Gizmos.matrix = col.transform.localToWorldMatrix;

        if (col is BoxCollider box)
            Gizmos.DrawCube(box.center, box.size);
        else if (col is SphereCollider sphere)
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        else if (col is CapsuleCollider capsule)
            Gizmos.DrawWireSphere(capsule.center, capsule.radius);
    }
#endif
}
