using UnityEngine;
using System;

public class BasicBulding : Health
{
    [Header("Building Settings")]
    [SerializeField] private int baseHealth = 500;

    public event Action<BasicBulding> OnBuildingDestroyed;

    private bool isBuilt = false;

    protected override void Start()
    {
        maxHealth = baseHealth;
        base.Start();
        InitializeBuilding();

        // === НОВОЕ: применяем эффект улучшения зданий, если куплено ===
        if (ShopEffectManager.Instance != null)
            ShopEffectManager.Instance.ApplyBuildingUpgrade(this);
    }

    private void InitializeBuilding()
    {
        if (isBuilt) return;
        isBuilt = true;
        SetHealth(baseHealth);
    }

    public void DamageBuilding(int damage)
    {
        if (!isBuilt) InitializeBuilding();
        TakeDamage(damage);
    }

    protected virtual void HandleBuildingDestruction()
    {
        OnBuildingDestroyed?.Invoke(this);
    }

    protected override void Die()
    {
        HandleBuildingDestruction();
        base.Die();
    }

    private void OnDestroy()
    {
        OnBuildingDestroyed = null;
    }

    public void SetupBuilding(int customHealth)
    {
        baseHealth = customHealth;
        maxHealth = customHealth;
        SetHealth(customHealth);
        isBuilt = true;
    }

    public bool IsBuilt => isBuilt;
}