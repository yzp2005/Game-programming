#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillLoadoutDebugView))]
public class SkillLoadoutDebugViewEditor : Editor
{
    void OnEnable()
    {
        EditorApplication.update += RepaintOnPlay;
    }

    void OnDisable()
    {
        EditorApplication.update -= RepaintOnPlay;
    }

    void RepaintOnPlay()
    {
        if (Application.isPlaying)
            Repaint();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Skill Loadout（运行时）", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("进入 Play 模式后可在此实时查看已选 skill id。", MessageType.Info);
            return;
        }

        int slotCount = SkillLoadout.SlotCount;
        EditorGUILayout.LabelField("槽位数", slotCount.ToString());
        EditorGUILayout.LabelField("已选数量", SkillLoadout.FilledSlotCount.ToString());

        if (slotCount == 0)
        {
            EditorGUILayout.HelpBox("尚未 Configure。", MessageType.Warning);
            return;
        }

        EditorGUI.indentLevel++;
        for (int i = 0; i < slotCount; i++)
        {
            string skillId = SkillLoadout.GetSlot(i);
            EditorGUILayout.TextField($"Slot {i}", string.IsNullOrEmpty(skillId) ? "(空)" : skillId);
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(4f);
        EditorGUILayout.SelectableLabel(SkillLoadout.ToDebugString(), EditorStyles.helpBox, GUILayout.Height(18f));
    }
}
#endif
