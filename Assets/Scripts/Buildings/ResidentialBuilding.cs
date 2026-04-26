using UnityEngine;

public abstract class ResidentialBuilding : BasicBulding
{
    public int capacity; // вместимость (задаётся в наследниках)

    protected override void Start()
    {
        base.Start();
        BuildingManager.Instance.RegisterResidential(this);
    }



    protected override void HandleBuildingDestruction()
    {
        // Доп. логика при разрушении жилого здания
        base.HandleBuildingDestruction();
    }
}