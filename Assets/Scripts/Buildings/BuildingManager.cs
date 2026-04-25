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

        // Если вместимость уменьшилась, удаляем лишних юнитов
        if (PlayerData.Instance != null && oldCapacity > totalCapacity)
        {
            int currentUnits = PlayerData.Instance.units;
            if (currentUnits > totalCapacity)
            {
                Debug.Log($"Вместимость жилья уменьшена с {oldCapacity} до {totalCapacity}. Лишние юниты ({currentUnits - totalCapacity}) удалены.");
                PlayerData.Instance.ForceSetUnits(totalCapacity);
            }
        }
        else if (PlayerData.Instance != null && totalCapacity == 0)
        {
            // Если вообще нет жилья, юнитов быть не должно
            if (PlayerData.Instance.units > 0)
            {
                Debug.Log("Нет ни одного жилого здания! Все юниты погибли.");
                PlayerData.Instance.ForceSetUnits(0);
            }
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