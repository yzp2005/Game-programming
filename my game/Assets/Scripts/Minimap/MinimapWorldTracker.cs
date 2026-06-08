using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 持续收集玩家与 MinimapTrackable 的世界坐标（LateUpdate）。
/// 小地图 UI 与其它系统都从这里读位置，不用各自 Find。
/// </summary>
[DisallowMultipleComponent]
public class MinimapWorldTracker : MonoBehaviour
{
    public struct EntitySnapshot
    {
        public MinimapTrackable Source;
        public Vector3 WorldPosition;
        public float Yaw;
        public MinimapTrackable.BlipKind Kind;
        public bool IsValid;
    }

    public struct PlayerSnapshot
    {
        public Vector3 WorldPosition;
        public float Yaw;
        public bool IsValid;
    }

    public static MinimapWorldTracker Instance { get; private set; }

    static readonly List<MinimapTrackable> PendingRegistration = new List<MinimapTrackable>();

    [Header("玩家")]
    [SerializeField] Transform player;

    [Header("刷新")]
    [Tooltip("0 = 每帧刷新；例如 0.1 = 每秒约 10 次")]
    [SerializeField] float refreshInterval;

    readonly List<MinimapTrackable> trackables = new List<MinimapTrackable>();
    readonly List<EntitySnapshot> entitySnapshots = new List<EntitySnapshot>();

    PlayerSnapshot playerSnapshot;
    float nextRefreshTime;

    public PlayerSnapshot CurrentPlayer => playerSnapshot;
    public IReadOnlyList<EntitySnapshot> CurrentEntities => entitySnapshots;

    public static void Register(MinimapTrackable trackable)
    {
        if (trackable == null)
            return;

        if (Instance != null)
            Instance.RegisterInternal(trackable);
        else if (!PendingRegistration.Contains(trackable))
            PendingRegistration.Add(trackable);
    }

    public static void Unregister(MinimapTrackable trackable)
    {
        if (trackable == null)
            return;

        PendingRegistration.Remove(trackable);

        if (Instance != null)
            Instance.UnregisterInternal(trackable);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{name}: 场景里已有 MinimapWorldTracker，重复实例将被禁用。", this);
            enabled = false;
            return;
        }

        Instance = this;
        RegisterExistingTrackables();
        RefreshNow();
    }

    void RegisterExistingTrackables()
    {
        for (int i = 0; i < PendingRegistration.Count; i++)
        {
            MinimapTrackable trackable = PendingRegistration[i];
            if (trackable != null)
                RegisterInternal(trackable);
        }

        PendingRegistration.Clear();

        MinimapTrackable[] existing = FindObjectsOfType<MinimapTrackable>(true);
        foreach (MinimapTrackable trackable in existing)
            RegisterInternal(trackable);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void LateUpdate()
    {
        if (refreshInterval <= 0f)
        {
            RefreshNow();
            return;
        }

        if (Time.time >= nextRefreshTime)
        {
            nextRefreshTime = Time.time + refreshInterval;
            RefreshNow();
        }
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
    }

    void RegisterInternal(MinimapTrackable trackable)
    {
        if (!trackables.Contains(trackable))
            trackables.Add(trackable);
    }

    void UnregisterInternal(MinimapTrackable trackable)
    {
        trackables.Remove(trackable);
    }

    public void RefreshNow()
    {
        RefreshPlayer();
        RefreshEntities();
    }

    void RefreshPlayer()
    {
        if (player == null)
        {
            playerSnapshot = default;
            return;
        }

        playerSnapshot = new PlayerSnapshot
        {
            WorldPosition = player.position,
            Yaw = player.eulerAngles.y,
            IsValid = true
        };
    }

    void RefreshEntities()
    {
        entitySnapshots.Clear();

        for (int i = trackables.Count - 1; i >= 0; i--)
        {
            MinimapTrackable trackable = trackables[i];
            if (trackable == null || !trackable.isActiveAndEnabled)
            {
                trackables.RemoveAt(i);
                continue;
            }

            entitySnapshots.Add(new EntitySnapshot
            {
                Source = trackable,
                WorldPosition = trackable.Target.position,
                Yaw = trackable.Target.eulerAngles.y,
                Kind = trackable.Kind,
                IsValid = true
            });
        }
    }
}
