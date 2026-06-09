using UnityEngine;

/// <summary>
/// NPC 专用追踪弹：持续飞向锁定目标，到达后扣血并播放命中特效（不依赖物理碰撞）。
/// </summary>
[DisallowMultipleComponent]
public class NPCHomingProjectile : MonoBehaviour
{
    Transform target;
    float damage;
    float speed;
    float hitDistance;
    float aimHeightOffset;
    GameObject hitVfxPrefab;

    public void Configure(
        Transform targetTransform,
        float damageAmount,
        float moveSpeed,
        GameObject hitEffectPrefab,
        float heightOffset,
        float reachDistance = 0.35f)
    {
        target = targetTransform;
        damage = damageAmount;
        speed = moveSpeed;
        hitVfxPrefab = hitEffectPrefab;
        aimHeightOffset = heightOffset;
        hitDistance = reachDistance;
    }

    void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        if (target.TryGetComponent(out MonsterHealth health) && health.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 aimPoint = GetAimPoint(target, aimHeightOffset);
        Vector3 toTarget = aimPoint - transform.position;
        float distance = toTarget.magnitude;

        if (distance <= hitDistance)
        {
            ApplyLockedHit(aimPoint);
            return;
        }

        Vector3 direction = toTarget / distance;
        transform.position += direction * (speed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(direction);
    }

    void ApplyLockedHit(Vector3 hitPoint)
    {
        if (target != null && target.TryGetComponent(out MonsterHealth health) && !health.IsDead)
            health.TakeDamage(damage);

        SpawnHitEffect(hitPoint);
        Destroy(gameObject);
    }

    void SpawnHitEffect(Vector3 hitPoint)
    {
        if (hitVfxPrefab == null)
            return;

        Vector3 direction = (hitPoint - transform.position).normalized;
        if (direction.sqrMagnitude < 0.0001f && target != null)
            direction = (hitPoint - target.position).normalized;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.up;

        Quaternion rotation = Quaternion.LookRotation(direction);
        GameObject hitInstance = Instantiate(hitVfxPrefab, hitPoint, rotation);
        DestroyAfterParticles(hitInstance);
    }

    static Vector3 GetAimPoint(Transform targetTransform, float heightOffset)
    {
        if (targetTransform.TryGetComponent(out Collider col))
            return col.bounds.center;

        return targetTransform.position + Vector3.up * heightOffset;
    }

    public static void SpawnMuzzleFlash(GameObject flashPrefab, Vector3 position, Vector3 forward)
    {
        if (flashPrefab == null)
            return;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        GameObject flashInstance = Instantiate(flashPrefab, position, Quaternion.LookRotation(forward));
        DestroyAfterParticles(flashInstance);
    }

    static void DestroyAfterParticles(GameObject instance)
    {
        if (instance == null)
            return;

        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            Destroy(instance, ps.main.duration);
            return;
        }

        if (instance.transform.childCount > 0)
        {
            ps = instance.transform.GetChild(0).GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(instance, ps.main.duration);
                return;
            }
        }

        Destroy(instance, 2f);
    }
}
