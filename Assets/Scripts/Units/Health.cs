using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int health;

    // Событие для оповещения других систем о смерти юнита
    public static event System.Action<GameObject, string> OnUnitKilled;

    void Start()
    {
        health = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Проверяем, является ли объект врагом по его тегу
        bool isEnemy = false;

        // Список тегов, которые считаются вражескими
        string[] enemyTags = { "Enemy", "Archer", "Knight", "EnemyBase", "EnemyArcher", "EnemyKnight" };

        foreach (string tag in enemyTags)
        {
            if (CompareTag(tag))
            {
                isEnemy = true;
                break;
            }
        }

        // Если это враг – уведомляем систему достижений через PlayerData
        if (isEnemy && PlayerData.Instance != null)
        {
            // Передаём тег убитого объекта (можно использовать для более точной статистики)
            PlayerData.Instance.OnEnemyUnitKilled(tag);

            // Также вызываем статическое событие для других подписчиков (опционально)
            OnUnitKilled?.Invoke(gameObject, tag);
        }

        // Уничтожаем объект
        Destroy(gameObject);
    }
}