using UnityEngine;

/// <summary>
/// 挂在小地图上需要显示的物体上（怪物、核心等）。启用/禁用时自动注册。
/// </summary>
[DisallowMultipleComponent]
public class MinimapTrackable : MonoBehaviour
{
    public enum BlipKind
    {
        Enemy,
        Objective,
        Friendly
    }

    [SerializeField] BlipKind kind = BlipKind.Enemy;

    public BlipKind Kind => kind;
    public Transform Target => transform;

    void OnEnable()
    {
        MinimapWorldTracker.Register(this);
    }

    void OnDisable()
    {
        MinimapWorldTracker.Unregister(this);
    }
}
