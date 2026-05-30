using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] public int maxHealth = 100;

    [HideInInspector] public int health;

    public int HealthCurrent
    {
        get => _healthCurrent;
        private set
        {
            _healthCurrent = value;
            health = value; // синхронизация с публичным полем
            OnHealthChanged?.Invoke(_healthCurrent, maxHealth);
        }
    }
    private int _healthCurrent;

    public event Action<int, int> OnHealthChanged;
    public static event Action<GameObject, string> OnUnitKilled;

    protected bool isInitialized = false;

    public int MaxHealth => maxHealth;

    protected virtual void Start()
    {
        InitializeHealth();
    }

    protected void InitializeHealth()
    {
        if (isInitialized) return;
        isInitialized = true;
        _healthCurrent = maxHealth;
        health = maxHealth; // синхронизация
        OnHealthChanged?.Invoke(_healthCurrent, maxHealth);
    }

    public virtual void TakeDamage(int damage)
    {
        if (!isInitialized) InitializeHealth();

        HealthCurrent = Mathf.Max(0, _healthCurrent - damage);

        if (_healthCurrent <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        string enemyTag = string.Empty;
        string[] enemyTags = { "Enemy", "Archer","EnemyBase"};

        foreach (string tag in enemyTags)
        {
            if (CompareTag(tag))
            {
                enemyTag = tag;
                break;
            }
        }

        if (PlayerData.Instance != null && !string.IsNullOrEmpty(enemyTag))
        {
            PlayerData.Instance.OnEnemyUnitKilled(enemyTag);
        }

        OnUnitKilled?.Invoke(gameObject, enemyTag);
        Destroy(gameObject);
    }

    public void SetHealth(int value)
    {
        HealthCurrent = Mathf.Clamp(value, 0, maxHealth);
        isInitialized = true;
    }

    public void SetMaxHealth(int value)
    {
        maxHealth = Mathf.Max(1, value);
        HealthCurrent = Mathf.Clamp(_healthCurrent, 0, maxHealth);
    }

    public void Heal(int amount)
    {
        if (!isInitialized) InitializeHealth();
        HealthCurrent = Mathf.Min(_healthCurrent + amount, maxHealth);
    }

    public bool IsAlive => _healthCurrent > 0;
}