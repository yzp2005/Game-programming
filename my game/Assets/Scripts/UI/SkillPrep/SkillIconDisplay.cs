using UnityEngine;

/// <summary>在 UI 容器下实例化图标 Prefab。</summary>
public static class SkillIconDisplay
{
    public static void Apply(SkillDefinition skill, Transform container)
    {
        if (skill == null || skill.iconPrefab == null)
        {
            Clear(container);
            return;
        }

        ApplyPrefab(skill.iconPrefab, container);
    }

    public static void ApplyPrefab(GameObject prefab, Transform container)
    {
        Clear(container);

        if (prefab == null || container == null)
            return;

        GameObject instance = Object.Instantiate(prefab, container);
        RectTransform rect = instance.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
        else
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }
    }

    public static void Clear(Transform container)
    {
        if (container == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
            Object.Destroy(container.GetChild(i).gameObject);
    }
}
