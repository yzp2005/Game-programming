using UnityEngine;

[CreateAssetMenu(fileName = "SkillDefinition", menuName = "Game/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    [Tooltip("全项目唯一，如 skill_heal")]
    public string skillId;
    public string displayName;
    [Tooltip("UI 图标 Prefab（可含多个 Image）")]
    public GameObject iconPrefab;
    [TextArea(2, 6)]
    public string description;
    [Min(0f)]
    public float cooldown;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(skillId) && iconPrefab != null;
}
