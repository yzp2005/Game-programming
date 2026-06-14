using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗场景技能栏：进入场景后把 SkillLoadout 里选好的技能图标显示到 Image 上。
/// SkillLoadout 在同一次 Play 内跨场景保留。
/// </summary>
[DisallowMultipleComponent]
public class SkillLoadoutBarView : MonoBehaviour
{
    [SerializeField] SkillDatabase database;
    [SerializeField] Image[] skillImages;
    [SerializeField] bool hideEmptySlots = true;
    [SerializeField] bool refreshOnLoadoutChanged;

    void OnEnable()
    {
        if (refreshOnLoadoutChanged)
            SkillLoadout.Changed += Refresh;

        Refresh();
    }

    void Start() => Refresh();

    void OnDisable()
    {
        SkillLoadout.Changed -= Refresh;
    }

    public void Refresh()
    {
        if (skillImages == null)
            return;

        for (int i = 0; i < skillImages.Length; i++)
        {
            SkillDefinition skill = SkillLoadout.GetDefinition(database, i);
            SkillIconDisplay.ApplyToImage(skill, skillImages[i], hideEmptySlots);
        }
    }
}
