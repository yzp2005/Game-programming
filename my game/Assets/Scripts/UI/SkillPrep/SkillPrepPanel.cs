using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 备战 UI：左侧技能列表 → 右侧详情 → 选择后填入下方技能栏。
/// 图标统一使用 Prefab。
/// </summary>
[DisallowMultipleComponent]
public class SkillPrepPanel : MonoBehaviour
{
    [Header("数据")]
    [SerializeField] SkillDatabase database;

    [Header("左侧列表")]
    [Tooltip("勾选：使用 LeftList 下已摆好的 SkillPrepListItem，不自动生成")]
    [SerializeField] bool usePreplacedListItems;
    [SerializeField] Transform listRoot;
    [Tooltip("仅自动生成模式需要；手动摆放时可留空")]
    [SerializeField] SkillPrepListItem listItemPrefab;

    [Header("右侧详情")]
    [SerializeField] GameObject detailRoot;
    [SerializeField] TMP_Text detailNameText;
    [SerializeField] TMP_Text detailDescriptionText;
    [SerializeField] TMP_Text detailCooldownText;
    [SerializeField] Transform detailIconContainer;
    [SerializeField] Button selectSkillButton;
    [SerializeField] Button closeDetailButton;
    [SerializeField] string cooldownFormat = "{0:0.#}s";

    [Header("下方技能栏")]
    [SerializeField] Transform[] loadoutIconSlots;

    [Header("面板")]
    [Tooltip("留空则关闭本物体（Preparation System）")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] Button closePanelButton;
    [Tooltip("勾选后必须选满所有技能槽才能关闭")]
    [SerializeField] bool requireFullLoadoutToClose = true;
    [SerializeField] TMP_Text closeHintText;
    [SerializeField] string closeHintWhenIncomplete = "请选满所有技能后再关闭";

    [Header("清空")]
    [SerializeField] Button clearLoadoutButton;

    SkillDefinition pendingSkill;

    public event Action PanelOpened;
    public event Action PanelClosed;

    GameObject PanelRootObject => panelRoot != null ? panelRoot : gameObject;

    void Awake()
    {
        SkillLoadout.Configure(loadoutIconSlots != null ? loadoutIconSlots.Length : 3);
        SkillLoadout.Changed += OnLoadoutChanged;

        if (selectSkillButton != null)
            selectSkillButton.onClick.AddListener(OnSelectSkillClicked);

        if (closeDetailButton != null)
            closeDetailButton.onClick.AddListener(HideDetail);

        if (clearLoadoutButton != null)
            clearLoadoutButton.onClick.AddListener(OnClearLoadoutClicked);

        if (closePanelButton != null)
            closePanelButton.onClick.AddListener(OnClosePanelClicked);

        BuildList();
        HideDetail();
        RefreshLoadoutBar();
        RefreshClosePanelButton();
    }

    void Start()
    {
        if (usePreplacedListItems)
            BuildList();
    }

    void OnDestroy()
    {
        SkillLoadout.Changed -= OnLoadoutChanged;

        if (selectSkillButton != null)
            selectSkillButton.onClick.RemoveListener(OnSelectSkillClicked);

        if (closeDetailButton != null)
            closeDetailButton.onClick.RemoveListener(HideDetail);

        if (clearLoadoutButton != null)
            clearLoadoutButton.onClick.RemoveListener(OnClearLoadoutClicked);

        if (closePanelButton != null)
            closePanelButton.onClick.RemoveListener(OnClosePanelClicked);
    }

    void OnLoadoutChanged()
    {
        RefreshLoadoutBar();
        RefreshSelectButton();
        RefreshClosePanelButton();
    }

    public void OpenPanel()
    {
        PanelRootObject.SetActive(true);
        RefreshClosePanelButton();
        PanelOpened?.Invoke();
    }

    public bool TryClosePanel()
    {
        if (requireFullLoadoutToClose && !SkillLoadout.IsFull)
            return false;

        ClosePanel();
        return true;
    }

    void OnClosePanelClicked() => TryClosePanel();

    public void ClosePanel()
    {
        HideDetail();

        PanelRootObject.SetActive(false);
        PanelClosed?.Invoke();
    }

    public void EnsureHidden()
    {
        HideDetail();
        PanelRootObject.SetActive(false);
    }

    void BuildList()
    {
        if (listRoot == null)
            return;

        if (usePreplacedListItems)
        {
            SkillPrepListItem[] items = listRoot.GetComponentsInChildren<SkillPrepListItem>(true);
            for (int i = 0; i < items.Length; i++)
                items[i].SetupForPreplaced(ShowDetail);

            return;
        }

        if (listItemPrefab == null || database == null)
            return;

        for (int i = listRoot.childCount - 1; i >= 0; i--)
            Destroy(listRoot.GetChild(i).gameObject);

        SkillDefinition[] skills = database.Skills;
        if (skills == null)
            return;

        for (int i = 0; i < skills.Length; i++)
        {
            SkillDefinition skill = skills[i];
            if (skill == null || !skill.IsValid)
                continue;

            SkillPrepListItem item = Instantiate(listItemPrefab, listRoot);
            item.Bind(skill, ShowDetail);
        }
    }

    void ShowDetail(SkillDefinition skill)
    {
        pendingSkill = skill;

        if (detailRoot != null)
            detailRoot.SetActive(true);

        if (detailNameText != null)
            detailNameText.text = skill.displayName;

        if (detailDescriptionText != null)
            detailDescriptionText.text = skill.description;

        if (detailCooldownText != null)
            detailCooldownText.text = string.Format(cooldownFormat, skill.cooldown);

        SkillIconDisplay.Apply(skill, detailIconContainer);
        RefreshSelectButton();
    }

    void HideDetail()
    {
        pendingSkill = null;

        if (detailRoot != null)
            detailRoot.SetActive(false);
    }

    void OnSelectSkillClicked()
    {
        if (pendingSkill == null)
            return;

        if (!SkillLoadout.TryAdd(pendingSkill.skillId))
            RefreshSelectButton();
    }

    void OnClearLoadoutClicked()
    {
        SkillLoadout.Clear();
    }

    void RefreshLoadoutBar()
    {
        if (loadoutIconSlots == null)
            return;

        for (int i = 0; i < loadoutIconSlots.Length; i++)
        {
            Transform container = loadoutIconSlots[i];
            if (container == null)
                continue;

            string skillId = SkillLoadout.GetSlot(i);
            SkillDefinition skill = database != null ? database.GetById(skillId) : null;

            if (skill != null && skill.iconPrefab != null)
                SkillIconDisplay.Apply(skill, container);
            else
                SkillIconDisplay.Clear(container);
        }
    }

    void RefreshClosePanelButton()
    {
        bool canClose = !requireFullLoadoutToClose || SkillLoadout.IsFull;

        if (closePanelButton != null)
            closePanelButton.interactable = canClose;

        if (closeHintText != null)
            closeHintText.text = canClose ? string.Empty : closeHintWhenIncomplete;
    }

    void RefreshSelectButton()
    {
        if (selectSkillButton == null || pendingSkill == null)
            return;

        bool alreadySelected = SkillLoadout.Contains(pendingSkill.skillId);
        bool full = SkillLoadout.IsFull && !alreadySelected;
        selectSkillButton.interactable = !alreadySelected && !full;
    }
}
