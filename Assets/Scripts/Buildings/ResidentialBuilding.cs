using System.Collections;
using UnityEngine;

public abstract class ResidentialBuilding : BasicBulding
{
    public int capacity; // вместимость (задаётся в наследниках)

    protected override void Start()
    {
        base.Start();

        // Если BuildingManager уже инициализирован — регистрируемся сразу
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.RegisterResidential(this);
        }
        else
        {
            // Иначе ждём его появления в корутине
            StartCoroutine(WaitForBuildingManager());
        }
    }

    private IEnumerator WaitForBuildingManager()
    {
        // Ждём, пока синглтон не будет создан
        while (BuildingManager.Instance == null)
            yield return null;

        BuildingManager.Instance.RegisterResidential(this);
    }

    protected override void HandleBuildingDestruction()
    {
        // При разрушении снимаем здание с учёта
        if (BuildingManager.Instance != null)
            BuildingManager.Instance.UnregisterResidential(this);

        base.HandleBuildingDestruction();
    }

    // Дополнительная страховка: если здание уничтожается напрямую (минуя Die)
    private void OnDestroy()
    {
        if (BuildingManager.Instance != null)
            BuildingManager.Instance.UnregisterResidential(this);
    }
}