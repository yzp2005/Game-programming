using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单：Image 点击区触发 New Game / Continue；本场景内始终显示鼠标。
/// </summary>
[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [SerializeField] Image newGameImage;
    [SerializeField] Image continueImage;

    [Tooltip("Build Settings 里 Suntail Village 的 buildIndex（Main Menu=0, Suntail=1, North=2）")]
    [SerializeField] int gameplaySceneBuildIndex = 1;

    void OnEnable()
    {
        ApplyMenuCursor();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == SceneManager.GetActiveScene().buildIndex)
            ApplyMenuCursor();
    }

    void LateUpdate()
    {
        ApplyMenuCursor();
    }

    static void ApplyMenuCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void Awake()
    {
        BindClick(newGameImage, StartNewGame);
        BindClick(continueImage, StartContinue, GameSaveSystem.HasSaveFile);
    }

    public void StartNewGame()
    {
        GameSaveSystem.BeginNewGame(gameplaySceneBuildIndex);
    }

    void StartContinue()
    {
        if (!GameSaveSystem.HasSaveFile)
            return;

        GameSaveSystem.ContinueGame();
    }

    static void BindClick(Image image, Action handler, bool enabled = true)
    {
        if (image == null)
            return;

        image.raycastTarget = enabled;
        if (!enabled)
            return;

        UIClickImage click = image.GetComponent<UIClickImage>();
        if (click == null)
            click = image.gameObject.AddComponent<UIClickImage>();

        click.Clicked += handler;
    }
}
