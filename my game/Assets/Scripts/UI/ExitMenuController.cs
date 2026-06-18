using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ESC 打开退出菜单：暂停游戏、锁定玩法输入、显示鼠标。
/// 再按 ESC 返回游戏；点击 Exit 文字退出应用。
/// 挂在 ExitUI 根物体上。
/// </summary>
[DisallowMultipleComponent]
public class ExitMenuController : MonoBehaviour
{
    public static ExitMenuController Instance { get; private set; }

    public static bool IsOpen => Instance != null && Instance.isOpen;

    [Header("UI")]
    [Tooltip("暂停时显示的面板（如 Panel）；留空则找子物体 Panel")]
    [SerializeField] GameObject menuRoot;
    [Tooltip("点击此 TMP 文字退出游戏；留空则自动找 l-click")]
    [SerializeField] TMP_Text exitClickText;
    [Tooltip("若 Exit 仍用 Legacy Text，可拖到这里；留空则自动找 l-click 上的 Text")]
    [SerializeField] Text exitClickLegacyText;

    [Header("按键")]
    [SerializeField] KeyCode toggleKey = KeyCode.Escape;

    [Header("场景")]
    [Tooltip("这些场景不响应 ESC（如主菜单）")]
    [SerializeField] string[] disabledSceneNames = { "Main Menu" };

    [Header("Canvas")]
    [SerializeField] int canvasSortOrder = 9000;

    bool isOpen;
    bool inputLockedByMenu;
    UIClickTMP exitTmpClick;
    UIClickText exitLegacyClick;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);

        ResolveMenuRoot();
        ResolveExitClickTargets();
        BindExitClick();
        EnsureCanvasOnTop();
        SetMenuVisible(false);
        SetExitClickEnabled(false);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (exitTmpClick != null)
            exitTmpClick.Clicked -= OnExitClicked;

        if (exitLegacyClick != null)
            exitLegacyClick.Clicked -= OnExitClicked;

        if (Instance == this)
        {
            if (isOpen)
                RestoreGameplayImmediate();

            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isOpen)
            Close();
    }

    void Update()
    {
        if (!Input.GetKeyDown(toggleKey))
            return;

        if (IsDisabledScene())
            return;

        if (isOpen)
        {
            Close();
            return;
        }

        if (PlayerInputLock.IsLocked)
            return;

        Open();
    }

    void Open()
    {
        if (isOpen)
            return;

        isOpen = true;
        Time.timeScale = 0f;
        PlayerInputLock.SetLocked(true, dialogueCursor: true);
        inputLockedByMenu = true;
        SetMenuVisible(true);
        SetExitClickEnabled(true);
    }

    void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        RestoreGameplayImmediate();
        SetMenuVisible(false);
        SetExitClickEnabled(false);
    }

    void RestoreGameplayImmediate()
    {
        Time.timeScale = 1f;

        if (inputLockedByMenu)
        {
            PlayerInputLock.SetLocked(false);
            inputLockedByMenu = false;
        }
    }

    void SetMenuVisible(bool visible)
    {
        if (menuRoot != null)
            menuRoot.SetActive(visible);
    }

    void ResolveMenuRoot()
    {
        if (menuRoot != null)
            return;

        Transform panel = transform.Find("Panel");
        if (panel != null)
            menuRoot = panel.gameObject;
    }

    void ResolveExitClickTargets()
    {
        if (exitClickText != null || exitClickLegacyText != null || menuRoot == null)
            return;

        foreach (Transform child in menuRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "l-click")
                continue;

            exitClickText = child.GetComponent<TMP_Text>();
            if (exitClickText == null)
                exitClickLegacyText = child.GetComponent<Text>();
            return;
        }

        foreach (TMP_Text tmp in menuRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.text.IndexOf("exit", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                exitClickText = tmp;
                return;
            }
        }
    }

    void BindExitClick()
    {
        if (exitClickText != null)
        {
            exitClickText.raycastTarget = true;
            exitTmpClick = exitClickText.GetComponent<UIClickTMP>();
            if (exitTmpClick == null)
                exitTmpClick = exitClickText.gameObject.AddComponent<UIClickTMP>();

            exitTmpClick.Clicked -= OnExitClicked;
            exitTmpClick.Clicked += OnExitClicked;
            return;
        }

        if (exitClickLegacyText != null)
        {
            exitClickLegacyText.raycastTarget = true;
            exitLegacyClick = exitClickLegacyText.GetComponent<UIClickText>();
            if (exitLegacyClick == null)
                exitLegacyClick = exitClickLegacyText.gameObject.AddComponent<UIClickText>();

            exitLegacyClick.Clicked -= OnExitClicked;
            exitLegacyClick.Clicked += OnExitClicked;
            return;
        }

        Debug.LogWarning($"{name}: 未找到 Exit 点击文字（TMP 或 l-click 上的 Text）。", this);
    }

    void SetExitClickEnabled(bool enabled)
    {
        if (exitClickText != null)
            exitClickText.raycastTarget = enabled;

        if (exitClickLegacyText != null)
            exitClickLegacyText.raycastTarget = enabled;
    }

    void OnExitClicked()
    {
        if (!isOpen)
            return;

        QuitGame();
    }

    void EnsureCanvasOnTop()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>(true);

        if (canvas == null)
            return;

        canvas.overrideSorting = true;
        canvas.sortingOrder = canvasSortOrder;
    }

    void QuitGame()
    {
        if (!isOpen)
            return;

        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    bool IsDisabledScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName) || disabledSceneNames == null)
            return false;

        for (int i = 0; i < disabledSceneNames.Length; i++)
        {
            string disabled = disabledSceneNames[i];
            if (!string.IsNullOrWhiteSpace(disabled) && sceneName == disabled.Trim())
                return true;
        }

        return false;
    }
}
