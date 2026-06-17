using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 挂在 TMP_Text 上即可点击。需勾选 Raycast Target。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public class UIClickTMP : MonoBehaviour, IPointerClickHandler
{
    public event Action Clicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke();
    }

    void Reset()
    {
        TMP_Text text = GetComponent<TMP_Text>();
        if (text != null)
            text.raycastTarget = true;
    }
}
