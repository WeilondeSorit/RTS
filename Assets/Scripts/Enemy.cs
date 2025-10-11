using UnityEngine;

public class Enemy : Health
{
    public float attackRange = 5f;
    public float attackCooldown = 2f;
    public int damage = 15;
    public LayerMask targetLayer;

    private float lastAttackTime = 0f;

    void Update()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    void Attack()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, attackRange, targetLayer);
        bool damageDealt = false;

        foreach (Collider target in targets)
        {
            // Пропускаем себя
            if (target.transform == transform) continue;

            // Проверяем запрещенные категории
            if (IsInvalidTarget(target)) continue;

            // Наносим урон всем валидным целям
            if (TryDealDamage(target))
            {
                damageDealt = true;
            }
        }

        if (damageDealt)
        {
            lastAttackTime = Time.time;
        }
    }

    bool IsInvalidTarget(Collider target)
    {
        // Проверка на компонент Enemy и тег EnemyBase
        return target.GetComponent<Enemy>() != null || target.CompareTag("EnemyBase");
    }

    bool TryDealDamage(Collider target)
    {
        bool damageDealt = false;

        // Используем TryGetComponent для оптимизации
        if (target.TryGetComponent<Health>(out var health) && health != this)
        {
            health.TakeDamage(damage);
            damageDealt = true;
        }

        if (target.TryGetComponent<BasicBulding>(out var building))
        {
            building.TakeDamage(damage);
            damageDealt = true;
        }

        return damageDealt;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}