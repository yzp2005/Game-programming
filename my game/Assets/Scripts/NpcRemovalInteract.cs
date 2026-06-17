using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 战时准星对准带指定 Tag 的 NPC，按 ~ 移除（不返还巧克力；若挂有 NpcPlacementOccupant 会释放占位并减少人数）。
/// </summary>
[DisallowMultipleComponent]
public class NpcRemovalInteract : MonoBehaviour
{
    [Header("检测")]
    [SerializeField] Camera playerCamera;
    [SerializeField] float maxDistance = 12f;
    [Tooltip("默认 Everything 即可；NPC 在 Default 层，一般不用改")]
    [SerializeField] LayerMask targetLayers = ~0;
    [Tooltip("NPC 的检测 Collider 多为 Trigger，需开启才能命中")]
    [SerializeField] bool includeTriggerColliders = true;
    [Tooltip("与 NPC Prefab 根物体 Tag 一致")]
    [SerializeField] string npcTag = "PlacedNPC";

    [Header("输入")]
    [Tooltip("默认 ~ 键")]
    [SerializeField] KeyCode removeKey = KeyCode.BackQuote;

    [Header("提示")]
    [SerializeField] GameObject promptRoot;
    [SerializeField] TMP_Text promptText;
    [SerializeField] string removePrompt = "Remove NPC [~]";

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        EventHintUI.Hide(this, promptRoot);
    }

    void OnDisable()
    {
        EventHintUI.Hide(this, promptRoot);
    }

    void Update()
    {
        FightLevelInputGate.EnsureSubscribed();

        if (!CanRemove())
        {
            HidePrompt();
            return;
        }

        if (!TryGetRemovableNpc(out GameObject npc))
        {
            HidePrompt();
            return;
        }

        ShowPrompt();

        if (Input.GetKeyDown(removeKey) && !IsPointerOverUI())
            NpcPlacementOccupant.TryRemove(npc);
    }

    bool CanRemove()
    {
        if (PlayerInputLock.IsLocked || !FightLevelInputGate.IsInFightLevel)
            return false;

        return playerCamera != null && !string.IsNullOrEmpty(npcTag);
    }

    bool TryGetRemovableNpc(out GameObject npc)
    {
        npc = null;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        QueryTriggerInteraction triggerInteraction = includeTriggerColliders
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, targetLayers, triggerInteraction);

        float closestDistance = float.MaxValue;
        GameObject closest = null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.distance >= closestDistance)
                continue;

            Transform tagged = FindTaggedTransform(hit.collider.transform);
            if (tagged == null)
                continue;

            closestDistance = hit.distance;
            closest = tagged.gameObject;
        }

        npc = closest;
        return npc != null;
    }

    Transform FindTaggedTransform(Transform start)
    {
        for (Transform node = start; node != null; node = node.parent)
        {
            if (node.CompareTag(npcTag))
                return node;
        }

        return null;
    }

    void ShowPrompt()
    {
        EventHintUI.Show(this, promptRoot, promptText, removePrompt);
    }

    void HidePrompt()
    {
        EventHintUI.Hide(this, promptRoot);
    }

    static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
