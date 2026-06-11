using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Game/Skill Database")]
public class SkillDatabase : ScriptableObject
{
    [SerializeField] SkillDefinition[] skills;

    public SkillDefinition[] Skills => skills;

    public SkillDefinition GetById(string skillId)
    {
        if (skills == null || string.IsNullOrEmpty(skillId))
            return null;

        for (int i = 0; i < skills.Length; i++)
        {
            SkillDefinition skill = skills[i];
            if (skill != null && skill.skillId == skillId)
                return skill;
        }

        return null;
    }
}
