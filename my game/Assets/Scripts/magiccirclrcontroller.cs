using UnityEngine;

public class MagicCircleFade : MonoBehaviour
{
    [Header("淡入淡出速度")]
    public float fadeSpeed = 8f;

    private Renderer _renderer;
    private Material _mat;
    private Color _originalColor;
    private CharacterController characterController;
    private CharactorController playerController;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        _mat = _renderer.material;
        _originalColor = _mat.color;
        characterController = GetComponentInParent<CharacterController>();
        playerController = GetComponentInParent<CharactorController>();

        _mat.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0);
    }

    void Update()
    {
        float targetAlpha = ShouldShow() ? 1f : 0f;

        float currentAlpha = Mathf.Lerp(_mat.color.a, targetAlpha, fadeSpeed * Time.deltaTime);
        _mat.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, currentAlpha);
    }

    bool ShouldShow()
    {
        if (PlayerInputLock.IsLocked || NpcPlacementController.IsActive)
            return false;

        if (playerController != null)
            return playerController.IsAimInputActive;

        if (!Input.GetMouseButton(1))
            return false;

        return characterController == null || characterController.isGrounded;
    }
}