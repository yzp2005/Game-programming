using UnityEngine;

/// <summary>
/// 挂在空物体上测试全局输入锁。正式对话仍由 DialogueManager 调用 PlayerInputLock.SetLocked。
/// </summary>
public class PlayerInputLockController : MonoBehaviour
{
    [Header("进入场景")]
    [Tooltip("勾选后一进场就锁定（模拟开场对话）。")]
    [SerializeField] private bool lockOnStart = true;

    [Header("运行时快捷键")]
    [Tooltip("运行时按切换键在锁定/解锁之间切换，方便对比效果。")]
    [SerializeField] private bool enableToggleKey = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.L;

    void Awake()
    {
        if (lockOnStart)
            PlayerInputLock.SetLocked(true);
    }

    void Update()
    {
        if (!enableToggleKey || !Input.GetKeyDown(toggleKey))
            return;

        bool next = !PlayerInputLock.IsLocked;
        PlayerInputLock.SetLocked(next);
        Debug.Log($"[PlayerInputLock] 测试切换 → IsLocked = {next}", this);
    }

    public void Lock()
    {
        PlayerInputLock.SetLocked(true);
    }

    public void Unlock()
    {
        PlayerInputLock.SetLocked(false);
    }

    [ContextMenu("Lock (测试)")]
    void ContextLock() => Lock();

    [ContextMenu("Unlock (测试)")]
    void ContextUnlock() => Unlock();
}
