using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 传送切场景：白屏淡入 → Loading 文字 → 加载 → 白屏淡出 → 进入新场景。
/// </summary>
public class SceneLoadRunner : MonoBehaviour
{
    public static SceneLoadRunner Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private Image whiteFade;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private string loadingMessage = "Loading...";

    [Header("时间")]
    [SerializeField] private float fadeToWhiteDuration = 0.45f;
    [SerializeField] private float fadeFromWhiteDuration = 0.45f;
    [SerializeField] private float minLoadingPanelSeconds = 0.5f;

    bool _loading;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        HideAll();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void LoadScene(int buildIndex, string spawnId)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[SceneLoadRunner] 场景中未放置 SceneLoadRunner，使用同步加载。");
            SceneTransition.NextSpawnPointId = spawnId;
            SceneManager.LoadScene(buildIndex);
            return;
        }

        Instance.StartLoad(buildIndex, spawnId);
    }

    void StartLoad(int buildIndex, string spawnId)
    {
        if (_loading)
            return;

        StartCoroutine(LoadRoutine(buildIndex, spawnId));
    }

    IEnumerator LoadRoutine(int buildIndex, string spawnId)
    {
        _loading = true;
        PlayerInputLock.SetLocked(true);

        if (uiRoot != null)
            uiRoot.SetActive(true);

        SetWhiteAlpha(0f);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        yield return FadeWhite(0f, 1f, fadeToWhiteDuration);

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        if (loadingText != null)
            loadingText.text = loadingMessage;

        float panelShownAt = Time.unscaledTime;
        SceneTransition.NextSpawnPointId = spawnId;

        AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            yield return null;

        float remain = minLoadingPanelSeconds - (Time.unscaledTime - panelShownAt);
        if (remain > 0f)
            yield return new WaitForSecondsRealtime(remain);

        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        yield return FadeWhite(1f, 0f, fadeFromWhiteDuration);

        if (uiRoot != null)
            uiRoot.SetActive(false);

        PlayerInputLock.SetLocked(false);
        _loading = false;
    }

    IEnumerator FadeWhite(float from, float to, float duration)
    {
        if (whiteFade == null)
            yield break;

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
        if (whiteFade == null)
            return;

        Color c = whiteFade.color;
        c.a = alpha;
        whiteFade.color = c;
    }

    void HideAll()
    {
        SetWhiteAlpha(0f);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        if (uiRoot != null)
            uiRoot.SetActive(false);
    }
}
