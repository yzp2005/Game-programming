using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TeleportPortal : MonoBehaviour
{
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string promptMessage = "Teleport [F]";

    [Header("条件（可选）")]
    [Tooltip("需全部存在才能传送；留空则不限制")]
    [SerializeField] private string[] requiredFlags;
    [Tooltip("条件未满足时显示的提示；留空则不显示")]
    [SerializeField] private string lockedPromptMessage;

    [Header("切换场景")]
    [Tooltip("File → Build Settings 里 Scenes In Build 左侧的序号，从 0 开始")]
    [SerializeField] private int targetSceneBuildIndex = 1;
    [Tooltip("与目标场景 PlayerSpawnPoint 的 Spawn Id 一致")]
    [SerializeField] private string targetSpawnId;

    [Header("传送瞬间 Flag（可选）")]
    [Tooltip("按 F 确认传送、加载新场景前 GameEventManager.Set")]
    [SerializeField] private string[] flagsToAddOnTeleport;
    [Tooltip("按 F 确认传送、加载新场景前 GameEventManager.Remove")]
    [SerializeField] private string[] flagsToRemoveOnTeleport;

    [Header("Debug（运行时只读）")]
    [SerializeField] private bool _inside;
    [SerializeField] private bool playerInputLocked;

    Transform _player;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (promptRoot != null)
            promptRoot.SetActive(false);
    }

    void ShowPrompt()
    {
        EventHintUI.Show(this, promptRoot, promptText, promptMessage);
    }

    void HidePrompt()
    {
        EventHintUI.Hide(this, promptRoot);
    }

    void Update()
    {
        playerInputLocked = PlayerInputLock.IsLocked;

        if (!_inside || playerInputLocked)
        {
            HidePrompt();
            return;
        }

        if (!CanTeleport())
        {
            if (string.IsNullOrWhiteSpace(lockedPromptMessage))
                HidePrompt();
            else
                EventHintUI.Show(this, promptRoot, promptText, lockedPromptMessage);
            return;
        }

        ShowPrompt();

        if (!Input.GetKeyDown(KeyCode.F) || _player == null)
            return;

        if (string.IsNullOrEmpty(targetSpawnId))
            return;

        ApplyTeleportFlags();
        SceneLoadRunner.LoadScene(targetSceneBuildIndex, targetSpawnId);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && other.GetComponent<CharacterController>() == null)
            return;

        _inside = true;
        _player = other.transform;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") && other.GetComponent<CharacterController>() == null)
            return;

        _inside = false;
        _player = null;
        HidePrompt();
    }

    bool CanTeleport()
    {
        if (requiredFlags == null || requiredFlags.Length == 0)
            return true;

        foreach (string flag in requiredFlags)
        {
            if (string.IsNullOrWhiteSpace(flag))
                continue;

            if (!GameEventManager.Has(flag.Trim()))
                return false;
        }

        return true;
    }

    void ApplyTeleportFlags()
    {
        if (flagsToAddOnTeleport != null)
        {
            foreach (string flag in flagsToAddOnTeleport)
            {
                if (!string.IsNullOrWhiteSpace(flag))
                    GameEventManager.Set(flag.Trim());
            }
        }

        if (flagsToRemoveOnTeleport != null)
        {
            foreach (string flag in flagsToRemoveOnTeleport)
            {
                if (!string.IsNullOrWhiteSpace(flag))
                    GameEventManager.Remove(flag.Trim());
            }
        }
    }
}
