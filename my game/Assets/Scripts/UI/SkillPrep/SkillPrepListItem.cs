using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>左侧技能项。点击由根物体接收（子 Image 不参与射线）。</summary>
[DisallowMultipleComponent]
public class SkillPrepListItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Transform iconContainer;

    [Header("手动摆放")]
    [SerializeField] SkillDefinition assignedSkill;
    [SerializeField] bool useExistingVisual;

    SkillDefinition skill;
    Action<SkillDefinition> onClicked;

    public SkillDefinition Skill => skill;

    void Awake()
    {
        if (iconContainer == null)
            iconContainer = transform;

        EnsureRootReceivesClick();
    }

    public void Bind(SkillDefinition definition, Action<SkillDefinition> clicked, bool spawnIcon = true)
    {
        skill = definition;
        onClicked = clicked;

        if (spawnIcon)
            SkillIconDisplay.Apply(definition, iconContainer);
    }

    public void SetupForPreplaced(Action<SkillDefinition> clicked)
    {
        EnsureRootReceivesClick();

        if (assignedSkill == null)
        {
            Debug.LogWarning($"{name}: 手动列表项未指定 Assigned Skill。", this);
            return;
        }

        Bind(assignedSkill, clicked, spawnIcon: !useExistingVisual);
    }

    public void OnPointerClick(PointerEventData eventData) => HandleClick();

    void HandleClick()
    {
        if (skill == null)
        {
            Debug.LogWarning($"{name}: 点击时 skill 为空，检查 Assigned Skill 是否已拖引用。", this);
            return;
        }

        onClicked?.Invoke(skill);
    }

    void EnsureRootReceivesClick()
    {
        Image rootImage = GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = gameObject.AddComponent<Image>();
            rootImage.color = Color.clear;
        }

        rootImage.raycastTarget = true;

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i].gameObject != gameObject)
                graphics[i].raycastTarget = false;
        }
    }
}
