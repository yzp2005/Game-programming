using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TeleportPortal : MonoBehaviour
{
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string promptMessage = "Teleport [F]";

    [Header("切换场景")]
    [Tooltip("File → Build Settings 里 Scenes In Build 左侧的序号，从 0 开始")]
    [SerializeField] private int targetSceneBuildIndex = 1;
    [Tooltip("与目标场景 PlayerSpawnPoint 的 Spawn Id 一致")]
    [SerializeField] private string targetSpawnId;

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

        ShowPrompt();

        if (!Input.GetKeyDown(KeyCode.F) || _player == null)
            return;

        if (string.IsNullOrEmpty(targetSpawnId))
            return;

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
}
