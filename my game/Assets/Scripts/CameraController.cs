using UnityEngine;

[DefaultExecutionOrder(200)]
public class CameraController : MonoBehaviour
{
    public Transform target;
    public float mouseSensitivity = 3f;
    public Vector3 offset = new Vector3(0f, 3f, -5.5f);
    public Vector3 zoomOffset = new Vector3(0f, 2.5f, -2.5f);
    public float minPitch = 18f;
    public float maxPitch = 42f;
    public float zoomSmooth = 5f;

    [Header("室内镜头")]
    [SerializeField] private Vector3 indoorOffset = new Vector3(0f, 2.8f, -2.5f);
    [SerializeField] private float indoorBlendSpeed = 4f;

    private float yaw;
    private float pitch;
    private Vector3 currentOffset;
    private int indoorZoneCount;
    private CharacterController characterController;
    private bool rmbReleasedSinceAir = true;

    public bool IsIndoor => indoorZoneCount > 0;

    public void EnterIndoorZone() => indoorZoneCount++;

    public void ExitIndoorZone() => indoorZoneCount = Mathf.Max(0, indoorZoneCount - 1);

    void Start()
    {
        if (!PlayerInputLock.IsLocked)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        yaw = transform.eulerAngles.y;
        pitch = NormalizePitch(transform.eulerAngles.x);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        currentOffset = offset;

        if (target != null)
            characterController = target.GetComponent<CharacterController>();
    }

    void Update()
    {
        if (PlayerInputLock.IsLocked || NpcPlacementController.IsActive)
            return;

        if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        bool airborne = characterController != null && !characterController.isGrounded;
        Vector3 baseOffset = IsIndoor ? indoorOffset : offset;
        float blendSpeed = IsIndoor ? indoorBlendSpeed : zoomSmooth;

        if (!PlayerInputLock.IsLocked)
        {
            UpdateRmbAirRule(airborne);
            bool canZoom = !airborne
                && Input.GetMouseButton(1)
                && rmbReleasedSinceAir
                && !NpcPlacementController.IsActive;
            currentOffset = Vector3.Lerp(currentOffset, canZoom ? zoomOffset : baseOffset, blendSpeed * Time.deltaTime);
        }
        else
        {
            currentOffset = Vector3.Lerp(currentOffset, baseOffset, blendSpeed * Time.deltaTime);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        transform.rotation = rotation;
        transform.position = target.position + rotation * currentOffset;
    }

    void UpdateRmbAirRule(bool airborne)
    {
        if (airborne)
        {
            if (Input.GetMouseButton(1))
                rmbReleasedSinceAir = false;
            return;
        }

        if (Input.GetMouseButtonUp(1) || !Input.GetMouseButton(1))
            rmbReleasedSinceAir = true;
    }

    static float NormalizePitch(float eulerX)
    {
        if (eulerX > 180f)
            eulerX -= 360f;
        return eulerX;
    }
}
