using UnityEngine;

[DefaultExecutionOrder(-100)]
public class CameraController : MonoBehaviour
{
    public Transform target;
    public float mouseSensitivity = 3f;
    public Vector3 offset = new Vector3(0f, 2f, -5f);
    public Vector3 zoomOffset = new Vector3(0f, 1f, -2f);
    public float minPitch = -30f;
    public float maxPitch = 60f;
    public float zoomSmooth = 5f;

    [Header("室内镜头")]
    [SerializeField] private Vector3 indoorOffset = new Vector3(0f, 1.8f, -2.5f);
    [SerializeField] private float indoorBlendSpeed = 4f;

    private float yaw = 0f;
    private float pitch = 0f;
    private Vector3 currentOffset;
    private int indoorZoneCount;
    private CharacterController _characterController;
    private bool _hasRightButtonBeenReleasedSinceAir = true;

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
        pitch = transform.eulerAngles.x;
        currentOffset = offset;

        if (target != null)
            _characterController = target.GetComponent<CharacterController>();
    }

    void Update()
    {
        if (PlayerInputLock.IsLocked)
            return;

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * mouseSensitivity;
            pitch -= mouseY * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 baseOffset = IsIndoor ? indoorOffset : offset;
        float blendSpeed = IsIndoor ? indoorBlendSpeed : zoomSmooth;

        if (!PlayerInputLock.IsLocked)
        {
            bool isJumping = _characterController != null && !_characterController.isGrounded;

            if (isJumping)
            {
                if (Input.GetMouseButton(1))
                    _hasRightButtonBeenReleasedSinceAir = false;
            }
            else
            {
                if (Input.GetMouseButtonUp(1))
                    _hasRightButtonBeenReleasedSinceAir = true;
                if (!Input.GetMouseButton(1))
                    _hasRightButtonBeenReleasedSinceAir = true;
            }

            bool canZoom = !isJumping && Input.GetMouseButton(1) && _hasRightButtonBeenReleasedSinceAir;
            Vector3 targetOffset = canZoom ? zoomOffset : baseOffset;
            currentOffset = Vector3.Lerp(currentOffset, targetOffset, blendSpeed * Time.deltaTime);
        }
        else
        {
            currentOffset = Vector3.Lerp(currentOffset, baseOffset, blendSpeed * Time.deltaTime);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        transform.rotation = rotation;
        transform.position = target.position + rotation * currentOffset;
    }
}
