using TMPro;
using UnityEngine;

/// <summary>显示当前任务名与内容；空内容时隐藏 panelRoot（默认为自己）。</summary>
public class QuestDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questContentText;
    [SerializeField] private GameObject panelRoot;
    [Tooltip("内容行在标题下方的 Y 偏移（anchoredPosition）")]
    [SerializeField] private float contentOffsetY = -40f;

    void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        SetCurrentQuest("", "");
    }

    public void SetCurrentQuest(string questName, string questContent)
    {
        if (questNameText != null)
        {
            EnsureActive(questNameText.transform);
            questNameText.text = questName ?? "";
        }

        if (questContentText != null)
        {
            EnsureActive(questContentText.transform);
            questContentText.text = questContent ?? "";

            if (questNameText != null)
                FixContentLayoutIfOffScreen();
        }

        bool show = !string.IsNullOrWhiteSpace(questName) || !string.IsNullOrWhiteSpace(questContent);
        panelRoot.SetActive(show);
    }

    /// <summary>Contenttext 若坐标离标题太远（如 722,411），会移到标题下方。</summary>
    void FixContentLayoutIfOffScreen()
    {
        RectTransform title = questNameText.rectTransform;
        RectTransform content = questContentText.rectTransform;

        if (Vector2.Distance(content.anchoredPosition, title.anchoredPosition) < 100f)
            return;

        content.anchorMin = title.anchorMin;
        content.anchorMax = title.anchorMax;
        content.pivot = title.pivot;
        content.anchoredPosition = title.anchoredPosition + new Vector2(0f, contentOffsetY);
        content.sizeDelta = new Vector2(title.sizeDelta.x, Mathf.Max(80f, content.sizeDelta.y));
    }

    static void EnsureActive(Transform t)
    {
        while (t != null)
        {
            t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("把 Content 对齐到 Title 下方")]
    void EditorAlignContent()
    {
        if (questNameText == null || questContentText == null)
            return;

        FixContentLayoutIfOffScreen();
    }
#endif
}
