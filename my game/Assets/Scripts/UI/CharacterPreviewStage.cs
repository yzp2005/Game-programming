using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 角色展示：专用相机 → RenderTexture → RawImage。
/// 按 Q 切换展示编号；模型数据来自 NpcCharacterRegistry。
/// </summary>
[DisallowMultipleComponent]
public class CharacterPreviewStage : MonoBehaviour
{
    [Header("数据")]
    [SerializeField] NpcCharacterRegistry registry;

    [Header("输出")]
    [SerializeField] Camera displayCamera;
    [SerializeField] RenderTexture renderTexture;
    [SerializeField] RawImage targetImage;
    [SerializeField] int renderTextureSize = 512;

    [Header("展示")]
    [SerializeField] Transform modelPivot;
    [SerializeField] float rotateSpeed = 40f;
    [SerializeField] bool stripGameplayComponents = true;
    [SerializeField] string displayLayerName = "UI3D";

    [Header("输入")]
    [SerializeField] KeyCode nextCharacterKey = KeyCode.Q;
    [SerializeField] bool requirePanelActive;
    [SerializeField] GameObject panelRoot;

    [Header("信息 UI")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text costText;
    [Tooltip("{0} 为巧克力数量")]
    [SerializeField] string costFormat = "{0}";

    [Header("相机")]
    [SerializeField] Color cameraBackground = new Color(0f, 0f, 0f, 0f);

    Transform spawnRoot;
    GameObject currentInstance;
    int currentDisplayIndex = -1;
    int displayLayer = -1;

    public NpcCharacterRegistry Registry => registry;
    public int CurrentDisplayIndex => currentDisplayIndex >= 0 ? currentDisplayIndex : 0;

    void Awake()
    {
        ResolveRegistry();
        EnsureModelPivot();
        EnsureSpawnRoot();

        displayLayer = LayerMask.NameToLayer(displayLayerName);
        if (displayLayer < 0)
            Debug.LogWarning($"{name}: 未找到 Layer \"{displayLayerName}\"。", this);

        EnsureRenderTexture();
        SetupCamera();
        BindUiTexture();
    }

    void OnEnable()
    {
        if (registry == null || registry.SlotCount == 0)
        {
            ClearCharacterInfoUI();
            return;
        }

        ShowCharacter(currentDisplayIndex < 0 ? 0 : currentDisplayIndex);
    }

    void OnDisable()
    {
        ClearSpawnRoot();
        currentDisplayIndex = -1;
        ClearCharacterInfoUI();
    }

    void Update()
    {
        if (modelPivot != null && rotateSpeed != 0f)
            modelPivot.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.Self);

        if (!CanAcceptInput())
            return;

        if (FightLevelInputGate.GetKeyDown(nextCharacterKey))
            ShowNextCharacter();
    }

    void OnDestroy()
    {
        if (renderTexture != null && renderTexture.name.StartsWith("CharacterPreviewRT_Runtime"))
            renderTexture.Release();
    }

    public void ShowNextCharacter()
    {
        if (registry == null || registry.SlotCount == 0)
            return;

        int next = currentDisplayIndex < 0 ? 0 : (currentDisplayIndex + 1) % registry.SlotCount;
        ShowCharacter(next);
    }

    public void ShowCharacter(int index)
    {
        if (registry == null || registry.SlotCount == 0)
            return;

        GameObject displayPrefab = registry.GetDisplayPrefab(index);
        if (displayPrefab == null)
        {
            Debug.LogWarning($"{name}: Registry Slot {index} 未配置 Display Prefab。", this);
            return;
        }

        index = Mathf.Clamp(index, 0, registry.SlotCount - 1);
        if (index == currentDisplayIndex && currentInstance != null)
            return;

        EnsureModelPivot();
        EnsureSpawnRoot();
        ClearSpawnRoot();

        currentDisplayIndex = index;
        currentInstance = Instantiate(displayPrefab, spawnRoot);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = Vector3.one;

        ApplyDisplayLayer(currentInstance);
        if (stripGameplayComponents)
            StripGameplayComponents(currentInstance);

        RefreshCharacterInfoUI(index);
    }

    void RefreshCharacterInfoUI(int index)
    {
        if (registry == null)
        {
            ClearCharacterInfoUI();
            return;
        }

        if (nameText != null)
            nameText.text = registry.GetDisplayName(index);

        if (costText != null)
            costText.text = string.Format(costFormat, registry.GetChocolateCost(index));
    }

    void ClearCharacterInfoUI()
    {
        if (nameText != null)
            nameText.text = string.Empty;

        if (costText != null)
            costText.text = string.Empty;
    }

    public void EnsureCurrentCharacterVisible()
    {
        if (currentInstance == null && registry != null && registry.SlotCount > 0)
            ShowCharacter(CurrentDisplayIndex);
    }

    void ResolveRegistry()
    {
        if (registry != null)
            return;

        registry = GetComponent<NpcCharacterRegistry>();
        if (registry == null)
            registry = FindObjectOfType<NpcCharacterRegistry>(true);
    }

    bool CanAcceptInput()
    {
        if (PlayerInputLock.IsLocked || NpcPlacementController.IsActive)
            return false;

        if (requirePanelActive && panelRoot != null && !panelRoot.activeInHierarchy)
            return false;

        return isActiveAndEnabled;
    }

    void EnsureRenderTexture()
    {
        if (renderTexture != null)
            return;

        renderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16, RenderTextureFormat.ARGB32)
        {
            name = "CharacterPreviewRT_Runtime",
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false
        };
        renderTexture.Create();
    }

    void SetupCamera()
    {
        if (displayCamera == null)
            return;

        displayCamera.targetTexture = renderTexture;
        displayCamera.clearFlags = CameraClearFlags.SolidColor;
        displayCamera.backgroundColor = cameraBackground;
        displayCamera.depth = -10;

        if (displayLayer >= 0)
            displayCamera.cullingMask = 1 << displayLayer;

        if (displayCamera.TryGetComponent(out AudioListener listener))
            listener.enabled = false;
    }

    void BindUiTexture()
    {
        if (targetImage != null && renderTexture != null)
            targetImage.texture = renderTexture;
    }

    void EnsureModelPivot()
    {
        if (modelPivot != null)
            return;

        Transform existing = transform.Find("display area") ?? transform.Find("ModelPivot");
        if (existing != null)
        {
            modelPivot = existing;
            return;
        }

        var pivotObject = new GameObject("ModelPivot");
        pivotObject.transform.SetParent(transform, false);
        modelPivot = pivotObject.transform;
    }

    void EnsureSpawnRoot()
    {
        EnsureModelPivot();

        if (spawnRoot != null)
            return;

        Transform existing = modelPivot.Find("CharacterSpawn");
        if (existing != null)
        {
            spawnRoot = existing;
            return;
        }

        var spawnObject = new GameObject("CharacterSpawn");
        spawnObject.transform.SetParent(modelPivot, false);
        spawnRoot = spawnObject.transform;
    }

    void ClearSpawnRoot()
    {
        if (spawnRoot == null)
            return;

        for (int i = spawnRoot.childCount - 1; i >= 0; i--)
            Destroy(spawnRoot.GetChild(i).gameObject);

        currentInstance = null;
    }

    void ApplyDisplayLayer(GameObject root)
    {
        if (displayLayer < 0)
            return;

        SetLayerRecursively(root, displayLayer);
    }

    static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        Transform t = root.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
    }

    static void StripGameplayComponents(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (Rigidbody rb in root.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null)
                behaviour.enabled = false;
        }
    }
}
