using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharactorController : MonoBehaviour
{
    static readonly int AnimParam = Animator.StringToHash("animation");
    static readonly int UpperAnimParam = Animator.StringToHash("upperanimation");

    const int IdleAnim = 1;
    const int MoveAnim = 10;
    const int JumpAnim = 13;
    const int UpperAttackAnim = 1;

    CharacterController characterController;
    Animator animator;
    Camera aimCamera;
    float verticalVelocity;

    bool isHoldingAttack;
    bool rmbReleasedSinceAir = true;

    [Header("移动")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float jumpHeight = 1.5f;
    [SerializeField] float gravity = -20f;

    [Header("攻击")]
    [SerializeField] Animation camAnim;

    [Header("法球")]
    public GameObject firePoint;
    public GameObject[] projectilePrefabs;
    [SerializeField] float aimMaxDistance = 1000f;
    [SerializeField] float minAimDistance = 3f;
    [SerializeField] LayerMask aimLayers = ~0;
    int currentProjectileIndex;

    public bool IsAimInputActive { get; private set; }

    bool CanEnterCombat =>
        characterController.isGrounded
        && !NpcPlacementController.IsActive
        && rmbReleasedSinceAir;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        aimCamera = Camera.main;

        if (animator != null)
        {
            animator.applyRootMotion = false;
            ApplyAnimation(forceIdle: true);
        }
    }

    void Update()
    {
        if (NpcPlacementController.IsActive)
            ResetAttackState();

        if (PlayerInputLock.IsLocked)
        {
            IsAimInputActive = false;
            ApplyVerticalMotionOnly();
            ApplyAnimation(forceIdle: true);
            return;
        }

        UpdateRmbAirRule();
        IsAimInputActive = CanEnterCombat && Input.GetMouseButton(1);

        HandleMovement();
        HandleAttack();
        ApplyAnimation();
    }

    void HandleMovement()
    {
        if (IsAimInputActive)
        {
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(camForward, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * vertical + cameraRight * horizontal;
        bool isMoving = moveDirection.sqrMagnitude > 0.0001f;
        bool isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (isGrounded && Input.GetButtonDown("Jump"))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 motion = moveDirection * moveSpeed;
        motion.y = verticalVelocity;
        characterController.Move(motion * Time.deltaTime);
    }

    void HandleAttack()
    {
        if (NpcPlacementController.IsActive)
            return;

        if (Input.GetMouseButtonUp(1))
            ResetAttackState(true);

        if (!CanEnterCombat)
            return;

        if (Input.GetMouseButton(1) && Input.GetMouseButtonDown(0))
            FireProjectile();

        if (Input.GetMouseButtonDown(1))
            isHoldingAttack = true;
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

    void ApplyVerticalMotionOnly()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        characterController.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
    }

    void ApplyAnimation(bool forceIdle = false)
    {
        if (animator == null)
            return;

        if (forceIdle)
            ResetAttackState(true);

        animator.SetInteger(AnimParam, ResolveBodyAnim(forceIdle));
        bool upperAttack = !forceIdle && isHoldingAttack && CanEnterCombat;
        animator.SetInteger(UpperAnimParam, upperAttack ? UpperAttackAnim : 0);
    }

    int ResolveBodyAnim(bool forceIdle)
    {
        if (forceIdle)
            return IdleAnim;

        if (!characterController.isGrounded)
            return JumpAnim;

        if (isHoldingAttack && CanEnterCombat)
            return IdleAnim;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        return Mathf.Abs(horizontal) + Mathf.Abs(vertical) > 0.0001f ? MoveAnim : IdleAnim;
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

    public void OnAttackHoldPoint()
    {
        if (isHoldingAttack && CanEnterCombat && animator != null)
            animator.speed = 0f;
    }

    public void OnAttackFinished()
    {
        if (animator != null)
            animator.SetInteger(UpperAnimParam, 0);
    }

    void FireProjectile()
    {
        if (firePoint == null || projectilePrefabs == null || projectilePrefabs.Length == 0)
            return;

        if (aimCamera == null)
            aimCamera = Camera.main;
        if (aimCamera == null)
            return;

        Vector3 spawnPosition = firePoint.transform.position;
        Ray aimRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = GetAimPoint(aimRay, spawnPosition);

        Vector3 direction = targetPoint - spawnPosition;
        if (direction.sqrMagnitude < 0.0001f)
            direction = aimRay.direction;

        GameObject projectile = Instantiate(
            projectilePrefabs[currentProjectileIndex],
            spawnPosition,
            Quaternion.LookRotation(direction.normalized));

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
