using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Archer : BasicUnit
{
    public float attackRange = 5f;
    public float attackCooldown = 2f;
    public int damage = 15;
    public LayerMask targetLayer;

    private float lastAttackTime = 0f;

    void Update()
    {
        attackRange = 5f;
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    void Attack()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, attackRange, targetLayer);


        Health targetHealth = null;
        BasicBulding targetBuld = null;

        foreach (Collider targetCollider in targets)
        {
            // Пропускаем себя и юнитов
            if (targetCollider.transform == transform ||
                targetCollider.GetComponent<BasicUnit>() != null)
            {
                continue;
            }

            // Сначала проверяем здания с тегом EnemyBase
            var building = targetCollider.GetComponent<BasicBulding>();
            if (building != null)
            {
                if (targetCollider.CompareTag("EnemyBase"))
                {
                    targetBuld = building;
                    break; // Нашли нужное здание - прерываем поиск
                }
                continue; // Игнорируем другие здания
            }

            // Затем проверяем здоровье у не-зданий
            var health = targetCollider.GetComponent<Health>();
            if (health != null)
            {
                targetHealth = health;
                break; // Нашли любой объект с Health
            }
        }

        // Пример применения урона
        if (targetBuld != null)
        {
            // Наносим урон зданию EnemyBase
            targetBuld.TakeDamage(damage);
            lastAttackTime = Time.time;
        }
        else if (targetHealth != null)
        {
            // Наносим урон другим объектам с Health
            targetHealth.TakeDamage(damage);
            lastAttackTime = Time.time;
        }
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
