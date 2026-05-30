using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    private List<ResidentialBuilding> residentialBuildings = new List<ResidentialBuilding>();
    private int totalCapacity = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterResidential(ResidentialBuilding building)
    {
        if (!residentialBuildings.Contains(building))
        {
            residentialBuildings.Add(building);
            RecalculateTotalCapacity();
        }
    }

    public void UnregisterResidential(ResidentialBuilding building)
    {
        if (residentialBuildings.Contains(building))
        {
            residentialBuildings.Remove(building);
            RecalculateTotalCapacity();
        }
    }

    /// <summary>
    /// Пересчитывает общую вместимость жилых зданий и корректирует количество юнитов,
    /// если оно превышает новую вместимость.
    /// </summary>
    private void RecalculateTotalCapacity()
    {
        int oldCapacity = totalCapacity;
        totalCapacity = 0;

        foreach (var b in residentialBuildings)
        {
            if (b != null)
                totalCapacity += b.capacity;
        }

    }

    public int GetTotalCapacity()
    {
        return totalCapacity;
    }

    public bool HasAnyResidential()
    {
        return totalCapacity > 0;
    }
}