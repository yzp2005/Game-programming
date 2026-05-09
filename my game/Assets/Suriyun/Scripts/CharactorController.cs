using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharactorController : MonoBehaviour
{
    // ==================== 基础组件 ====================
    private CharacterController characterController;
    private Animator animator;
    private float verticalVelocity;

    // ==================== 动画参数 ====================
    private const int IdleAnimValue = 1;
    private const int MoveAnimValue = 10;
    private const int JumpAnimValue = 13;
    private const int UpperAttackAnimValue = 1;

    private static readonly int AnimParam = Animator.StringToHash("animation");
    private static readonly int UpperAnimParam = Animator.StringToHash("upperanimation");

    // ==================== 移动设置 ====================
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    // ==================== 攻击设置 ====================
    [Header("攻击设置")]
    [SerializeField] private Animation camAnim;
    private bool isHoldingAttack;

    // ==================== 法球设置 ====================
    [Header("法球设置")]
    public GameObject firePoint;
    public GameObject[] projectilePrefabs;
    private int currentProjectileIndex;

    // ==================== 生命周期 ====================
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetInteger(AnimParam, IdleAnimValue);
        }
    }

    void Update()
    {
        HandleMovement();
        HandleAttack();
        HandleAnimation();
    }

    // ==================== 移动逻辑 ====================
    void HandleMovement()
    {
        // 瞄准模式：只能转向，不能移动
        if (Input.GetMouseButton(1))
        {
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0;
            if (camForward.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(camForward, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            return;
        }

        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 计算移动方向（基于相机朝向）
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * vertical + cameraRight * horizontal;
        bool isMoving = moveDirection.sqrMagnitude > 0.0001f;
        bool isGrounded = characterController.isGrounded;

        // 重力与跳跃
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 移动时转向
        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 应用移动
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 motion = moveDirection * moveSpeed;
        motion.y = verticalVelocity;
        characterController.Move(motion * Time.deltaTime);
    }

    // ==================== 攻击逻辑 ====================
    void HandleAttack()
    {
        // 按住右键 + 点击左键：攻击
        if (Input.GetMouseButton(1) && Input.GetMouseButtonDown(0))
        {
            FireProjectile();
        }

        // 按住右键：播放攻击动画并暂停在蓄力帧
        if (Input.GetMouseButtonDown(1))
        {
            isHoldingAttack = true;
        }

        // 松开右键：恢复动画速度
        if (Input.GetMouseButtonUp(1))
        {
            isHoldingAttack = false;
            animator.speed = 1f;
        }
    }

    void HandleAnimation()
    {
        if (animator == null) return;

        // 下肢动画
        int bodyAnimValue;
        if (!characterController.isGrounded)
        {
            bodyAnimValue = JumpAnimValue;
        }
        else if (isHoldingAttack)
        {
            bodyAnimValue = IdleAnimValue;
        }
        else
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            bool isMoving = Mathf.Abs(horizontal) + Mathf.Abs(vertical) > 0.0001f;
            bodyAnimValue = isMoving ? MoveAnimValue : IdleAnimValue;
        }

        animator.SetInteger(AnimParam, bodyAnimValue);
        animator.SetInteger(UpperAnimParam, isHoldingAttack ? UpperAttackAnimValue : 0);
    }

    // 动画事件：攻击动作到达蓄力帧时暂停
    public void OnAttackHoldPoint()
    {
        if (isHoldingAttack && animator != null)
        {
            animator.speed = 0f;
        }
    }

    // 动画事件：攻击动画播放完毕时重置状态
    public void OnAttackFinished()
    {
        animator.SetInteger(UpperAnimParam, 0);
    }

    void FireProjectile()
    {
        if (firePoint == null || projectilePrefabs == null || projectilePrefabs.Length == 0) return;
        if (Camera.main == null) return;

        // 相机震动
        if (camAnim != null)
        {
            camAnim.Play(camAnim.clip.name);
        }

        // 从相机中心发射射线，检测与障碍物的交点
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(1000f);
        }

        // 计算发射方向（从FirePoint指向目标点）
        Vector3 direction = (targetPoint - firePoint.transform.position).normalized;

        // 生成法球，朝目标点方向发射
        Instantiate(projectilePrefabs[currentProjectileIndex], firePoint.transform.position, Quaternion.LookRotation(direction));
    }
}
