using Suntail;
using TMPro;
using UnityEngine;

/// <summary>
/// 准星射线：对准门可开关，对准 Tag=wall 的空气墙显示 Unknown area。
/// </summary>
public class PlayerAimInteract : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxDistance = 5f;
    [Tooltip("门所在 Layer（如 Suntail 第 10 层）")]
    [SerializeField] private LayerMask doorLayers;
    [Tooltip("空气墙所在 Layer；留空则默认只检测 Object 层")]
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string openPrompt = "Open [E]";
    [SerializeField] private string closePrompt = "Close [E]";
    [SerializeField] private string wallPrompt = "Unknown area";

    void Awake()
    {
        if (doorLayers.value == 0)
            doorLayers = 1 << 10;
        if (wallLayers.value == 0)
            wallLayers = LayerMask.GetMask("Object");
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (promptRoot != null)
            promptRoot.SetActive(false);
    }

    void Update()
    {
        if (PlayerInputLock.IsLocked || NpcPlacementController.IsActive)
        {
            HidePrompt();
            return;
        }

        if (TryGetDoor(out Door door))
        {
            ShowPrompt(door.doorOpen ? closePrompt : openPrompt);
            if (Input.GetKeyDown(interactKey))
                door.PlayDoorAnimation();
            return;
        }

        if (TryGetWall())
        {
            ShowPrompt(wallPrompt);
            return;
        }

        HidePrompt();
    }

    void ShowPrompt(string message)
    {
        if (promptRoot != null)
            promptRoot.SetActive(true);
        if (promptText != null)
            promptText.text = message;
    }

    void HidePrompt()
    {
        if (promptRoot != null)
            promptRoot.SetActive(false);
    }

    bool TryGetDoor(out Door door)
    {
        door = null;
        if (playerCamera == null || !RaycastCenter(out RaycastHit hit, doorLayers))
            return false;

        if (!hit.collider.CompareTag("Door"))
            return false;

        door = hit.collider.GetComponentInParent<Door>();
        return door != null
            && Vector3.Distance(transform.position, door.transform.position) <= maxDistance;
    }

    bool TryGetWall()
    {
        if (playerCamera == null || !RaycastCenter(out RaycastHit hit, wallLayers))
            return false;

        // 用大墙时 bounds.center 离玩家很远，不能用中心算距离；射线 hit 已在 maxDistance 内
        return hit.collider.CompareTag("wall");
    }

    bool RaycastCenter(out RaycastHit hit, LayerMask layers)
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return Physics.Raycast(ray, out hit, maxDistance, layers, QueryTriggerInteraction.Ignore);
    }
}
