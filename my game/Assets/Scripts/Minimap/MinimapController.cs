using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按住按键/屏幕区域显示小地图，将世界 XZ 坐标映射到 UI 上。
/// </summary>
[DisallowMultipleComponent]
public class MinimapController : MonoBehaviour
{
    public enum MapBoundsMode
    {
        /// <summary>拖大地图 Plane，按 Unity 默认 10×10 平面 + Transform 自动算边界。</summary>
        MapTransform,
        /// <summary>直接填 Position / Scale / Rotation，不用摆四个角。</summary>
        ManualValues,
        /// <summary>拖四个角空物体（旧方式）。</summary>
        FourCorners
    }

    /// <summary>Unity 内置 Plane 网格边长为 10，半宽 5。</summary>
    const float UnityPlaneHalfExtent = 5f;

    [Header("地图边界")]
    [SerializeField] MapBoundsMode boundsMode = MapBoundsMode.MapTransform;
    [Tooltip("大地图 Plane 物体（推荐）")]
    [SerializeField] Transform mapTransform;
    [Tooltip("Bounds Mode = Manual Values 时使用")]
    [SerializeField] Vector3 manualPosition;
    [SerializeField] Vector3 manualScale = Vector3.one;
    [SerializeField] Vector3 manualEulerAngles;
    [Tooltip("Bounds Mode = Four Corners 时使用，顺序不限")]
    [SerializeField] Transform[] mapCorners = new Transform[4];

    [Header("追踪")]
    [SerializeField] MinimapWorldTracker worldTracker;
    [SerializeField] bool rotatePlayerBlip = true;

    [Header("UI")]
    [SerializeField] GameObject minimapRoot;
    [SerializeField] RectTransform blipContainer;
    [SerializeField] RectTransform playerBlip;
    [SerializeField] GameObject enemyBlipPrefab;
    [SerializeField] GameObject friendlyBlipPrefab;

    [Header("摆放区域 Blip（NpcPlacementZone 专用）")]
    [SerializeField] GameObject availableZoneBlipPrefab;
    [SerializeField] GameObject occupiedZoneBlipPrefab;
    [SerializeField] Color availableZoneFallbackColor = new Color(0.2f, 0.85f, 1f, 1f);
    [SerializeField] Color occupiedZoneFallbackColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("图标颜色（未指定 Prefab 颜色时使用）")]
    [SerializeField] Color enemyColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] Color objectiveColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] Color friendlyColor = new Color(0.3f, 0.9f, 0.4f, 1f);
    [SerializeField] Vector2 defaultBlipSize = new Vector2(10f, 10f);

    [Header("输入")]
    [SerializeField] KeyCode holdKey = KeyCode.M;
    [SerializeField] bool enableTouchHold = true;
    [Tooltip("按住屏幕右下角此比例区域时显示小地图（0~1）")]
    [SerializeField] Rect touchHoldScreenRect = new Rect(0.75f, 0f, 0.25f, 0.25f);

    float minX, maxX, minZ, maxZ;
    bool boundsReady;
    bool isVisible;

    readonly Dictionary<MinimapTrackable, RectTransform> blipByTrackable = new Dictionary<MinimapTrackable, RectTransform>();
    readonly Dictionary<NpcPlacementZone, RectTransform> blipByPlacementZone = new Dictionary<NpcPlacementZone, RectTransform>();
    readonly Dictionary<NpcPlacementZone, bool> placementZoneOccupiedState = new Dictionary<NpcPlacementZone, bool>();

    [ContextMenu("从 Map Transform 同步 Manual Values")]
    void SyncManualFromMapTransform()
    {
        if (mapTransform == null)
            return;

        manualPosition = mapTransform.position;
        manualScale = mapTransform.lossyScale;
        manualEulerAngles = mapTransform.eulerAngles;
    }

    void Awake()
    {
        if (worldTracker == null)
            worldTracker = MinimapWorldTracker.Instance;

        RebuildBounds();
        ForceHideMinimap();
    }

    void Start()
    {
        ForceHideMinimap();
    }

    void ForceHideMinimap()
    {
        isVisible = false;
        if (minimapRoot != null)
            minimapRoot.SetActive(false);
    }

    void Update()
    {
        if (PlayerInputLock.IsLocked)
        {
            SetVisible(false);
            return;
        }

        bool shouldShow = Input.GetKey(holdKey);
        if (!shouldShow && enableTouchHold)
            shouldShow = IsTouchHoldingCorner();

        SetVisible(shouldShow);

        if (!boundsReady)
            RebuildBounds();

        if (isVisible)
            UpdatePlayerBlip();

        SyncTrackableBlips();
        SyncPlacementZoneBlips();
    }

    bool IsTouchHoldingCorner()
    {
        if (Input.touchCount == 0)
            return false;

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            return false;

        float nx = touch.position.x / Screen.width;
        float ny = touch.position.y / Screen.height;
        return touchHoldScreenRect.Contains(new Vector2(nx, ny));
    }

    void SetVisible(bool visible)
    {
        if (isVisible == visible)
            return;

        isVisible = visible;
        if (minimapRoot != null)
            minimapRoot.SetActive(visible);

        if (visible)
        {
            MinimapWorldTracker tracker = ResolveTracker();
            tracker?.RefreshNow();
        }
    }

    void RebuildBounds()
    {
        boundsReady = false;
        minX = float.PositiveInfinity;
        maxX = float.NegativeInfinity;
        minZ = float.PositiveInfinity;
        maxZ = float.NegativeInfinity;

        switch (boundsMode)
        {
            case MapBoundsMode.MapTransform:
                if (mapTransform != null)
                    EncapsulatePlaneTransform(mapTransform.position, mapTransform.lossyScale, mapTransform.rotation);
                break;

            case MapBoundsMode.ManualValues:
                EncapsulatePlaneTransform(manualPosition, manualScale, Quaternion.Euler(manualEulerAngles));
                break;

            case MapBoundsMode.FourCorners:
                RebuildBoundsFromCorners();
                break;
        }

        boundsReady = maxX > minX && maxZ > minZ;
    }

    void RebuildBoundsFromCorners()
    {
        if (mapCorners == null)
            return;

        int valid = 0;
        foreach (Transform corner in mapCorners)
        {
            if (corner == null)
                continue;

            valid++;
            EncapsulateXZ(corner.position);
        }

        if (valid < 2)
        {
            minX = maxX = minZ = maxZ = 0f;
        }
    }

    void EncapsulatePlaneTransform(Vector3 center, Vector3 lossyScale, Quaternion rotation)
    {
        Vector3 halfRight = rotation * Vector3.right * (lossyScale.x * UnityPlaneHalfExtent);
        Vector3 halfForward = rotation * Vector3.forward * (lossyScale.z * UnityPlaneHalfExtent);

        EncapsulateXZ(center - halfRight - halfForward);
        EncapsulateXZ(center + halfRight - halfForward);
        EncapsulateXZ(center + halfRight + halfForward);
        EncapsulateXZ(center - halfRight + halfForward);
    }

    void EncapsulateXZ(Vector3 world)
    {
        if (world.x < minX) minX = world.x;
        if (world.x > maxX) maxX = world.x;
        if (world.z < minZ) minZ = world.z;
        if (world.z > maxZ) maxZ = world.z;
    }

    Vector2 WorldToBlipLocal(Vector3 world)
    {
        float width = blipContainer != null ? blipContainer.rect.width : 200f;
        float height = blipContainer != null ? blipContainer.rect.height : 200f;

        float u = Mathf.InverseLerp(minX, maxX, world.x);
        float v = Mathf.InverseLerp(minZ, maxZ, world.z);
        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        return new Vector2((u - 0.5f) * width, (v - 0.5f) * height);
    }

    void UpdatePlayerBlip()
    {
        if (playerBlip == null || blipContainer == null)
            return;

        MinimapWorldTracker tracker = ResolveTracker();
        if (tracker == null || !tracker.CurrentPlayer.IsValid)
            return;

        MinimapWorldTracker.PlayerSnapshot player = tracker.CurrentPlayer;
        playerBlip.anchoredPosition = WorldToBlipLocal(player.WorldPosition);

        if (rotatePlayerBlip)
            playerBlip.localRotation = Quaternion.Euler(0f, 0f, -player.Yaw);
    }

    void SyncTrackableBlips()
    {
        if (blipContainer == null)
            return;

        MinimapWorldTracker tracker = ResolveTracker();
        if (tracker == null)
            return;

        IReadOnlyList<MinimapWorldTracker.EntitySnapshot> entities = tracker.CurrentEntities;
        for (int i = 0; i < entities.Count; i++)
        {
            MinimapWorldTracker.EntitySnapshot snapshot = entities[i];
            if (!snapshot.IsValid || snapshot.Source == null)
                continue;

            MinimapTrackable trackable = snapshot.Source;
            if (!blipByTrackable.TryGetValue(trackable, out RectTransform blip))
            {
                if (!isVisible)
                    continue;

                blip = CreateBlip(snapshot.Kind);
                blipByTrackable[trackable] = blip;
            }

            if (isVisible)
                blip.anchoredPosition = WorldToBlipLocal(snapshot.WorldPosition);
        }

        var toRemove = new List<MinimapTrackable>();
        foreach (KeyValuePair<MinimapTrackable, RectTransform> pair in blipByTrackable)
        {
            MinimapTrackable trackable = pair.Key;
            if (trackable == null)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
                toRemove.Add(trackable);
                continue;
            }

            bool stillTracked = false;
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].Source == trackable)
                {
                    stillTracked = true;
                    break;
                }
            }

            if (!stillTracked)
                toRemove.Add(trackable);
        }

        foreach (MinimapTrackable trackable in toRemove)
            RemoveBlip(trackable);
    }

    void SyncPlacementZoneBlips()
    {
        if (blipContainer == null)
            return;

        IReadOnlyList<NpcPlacementZone> zones = NpcPlacementZone.RegisteredZones;
        for (int i = 0; i < zones.Count; i++)
        {
            NpcPlacementZone zone = zones[i];
            if (zone == null || !zone.ShowMinimapBlip)
                continue;

            bool occupied = !zone.IsAvailable;
            if (!blipByPlacementZone.TryGetValue(zone, out RectTransform blip)
                || !placementZoneOccupiedState.TryGetValue(zone, out bool knownOccupied)
                || knownOccupied != occupied)
            {
                RemovePlacementZoneBlip(zone);
                if (isVisible)
                {
                    blip = CreatePlacementZoneBlip(zone, occupied);
                    blipByPlacementZone[zone] = blip;
                    placementZoneOccupiedState[zone] = occupied;
                }
            }

            if (isVisible && blipByPlacementZone.TryGetValue(zone, out blip) && blip != null)
                blip.anchoredPosition = WorldToBlipLocal(zone.MinimapWorldPosition);
        }

        var toRemove = new List<NpcPlacementZone>();
        foreach (KeyValuePair<NpcPlacementZone, RectTransform> pair in blipByPlacementZone)
        {
            NpcPlacementZone zone = pair.Key;
            if (zone == null || !zone.isActiveAndEnabled || !zone.ShowMinimapBlip)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
                toRemove.Add(zone);
                continue;
            }

            bool stillRegistered = false;
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i] == zone)
                {
                    stillRegistered = true;
                    break;
                }
            }

            if (!stillRegistered)
                toRemove.Add(zone);
        }

        foreach (NpcPlacementZone zone in toRemove)
            RemovePlacementZoneBlip(zone);
    }

    void RemovePlacementZoneBlip(NpcPlacementZone zone)
    {
        if (zone != null && blipByPlacementZone.TryGetValue(zone, out RectTransform blip))
        {
            if (blip != null)
                Destroy(blip.gameObject);
            blipByPlacementZone.Remove(zone);
            placementZoneOccupiedState.Remove(zone);
        }
    }

    RectTransform CreatePlacementZoneBlip(NpcPlacementZone zone, bool occupied)
    {
        GameObject prefab = occupied
            ? zone.ResolveOccupiedBlipPrefab(occupiedZoneBlipPrefab)
            : zone.ResolveAvailableBlipPrefab(availableZoneBlipPrefab);

        Color fallbackColor = occupied ? occupiedZoneFallbackColor : availableZoneFallbackColor;
        string label = occupied ? "OccupiedZoneBlip" : "AvailableZoneBlip";

        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab, blipContainer);
        }
        else
        {
            go = new GameObject(label, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(blipContainer, false);
            Image image = go.GetComponent<Image>();
            image.color = fallbackColor;
            image.raycastTarget = false;
            EnsureBlipSprite(image);
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        if (rt.sizeDelta == Vector2.zero)
            rt.sizeDelta = defaultBlipSize;

        rt.SetAsLastSibling();
        return rt;
    }

    MinimapWorldTracker ResolveTracker()
    {
        if (worldTracker == null)
            worldTracker = MinimapWorldTracker.Instance;

        return worldTracker;
    }

    void RemoveBlip(MinimapTrackable trackable)
    {
        if (!blipByTrackable.TryGetValue(trackable, out RectTransform blip))
            return;

        if (blip != null)
            Destroy(blip.gameObject);

        blipByTrackable.Remove(trackable);
    }

    RectTransform CreateBlip(MinimapTrackable.BlipKind kind)
    {
        GameObject prefab = GetBlipPrefab(kind);
        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab, blipContainer);
        }
        else
        {
            go = new GameObject($"MinimapBlip_{kind}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(blipContainer, false);
            Image image = go.GetComponent<Image>();
            image.color = GetColor(kind);
            image.raycastTarget = false;
            EnsureBlipSprite(image);
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        if (rt.sizeDelta == Vector2.zero)
            rt.sizeDelta = defaultBlipSize;

        if (go.TryGetComponent(out Image prefabImage))
        {
            prefabImage.color = GetColor(kind);
            EnsureBlipSprite(prefabImage);
        }

        rt.SetAsLastSibling();
        return rt;
    }

    static void EnsureBlipSprite(Image image)
    {
        if (image.sprite != null)
            return;

        image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
    }

    Color GetColor(MinimapTrackable.BlipKind kind)
    {
        switch (kind)
        {
            case MinimapTrackable.BlipKind.Objective: return objectiveColor;
            case MinimapTrackable.BlipKind.Friendly: return friendlyColor;
            default: return enemyColor;
        }
    }

    GameObject GetBlipPrefab(MinimapTrackable.BlipKind kind)
    {
        switch (kind)
        {
            case MinimapTrackable.BlipKind.Friendly:
                return friendlyBlipPrefab != null ? friendlyBlipPrefab : enemyBlipPrefab;
            default:
                return enemyBlipPrefab;
        }
    }

    void OnDrawGizmosSelected()
    {
        RebuildBounds();
        if (!boundsReady)
            return;

        float y = mapTransform != null ? mapTransform.position.y : manualPosition.y;
        if (boundsMode == MapBoundsMode.FourCorners && mapCorners != null)
        {
            foreach (Transform corner in mapCorners)
            {
                if (corner != null)
                {
                    y = corner.position.y;
                    break;
                }
            }
        }

        Vector3 a = new Vector3(minX, y, minZ);
        Vector3 b = new Vector3(maxX, y, minZ);
        Vector3 c = new Vector3(maxX, y, maxZ);
        Vector3 d = new Vector3(minX, y, maxZ);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }

    void OnValidate()
    {
        if (mapCorners == null || mapCorners.Length != 4)
            System.Array.Resize(ref mapCorners, 4);
    }
}
