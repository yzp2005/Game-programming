using TMPro;
using UnityEngine;

/// <summary>Play 模式下实时显示 SkillLoadout 的 skill id 数组，便于调试。</summary>
[DisallowMultipleComponent]
public class SkillLoadoutDebugView : MonoBehaviour
{
    [SerializeField] TMP_Text outputText;
    [SerializeField] bool logToConsoleOnChange;
    [SerializeField] bool showOnScreenOverlay;

    void OnEnable()
    {
        SkillLoadout.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        SkillLoadout.Changed -= Refresh;
    }

    void Refresh()
    {
        string text = SkillLoadout.ToDebugString();

        if (outputText != null)
            outputText.text = text;

        if (logToConsoleOnChange)
            Debug.Log($"[SkillLoadout] {text}");
    }

    void OnGUI()
    {
        if (!showOnScreenOverlay || outputText != null)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperLeft
        };

        GUI.Box(new Rect(10f, 10f, 420f, 36f), $"SkillLoadout: {SkillLoadout.ToDebugString()}", style);
    }
}
