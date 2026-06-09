using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public Transform player;

    // 鼠标旋转速度
    public float mouseSensitivity = 2f;

    // 相机距离
    public float distance = 4f;
    public float minDistance = 2f;
    public float maxDistance = 8f;
    public float scrollSpeed = 10f;

    // 上下角度限制
    public float minPitch = -20f;
    public float maxPitch = 60f;

    // ===== 初始视角偏差（你要的公开变量）=====
    [Header("初始镜头角度")]
    public float startYaw = 0f;     // 初始左右角度
    public float startPitch = 25f;  // 初始上下角度

    // 相机看向人物的高度偏移
    [Header("镜头位置偏移")]
    public float cameraHeight = 1.5f;   // 相机高度
    public float lookAtHeight = 1.2f;   // 相机看的高度

    // 运行时角度
    private float yaw;
    private float pitch;

    void Start()
    {
        // 一开始就设置初始角度
        yaw = startYaw;
        pitch = startPitch;
    }

    void LateUpdate()
    {
        // 鼠标控制旋转
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 滚轮缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * scrollSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // 计算旋转
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        // 相机目标位置（高度用公开变量）
        Vector3 targetPos = player.position + rotation * new Vector3(0, cameraHeight, -distance);

        // 平滑移动
        transform.position = Vector3.Lerp(transform.position, targetPos, 8f * Time.deltaTime);

        // 看向玩家（高度可调）
        transform.LookAt(player.position + Vector3.up * lookAtHeight);
    }
}