using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 挂在 Image 上即可点击（可透明，Alpha=0 也行）。需勾选 Image Raycast Target。
/// </summary>
[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class UIClickImage : MonoBehaviour, IPointerClickHandler
{
    public event Action Clicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke();
    }

    void Reset()
    {
        Image image = GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;
    }
}
