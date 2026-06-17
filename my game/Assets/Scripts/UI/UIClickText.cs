using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 挂在 Legacy Text 上即可点击。需勾选 Raycast Target。
/// </summary>
[RequireComponent(typeof(Text))]
[DisallowMultipleComponent]
public class UIClickText : MonoBehaviour, IPointerClickHandler
{
    public event Action Clicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke();
    }

    void Reset()
    {
        Text text = GetComponent<Text>();
        if (text != null)
            text.raycastTarget = true;
    }
}
