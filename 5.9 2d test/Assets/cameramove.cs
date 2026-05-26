using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target; // 把你的飞机拖到这里

    [Header("跟随参数")]
    public float smoothSpeed = 0.125f; // 数值越小越丝滑
    public Vector3 offset = new Vector3(0, 0, -10); // 相机和飞机的偏移

    void LateUpdate()
    {
        // 飞机先移动，相机再跟随，避免卡顿
        Vector3 desiredPos = target.position + offset;
        Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);
        transform.position = smoothedPos;
    }
}