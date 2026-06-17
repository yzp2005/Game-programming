using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>消息栏单行 UI，挂 MessageLine 预制体根物体上。</summary>
[DisallowMultipleComponent]
public class GameMessageLineView : MonoBehaviour
{
    [SerializeField] TMP_Text timeText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] Image categoryStrip;

    public void Set(string time, string body, Color bodyColor, Color stripColor)
    {
        if (timeText != null)
            timeText.text = time;

        if (bodyText != null)
        {
            bodyText.text = body;
            bodyText.color = bodyColor;
        }

        if (categoryStrip != null)
            categoryStrip.color = stripColor;
    }

    void Reset()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length > 0 && timeText == null)
            timeText = texts[0];
        if (texts.Length > 1 && bodyText == null)
            bodyText = texts[1];

        categoryStrip = GetComponentInChildren<Image>();
    }
}
