using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TeleportPortal : MonoBehaviour
{
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string promptMessage = "Teleport [F]";
    [SerializeField] private Transform destination;

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

        if (!Input.GetKeyDown(KeyCode.F) || destination == null || _player == null)
            return;

        TeleportPlayer();
    }

    void TeleportPlayer()
    {
        CharacterController cc = _player.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        _player.position = destination.position;

        if (cc != null)
            cc.enabled = true;

        _inside = false;
        _player = null;
        HidePrompt();
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
