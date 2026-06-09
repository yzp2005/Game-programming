#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameEventManager))]
public class GameEventManagerEditor : Editor
{
    void OnEnable()
    {
        EditorApplication.update += RepaintInspector;
    }

    void OnDisable()
    {
        EditorApplication.update -= RepaintInspector;
    }

    void RepaintInspector()
    {
        if (Application.isPlaying)
            Repaint();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("当前 Flag 列表", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("进入 Play 模式后可实时查看已 Set 的标签。", MessageType.Info);
            return;
        }

        if (GameEventManager.Instance == null)
        {
            EditorGUILayout.HelpBox("GameEventManager 尚未初始化。", MessageType.Warning);
            return;
        }

        IReadOnlyList<string> flags = GameEventManager.GetAllFlagsSorted();
        EditorGUILayout.LabelField("数量", GameEventManager.FlagCount.ToString());

        if (flags.Count == 0)
        {
            EditorGUILayout.HelpBox("（暂无标签）", MessageType.None);
            return;
        }

        EditorGUI.indentLevel++;
        for (int i = 0; i < flags.Count; i++)
            EditorGUILayout.LabelField($"{i + 1}.", flags[i]);
        EditorGUI.indentLevel--;
    }
}
#endif
