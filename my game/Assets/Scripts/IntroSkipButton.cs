using TMPro;
using UnityEngine;

/// <summary>
/// 开场跳过提示：显示在左上角，按 E 跳过黑幕与旁白。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class IntroSkipButton : MonoBehaviour
{
    [SerializeField] private OpeningBlackCurtain blackCurtain;
    [SerializeField] private KeyCode skipKey = KeyCode.E;
    [SerializeField] private string label = "Skip(press 'e')";

    private TMP_Text tmp;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        tmp.text = label;
        tmp.raycastTarget = false;
    }

    void Update()
    {
        if (blackCurtain == null || !blackCurtain.IsIntroPlaying)
            return;

        if (Input.GetKeyDown(skipKey))
            blackCurtain.SkipIntro();
    }
}
