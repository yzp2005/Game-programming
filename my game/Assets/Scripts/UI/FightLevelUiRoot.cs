using UnityEngine;

/// <summary>
/// 战时 UI 根节点：挂在始终激活的空物体上，子物体放技能栏等战斗 HUD。
/// 仅当存在 fight_level{n}（n 为正整数）标签时显示 UI 容器。
/// </summary>
[DisallowMultipleComponent]
public class FightLevelUiRoot : MonoBehaviour
{
    [Tooltip("要显示/隐藏的 UI 容器（建议拖子物体，本物体保持激活以便监听 flag）")]
    [SerializeField] GameObject uiRoot;
    [SerializeField] bool checkFlagOnStart = true;
    [SerializeField] bool hideWhenFightFlagRemoved = true;

    public int ActiveFightLevel { get; private set; }

    void Awake()
    {
        GameEventManager.FlagAdded += OnFlagAdded;
        GameEventManager.FlagRemoved += OnFlagRemoved;

        ResolveUiRoot();
        Hide();

        if (checkFlagOnStart && TryGetActiveFightLevel(out int level))
            Show(level);
    }

    void OnDestroy()
    {
        GameEventManager.FlagAdded -= OnFlagAdded;
        GameEventManager.FlagRemoved -= OnFlagRemoved;
    }

    void OnFlagAdded(string flag)
    {
        if (!TryParseFightLevelNumber(flag, out int level))
            return;

        Show(level);
    }

    void OnFlagRemoved(string flag)
    {
        if (!hideWhenFightFlagRemoved || !TryParseFightLevelNumber(flag, out _))
            return;

        if (TryGetActiveFightLevel(out int level))
            Show(level);
        else
            Hide();
    }

    void ResolveUiRoot()
    {
        if (uiRoot != null)
            return;

        if (transform.childCount == 1)
            uiRoot = transform.GetChild(0).gameObject;
        else if (transform.childCount > 1)
            Debug.LogWarning($"{name}: 有多个子物体，请在 Inspector 指定 Ui Root。", this);
        else
            Debug.LogWarning($"{name}: 未指定 Ui Root 且无子物体。", this);
    }

    void Show(int level)
    {
        ActiveFightLevel = level;
        FightLevelInputGate.EnsureSubscribed();
        FightLevelInputGate.RefreshFromFlags();

        if (uiRoot != null)
            uiRoot.SetActive(true);
    }

    void Hide()
    {
        ActiveFightLevel = 0;

        if (uiRoot != null)
            uiRoot.SetActive(false);

        FightLevelInputGate.RefreshFromFlags();
    }

    static bool TryGetActiveFightLevel(out int level)
    {
        level = 0;

        if (GameEventManager.Instance == null)
            return false;

        foreach (string flag in GameEventManager.GetAllFlagsSorted())
        {
            if (FightLevelInputGate.TryParseFightLevelNumber(flag, out level))
                return true;
        }

        return false;
    }

    public static bool TryParseFightLevelNumber(string flag, out int levelNumber) =>
        FightLevelInputGate.TryParseFightLevelNumber(flag, out levelNumber);
}
