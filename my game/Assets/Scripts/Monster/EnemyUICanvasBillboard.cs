using UnityEngine;

/// <summary>
/// 挂在敌人 World Space Canvas 上，每帧用 LookAt 让 UI 朝向摄像机。
/// </summary>
[DisallowMultipleComponent]
public class EnemyUICanvasBillboard : MonoBehaviour
{
    [SerializeField] Camera targetCamera;

    [Tooltip("勾选后 LookAt 时保持血条竖直（目标点与 Canvas 同高）")]
    [SerializeField] bool lockYAxis = true;

    [Tooltip("UI 显示反了时勾选")]
    [SerializeField] bool flipFacing = false;

    Transform camTransform;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        camTransform = targetCamera != null ? targetCamera.transform : null;
    }

    void LateUpdate()
    {
        if (camTransform == null)
            return;

        Vector3 lookTarget = camTransform.position;

        if (lockYAxis)
            lookTarget.y = transform.position.y;

        transform.LookAt(lookTarget, Vector3.up);

        if (flipFacing)
            transform.Rotate(0f, 180f, 0f);
    }
}
