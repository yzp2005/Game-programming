using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 使用场景里挂好的全屏黑幕 Image：点击后立刻盖住屏幕 → 加载场景 → 淡出。
/// 挂在主菜单；将 BlackFade Image 拖到 Fade Image。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-250)]
public class SceneFadeLoader : MonoBehaviour
{
    public static SceneFadeLoader Instance { get; private set; }

    [Header("黑幕")]
    [Tooltip("全屏黑色 Image，Color 设为黑色，初始 Alpha = 0")]
    [SerializeField] Image fadeImage;

    [Header("Canvas 层级")]
    [Tooltip("须高于主菜单 Canvas 的 Sort Order（常见主菜单为 0~10）")]
    [SerializeField] int canvasSortOrder = 10000;

    [Header("时间")]
    [Tooltip("勾选：点击后立即全黑；取消则使用 Fade In Duration 渐变")]
    [SerializeField] bool snapToBlack = false;
    [SerializeField] float fadeInDuration = 0.2f;
    [SerializeField] float fadeOutDuration = 0.4f;

    bool loading;

    public static void LoadScene(int buildIndex, Action onFadeToBlack = null)
    {
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError(
                $"[SceneFadeLoader] buildIndex={buildIndex} 无效。Build Settings 共 {SceneManager.sceneCountInBuildSettings} 个场景。");
            return;
        }

        SceneFadeLoader loader = Instance != null ? Instance : FindObjectOfType<SceneFadeLoader>();
        if (loader == null)
        {
            Debug.LogWarning("[SceneFadeLoader] 未找到 SceneFadeLoader，直接加载场景。");
            onFadeToBlack?.Invoke();
            SceneManager.LoadScene(buildIndex);
            return;
        }

        loader.StartCoroutine(loader.LoadRoutine(buildIndex, onFadeToBlack));
    }

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

        if (fadeImage == null)
            fadeImage = GetComponentInChildren<Image>(true);

        if (fadeImage == null)
        {
            Debug.LogWarning("[SceneFadeLoader] 未绑定黑幕 Image。", this);
            return;
        }

        EnsureCanvasOnTop();
        SetFadeAlpha(0f);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    IEnumerator LoadRoutine(int buildIndex, Action onFadeToBlack)
    {
        if (loading || fadeImage == null)
            yield break;

        loading = true;

        EnsureCanvasOnTop();

        if (snapToBlack)
            SetFadeAlpha(1f);
        else
            yield return Fade(0f, 1f, fadeInDuration);

        onFadeToBlack?.Invoke();

        AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        if (op == null)
        {
            Debug.LogError($"[SceneFadeLoader] LoadSceneAsync 失败，buildIndex={buildIndex}。");
            yield return Fade(1f, 0f, fadeOutDuration);
            loading = false;
            yield break;
        }

        while (!op.isDone)
            yield return null;

        yield return null;

        yield return Fade(1f, 0f, fadeOutDuration);
        loading = false;
    }

    void EnsureCanvasOnTop()
    {
        if (fadeImage == null)
            return;

        fadeImage.transform.SetAsLastSibling();

        Canvas canvas = fadeImage.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        canvas.overrideSorting = true;
        canvas.sortingOrder = canvasSortOrder;
        canvas.gameObject.SetActive(true);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        fadeImage.gameObject.SetActive(true);

        if (duration <= 0f)
        {
            SetFadeAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetFadeAlpha(to);
    }

    void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color c = fadeImage.color;
        c.r = 0f;
        c.g = 0f;
        c.b = 0f;
        c.a = alpha;
        fadeImage.color = c;
        fadeImage.raycastTarget = alpha > 0.01f;
    }
}
