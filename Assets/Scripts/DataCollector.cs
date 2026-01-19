using System.Collections.Generic;
using UnityEngine;

public class DataCollector : MonoBehaviour
{
    // Метод для сбора всех зданий
    public static List<BuildingEntity> CollectAllBuildings()
    {
        List<BuildingEntity> buildings = new List<BuildingEntity>();

        // Ищем все здания по тегу Building (или Base)
        string[] buildingTags = { "Building", "Base" };

        foreach (string tag in buildingTags)
        {
            GameObject[] buildingObjects = GameObject.FindGameObjectsWithTag(tag);

            foreach (GameObject building in buildingObjects)
            {
                Health health = building.GetComponent<Health>();
                if (health == null) continue;

                BuildingIdentifier identifier = building.GetComponent<BuildingIdentifier>();
                string buildingType = "Building";

                if (identifier != null && identifier.prefab != null)
                {
                    buildingType = GetBuildingType(identifier.prefab.name);
                }
                else
                {
                    // Определяем тип по имени объекта или тегу
                    buildingType = building.CompareTag("Base") ? "MainBuilding" : "Building";
                }

                buildings.Add(new BuildingEntity
                {
                    Id = building.GetInstanceID().ToString(),
                    PlayerId = "player_1",
                    BuildingType = buildingType,
                    CoordX = Mathf.RoundToInt(building.transform.position.x),
                    CoordY = Mathf.RoundToInt(building.transform.position.z),
                    CurrentHealth = (int)health.health,
                    MaxHealth = (int)health.maxHealth,
                    Level = 1
                });
            }
        }

        return buildings;
    }

    // Метод для сбора всех юнитов
    public static List<UnitEntity> CollectAllUnits()
    {
        List<UnitEntity> units = new List<UnitEntity>();

        // Ищем юнитов по тегу Unit (или другие теги юнитов)
        string[] unitTags = { "Unit", "Villager", "Archer", "Healer", "Warrior" };

        foreach (string tag in unitTags)
        {
            GameObject[] unitObjects = GameObject.FindGameObjectsWithTag(tag);

            foreach (GameObject unit in unitObjects)
            {
                Health health = unit.GetComponent<Health>();
                if (health == null) continue;

                // Определяем тип юнита
                string unitType = GetUnitType(unit);

                units.Add(new UnitEntity
                {
                    Id = unit.GetInstanceID().ToString(),
                    PlayerId = "player_1",
                    UnitType = unitType,
                    CoordX = Mathf.RoundToInt(unit.transform.position.x),
                    CoordY = Mathf.RoundToInt(unit.transform.position.z),
                    CurrentHealth = (int)health.health,
                    MaxHealth = (int)health.maxHealth,
                    Properties = new System.Collections.Generic.Dictionary<string, object>()
                });
            }
        }

        return units;
    }

    // Метод для сбора ресурсов (деревьев и камней)
    public static List<ResourceEntity> CollectResources()
    {
        List<ResourceEntity> resources = new List<ResourceEntity>();

        // Ищем деревья и камни
        GameObject[] trees = GameObject.FindGameObjectsWithTag("Tree");
        GameObject[] rocks = GameObject.FindGameObjectsWithTag("Rock");

        foreach (GameObject tree in trees)
        {
            resources.Add(new ResourceEntity
            {
                Type = "Tree",
                CoordX = Mathf.RoundToInt(tree.transform.position.x),
                CoordY = Mathf.RoundToInt(tree.transform.position.z),
                ResourceType = "Wood",
                Amount = 100 // Количество ресурса в дереве
            });
        }

        foreach (GameObject rock in rocks)
        {
            resources.Add(new ResourceEntity
            {
                Type = "Rock",
                CoordX = Mathf.RoundToInt(rock.transform.position.x),
                CoordY = Mathf.RoundToInt(rock.transform.position.z),
                ResourceType = "Rock",
                Amount = 100 // Количество ресурса в камне
            });
        }

        return resources;
    }

    // Вспомогательные методы
    private static string GetBuildingType(string prefabName)
    {
        if (prefabName.ToLower().Contains("farm")) return "Farm";
        if (prefabName.ToLower().Contains("tower")) return "DefenseTower";
        if (prefabName.ToLower().Contains("house")) return "MainBuilding";
        if (prefabName.ToLower().Contains("barrack")) return "Barracks";
        return "Building";
    }

    private static string GetUnitType(GameObject unit)
    {
        if (unit.CompareTag("Villager")) return "Villager";
        if (unit.CompareTag("Archer")) return "Archer";
        if (unit.CompareTag("Healer")) return "Healer";
        if (unit.CompareTag("Warrior")) return "Warrior";
        return "Unit";
    }
}

// Классы для хранения данных
[System.Serializable]
public class GameState
{
    public PlayerDataEntity PlayerData;
    public List<UnitEntity> Units;
    public List<BuildingEntity> Buildings;
    public List<ResourceEntity> Resources;
}

[System.Serializable]
public class UnitEntity
{
    public string Id;
    public string PlayerId;
    public string UnitType;
    public int CoordX;
    public int CoordY;
    public int CurrentHealth;
    public int MaxHealth;
    public System.Collections.Generic.Dictionary<string, object> Properties;
}

[System.Serializable]
public class BuildingEntity
{
    public string Id;
    public string PlayerId;
    public string BuildingType;
    public int CoordX;
    public int CoordY;
    public int CurrentHealth;
    public int MaxHealth;
    public int Level;
}

[System.Serializable]
public class ResourceEntity
{
    public string Type;
    public int CoordX;
    public int CoordY;
    public string ResourceType;
    public int Amount;
}