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
    public float morale = 100f;
    public float maxMorale = 100f;
    [SerializeField] private float moraleDecreaseRate = 5f;   // единиц в секунду при голоде
    [SerializeField] private float moraleRecoveryRate = 2f;   // восстановление при наличии еды
    [SerializeField] private float healthDamageRate = 2f;     // урон здоровью в секунду при morale ≤ 0

    // Статический счётчик церквей (MoraleBuilding)
    public static int churchesCount = 0;

    private void Start()
    {
        // Применяем постоянные улучшения
        if (ShopEffectManager.Instance != null)
        {
            ShopEffectManager.Instance.ApplyUnitUpgrade(this);
            ShopEffectManager.Instance.ApplySpeedBoost(this);
        }
    }
    void Update()
    {
        attackRange = 5f;

        // ===== ОБРАБОТКА МОРАЛИ =====
        if (churchesCount > 0)
        {
            // Если есть хотя бы одна церковь — мораль всегда максимум
            morale = maxMorale;
        }
        else
        {
            // Обычная логика голода/восстановления
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
            }
        }

        // Если мораль упала до нуля – урон и блокировка действий
        if (morale <= 0f)
        {
            Health myHealth = GetComponent<Health>();
            if (myHealth != null)
            {
                myHealth.TakeDamage(Mathf.RoundToInt(healthDamageRate * Time.deltaTime));
            }
            return; // Юнит не может атаковать, когда дух сломлен
        }

        // Атака только при положительной морали
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    void Attack()
    {
        if (morale <= 0f) return;

        Collider[] targets = Physics.OverlapSphere(transform.position, attackRange, targetLayer);

        Health targetHealth = null;
        BasicBulding targetBuld = null;

        foreach (Collider targetCollider in targets)
        {
            if (targetCollider.transform == transform ||
                targetCollider.GetComponent<BasicUnit>() != null)
                continue;

            var building = targetCollider.GetComponent<BasicBulding>();
            if (building != null)
            {
                if (targetCollider.CompareTag("EnemyBase"))
                {
                    targetBuld = building;
                    break;
                }
                continue;
            }

            var health = targetCollider.GetComponent<Health>();
            if (health != null)
            {
                targetHealth = health;
                break;
            }
        }

        if (targetBuld != null)
        {
            targetBuld.TakeDamage(damage);
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
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}