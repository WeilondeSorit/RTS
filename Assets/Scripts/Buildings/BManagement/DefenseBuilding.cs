using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefenseBuilding : BasicBulding
{
    [Header("Attack Settings")]
    public float attackRange = 12f;
    public float attackCooldown = 2f;
    public int damage = 25;
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
        Collider[] targets;

        // Если targetLayer не назначен, ищем по всем слоям
        if (targetLayer.value == 0)
        {
            targets = Physics.OverlapSphere(transform.position, attackRange);
        }
        else
        {
            targets = Physics.OverlapSphere(transform.position, attackRange, targetLayer);
        }

        // 1. Приоритет: вражеские юниты (компонент Enemy)
        foreach (Collider col in targets)
        {
            if (col.transform == transform) continue;               // себя не бьём
            if (col.GetComponent<BasicBulding>() != null) continue; // здания не трогаем
            if (col.GetComponent<BasicUnit>() != null) continue;    // своих юнитов пропускаем

            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                lastAttackTime = Time.time;
                return;
            }
        }

        // 2. Остальные враги (Health, но без BasicBulding и BasicUnit)
        foreach (Collider col in targets)
        {
            if (col.transform == transform) continue;
            if (col.GetComponent<BasicBulding>() != null) continue;
            if (col.GetComponent<BasicUnit>() != null) continue;

            Health health = col.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
                lastAttackTime = Time.time;
                return;
            }
        }
        // Если ничего не нашли – атака не происходит, кулдаун не тратится
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}