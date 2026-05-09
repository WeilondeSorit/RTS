using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefenseBuilding : BasicBulding
{
    [Header("Attack Settings")]
    public float attackRange = 12f;        // увеличенный радиус атаки
    public float attackCooldown = 2f;
    public int damage = 25;
    public LayerMask targetLayer;

    private float lastAttackTime = 0f;

    void Update()
    {
        // Атака только когда прошло время перезарядки
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    void Attack()
    {
        // Находим все цели в радиусе атаки
        Collider[] targets = Physics.OverlapSphere(transform.position, attackRange, targetLayer);

        Health targetHealth = null;
        BasicBulding targetBuilding = null; // для вражеской базы

        foreach (Collider targetCollider in targets)
        {
            // Не атакуем себя
            if (targetCollider.transform == transform)
                continue;

            // Пропускаем союзных юнитов (у которых есть компонент BasicUnit)
            if (targetCollider.GetComponent<BasicUnit>() != null)
                continue;

            // Проверяем, является ли цель зданием
            var building = targetCollider.GetComponent<BasicBulding>();
            if (building != null)
            {
                // Атакуем вражескую базу (приоритет)
                if (targetCollider.CompareTag("EnemyBase"))
                {
                    targetBuilding = building;
                    break;
                }
                // Свои здания не трогаем
                continue;
            }

            // Ищем вражеского юнита через компонент Health
            var health = targetCollider.GetComponent<Health>();
            if (health != null)
            {
                targetHealth = health;
                break;
            }
        }

        // Наносим урон выбранной цели
        if (targetBuilding != null)
        {
            targetBuilding.TakeDamage(damage);
            lastAttackTime = Time.time;
        }
        else if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
            lastAttackTime = Time.time;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}