using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 左上角跳过按钮：点击 TMP 后跳过开场黑幕与旁白。
/// 需场景中有 EventSystem；TMP 需开启 Raycast Target。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class IntroSkipButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private OpeningBlackCurtain blackCurtain;
    [SerializeField] private string label = "Skip";

    private TMP_Text tmp;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        tmp.text = label;
        tmp.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (blackCurtain != null)
            blackCurtain.SkipIntro();
    }
}
