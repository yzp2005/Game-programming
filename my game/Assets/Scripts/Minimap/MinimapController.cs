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

    readonly HashSet<MinimapTrackable> activeTrackables = new HashSet<MinimapTrackable>();
    readonly List<MinimapTrackable> staleTrackables = new List<MinimapTrackable>();
    readonly HashSet<NpcPlacementZone> activeZones = new HashSet<NpcPlacementZone>();
    readonly List<NpcPlacementZone> staleZones = new List<NpcPlacementZone>();

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
        SetVisible(false, force: true);
    }

    void Update()
    {
        if (PlayerInputLock.IsLocked)
        {
            SetVisible(false);
            return;
        }

        bool shouldShow = Input.GetKey(holdKey)
            || (enableTouchHold && IsTouchHoldingCorner());

        SetVisible(shouldShow);

        if (!boundsReady)
            RebuildBounds();

        MinimapWorldTracker tracker = ResolveTracker();
        if (tracker == null)
            return;

        if (isVisible)
            UpdatePlayerBlip(tracker);

        SyncTrackableBlips(tracker);
        SyncPlacementZoneBlips();
    }

    bool IsTouchHoldingCorner()
    {
        if (Input.touchCount == 0)
            return false;

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            return false;

        return touchHoldScreenRect.Contains(new Vector2(
            touch.position.x / Screen.width,
            touch.position.y / Screen.height));
    }

    void SetVisible(bool visible, bool force = false)
    {
        if (!force && isVisible == visible)
            return;

        isVisible = visible;
        if (minimapRoot != null)
            minimapRoot.SetActive(visible);

        if (visible)
            ResolveTracker()?.RefreshNow();
    }

    void RebuildBounds()
    {
        boundsReady = false;
        minX = minZ = float.PositiveInfinity;
        maxX = maxZ = float.NegativeInfinity;

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
            minX = maxX = minZ = maxZ = 0f;
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

        float u = Mathf.Clamp01(Mathf.InverseLerp(minX, maxX, world.x));
        float v = Mathf.Clamp01(Mathf.InverseLerp(minZ, maxZ, world.z));
        return new Vector2((u - 0.5f) * width, (v - 0.5f) * height);
    }

    void UpdatePlayerBlip(MinimapWorldTracker tracker)
    {
        if (playerBlip == null || blipContainer == null || !tracker.CurrentPlayer.IsValid)
            return;

        MinimapWorldTracker.PlayerSnapshot player = tracker.CurrentPlayer;
        playerBlip.anchoredPosition = WorldToBlipLocal(player.WorldPosition);

        if (rotatePlayerBlip)
            playerBlip.localRotation = Quaternion.Euler(0f, 0f, -player.Yaw);
    }

    void SyncTrackableBlips(MinimapWorldTracker tracker)
    {
        if (blipContainer == null)
            return;

        activeTrackables.Clear();
        IReadOnlyList<MinimapWorldTracker.EntitySnapshot> entities = tracker.CurrentEntities;

        for (int i = 0; i < entities.Count; i++)
        {
            MinimapWorldTracker.EntitySnapshot snapshot = entities[i];
            if (!snapshot.IsValid || snapshot.Source == null)
                continue;

            MinimapTrackable trackable = snapshot.Source;
            activeTrackables.Add(trackable);

            if (!blipByTrackable.TryGetValue(trackable, out RectTransform blip))
            {
                if (!isVisible)
                    continue;

                blipByTrackable[trackable] = CreateTrackableBlip(snapshot.Kind);
                blip = blipByTrackable[trackable];
            }

            if (isVisible)
                blip.anchoredPosition = WorldToBlipLocal(snapshot.WorldPosition);
        }

        staleTrackables.Clear();
        foreach (KeyValuePair<MinimapTrackable, RectTransform> pair in blipByTrackable)
        {
            if (pair.Key == null || !activeTrackables.Contains(pair.Key))
                staleTrackables.Add(pair.Key);
        }

        for (int i = 0; i < staleTrackables.Count; i++)
            RemoveTrackableBlip(staleTrackables[i]);
    }

    void SyncPlacementZoneBlips()
    {
        if (blipContainer == null)
            return;

        activeZones.Clear();
        IReadOnlyList<NpcPlacementZone> zones = NpcPlacementZone.RegisteredZones;

        for (int i = 0; i < zones.Count; i++)
        {
            NpcPlacementZone zone = zones[i];
            if (zone == null || !zone.ShowMinimapBlip)
                continue;

            activeZones.Add(zone);
            bool occupied = !zone.IsAvailable;

            if (blipByPlacementZone.TryGetValue(zone, out RectTransform blip)
                && blip != null
                && placementZoneOccupiedState.TryGetValue(zone, out bool knownOccupied)
                && knownOccupied == occupied)
            {
                if (isVisible)
                    blip.anchoredPosition = WorldToBlipLocal(zone.MinimapWorldPosition);
                continue;
            }

            RemovePlacementZoneBlip(zone);
            if (!isVisible)
                continue;

            blip = CreatePlacementZoneBlip(zone, occupied);
            blipByPlacementZone[zone] = blip;
            placementZoneOccupiedState[zone] = occupied;
            blip.anchoredPosition = WorldToBlipLocal(zone.MinimapWorldPosition);
        }

        staleZones.Clear();
        foreach (KeyValuePair<NpcPlacementZone, RectTransform> pair in blipByPlacementZone)
        {
            if (pair.Key == null || !activeZones.Contains(pair.Key))
                staleZones.Add(pair.Key);
        }

        for (int i = 0; i < staleZones.Count; i++)
            RemovePlacementZoneBlip(staleZones[i]);
    }

    void RemoveTrackableBlip(MinimapTrackable trackable)
    {
        if (!blipByTrackable.TryGetValue(trackable, out RectTransform blip))
            return;

        if (blip != null)
            Destroy(blip.gameObject);

        blipByTrackable.Remove(trackable);
    }

    void RemovePlacementZoneBlip(NpcPlacementZone zone)
    {
        if (zone == null || !blipByPlacementZone.TryGetValue(zone, out RectTransform blip))
            return;

        if (blip != null)
            Destroy(blip.gameObject);

        blipByPlacementZone.Remove(zone);
        placementZoneOccupiedState.Remove(zone);
    }

    RectTransform CreateTrackableBlip(MinimapTrackable.BlipKind kind)
    {
        return CreateBlipRect(
            GetBlipPrefab(kind),
            $"MinimapBlip_{kind}",
            GetColor(kind),
            applyTint: true);
    }

    RectTransform CreatePlacementZoneBlip(NpcPlacementZone zone, bool occupied)
    {
        GameObject prefab = occupied
            ? zone.ResolveOccupiedBlipPrefab(occupiedZoneBlipPrefab)
            : zone.ResolveAvailableBlipPrefab(availableZoneBlipPrefab);

        return CreateBlipRect(
            prefab,
            occupied ? "OccupiedZoneBlip" : "AvailableZoneBlip",
            occupied ? occupiedZoneFallbackColor : availableZoneFallbackColor,
            applyTint: prefab == null);
    }

    RectTransform CreateBlipRect(GameObject prefab, string fallbackName, Color color, bool applyTint)
    {
        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab, blipContainer);
        }
        else
        {
            go = CreateFallbackBlipObject(fallbackName, color);
            go.transform.SetParent(blipContainer, false);
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        if (rt.sizeDelta == Vector2.zero)
            rt.sizeDelta = defaultBlipSize;

        if (go.TryGetComponent(out Image image))
        {
            if (applyTint)
                image.color = color;

            EnsureBlipSprite(image);
        }

        rt.SetAsLastSibling();
        return rt;
    }

    static GameObject CreateFallbackBlipObject(string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    MinimapWorldTracker ResolveTracker()
    {
        if (worldTracker == null)
            worldTracker = MinimapWorldTracker.Instance;

        return worldTracker;
    }

    static void EnsureBlipSprite(Image image)
    {
        if (image.sprite == null)
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
        if (kind == MinimapTrackable.BlipKind.Friendly && friendlyBlipPrefab != null)
            return friendlyBlipPrefab;

        return enemyBlipPrefab;
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
                if (corner == null)
                    continue;

                y = corner.position.y;
                break;
            }
        }

        var a = new Vector3(minX, y, minZ);
        var b = new Vector3(maxX, y, minZ);
        var c = new Vector3(maxX, y, maxZ);
        var d = new Vector3(minX, y, maxZ);
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
