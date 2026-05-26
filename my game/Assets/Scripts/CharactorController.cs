using UnityEngine;

/// <summary>
/// 第三人称角色控制：移动、跳跃、右键瞄准、法球攻击。
/// 与 CameraController / MagicCircleFade 共用「空中按住右键须落地后松开一次」逻辑。
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(CharacterController))]
public class CharactorController : MonoBehaviour
{
    #region Constants

    private const int IdleAnimValue = 1;
    private const int MoveAnimValue = 10;
    private const int JumpAnimValue = 13;
    private const int UpperAttackAnimValue = 1;

    private const float MoveInputThreshold = 0.0001f;
    private const float AimDirectionMinSqr = 0.01f;
    private const float GroundedStickVelocity = -2f;

    private static readonly int AnimParam = Animator.StringToHash("animation");
    private static readonly int UpperAnimParam = Animator.StringToHash("upperanimation");

    #endregion

    #region Serialized Fields

    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("攻击设置")]
    [SerializeField] private Animation camAnim;

    [Header("法球设置")]
    public GameObject firePoint;
    public GameObject[] projectilePrefabs;
    [SerializeField] private int currentProjectileIndex;

    #endregion

    #region Private State

    private CharacterController characterController;
    private Animator animator;
    private float verticalVelocity;

    private bool isHoldingAttack;
    private bool pendingProjectileFire;
    /// <summary>曾在空中按住右键则落地后须先松一次，才进入站桩瞄准/开火。</summary>
    private bool hasRmbReleasedSinceAir = true;

    #endregion

    #region Properties

    private bool IsGrounded => characterController.isGrounded;
    private bool IsInAir => !IsGrounded;
    private bool CanGroundAim => IsGrounded && hasRmbReleasedSinceAir;

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        PlayerInputLock.OnLockChanged += OnPlayerInputLockChanged;
    }

    private void OnDisable()
    {
        PlayerInputLock.OnLockChanged -= OnPlayerInputLockChanged;
    }

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetInteger(AnimParam, IdleAnimValue);
        }

        if (PlayerInputLock.IsLocked)
            EnterDialogueStance();
    }

    private void Update()
    {
        if (PlayerInputLock.IsLocked)
            return;

        UpdateRmbReleasedSinceAirFlag();
        HandleMovement();
        HandleAttack();
        HandleAnimation();
    }

    private void LateUpdate()
    {
        if (PlayerInputLock.IsLocked || !pendingProjectileFire)
            return;

        pendingProjectileFire = false;

        if (!CanGroundAim || !Input.GetMouseButton(1))
            return;

        FireProjectile();
    }

    #endregion

    #region Input Lock

    private void OnPlayerInputLockChanged(bool locked)
    {
        if (locked)
            EnterDialogueStance();
    }

    private void EnterDialogueStance()
    {
        isHoldingAttack = false;
        pendingProjectileFire = false;

        if (animator == null)
            return;

        animator.speed = 1f;
        animator.SetInteger(AnimParam, IdleAnimValue);
        animator.SetInteger(UpperAnimParam, 0);
    }

    #endregion

    #region Right-Click Air Rule

    private void UpdateRmbReleasedSinceAirFlag()
    {
        if (IsInAir)
        {
            if (Input.GetMouseButton(1))
                hasRmbReleasedSinceAir = false;
            return;
        }

        if (Input.GetMouseButtonUp(1))
            hasRmbReleasedSinceAir = true;

        // 空中已松开右键时，落地不会收到 MouseButtonUp，需在地上且未按右键时恢复
        if (!Input.GetMouseButton(1))
            hasRmbReleasedSinceAir = true;
    }

    #endregion

    #region Movement

    private void HandleMovement()
    {
        if (IsInAir)
            ClearUpperBodyAttackState();

        if (Input.GetMouseButton(1) && CanGroundAim)
        {
            HandleGroundedAimMovement();
            return;
        }

        HandleLocomotion();
    }

    /// <summary>站桩瞄准：只转向与垂直位移，不走路面移动。</summary>
    private void HandleGroundedAimMovement()
    {
        if (Camera.main == null)
            return;

        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude > MoveInputThreshold)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camForward.normalized, Vector3.up);
            if (Input.GetMouseButtonDown(1))
                transform.rotation = targetRotation;
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        ApplyVerticalPhysics(allowJump: true);
        characterController.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
    }

    private void HandleLocomotion()
    {
        if (Camera.main == null)
            return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        GetCameraPlanarAxes(out Vector3 cameraForward, out Vector3 cameraRight);
        Vector3 moveDirection = cameraForward * vertical + cameraRight * horizontal;

        if (moveDirection.sqrMagnitude > MoveInputThreshold)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        ApplyVerticalPhysics(allowJump: true);

        Vector3 motion = moveDirection * moveSpeed;
        motion.y = verticalVelocity;
        characterController.Move(motion * Time.deltaTime);
    }

    private void ApplyVerticalPhysics(bool allowJump)
    {
        if (IsGrounded && verticalVelocity < 0f)
            verticalVelocity = GroundedStickVelocity;

        if (allowJump && IsGrounded && Input.GetButtonDown("Jump"))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void GetCameraPlanarAxes(out Vector3 forward, out Vector3 right)
    {
        forward = Camera.main.transform.forward;
        right = Camera.main.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
    }

    #endregion

    #region Attack

    private void HandleAttack()
    {
        if (IsInAir)
        {
            isHoldingAttack = false;
            return;
        }

        if (Input.GetMouseButton(1) && Input.GetMouseButtonDown(0) && hasRmbReleasedSinceAir)
            pendingProjectileFire = true;

        if (Input.GetMouseButtonDown(1))
            isHoldingAttack = true;

        if (Input.GetMouseButtonUp(1))
        {
            isHoldingAttack = false;
            if (animator != null)
                animator.speed = 1f;
        }
    }

    private void ClearUpperBodyAttackState()
    {
        isHoldingAttack = false;
        if (animator == null)
            return;

        animator.speed = 1f;
        animator.SetInteger(UpperAnimParam, 0);
    }

    #endregion

    #region Animation

    private void HandleAnimation()
    {
        if (animator == null)
            return;

        int bodyAnim = GetBodyAnimValue();
        int upperAnim = IsInAir ? 0 : (isHoldingAttack ? UpperAttackAnimValue : 0);

        animator.SetInteger(AnimParam, bodyAnim);
        animator.SetInteger(UpperAnimParam, upperAnim);
    }

    private int GetBodyAnimValue()
    {
        if (IsInAir)
            return JumpAnimValue;

        if (isHoldingAttack)
            return IdleAnimValue;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool isMoving = Mathf.Abs(horizontal) + Mathf.Abs(vertical) > MoveInputThreshold;
        return isMoving ? MoveAnimValue : IdleAnimValue;
    }

    #endregion

    #region Projectile

    private void FireProjectile()
    {
        if (firePoint == null || projectilePrefabs == null || projectilePrefabs.Length == 0 || Camera.main == null)
            return;

        if (camAnim != null)
            camAnim.Play(camAnim.clip.name);

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = GetAimWorldPoint(ray, 1000f);
        Vector3 direction = GetFireDirection(ray, targetPoint);

        GameObject prefab = projectilePrefabs[currentProjectileIndex];
        Instantiate(prefab, firePoint.transform.position, Quaternion.LookRotation(direction));
    }

    private Vector3 GetFireDirection(Ray ray, Vector3 targetPoint)
    {
        Vector3 toTarget = targetPoint - firePoint.transform.position;

        if (toTarget.sqrMagnitude < AimDirectionMinSqr)
            return ray.direction.normalized;

        Vector3 direction = toTarget.normalized;
        return direction.sqrMagnitude < 1e-6f ? ray.direction.normalized : direction;
    }

    /// <summary>视口中心射线，跳过角色自身碰撞体。</summary>
    private Vector3 GetAimWorldPoint(Ray ray, float maxDistance)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
        if (hits == null || hits.Length == 0)
            return ray.GetPoint(maxDistance);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || IsUnderSameCharacter(hit.collider.transform))
                continue;
            return hit.point;
        }

        return ray.GetPoint(maxDistance);
    }

    private bool IsUnderSameCharacter(Transform t)
    {
        return t == transform || t.IsChildOf(transform);
    }

    #endregion

    #region Animation Events

    public void OnAttackHoldPoint()
    {
        if (isHoldingAttack && animator != null && IsGrounded)
            animator.speed = 0f;
    }

    public void OnAttackFinished()
    {
        if (animator != null)
            animator.SetInteger(UpperAnimParam, 0);
    }

    #endregion
}
