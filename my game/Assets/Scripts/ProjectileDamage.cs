using UnityEngine;

/// <summary>
/// 挂到玩家法球/弹道预制体上，碰撞时对怪物 MonsterHealth 造成伤害。
/// </summary>
[DisallowMultipleComponent]
public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] float damage = 15f;

    public float Damage => damage;

    public void SetDamage(float amount)
    {
        damage = Mathf.Max(0f, amount);
    }

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
        MonsterHealth health = col.GetComponentInParent<MonsterHealth>();
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(damage);
            return;
        }

        Health legacy = col.GetComponentInParent<Health>();
        if (legacy != null && !legacy.IsDead)
            legacy.TakeDamage(damage);
    }
}
