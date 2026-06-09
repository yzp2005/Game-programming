using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 多按键对应不同 NPC 的摆放模式。按配置键进入/切换/退出，左键放置。
/// 挂到主角上。
/// </summary>
[DisallowMultipleComponent]
public class NpcPlacementController : MonoBehaviour
{
    [Serializable]
    public class NpcPlacementEntry
    {
        [Tooltip("按此键进入该 NPC 摆放；摆放中再按一次退出，按其他键则切换")]
        public KeyCode placementKey = KeyCode.E;
        public GameObject npcPrefab;
        [Tooltip("从该预制体拖入需要换预览材质的 Renderer")]
        public Renderer[] previewRenderers;
    }

    [Header("输入")]
    [SerializeField] int placeMouseButton = 0;
    [SerializeField] NpcPlacementEntry[] placementEntries;

    [Header("检测")]
    [SerializeField] Camera playerCamera;
    [SerializeField] float maxRayDistance = 80f;
    [SerializeField] LayerMask obstacleLayers = ~0;
    [SerializeField] LayerMask groundLayers = ~0;
    [Tooltip("人物水平占地半径，用于 SphereCast / CapsuleCast")]
    [SerializeField] float placementRadius = 0.45f;
    [Tooltip("胶囊体高度（脚底到顶）")]
    [SerializeField] float placementHeight = 1.8f;
    [SerializeField] float obstacleSkin = 0.05f;
    [SerializeField] float groundProbeUp = 3f;
    [SerializeField] float groundProbeDown = 6f;
    [Tooltip("预览与主角的最小水平距离，避免鼠标靠近时贴在身上")]
    [SerializeField] float minDistanceFromPlayer = 2.5f;
    [Tooltip("鼠标离屏幕中心低于此像素时，沿鼠标方向推到最小距离处")]
    [SerializeField] float minScreenPointerDistance = 80f;

    [Header("预览材质")]
    [SerializeField] Material validPreviewMaterial;
    [SerializeField] Material invalidPreviewMaterial;

    GameObject previewInstance;
    float referenceGroundY;
    bool lastPreviewValid = true;
    int activeEntryIndex = -1;

    readonly System.Collections.Generic.List<Behaviour> previewDisabledBehaviours = new();
    readonly System.Collections.Generic.List<Collider> previewDisabledColliders = new();
    readonly System.Collections.Generic.List<Animator> previewDisabledAnimators = new();
    readonly System.Collections.Generic.List<(Rigidbody rb, bool kinematic, bool useGravity)> previewRigidbodies = new();
    readonly System.Collections.Generic.List<(Renderer renderer, Material[] originalSharedMaterials)> previewMaterialBackups = new();

    public static bool IsActive { get; private set; }

    NpcPlacementEntry ActiveEntry =>
        activeEntryIndex >= 0 && placementEntries != null && activeEntryIndex < placementEntries.Length
            ? placementEntries[activeEntryIndex]
            : null;

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        if (PlayerInputLock.IsLocked)
            return;

        if (TryHandleEntryInput())
            return;

        if (!IsActive)
            return;

        UpdatePreviewTransform();

        if (Input.GetMouseButtonDown(placeMouseButton) && !IsPointerOverUI() && CanPlaceAtCurrentPosition())
            PlacePreview();
    }

    bool TryHandleEntryInput()
    {
        if (placementEntries == null || placementEntries.Length == 0)
            return false;

        for (int i = 0; i < placementEntries.Length; i++)
        {
            NpcPlacementEntry entry = placementEntries[i];
            if (entry == null || !Input.GetKeyDown(entry.placementKey))
                continue;

            if (IsActive && activeEntryIndex == i)
                ExitPlacementMode();
            else if (IsActive)
                SwitchEntry(i);
            else
                EnterPlacementMode(i);

            return true;
        }

        return false;
    }

    void OnDisable()
    {
        if (IsActive)
            ExitPlacementMode();
    }

    public void EnterPlacementMode(int entryIndex)
    {
        if (!TryGetEntry(entryIndex, out NpcPlacementEntry entry))
            return;

        if (entry.npcPrefab == null)
        {
            Debug.LogWarning($"{name}: Placement Entries[{entryIndex}] 未指定 NPC Prefab。", this);
            return;
        }

        IsActive = true;
        activeEntryIndex = entryIndex;
        referenceGroundY = transform.position.y;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SpawnPreview();
    }

    void SwitchEntry(int entryIndex)
    {
        if (!TryGetEntry(entryIndex, out NpcPlacementEntry entry))
            return;

        if (entry.npcPrefab == null)
        {
            Debug.LogWarning($"{name}: Placement Entries[{entryIndex}] 未指定 NPC Prefab。", this);
            return;
        }

        activeEntryIndex = entryIndex;
        SpawnPreview();
    }

    bool TryGetEntry(int entryIndex, out NpcPlacementEntry entry)
    {
        entry = null;
        if (placementEntries == null || entryIndex < 0 || entryIndex >= placementEntries.Length)
            return false;

        entry = placementEntries[entryIndex];
        return entry != null;
    }

    public void ExitPlacementMode()
    {
        IsActive = false;
        activeEntryIndex = -1;
        DestroyPreview();

        if (!PlayerInputLock.IsLocked)
            PlayerInputLock.ApplyGameplayCursor();
    }

    void SpawnPreview()
    {
        NpcPlacementEntry entry = ActiveEntry;
        if (entry == null || entry.npcPrefab == null)
            return;

        DestroyPreview();

        previewInstance = Instantiate(entry.npcPrefab);
        previewInstance.name = entry.npcPrefab.name + " (Preview)";
        ApplyPreviewState(previewInstance);
        CachePreviewMaterials(entry);
        lastPreviewValid = true;
        ApplyPreviewMaterials(true);
        UpdatePreviewTransform();
    }

    void PlacePreview()
    {
        NpcPlacementEntry entry = ActiveEntry;
        if (previewInstance == null || entry == null || entry.npcPrefab == null)
            return;

        RestorePreviewMaterials();
        ActivatePreview(previewInstance);
        previewInstance.name = entry.npcPrefab.name;
        previewInstance = null;
        ClearPreviewTracking();

        SpawnPreview();
    }

    void DestroyPreview()
    {
        if (previewInstance == null)
            return;

        Destroy(previewInstance);
        previewInstance = null;
        ClearPreviewTracking();
    }

    void CachePreviewMaterials(NpcPlacementEntry entry)
    {
        previewMaterialBackups.Clear();

        if (entry.previewRenderers == null || entry.previewRenderers.Length == 0)
        {
            Debug.LogWarning($"{name}: {entry.npcPrefab.name} 的 Preview Renderers 未配置。", this);
            return;
        }

        foreach (Renderer prefabRenderer in entry.previewRenderers)
        {
            Renderer instanceRenderer = ResolveInstanceRenderer(previewInstance, prefabRenderer);
            if (instanceRenderer == null)
            {
                Debug.LogWarning($"{name}: 无法在预览实例上找到 {prefabRenderer.name} 对应的 Renderer。", this);
                continue;
            }

            previewMaterialBackups.Add((instanceRenderer, (Material[])instanceRenderer.sharedMaterials.Clone()));
        }
    }

    static Renderer ResolveInstanceRenderer(GameObject instance, Renderer prefabRenderer)
    {
        if (instance == null || prefabRenderer == null || instance.transform == null)
            return null;

        GameObject prefabRoot = prefabRenderer.transform.root.gameObject;
        Transform current = instance.transform;
        var path = new System.Collections.Generic.List<string>();
        Transform node = prefabRenderer.transform;

        while (node != null && node.gameObject != prefabRoot)
        {
            path.Add(node.name);
            node = node.parent;
        }

        path.Reverse();
        foreach (string part in path)
        {
            current = current.Find(part);
            if (current == null)
                return null;
        }

        return current.GetComponent<Renderer>();
    }

    void ApplyPreviewMaterials(bool valid)
    {
        Material previewMaterial = valid ? validPreviewMaterial : invalidPreviewMaterial;
        if (previewMaterial == null)
            return;

        foreach ((Renderer renderer, Material[] _) in previewMaterialBackups)
        {
            if (renderer == null)
                continue;

            Material[] slots = renderer.materials;
            for (int i = 0; i < slots.Length; i++)
                slots[i] = previewMaterial;
            renderer.materials = slots;
        }
    }

    void RestorePreviewMaterials()
    {
        foreach ((Renderer renderer, Material[] originalSharedMaterials) in previewMaterialBackups)
        {
            if (renderer == null)
                continue;

            renderer.sharedMaterials = originalSharedMaterials;
        }

        previewMaterialBackups.Clear();
    }

    void UpdatePreviewTransform()
    {
        if (previewInstance == null)
            return;

        if (TryGetPlacementPose(out Vector3 position, out Quaternion rotation))
            previewInstance.transform.SetPositionAndRotation(position, rotation);

        bool canPlace = CanPlaceAtCurrentPosition();
        if (canPlace != lastPreviewValid)
        {
            lastPreviewValid = canPlace;
            ApplyPreviewMaterials(canPlace);
        }
    }

    /// <summary>当前位置是否可放置。区域、重叠等规则在此扩展。</summary>
    bool CanPlaceAtCurrentPosition()
    {
        if (previewInstance == null)
            return false;

        // TODO: 区域检测、与其他物体重叠等
        return true;
    }

    bool TryGetPlacementPose(out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;

        if (playerCamera == null)
            return false;

        Ray crosshairRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Ray mouseRay = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (!TryGetRayGroundPoint(crosshairRay, out Vector3 crosshairPoint))
            crosshairPoint = transform.position;

        if (!TryGetRayGroundPoint(mouseRay, out Vector3 mousePoint))
            mousePoint = crosshairPoint;

        referenceGroundY = crosshairPoint.y;
        position = ResolveHorizontalPosition(crosshairPoint, mousePoint);
        position = SnapToGround(position);
        position = EnforceMinDistanceFromPlayer(position);

        Vector3 faceDir = mousePoint - crosshairPoint;
        faceDir.y = 0f;
        rotation = faceDir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(faceDir.normalized, Vector3.up)
            : transform.rotation;

        return true;
    }

    bool TryGetRayGroundPoint(Ray ray, out Vector3 point)
    {
        RaycastHit[] sphereHits = Physics.SphereCastAll(
            ray.origin,
            placementRadius,
            ray.direction,
            maxRayDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);

        if (TryGetClosestHit(sphereHits, out RaycastHit hit))
        {
            point = hit.point + hit.normal * (placementRadius + obstacleSkin);
            return true;
        }

        int combinedMask = obstacleLayers | groundLayers;
        RaycastHit[] rayHits = Physics.RaycastAll(ray, maxRayDistance, combinedMask, QueryTriggerInteraction.Ignore);
        if (TryGetClosestHit(rayHits, out hit))
        {
            point = hit.point;
            return true;
        }

        if (TryIntersectHorizontalPlane(ray, referenceGroundY, out point))
            return true;

        point = default;
        return false;
    }

    Vector3 ResolveHorizontalPosition(Vector3 crosshairPoint, Vector3 mousePoint)
    {
        Vector3 start = crosshairPoint;
        Vector3 end = mousePoint;
        start.y = referenceGroundY;
        end.y = referenceGroundY;

        if (IsMouseNearScreenCenter())
            end = GetMinDistancePointFromMouseDirection();

        Vector3 offset = end - start;
        offset.y = 0f;
        float distance = offset.magnitude;
        if (distance < 0.001f)
            return EnforceMinDistanceFromPlayer(start);

        Vector3 direction = offset / distance;
        GetCapsulePoints(start, out Vector3 capsuleBottom, out Vector3 capsuleTop);

        if (Physics.CapsuleCast(capsuleBottom, capsuleTop, placementRadius, direction, out RaycastHit hit, distance, obstacleLayers, QueryTriggerInteraction.Ignore)
            && !ShouldIgnoreCollider(hit.collider))
            return hit.point - direction * (placementRadius + obstacleSkin);

        return end;
    }

    bool IsMouseNearScreenCenter()
    {
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        return Vector2.Distance(Input.mousePosition, center) < minScreenPointerDistance;
    }

    Vector3 GetMinDistancePointFromMouseDirection()
    {
        Ray mouseRay = playerCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 direction = mouseRay.direction;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = playerCamera.transform.forward;

        direction.Normalize();
        Vector3 point = transform.position + direction * minDistanceFromPlayer;
        point.y = referenceGroundY;
        return point;
    }

    Vector3 EnforceMinDistanceFromPlayer(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude >= minDistanceFromPlayer * minDistanceFromPlayer)
            return position;

        if (offset.sqrMagnitude < 0.0001f)
            offset = GetMinDistancePointFromMouseDirection() - transform.position;

        offset.y = 0f;
        offset = offset.normalized * minDistanceFromPlayer;
        position.x = transform.position.x + offset.x;
        position.z = transform.position.z + offset.z;
        return SnapToGround(position);
    }

    bool ShouldIgnoreCollider(Collider collider)
    {
        if (collider == null)
            return true;

        Transform hitTransform = collider.transform;
        if (hitTransform == transform || hitTransform.IsChildOf(transform))
            return true;

        if (previewInstance != null
            && (hitTransform == previewInstance.transform || hitTransform.IsChildOf(previewInstance.transform)))
            return true;

        return false;
    }

    bool TryGetClosestHit(RaycastHit[] hits, out RaycastHit closestHit)
    {
        closestHit = default;
        float closestDistance = float.MaxValue;
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            if (ShouldIgnoreCollider(hit.collider))
                continue;

            if (hit.distance >= closestDistance)
                continue;

            closestDistance = hit.distance;
            closestHit = hit;
            found = true;
        }

        return found;
    }

    Vector3 SnapToGround(Vector3 worldPoint)
    {
        Vector3 probeOrigin = worldPoint + Vector3.up * groundProbeUp;
        if (Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit hit, groundProbeUp + groundProbeDown, groundLayers, QueryTriggerInteraction.Ignore))
            return hit.point;

        worldPoint.y = referenceGroundY;
        return worldPoint;
    }

    void GetCapsulePoints(Vector3 feetPosition, out Vector3 bottom, out Vector3 top)
    {
        bottom = feetPosition + Vector3.up * (placementRadius + 0.01f);
        top = feetPosition + Vector3.up * Mathf.Max(placementRadius * 2f, placementHeight - placementRadius);
    }

    static bool TryIntersectHorizontalPlane(Ray ray, float planeY, out Vector3 point)
    {
        if (Mathf.Abs(ray.direction.y) < 0.0001f)
        {
            point = default;
            return false;
        }

        float t = (planeY - ray.origin.y) / ray.direction.y;
        if (t < 0f)
        {
            point = default;
            return false;
        }

        point = ray.GetPoint(t);
        return true;
    }

    void ClearPreviewTracking()
    {
        previewDisabledBehaviours.Clear();
        previewDisabledColliders.Clear();
        previewDisabledAnimators.Clear();
        previewRigidbodies.Clear();
    }

    void ApplyPreviewState(GameObject root)
    {
        ClearPreviewTracking();

        foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
        {
            if (!animator.enabled)
                continue;

            previewDisabledAnimators.Add(animator);
            animator.enabled = false;
        }

        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
        {
            if (!collider.enabled)
                continue;

            previewDisabledColliders.Add(collider);
            collider.enabled = false;
        }

        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || !behaviour.enabled)
                continue;

            previewDisabledBehaviours.Add(behaviour);
            behaviour.enabled = false;
        }

        foreach (Rigidbody rb in root.GetComponentsInChildren<Rigidbody>(true))
        {
            previewRigidbodies.Add((rb, rb.isKinematic, rb.useGravity));
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    void ActivatePreview(GameObject root)
    {
        foreach (Animator animator in previewDisabledAnimators)
        {
            if (animator != null)
                animator.enabled = true;
        }

        foreach (Collider collider in previewDisabledColliders)
        {
            if (collider != null)
                collider.enabled = true;
        }

        foreach (Behaviour behaviour in previewDisabledBehaviours)
        {
            if (behaviour != null)
                behaviour.enabled = true;
        }

        foreach ((Rigidbody rb, bool kinematic, bool useGravity) entry in previewRigidbodies)
        {
            if (entry.rb == null)
                continue;

            entry.rb.isKinematic = entry.kinematic;
            entry.rb.useGravity = entry.useGravity;
        }
    }

    static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
