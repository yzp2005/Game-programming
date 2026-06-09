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
    private bool rmbReleasedSinceAir = true;

    public bool IsAimInputActive { get; private set; }

    bool CanEnterCombat =>
        characterController.isGrounded
        && !NpcPlacementController.IsActive
        && rmbReleasedSinceAir;

    // ==================== 法球设置 ====================
    [Header("法球设置")]
    public GameObject firePoint;
    public GameObject[] projectilePrefabs;
    [SerializeField] private float aimMaxDistance = 1000f;
    [SerializeField] private float minAimDistance = 3f;
    [SerializeField] private LayerMask aimLayers = ~0;
    private int currentProjectileIndex;
    private Camera aimCamera;

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

        aimCamera = Camera.main;
    }

    void Update()
    {
        if (NpcPlacementController.IsActive)
            ResetAttackState();

        UpdateRmbAirRule();
        IsAimInputActive = CanEnterCombat && Input.GetMouseButton(1);

        HandleMovement();
        HandleAttack();
        HandleAnimation();
    }

    // ==================== 移动逻辑 ====================
    void HandleMovement()
    {
        // 瞄准模式：只能在地面转向，不能移动（空中按住右键落地后须先松开）
        if (IsAimInputActive)
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
        if (NpcPlacementController.IsActive)
            return;

        if (Input.GetMouseButtonUp(1))
        {
            isHoldingAttack = false;
            if (animator != null)
                animator.speed = 1f;
        }

        if (!CanEnterCombat)
            return;

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
    }

    void UpdateRmbAirRule()
    {
        if (!characterController.isGrounded)
        {
            if (Input.GetMouseButton(1))
                rmbReleasedSinceAir = false;

            ResetAttackState(true);
            return;
        }

        if (Input.GetMouseButtonUp(1) || !Input.GetMouseButton(1))
            rmbReleasedSinceAir = true;
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
        else if (isHoldingAttack && CanEnterCombat)
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
        bool upperAttack = isHoldingAttack && CanEnterCombat;
        animator.SetInteger(UpperAnimParam, upperAttack ? UpperAttackAnimValue : 0);
    }

    void ResetAttackState(bool force = false)
    {
        if (!force && !isHoldingAttack && (animator == null || animator.speed >= 1f))
            return;

        isHoldingAttack = false;
        if (animator == null)
            return;

        animator.speed = 1f;
        animator.SetInteger(UpperAnimParam, 0);
    }

    // 动画事件：攻击动作到达蓄力帧时暂停
    public void OnAttackHoldPoint()
    {
        if (isHoldingAttack && CanEnterCombat && animator != null)
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
        if (aimCamera == null)
            aimCamera = Camera.main;
        if (aimCamera == null) return;

        Vector3 spawnPosition = firePoint.transform.position;
        Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = GetAimPoint(aimRay, spawnPosition);

        Vector3 direction = targetPoint - spawnPosition;
        if (direction.sqrMagnitude < 0.0001f)
            direction = aimRay.direction;
        Quaternion spawnRotation = Quaternion.LookRotation(direction.normalized);

        GameObject projectile = Instantiate(
            projectilePrefabs[currentProjectileIndex],
            spawnPosition,
            spawnRotation) as GameObject;

        if (projectile.TryGetComponent(out ProjectileMover mover))
            mover.targetPoint = targetPoint;

        if (camAnim != null)
            camAnim.Play(camAnim.clip.name);
    }

    Vector3 GetAimPoint(Ray ray, Vector3 spawnPosition)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            ray, aimMaxDistance, aimLayers, QueryTriggerInteraction.Ignore);

        float closestDistance = float.MaxValue;
        bool foundHit = false;
        Vector3 hitPoint = default;

        foreach (RaycastHit hit in hits)
        {
            if (!IsValidAimTarget(hit.collider) || hit.distance >= closestDistance)
                continue;

            closestDistance = hit.distance;
            hitPoint = hit.point;
            foundHit = true;
        }

        if (foundHit && (hitPoint - spawnPosition).sqrMagnitude >= minAimDistance * minAimDistance)
            return hitPoint;

        return spawnPosition + ray.direction * aimMaxDistance;
    }

    bool IsValidAimTarget(Collider collider)
    {
        if (collider == null)
            return false;

        Transform hitTransform = collider.transform;
        return hitTransform != transform && !hitTransform.IsChildOf(transform);
    }
}
