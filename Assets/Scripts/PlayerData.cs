using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

[System.Serializable]
public class BuildingData
{
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;
}

[System.Serializable]
public class PlayerSaveData
{
    public string playerName;
    public int units;
    public int food;
    public int wood;
    public int rock;
    public List<BuildingData> buildings = new List<BuildingData>();
}

public class PlayerData : MonoBehaviour
{
    private string savePath;

    public string playerName;
    public int units;
    public int food;
    public int wood;
    public int rock;
    public GameObject[] buildingPrefabs;
    public List<GameObject> placedBuildings = new List<GameObject>();

    public TextMeshProUGUI coutUnits;
    public TextMeshProUGUI coutFoods;
    public TextMeshProUGUI coutWoods;
    public TextMeshProUGUI coutRocks;

    void Start()
    {
        savePath = Path.Combine(Application.persistentDataPath, "save.json");
        LoadGame();
        StartCoroutine(SaveGamePeriodically());
    }

    IEnumerator SaveGamePeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            SaveAllGame();
        }
    }

    void SaveAllGame()
    {
        CleanDestroyedBuildings();

        var saveData = new PlayerSaveData
        {
            playerName = playerName,
            units = units,
            food = food,
            wood = wood,
            rock = rock,
            buildings = new List<BuildingData>()
        };

        for (int i = placedBuildings.Count - 1; i >= 0; i--)
        {
            var building = placedBuildings[i];
            if (building == null)
            {
                placedBuildings.RemoveAt(i);
                continue;
            }

            if (building.TryGetComponent<BuildingIdentifier>(out var identifier) && identifier.prefab != null)
            {
                saveData.buildings.Add(new BuildingData
                {
                    prefabName = identifier.prefab.name,
                    position = building.transform.position,
                    rotation = building.transform.rotation
                });
            }
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Game saved successfully");
    }

    void CleanDestroyedBuildings()
    {
        for (int i = placedBuildings.Count - 1; i >= 0; i--)
        {
            if (placedBuildings[i] == null)
            {
                placedBuildings.RemoveAt(i);
            }
        }
    }

    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);

            ClearExistingBuildings();
            ApplyLoadedData(data);
        }
        else
        {
            SetDefaultValues();
            Debug.Log("No save file found. Using default values.");
        }
    }

    private void ClearExistingBuildings()
    {
        foreach (var building in placedBuildings)
        {
            if (building != null)
            {
                Destroy(building);
            }
        }
        placedBuildings.Clear();
    }

    private void ApplyLoadedData(PlayerSaveData data)
    {
        playerName = data.playerName;
        units = data.units;
        food = data.food;
        wood = data.wood;
        rock = data.rock;

        if (data.buildings != null)
        {
            foreach (var buildingData in data.buildings)
            {
                GameObject prefab = System.Array.Find(buildingPrefabs,
                    p => p != null && p.name == buildingData.prefabName);

                if (prefab != null)
                {
                    var newBuilding = Instantiate(prefab,
                        buildingData.position,
                        buildingData.rotation);

                    if (newBuilding != null)
                    {
                        placedBuildings.Add(newBuilding);
                    }
                }
                else
                {
                    Debug.LogError($"Prefab {buildingData.prefabName} not found!");
                }
            }
        }
    }

    private void SetDefaultValues()
    {
        playerName = "Player";
        units = 0;
        food = 0;
        wood = 0;
        rock = 0;
    }

    void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        coutUnits.text = units.ToString();
        coutFoods.text = food.ToString();
        coutWoods.text = wood.ToString();
        coutRocks.text = rock.ToString();
    }
}

public class BuildingIdentifier : MonoBehaviour
{
    public GameObject prefab;
}