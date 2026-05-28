using Suntail;
using TMPro;
using UnityEngine;

/// <summary>
/// 从屏幕中心（准星）发射射线，只检测 Interact Layers 上的碰撞体，其它 Layer 的物体会被穿透。
/// </summary>
public class PlayerDoorInteract : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxDistance = 5f;
    [Tooltip("只勾门碰撞体所在 Layer（如 Suntail 第 10 层）。不要勾 Default，否则会被墙挡住")]
    [SerializeField] private LayerMask interactLayers;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string openPrompt = "Open [E]";
    [SerializeField] private string closePrompt = "Close [E]";

    void Awake()
    {
        if (interactLayers.value == 0)
            interactLayers = 1 << 10;
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (promptRoot != null)
            promptRoot.SetActive(false);
    }

    void Update()
    {
        if (PlayerInputLock.IsLocked || !TryGetDoor(out Door door))
        {
            if (promptRoot != null)
                promptRoot.SetActive(false);
            return;
        }

        if (promptRoot != null)
            promptRoot.SetActive(true);
        if (promptText != null)
            promptText.text = door.doorOpen ? closePrompt : openPrompt;

        if (Input.GetKeyDown(interactKey))
            door.PlayDoorAnimation();
    }

    bool TryGetDoor(out Door door)
    {
        door = null;
        if (playerCamera == null)
            return false;

        // 屏幕正中央 = 准星；从相机出发，不是从角色脚底
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactLayers, QueryTriggerInteraction.Ignore))
            return false;

        if (!hit.collider.CompareTag("Door"))
            return false;

        door = hit.collider.GetComponentInParent<Door>();
        return door != null
            && Vector3.Distance(transform.position, door.transform.position) <= maxDistance;
    }
}
