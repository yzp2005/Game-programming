using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)]
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
    private bool _pendingProjectileFire;
    /// <summary>与镜头逻辑一致：曾在空中按住右键则落地后须先松一次右键，才进入站桩瞄准、开火等；未满足时仍全速移动。</summary>
    private bool _hasRMBReleasedSinceAir = true;

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
        UpdateRmbReleasedSinceAirFlag();
        HandleMovement();
        HandleAttack();
        HandleAnimation();
    }

    void UpdateRmbReleasedSinceAirFlag()
    {
        bool inAir = !characterController.isGrounded;
        if (inAir)
        {
            if (Input.GetMouseButton(1))
                _hasRMBReleasedSinceAir = false;
        }
        else
        {
            if (Input.GetMouseButtonUp(1))
                _hasRMBReleasedSinceAir = true;
            // 在空中已松开右键时，落地那一帧不会再次触发 MouseButtonUp，否则会永远锁住
            if (!Input.GetMouseButton(1))
                _hasRMBReleasedSinceAir = true;
        }
    }

    // ==================== 移动逻辑 ====================
    void HandleMovement()
    {
        // 跳跃中 → 完全禁用右键功能，直接跳过瞄准逻辑
        if (!characterController.isGrounded)
        {
            isHoldingAttack = false;
            if (animator != null)
            {
                animator.speed = 1f;
                animator.SetInteger(UpperAnimParam, 0);
            }
        }

        // 站桩瞄准：不走路径移动（无边走边施法）；起跳后一直按住右键落地则 _hasRMBReleasedSinceAir 为 false，不会进这段，可全速移动
        if (Input.GetMouseButton(1) && characterController.isGrounded && _hasRMBReleasedSinceAir)
        {
            if (Camera.main == null) return;

            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0;
            if (camForward.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(camForward.normalized, Vector3.up);
                if (Input.GetMouseButtonDown(1))
                    transform.rotation = targetRotation;
                else
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            bool g = characterController.isGrounded;
            if (g && verticalVelocity < 0f)
                verticalVelocity = -2f;
            if (g && Input.GetButtonDown("Jump"))
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * Time.deltaTime;
            characterController.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
            return;
        }

        if (Camera.main == null) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * vertical + cameraRight * horizontal;
        bool isMoving = moveDirection.sqrMagnitude > 0.0001f;

        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        bool isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (isGrounded && Input.GetButtonDown("Jump"))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 motion = moveDirection * moveSpeed;
        motion.y = verticalVelocity;
        characterController.Move(motion * Time.deltaTime);
    }

    // ==================== 攻击逻辑 ====================
    void HandleAttack()
    {
        // 跳跃中 → 禁用所有攻击相关操作
        if (!characterController.isGrounded)
        {
            isHoldingAttack = false;
            return;
        }

        // 按住右键 + 点击左键：攻击（实际发射推迟到 LateUpdate，使射线与 CameraController.LateUpdate 拉近后的相机一致）
        if (Input.GetMouseButton(1) && Input.GetMouseButtonDown(0) && _hasRMBReleasedSinceAir)
        {
            _pendingProjectileFire = true;
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
        // 跳跃时强制关闭上半身攻击动画
        animator.SetInteger(UpperAnimParam, !characterController.isGrounded ? 0 : (isHoldingAttack ? UpperAttackAnimValue : 0));
    }

    void LateUpdate()
    {
        if (!_pendingProjectileFire) return;

        _pendingProjectileFire = false;

        if (!characterController.isGrounded) return;
        if (!Input.GetMouseButton(1)) return;
        if (!_hasRMBReleasedSinceAir) return;

        FireProjectile();
    }

    // 动画事件：攻击动作到达蓄力帧时暂停
    public void OnAttackHoldPoint()
    {
        // 跳跃中不暂停动画
        if (isHoldingAttack && animator != null && characterController.isGrounded)
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

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = GetAimWorldPoint(ray, 1000f);

        Vector3 origin = firePoint.transform.position;
        Vector3 toTarget = targetPoint - origin;
        // 射线先打到自己身上的碰撞体时，目标点会贴在身体附近，方向会横甩；过近则退回用准星射线方向
        Vector3 direction;
        if (toTarget.sqrMagnitude < 0.01f)
            direction = ray.direction.normalized;
        else
            direction = toTarget.normalized;

        if (direction.sqrMagnitude < 1e-6f)
            direction = ray.direction.normalized;

        Instantiate(projectilePrefabs[currentProjectileIndex], firePoint.transform.position, Quaternion.LookRotation(direction));
    }

    /// <summary>
    /// 视口中心射线：跳过角色自身碰撞体，否则经常会打在胸口/武器上，子弹朝向会偏离准星甚至朝屏幕外。
    /// </summary>
    Vector3 GetAimWorldPoint(Ray ray, float maxDistance)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
        if (hits == null || hits.Length == 0)
            return ray.GetPoint(maxDistance);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (IsUnderSameCharacter(hit.collider.transform))
                continue;
            return hit.point;
        }

        return ray.GetPoint(maxDistance);
    }

    bool IsUnderSameCharacter(Transform t)
    {
        return t == transform || t.IsChildOf(transform);
    }
}