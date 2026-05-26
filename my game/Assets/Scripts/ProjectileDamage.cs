using UnityEngine;

/// <summary>
/// 挂到玩家法球/弹道预制体上，碰撞时对怪物 Health 造成伤害。
/// </summary>
[DisallowMultipleComponent]
public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] float damage = 15f;

    void OnCollisionEnter(Collision collision)
    {
        TryDamage(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    void TryDamage(Collider col)
    {
        Health health = col.GetComponentInParent<Health>();
        if (health != null && !health.IsDead)
            health.TakeDamage(damage);
    }
}
