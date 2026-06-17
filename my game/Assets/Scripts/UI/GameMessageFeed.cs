using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameMessageCategory
{
    System,
    Placement,
    Countdown,
    Combat,
    Resource,
    Warning
}

/// <summary>
/// 战斗/摆放消息栏：向 ScrollView Content 追加 MessageLine，可选 Banner 单行提示。
/// </summary>
[DisallowMultipleComponent]
public class GameMessageFeed : MonoBehaviour
{
    public static GameMessageFeed Instance { get; private set; }

    [Header("ScrollView")]
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] RectTransform contentRoot;
    [SerializeField] GameMessageLineView messageLinePrefab;

    [Header("Banner（可选，单行倒计时）")]
    [SerializeField] GameObject bannerRoot;
    [SerializeField] TMP_Text bannerText;

    [Header("行为")]
    [SerializeField] int maxLines = 30;
    [SerializeField] float lineSpacing = 4f;
    [SerializeField] float defaultLineHeight = 82f;
    [SerializeField] bool scrollToBottomOnPost = true;
    [SerializeField] bool includeLevelTime = true;
    [SerializeField] bool debugPostOnStart;

    [Header("颜色")]
    [SerializeField] Color placementColor = Color.white;
    [SerializeField] Color countdownColor = Color.white;
    [SerializeField] Color alertColor = new Color(1f, 0.35f, 0.35f);

    readonly List<GameMessageLineView> activeLines = new List<GameMessageLineView>();
    Coroutine scrollRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{name}: 场景里存在多个 GameMessageFeed，将使用 {Instance.name}。", this);
            return;
        }

        Instance = this;

        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>(true);

        if (contentRoot == null && scrollRect != null)
            contentRoot = scrollRect.content;

        EnsureContentLayout();
        ClearContentChildrenUsedAsPreview();
        SetBannerVisible(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (debugPostOnStart)
            Post("Message feed ready.", GameMessageCategory.System);
    }

    public static void Post(string message, GameMessageCategory category = GameMessageCategory.System)
    {
        if (Instance == null || string.IsNullOrWhiteSpace(message))
            return;

        Instance.AddLine(message.Trim(), category);
    }

    public static void SetBanner(string message)
    {
        if (Instance == null)
            return;

        Instance.SetBannerInternal(message);
    }

    public static void ClearBanner()
    {
        if (Instance == null)
            return;

        Instance.SetBannerInternal(null);
    }

    public static bool HasBanner =>
        Instance != null && (Instance.bannerRoot != null || Instance.bannerText != null);

    void AddLine(string message, GameMessageCategory category)
    {
        if (messageLinePrefab == null || contentRoot == null)
        {
            Debug.LogWarning($"{name}: MessageLine Prefab 或 Content 未设置。", this);
            return;
        }

        GameMessageLineView line = Instantiate(messageLinePrefab, contentRoot);
        PrepareLineTransform(line);
        line.gameObject.SetActive(true);
        line.Set(
            includeLevelTime ? GetLevelTimeLabel() : string.Empty,
            message,
            GetBodyColor(category),
            GetStripColor(category));

        activeLines.Add(line);
        TrimOldLines();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        if (scrollToBottomOnPost)
            RequestScrollToBottom();
    }

    void EnsureContentLayout()
    {
        if (contentRoot == null)
            return;

        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = lineSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void PrepareLineTransform(GameMessageLineView line)
    {
        if (line == null)
            return;

        RectTransform rect = line.transform as RectTransform;
        if (rect == null)
            return;

        float height = rect.sizeDelta.y > 1f ? rect.sizeDelta.y : defaultLineHeight;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, height);

        LayoutElement layoutElement = line.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = line.gameObject.AddComponent<LayoutElement>();

        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
    }

    void TrimOldLines()
    {
        while (activeLines.Count > maxLines)
        {
            GameMessageLineView oldest = activeLines[0];
            activeLines.RemoveAt(0);

            if (oldest != null)
                Destroy(oldest.gameObject);
        }
    }

    void SetBannerInternal(string message)
    {
        bool hasMessage = !string.IsNullOrWhiteSpace(message);

        if (bannerText != null)
        {
            bannerText.text = hasMessage ? message.Trim() : string.Empty;
            if (hasMessage)
                bannerText.color = countdownColor;
        }

        SetBannerVisible(hasMessage);
    }

    void SetBannerVisible(bool visible)
    {
        if (bannerRoot != null)
            bannerRoot.SetActive(visible);
        else if (bannerText != null)
            bannerText.gameObject.SetActive(visible);
    }

    void RequestScrollToBottom()
    {
        if (scrollRect == null)
            return;

        if (scrollRoutine != null)
            StopCoroutine(scrollRoutine);

        scrollRoutine = StartCoroutine(ScrollToBottomNextFrame());
    }

    IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (contentRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;

        scrollRoutine = null;
    }

    void ClearContentChildrenUsedAsPreview()
    {
        if (contentRoot == null || messageLinePrefab == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            if (child.GetComponent<GameMessageLineView>() != null)
                Destroy(child.gameObject);
        }
    }

    string GetLevelTimeLabel()
    {
        if (GameStatsUI.Instance != null)
            return GameStatsUI.Instance.GetFormattedTime();

        return "--:--";
    }

    Color GetBodyColor(GameMessageCategory category)
    {
        switch (category)
        {
            case GameMessageCategory.Placement:
                return placementColor;
            case GameMessageCategory.Countdown:
                return countdownColor;
            default:
                return alertColor;
        }
    }

    Color GetStripColor(GameMessageCategory category)
    {
        Color color = GetBodyColor(category);
        color.a = 0.95f;
        return color;
    }
}
