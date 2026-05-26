using UnityEngine;

/// <summary>需在角色 CharacterController 更新之后再判断着地（CharactorController 为 100）。</summary>
[DefaultExecutionOrder(200)]
public class MagicCircleFade : MonoBehaviour
{
    [Header("角色")]
    [Tooltip("用于判断是否在地面。留空则自动查找 Tag 为 Player 的对象上的 CharacterController。")]
    [SerializeField] private CharacterController playerCharacter;

    [Header("淡入淡出速度")]
    public float fadeSpeed = 8f;

    private Renderer _renderer;
    private Material _mat;
    private Color _originalColor;
    private bool _hasRightButtonBeenReleasedSinceAir = true;
    private bool _loggedMissingCharacter;

    void OnEnable()
    {
        PlayerInputLock.OnLockChanged += OnPlayerInputLockChanged;
    }

    void OnDisable()
    {
        PlayerInputLock.OnLockChanged -= OnPlayerInputLockChanged;
    }

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        _mat = _renderer.material;
        _originalColor = _mat.color;
        _mat.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0);

        if (playerCharacter == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerCharacter = player.GetComponent<CharacterController>();
        }

        if (playerCharacter == null && !_loggedMissingCharacter)
        {
            _loggedMissingCharacter = true;
            Debug.LogWarning("[MagicCircleFade] 未找到 CharacterController，无法判断滞空，魔法阵可能异常显示。请在 Inspector 指定 Player Character 或为角色添加 Tag：Player。", this);
        }

        if (PlayerInputLock.IsLocked)
            HideForDialogue();
    }

    void OnPlayerInputLockChanged(bool locked)
    {
        if (locked)
            HideForDialogue();
    }

    void HideForDialogue()
    {
        if (_mat == null)
            return;

        _mat.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0f);
        if (_renderer != null)
            _renderer.enabled = false;
    }

    void Update()
    {
        if (PlayerInputLock.IsLocked)
            return;

        bool isAirborne = playerCharacter != null && !playerCharacter.isGrounded;

        if (isAirborne)
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
            // 与 CameraController / CharactorController 一致：空中已松开右键时落地不会收到 MouseButtonUp
            if (!Input.GetMouseButton(1))
            {
                _hasRightButtonBeenReleasedSinceAir = true;
            }
        }

        // 滞空时无论是否按住右键都不显示魔法阵（立刻 alpha=0）
        if (isAirborne)
        {
            _mat.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0f);
            if (_renderer != null)
                _renderer.enabled = false;
            return;
        }

        if (_renderer != null)
            _renderer.enabled = true;

        float targetAlpha = Input.GetMouseButton(1) && _hasRightButtonBeenReleasedSinceAir ? 1f : 0f;
        float currentAlpha = Mathf.Lerp(_mat.color.a, targetAlpha, fadeSpeed * Time.deltaTime);
        _mat.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, currentAlpha);
    }
}