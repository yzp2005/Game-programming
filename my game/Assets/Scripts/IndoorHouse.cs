using UnityEngine;

/// <summary>
/// 挂在房子上，加 Box Collider 勾 Is Trigger 盖住室内。玩家进入后相机拉近。
/// </summary>
[RequireComponent(typeof(Collider))]
public class IndoorHouse : MonoBehaviour
{
    [SerializeField] private CameraController cameraController;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (cameraController == null)
            cameraController = FindObjectOfType<CameraController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other) || cameraController == null)
            return;

        cameraController.EnterIndoorZone();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other) || cameraController == null)
            return;

        cameraController.ExitIndoorZone();
    }

    static bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
            return true;

        return other.GetComponent<CharacterController>() != null
            || other.GetComponent<CharactorController>() != null;
    }
}
