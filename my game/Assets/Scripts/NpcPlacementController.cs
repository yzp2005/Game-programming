using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 按 E 进入/退出摆放模式。幽灵用 Display Prefab，左键放置 Placement Prefab。
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class NpcPlacementController : MonoBehaviour
{
    [Header("输入")]
    [SerializeField] KeyCode toggleKey = KeyCode.E;
    [SerializeField] int placeMouseButton = 0;
    [SerializeField] CharacterPreviewStage previewStage;

    [Header("检测")]
    [SerializeField] Camera playerCamera;
    [SerializeField] float maxRayDistance = 80f;
    [SerializeField] LayerMask obstacleLayers = ~0;
    [SerializeField] LayerMask groundLayers = ~0;
    [SerializeField] float placementRadius = 0.45f;
    [SerializeField] float placementHeight = 1.8f;
    [SerializeField] float obstacleSkin = 0.05f;
    [SerializeField] float groundProbeUp = 3f;
    [SerializeField] float groundProbeDown = 6f;
    [SerializeField] float minDistanceFromPlayer = 2.5f;
    [SerializeField] float minScreenPointerDistance = 80f;

    [Header("预览材质")]
    [SerializeField] Material validPreviewMaterial;
    [SerializeField] Material invalidPreviewMaterial;
    [Tooltip("摆放幽灵使用的 Layer，需被主相机 Culling Mask 包含")]
    [SerializeField] string previewLayerName = "Default";

    GameObject previewInstance;
    GameObject displayPrefab;
    GameObject placementPrefab;
    float referenceGroundY;
    bool lastPreviewValid = true;

    readonly List<(Renderer renderer, Material[] originalSharedMaterials)> previewMaterialBackups = new();

    NpcCharacterRegistry registry;

    public static bool IsActive { get; private set; }
    public static bool HasAvailablePrefab { get; private set; }

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        EnsureRefs();
    }

    void Update()
    {
        EnsureRefs();

        HasAvailablePrefab = registry != null
            && previewStage != null
            && registry.CanPlace(previewStage.CurrentDisplayIndex)
            && CanAffordCurrentSlot();

        if (PlayerInputLock.IsLocked)
            return;

        if (Input.GetKeyDown(toggleKey))
        {
            if (IsActive)
                ExitPlacementMode();
            else
                EnterPlacementMode();
            return;
        }

        if (!IsActive)
            return;

        UpdatePreviewTransform();

        if (Input.GetMouseButtonDown(placeMouseButton) && !IsPointerOverUI() && CanAffordCurrentSlot())
            PlacePreview();
    }

    void OnDisable()
    {
        if (IsActive)
            ExitPlacementMode();
    }

    void EnsureRefs()
    {
        if (previewStage == null)
            previewStage = FindObjectOfType<CharacterPreviewStage>(true);

        if (registry != null)
            return;

        if (previewStage != null && previewStage.Registry != null)
            registry = previewStage.Registry;
        else
            registry = FindObjectOfType<NpcCharacterRegistry>(true);
    }

    public void EnterPlacementMode()
    {
        EnsureRefs();

        if (previewStage == null || registry == null)
        {
            Debug.LogWarning($"{name}: 缺少 CharacterPreviewStage 或 NpcCharacterRegistry。", this);
            return;
        }

        previewStage.EnsureCurrentCharacterVisible();

        int index = previewStage.CurrentDisplayIndex;
        displayPrefab = registry.GetDisplayPrefab(index);
        placementPrefab = registry.GetPlacementPrefab(index);

        if (displayPrefab == null || placementPrefab == null)
        {
            Debug.LogWarning($"{name}: Registry Slot {index} 配置不完整。", this);
            return;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (validPreviewMaterial == null)
            Debug.LogWarning($"{name}: Valid Preview Material 未设置，幽灵可能没有半透明白色效果。", this);

        IsActive = true;
        referenceGroundY = transform.position.y;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SpawnPreview();
    }

    public void ExitPlacementMode()
    {
        IsActive = false;
        DestroyPreview();

        if (!PlayerInputLock.IsLocked)
            PlayerInputLock.ApplyGameplayCursor();
    }

    void SpawnPreview()
    {
        if (displayPrefab == null)
            return;

        DestroyPreview();

        previewInstance = Instantiate(displayPrefab);
        previewInstance.name = displayPrefab.name + " (Preview)";
        ApplyPreviewLayer(previewInstance);
        ApplyPreviewState(previewInstance);
        CachePreviewMaterials();
        lastPreviewValid = true;
        ApplyPreviewMaterials(true);
        UpdatePreviewTransform();
    }

    void ApplyPreviewLayer(GameObject root)
    {
        int layer = LayerMask.NameToLayer(previewLayerName);
        if (layer < 0)
            layer = 0;

        SetLayerRecursively(root, layer);
    }

    static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        Transform t = root.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
    }

    void PlacePreview()
    {
        if (previewInstance == null || placementPrefab == null || previewStage == null || registry == null)
            return;

        int cost = registry.GetChocolateCost(previewStage.CurrentDisplayIndex);
        if (GameStatsUI.Instance != null && !GameStatsUI.Instance.TryPlaceNpc(cost))
            return;

        Vector3 position = previewInstance.transform.position;
        Quaternion rotation = previewInstance.transform.rotation;

        DestroyPreview();

        GameObject placed = Instantiate(placementPrefab, position, rotation);
        placed.name = placementPrefab.name;
        MinimapTrackable.EnsureOn(placed, MinimapTrackable.BlipKind.Friendly);

        SpawnPreview();
    }

    void DestroyPreview()
    {
        if (previewInstance == null)
            return;

        Destroy(previewInstance);
        previewInstance = null;
        previewMaterialBackups.Clear();
    }

    bool CanAffordCurrentSlot()
    {
        if (registry == null || previewStage == null || GameStatsUI.Instance == null)
            return true;

        return GameStatsUI.Instance.CanAfford(registry.GetChocolateCost(previewStage.CurrentDisplayIndex));
    }

    void CachePreviewMaterials()
    {
        previewMaterialBackups.Clear();

        Renderer[] configuredRenderers = registry != null && previewStage != null
            ? registry.GetPreviewRenderers(previewStage.CurrentDisplayIndex)
            : null;

        if (configuredRenderers != null && configuredRenderers.Length > 0)
        {
            foreach (Renderer prefabRenderer in configuredRenderers)
            {
                if (prefabRenderer == null)
                    continue;

                Renderer instanceRenderer = ResolveInstanceRenderer(previewInstance, prefabRenderer)
                    ?? FindRendererByName(previewInstance, prefabRenderer.name);
                if (instanceRenderer == null)
                    continue;

                previewMaterialBackups.Add((instanceRenderer, (Material[])instanceRenderer.sharedMaterials.Clone()));
            }

            if (previewMaterialBackups.Count > 0)
                return;
        }

        foreach (Renderer renderer in previewInstance.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null)
                previewMaterialBackups.Add((renderer, (Material[])renderer.sharedMaterials.Clone()));
        }
    }

    static Renderer ResolveInstanceRenderer(GameObject instance, Renderer prefabRenderer)
    {
        if (instance == null || prefabRenderer == null)
            return null;

        GameObject prefabRoot = prefabRenderer.transform.root.gameObject;
        Transform current = instance.transform;
        var path = new List<string>();
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

    static Renderer FindRendererByName(GameObject instance, string rendererName)
    {
        if (instance == null || string.IsNullOrEmpty(rendererName))
            return null;

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null && renderer.name == rendererName)
                return renderer;
        }

        return null;
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

            renderer.enabled = true;
            Material[] slots = renderer.materials;
            for (int i = 0; i < slots.Length; i++)
                slots[i] = previewMaterial;
            renderer.materials = slots;
        }
    }

    void UpdatePreviewTransform()
    {
        if (previewInstance == null)
            return;

        if (TryGetPlacementPose(out Vector3 position, out Quaternion rotation))
            previewInstance.transform.SetPositionAndRotation(position, rotation);
        else
            previewInstance.transform.position = GetFallbackPreviewPosition();

        bool canPlace = CanAffordCurrentSlot();
        if (canPlace != lastPreviewValid)
        {
            lastPreviewValid = canPlace;
            ApplyPreviewMaterials(canPlace);
        }
    }

    Vector3 GetFallbackPreviewPosition()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        Vector3 point = transform.position + forward.normalized * minDistanceFromPlayer;
        point.y = referenceGroundY;
        return SnapToGround(point);
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
            ray.origin, placementRadius, ray.direction, maxRayDistance, obstacleLayers, QueryTriggerInteraction.Ignore);

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
            if (ShouldIgnoreCollider(hit.collider) || hit.distance >= closestDistance)
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

    static void ApplyPreviewState(GameObject root)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null)
                renderer.enabled = true;
        }

        foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
        {
            if (animator == null)
                continue;

            animator.enabled = true;
            animator.speed = 0f;
            animator.Update(0f);
        }

        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null && behaviour.enabled)
                behaviour.enabled = false;
        }

        foreach (Rigidbody rb in root.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
