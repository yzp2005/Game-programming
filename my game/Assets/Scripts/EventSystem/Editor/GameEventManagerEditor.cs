#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameEventManager))]
public class GameEventManagerEditor : Editor
{
    string newFlagInput = string.Empty;
    string lastRenameError;

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
            EditorGUILayout.HelpBox("进入 Play 模式后可实时查看并修改标签名。", MessageType.Info);
            return;
        }

        if (GameEventManager.Instance == null)
        {
            EditorGUILayout.HelpBox("GameEventManager 尚未初始化。", MessageType.Warning);
            return;
        }

        IReadOnlyList<string> flags = GameEventManager.GetAllFlagsSorted();
        EditorGUILayout.LabelField("数量", GameEventManager.FlagCount.ToString());

        if (!string.IsNullOrEmpty(lastRenameError))
            EditorGUILayout.HelpBox(lastRenameError, MessageType.Warning);

        if (flags.Count == 0)
            EditorGUILayout.HelpBox("（暂无标签）", MessageType.None);
        else
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < flags.Count; i++)
                DrawFlagRow(flags[i]);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("添加标签", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        newFlagInput = EditorGUILayout.TextField(newFlagInput);
        EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(newFlagInput));
        if (GUILayout.Button("Set", GUILayout.Width(44f)))
        {
            string tag = newFlagInput.Trim();
            if (GameEventManager.Has(tag))
                lastRenameError = $"标签 \"{tag}\" 已存在。";
            else
            {
                GameEventManager.Set(tag);
                lastRenameError = null;
                newFlagInput = string.Empty;
            }
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    void DrawFlagRow(string currentName)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(GUIContent.none, GUILayout.Width(4f));

        EditorGUI.BeginChangeCheck();
        string edited = EditorGUILayout.TextField(currentName);
        if (EditorGUI.EndChangeCheck())
        {
            string trimmed = edited.Trim();
            if (string.IsNullOrEmpty(trimmed))
                lastRenameError = "标签名不能为空。";
            else if (!GameEventManager.Rename(currentName, trimmed))
            {
                if (trimmed == currentName)
                    lastRenameError = null;
                else if (GameEventManager.Has(trimmed))
                    lastRenameError = $"无法重命名：\"{trimmed}\" 已存在。";
                else
                    lastRenameError = $"无法重命名 \"{currentName}\"。";
            }
            else
                lastRenameError = null;
        }

        if (GUILayout.Button("×", GUILayout.Width(22f)))
        {
            GameEventManager.Remove(currentName);
            lastRenameError = null;
        }

        EditorGUILayout.EndHorizontal();
    }
}
#endif
