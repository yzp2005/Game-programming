using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在单个场景内：监听标签移动物体；进入本场景时若 flag 已存在也会立即摆放到目标点。
/// </summary>
[DisallowMultipleComponent]
public class FlagObjectMoveTrigger : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("存在此 flag 时移动")]
        public string whenFlag;
        [Tooltip("要移动的物体，留空则移动本脚本所在物体")]
        public Transform target;
        [Tooltip("场景里摆好的空物体，作为传送目标点")]
        public Transform destination;
        [Tooltip("相对 Destination 的本地偏移（可选）")]
        public Vector3 offset;
        [Tooltip("勾选则与 Destination 朝向一致")]
        public bool matchDestinationRotation = true;
        [Min(0f)] public float moveDuration;
        public bool onlyOnce = true;
        public string[] flagsToAddOnFinish;
        public string[] flagsToRemoveOnFinish;
    }

    [SerializeField] Entry[] entries;

    readonly HashSet<string> triggeredFlags = new HashSet<string>();
    readonly Dictionary<Transform, Coroutine> activeMoves = new Dictionary<Transform, Coroutine>();

    void Awake() => GameEventManager.FlagAdded += OnFlagAdded;

    void Start() => ApplyExistingFlags();

    void OnDestroy()
    {
        GameEventManager.FlagAdded -= OnFlagAdded;

        foreach (Coroutine routine in activeMoves.Values)
        {
            if (routine != null)
                StopCoroutine(routine);
        }
    }

    void OnFlagAdded(string flag)
    {
        if (entries == null || string.IsNullOrEmpty(flag))
            return;

        for (int i = 0; i < entries.Length; i++)
            TryMove(entries[i], flag);
    }

    void ApplyExistingFlags()
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.whenFlag))
                continue;

            if (!GameEventManager.Has(entry.whenFlag.Trim()))
                continue;

            ApplyMove(entry, immediate: true, applyFinishFlags: false);
        }
    }

    void TryMove(Entry entry, string flag)
    {
        if (entry == null || !FlagMatches(entry.whenFlag, flag))
            return;

        string key = entry.whenFlag.Trim();
        if (entry.onlyOnce && triggeredFlags.Contains(key))
            return;

        if (entry.onlyOnce)
            triggeredFlags.Add(key);

        ApplyMove(entry, immediate: entry.moveDuration <= 0f, applyFinishFlags: true);
    }

    void ApplyMove(Entry entry, bool immediate, bool applyFinishFlags)
    {
        Transform target = entry.target != null ? entry.target : transform;
        if (target == null || entry.destination == null)
        {
            if (entry.destination == null)
                Debug.LogWarning($"{name}: flag「{entry.whenFlag}」未指定 Destination。", this);
            return;
        }

        Vector3 position = entry.destination.TransformPoint(entry.offset);
        Quaternion rotation = entry.matchDestinationRotation
            ? entry.destination.rotation
            : target.rotation;

        if (immediate || entry.moveDuration <= 0f)
        {
            StopMove(target);
            target.SetPositionAndRotation(position, rotation);

            if (applyFinishFlags)
                FlagEventActions.Apply(entry.flagsToAddOnFinish, entry.flagsToRemoveOnFinish);

            return;
        }

        StopMove(target);
        activeMoves[target] = StartCoroutine(MoveRoutine(target, position, rotation, entry, applyFinishFlags));
    }

    IEnumerator MoveRoutine(
        Transform target,
        Vector3 position,
        Quaternion rotation,
        Entry entry,
        bool applyFinishFlags)
    {
        Vector3 startPos = target.position;
        Quaternion startRot = target.rotation;
        float duration = entry.moveDuration;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.SetPositionAndRotation(
                Vector3.Lerp(startPos, position, t),
                Quaternion.Slerp(startRot, rotation, t));
            yield return null;
        }

        target.SetPositionAndRotation(position, rotation);
        activeMoves.Remove(target);

        if (applyFinishFlags)
            FlagEventActions.Apply(entry.flagsToAddOnFinish, entry.flagsToRemoveOnFinish);
    }

    void StopMove(Transform target)
    {
        if (!activeMoves.TryGetValue(target, out Coroutine routine))
            return;

        if (routine != null)
            StopCoroutine(routine);

        activeMoves.Remove(target);
    }

    static bool FlagMatches(string expected, string actual)
    {
        return !string.IsNullOrWhiteSpace(expected)
            && string.Equals(expected.Trim(), actual, StringComparison.Ordinal);
    }
}
