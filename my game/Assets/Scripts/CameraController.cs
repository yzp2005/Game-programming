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

    private float yaw = 0f;
    private float pitch = 0f;
    private Vector3 currentOffset;
    private CharacterController _characterController;
    private bool _hasRightButtonBeenReleasedSinceAir = true;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        currentOffset = offset;

        if (target != null)
        {
            _characterController = target.GetComponent<CharacterController>();
        }
    }

    void Update()
    {
        // 只在按住左键 或 右键时 允许旋转视角
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * mouseSensitivity;
            pitch -= mouseY * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // 删掉你原来乱切换鼠标显隐的代码，不要手动改
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool isJumping = _characterController != null && !_characterController.isGrounded;

        if (isJumping)
        {
            if (Input.GetMouseButton(1))
            {
                _hasRightButtonBeenReleasedSinceAir = false;
            }
        }
        else
        {
            if (Input.GetMouseButtonUp(1))
            {
                _hasRightButtonBeenReleasedSinceAir = true;
            }
            // 与 CharactorController 一致：空中松开后落地不会收到 MouseButtonUp，需在地上且未按右键时恢复
            if (!Input.GetMouseButton(1))
            {
                _hasRightButtonBeenReleasedSinceAir = true;
            }
        }

        bool canZoom = !isJumping && Input.GetMouseButton(1) && _hasRightButtonBeenReleasedSinceAir;
        Vector3 targetOffset = canZoom ? zoomOffset : offset;
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, zoomSmooth * Time.deltaTime);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 position = target.position + rotation * currentOffset;

        transform.rotation = rotation;
        transform.position = position;
    }
}