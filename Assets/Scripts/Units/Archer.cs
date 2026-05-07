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

    // ===== ПАРАМЕТРЫ БОЕВОГО ДУХА (МОРАЛИ) =====
    [Header("Morale")]
    [SerializeField] public float morale = 100f;
    [SerializeField] public float maxMorale = 100f;
    [SerializeField] private float moraleDecreaseRate = 5f;   // единиц в секунду при голоде
    [SerializeField] private float moraleRecoveryRate = 2f;   // восстановление при наличии еды
    [SerializeField] private float healthDamageRate = 2f;     // урон здоровью в секунду при morale ≤ 0

    private float pendingDamage = 0f; // накопленный дробный урон, чтобы не терять остаток

    void Update()
    {
        attackRange = 5f;

        // Обновление морали в зависимости от наличия еды
        if (PlayerData.Instance != null)
        {
            if (PlayerData.Instance.food <= 0)
            {
                // Голод – мораль падает
                morale -= moraleDecreaseRate * Time.deltaTime;
                if (morale < 0f) morale = 0f;
            }
            else
            {
                // Еда есть – медленно восстанавливаем боевой дух
                morale += moraleRecoveryRate * Time.deltaTime;
                if (morale > maxMorale) morale = maxMorale;
            }

            // Если мораль на нуле – постепенный урон и невозможность атаковать
            if (morale <= 0f)
            {
                // Накапливаем урон
                pendingDamage += healthDamageRate * Time.deltaTime;

                // Наносим целочисленный урон, когда накопится >= 1
                int damageToApply = Mathf.FloorToInt(pendingDamage);
                if (damageToApply > 0)
                {
                    pendingDamage -= damageToApply;

                    Health myHealth = GetComponent<Health>();
                    if (myHealth != null)
                    {
                        myHealth.TakeDamage(damageToApply);
                    }
                }
                return; // Полная блокировка любых действий (включая атаку)
            }
        }

        // Атака только если мораль > 0
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    void Attack()
    {
        // Дополнительная страховка: нет морали – нет атаки
        if (morale <= 0f) return;

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