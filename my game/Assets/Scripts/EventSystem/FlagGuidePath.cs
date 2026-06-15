using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 监听 GameEventManager 标签：满足条件的 Entry 各显示一条贝塞尔引导线。
/// 每条 Entry 可配置多个 flag（任意一个存在即显示该线）。
/// </summary>
[DisallowMultipleComponent]
public class FlagGuidePath : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("任意一个 flag 存在时显示此引导线")]
        public string[] whenFlags;
        [Tooltip("兼容旧配置：单 flag（whenFlags 为空时使用）")]
        public string whenFlag;
        [Tooltip("引导终点")]
        public Transform target;
    }

    [Header("Flag")]
    [SerializeField] Entry[] entries;

    [Header("起点")]
    [Tooltip("留空则自动查找 Tag=Player 的对象")]
    [SerializeField] Transform player;

    [Header("曲线")]
    [Min(4)] [SerializeField] int segmentCount = 24;
    [SerializeField] float startHeightOffset = 0.3f;
    [SerializeField] float endHeightOffset = 0.3f;
    [Tooltip("相对 A-B 距离的抬升比例，越大弧度越高")]
    [SerializeField] float arcHeightFactor = 0.25f;
    [Tooltip("侧向偏移比例，0 为纯向上拱起")]
    [SerializeField] float arcSideFactor = 0.1f;

    [Header("线条")]
    [SerializeField] float lineWidth = 0.15f;
    [SerializeField] Color lineColor = new Color(0.4f, 0.85f, 1f, 0.85f);

    readonly List<LineRenderer> linePool = new List<LineRenderer>();
    Vector3[] pathPoints;

    void Awake()
    {
        pathPoints = new Vector3[Mathf.Max(4, segmentCount)];
        HideAllLines();
    }

    void OnEnable()
    {
        GameEventManager.FlagAdded += OnFlagsChanged;
        GameEventManager.FlagRemoved += OnFlagsChanged;
        RefreshFromFlags();
    }

    void OnDisable()
    {
        GameEventManager.FlagAdded -= OnFlagsChanged;
        GameEventManager.FlagRemoved -= OnFlagsChanged;
        HideAllLines();
    }

    void Update()
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            LineRenderer line = GetLine(i);
            Entry entry = entries[i];
            if (!line.enabled || entry == null || entry.target == null)
                continue;

            UpdatePath(line, entry.target);
        }
    }

    void OnFlagsChanged(string _) => RefreshFromFlags();

    void RefreshFromFlags()
    {
        if (entries == null || entries.Length == 0)
        {
            HideAllLines();
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            LineRenderer line = GetLine(i);

            if (entry != null && entry.target != null && EntryIsActive(entry))
            {
                line.enabled = true;
                UpdatePath(line, entry.target);
            }
            else
            {
                line.enabled = false;
            }
        }
    }

    static bool EntryIsActive(Entry entry)
    {
        if (entry.whenFlags != null)
        {
            for (int i = 0; i < entry.whenFlags.Length; i++)
            {
                string flag = entry.whenFlags[i];
                if (!string.IsNullOrWhiteSpace(flag) && GameEventManager.Has(flag.Trim()))
                    return true;
            }
        }

        return !string.IsNullOrWhiteSpace(entry.whenFlag)
            && GameEventManager.Has(entry.whenFlag.Trim());
    }

    LineRenderer GetLine(int index)
    {
        while (linePool.Count <= index)
            linePool.Add(CreateLineChild(linePool.Count));

        return linePool[index];
    }

    LineRenderer CreateLineChild(int index)
    {
        var lineObject = new GameObject($"GuideLine_{index}");
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        ApplyLineSettings(line);
        line.enabled = false;
        return line;
    }

    void HideAllLines()
    {
        for (int i = 0; i < linePool.Count; i++)
        {
            if (linePool[i] != null)
                linePool[i].enabled = false;
        }
    }

    void UpdatePath(LineRenderer line, Transform target)
    {
        Transform startTransform = ResolvePlayer();
        if (startTransform == null || target == null)
        {
            line.enabled = false;
            return;
        }

        Vector3 start = startTransform.position + Vector3.up * startHeightOffset;
        Vector3 end = target.position + Vector3.up * endHeightOffset;
        Vector3 control = BuildControlPoint(start, end, arcHeightFactor, arcSideFactor);

        int count = Mathf.Max(4, segmentCount);
        if (pathPoints == null || pathPoints.Length != count)
            pathPoints = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            float t = i / (count - 1f);
            pathPoints[i] = SampleQuadraticBezier(start, control, end, t);
        }

        line.positionCount = count;
        line.SetPositions(pathPoints);
    }

    Transform ResolvePlayer()
    {
        if (player != null)
            return player;

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
            player = tagged.transform;

        return player;
    }

    static Vector3 BuildControlPoint(Vector3 start, Vector3 end, float heightFactor, float sideFactor)
    {
        Vector3 mid = (start + end) * 0.5f;
        Vector3 dir = end - start;
        float distance = dir.magnitude;
        if (distance <= 0.001f)
            return mid + Vector3.up;

        dir /= distance;
        Vector3 side = Vector3.Cross(dir, Vector3.up);
        if (side.sqrMagnitude < 0.0001f)
            side = Vector3.right;
        else
            side.Normalize();

        return mid
            + Vector3.up * (distance * heightFactor)
            + side * (distance * sideFactor);
    }

    static Vector3 SampleQuadraticBezier(Vector3 a, Vector3 c, Vector3 b, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * c + t * t * b;
    }

    void ApplyLineSettings(LineRenderer line)
    {
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCapVertices = 4;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.startColor = lineColor;
        line.endColor = lineColor;

        if (line.sharedMaterial == null)
            line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    void OnValidate()
    {
        for (int i = 0; i < linePool.Count; i++)
        {
            LineRenderer line = linePool[i];
            if (line == null)
                continue;

            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.startColor = lineColor;
            line.endColor = lineColor;
        }
    }
}
