using TMPro;
using UnityEngine;

/// <summary>
/// 多个交互点共用同一个 Event Hint 时，避免互相 SetActive(false) 把提示关掉。
/// </summary>
public static class EventHintUI
{
    static object _owner;

    public static void Show(object owner, GameObject root, TMP_Text text, string message)
    {
        if (root == null)
            return;

        _owner = owner;
        root.SetActive(true);
        if (text != null)
            text.text = message;
    }

    public static void Hide(object owner, GameObject root)
    {
        if (root == null || _owner != owner)
            return;

        _owner = null;
        root.SetActive(false);
    }
}
