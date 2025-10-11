using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicBulding : Health
{
    public float healthBuld;
    [SerializeField] private Health healthComponent;
    [SerializeField] private int baseHealth = 500;

    void Start()
    {
        if (healthComponent == null)
            healthComponent = GetComponent<Health>();
        
        healthComponent.health = baseHealth;
    }

    public void DamageBuilding(int damage)
    {
        healthComponent.TakeDamage(damage);

        // Дополнительная логика для здания
        if (healthComponent.health <= 0)
        {
            HandleBuildingDestruction();
        }
    }

    private void HandleBuildingDestruction()
    {
        // Логика уничтожения здания
        Destroy(gameObject);
    }
}