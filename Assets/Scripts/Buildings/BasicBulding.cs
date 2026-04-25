using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicBulding : Health
{
    public float healthBuld;
    [SerializeField] private Health healthComponent;
    [SerializeField] private int baseHealth = 500;

    protected virtual void Start()
    {
        if (healthComponent == null)
            healthComponent = GetComponent<Health>();

        healthComponent.health = baseHealth;
    }

    public void DamageBuilding(int damage)
    {
        healthComponent.TakeDamage(damage);
        if (healthComponent.health <= 0)
        {
            HandleBuildingDestruction();
        }
    }

    protected virtual void HandleBuildingDestruction()
    {
        // Уничтожение объекта
        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        // Будет переопределено в жилых зданиях для дерегистрации
    }
}