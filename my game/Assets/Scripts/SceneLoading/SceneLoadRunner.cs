using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 传送切场景：白屏淡入 → Loading 文字 → 加载 → 白屏淡出 → 进入新场景。
/// </summary>
[DefaultExecutionOrder(-200)]
public class SceneLoadRunner : MonoBehaviour
{
    public static SceneLoadRunner Instance { get; private set; }

    [Header("UI")]
    [SerializeField] GameObject uiRoot;
    [SerializeField] Image whiteFade;
    [SerializeField] GameObject loadingPanel;
    [SerializeField] TMP_Text loadingText;
    [SerializeField] string loadingMessage = "Loading...";

    [Header("时间")]
    [SerializeField] float fadeToWhiteDuration = 0.45f;
    [SerializeField] float fadeFromWhiteDuration = 0.45f;
    [SerializeField] float minLoadingPanelSeconds = 0.5f;

    bool _loading;
    bool _lockedInputForLoad;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUiReady();
        HideAll();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ReleaseInputLockForLoad();

        if (Instance == this)
            Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_loading)
            return;

        StartCoroutine(UnlockAfterSceneLoaded());
    }

    IEnumerator UnlockAfterSceneLoaded()
    {
        yield return null;
        ReleaseInputLockForLoad();
    }

    public static void LoadScene(int buildIndex, string spawnId)
    {
        if (string.IsNullOrWhiteSpace(spawnId))
        {
            Debug.LogError("[SceneLoadRunner] spawnId 为空，取消加载。");
            return;
        }

        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError(
                $"[SceneLoadRunner] buildIndex={buildIndex} 无效。Build Settings 共 {SceneManager.sceneCountInBuildSettings} 个场景（0~{SceneManager.sceneCountInBuildSettings - 1}）。");
            return;
        }

        SceneTransition.NextSpawnPointId = spawnId.Trim();

        SceneLoadRunner runner = ResolveRunner();
        if (runner == null)
        {
            Debug.LogWarning("[SceneLoadRunner] 未找到 SceneLoadRunner，使用同步加载。");
            SceneManager.LoadScene(buildIndex);
            PlayerSpawnOnLoad.ApplyIfNeeded();
            PlayerInputLock.ForceUnlockGameplay();
            SceneTransition.ApplyLoadCompleteFlags();
            return;
        }

        runner.BeginLoad(buildIndex);
    }

    static SceneLoadRunner ResolveRunner()
    {
        if (Instance != null)
            return Instance;

        return FindObjectOfType<SceneLoadRunner>();
    }

    void BeginLoad(int buildIndex)
    {
        if (_loading)
        {
            Debug.LogWarning("[SceneLoadRunner] 正在加载中，忽略重复请求。", this);
            return;
        }

        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        StartCoroutine(LoadRoutine(buildIndex));
    }

    IEnumerator LoadRoutine(int buildIndex)
    {
        _loading = true;
        AcquireInputLockForLoad();
        EnsureUiReady();

        try
        {
            if (IsAlive(uiRoot))
                uiRoot.SetActive(true);

            SetWhiteAlpha(0f);

            if (IsAlive(loadingPanel))
                loadingPanel.SetActive(false);

            yield return FadeWhite(0f, 1f, fadeToWhiteDuration);

            if (IsAlive(loadingPanel))
                loadingPanel.SetActive(true);

            if (IsAlive(loadingText))
                loadingText.text = loadingMessage;

            float panelShownAt = Time.unscaledTime;

            AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[SceneLoadRunner] LoadSceneAsync 失败，buildIndex={buildIndex}。", this);
                yield break;
            }

            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
                yield return null;

            float remain = minLoadingPanelSeconds - (Time.unscaledTime - panelShownAt);
            if (remain > 0f)
                yield return new WaitForSecondsRealtime(remain);

            op.allowSceneActivation = true;

            while (!op.isDone)
                yield return null;

            ReleaseInputLockForLoad();
            PlayerSpawnOnLoad.ApplyIfNeeded();

            if (IsAlive(loadingPanel))
                loadingPanel.SetActive(false);

            yield return FadeWhite(1f, 0f, fadeFromWhiteDuration);

            HideAll();
            SceneTransition.ApplyLoadCompleteFlags();
        }
        finally
        {
            ReleaseInputLockForLoad();
            _loading = false;
        }
    }

    void AcquireInputLockForLoad()
    {
        if (_lockedInputForLoad)
            return;

        _lockedInputForLoad = true;
        PlayerInputLock.SetLocked(true, dialogueCursor: false);
    }

    void ReleaseInputLockForLoad()
    {
        if (!_lockedInputForLoad)
            return;

        _lockedInputForLoad = false;
        PlayerInputLock.ForceUnlockGameplay();
    }

    void EnsureUiReady()
    {
        if (!IsAlive(uiRoot) && transform.childCount > 0)
            uiRoot = transform.GetChild(0).gameObject;

        if (!IsAlive(uiRoot))
        {
            Debug.LogWarning("[SceneLoadRunner] 未配置 uiRoot。", this);
            return;
        }

        Transform rootTransform = uiRoot.transform;
        if (rootTransform.localScale.sqrMagnitude < 0.001f)
            rootTransform.localScale = Vector3.one;

        Canvas canvas = uiRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = uiRoot.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10000;

        if (uiRoot.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        if (uiRoot.GetComponent<GraphicRaycaster>() == null)
            uiRoot.AddComponent<GraphicRaycaster>();

        ResolveWhiteFade(rootTransform);

        if (!IsAlive(loadingPanel))
        {
            Transform panel = rootTransform.Find("LoadUI/Panel");
            if (panel != null)
                loadingPanel = panel.gameObject;
        }

        if (!IsAlive(loadingText) && IsAlive(loadingPanel))
            loadingText = loadingPanel.GetComponentInChildren<TMP_Text>(true);
    }

    void ResolveWhiteFade(Transform rootTransform)
    {
        if (IsAlive(whiteFade))
            return;

        Transform fade = rootTransform.Find("LoadUI/WhiteFade");
        if (fade == null)
            fade = rootTransform.Find("WhiteFade");

        if (fade != null)
            whiteFade = fade.GetComponent<Image>();

        if (IsAlive(whiteFade))
            return;

        Image[] images = uiRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].gameObject.name == "WhiteFade")
            {
                whiteFade = images[i];
                return;
            }
        }

        whiteFade = CreateRuntimeWhiteFade(rootTransform);
    }

    Image CreateRuntimeWhiteFade(Transform rootTransform)
    {
        var go = new GameObject("WhiteFade_Runtime", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(rootTransform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = false;
        return image;
    }

    IEnumerator FadeWhite(float from, float to, float duration)
    {
        if (!IsAlive(whiteFade))
            yield break;

        whiteFade.gameObject.SetActive(true);

        if (duration <= 0f)
        {
            SetWhiteAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetWhiteAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetWhiteAlpha(to);
    }

    void SetWhiteAlpha(float alpha)
    {
        if (!IsAlive(whiteFade))
            return;

        Color c = whiteFade.color;
        c.a = alpha;
        whiteFade.color = c;
        whiteFade.raycastTarget = alpha > 0.01f;
    }

    void HideAll()
    {
        SetWhiteAlpha(0f);

        if (IsAlive(loadingPanel))
            loadingPanel.SetActive(false);

        if (IsAlive(uiRoot))
            uiRoot.SetActive(false);
    }

    static bool IsAlive(UnityEngine.Object obj) => obj != null;
}
