using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Text;

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

// Классы для сериализации данных в Supabase
[System.Serializable]
public class SupabaseBuilding
{
    public string building_name;
    public int health;
    public int max_health;
    public int position_x;
    public int position_y;
}

[System.Serializable]
public class SupabaseUnit
{
    public string unit_name;
    public int health;
    public int max_health;
    public int position_x;
    public int position_y;
    public float speed;
    public int? attack_range;
    public int? damage;
    public int armor;
}

[System.Serializable]
public class SupabaseResponse
{
    public int id;
    public string building_name;
    public int position_x;
    public int position_y;
}

public class PlayerData : MonoBehaviour
{
    private string savePath;
    private string supabaseUrl = "https://ceqdjafzolfhtqjjlvwg.supabase.co/rest/v1/";
    private string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNlcWRqYWZ6b2xmaHRxampsdndnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjAyNTAyMzQsImV4cCI6MjA3NTgyNjIzNH0.N_RQNgbW0jx7mlyUI67sQMaZp38xqMzFR6fJjNN4338";

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
            yield return new WaitForSeconds(30f);
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

        // Сохраняем локально
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Game saved locally");

        // Сохраняем в Supabase
        StartCoroutine(SaveToSupabase(saveData));
    }

    IEnumerator SaveToSupabase(PlayerSaveData saveData)
    {
        Debug.Log("Starting Supabase save...");

        // Сохраняем здания
        yield return StartCoroutine(SaveBuildingsData(saveData.buildings));

        Debug.Log("Supabase save completed");
    }

    IEnumerator SaveBuildingsData(List<BuildingData> buildings)
    {
        // Очищаем старые здания
        string deleteUrl = $"{supabaseUrl}Building";
        using (UnityWebRequest deleteRequest = UnityWebRequest.Delete(deleteUrl))
        {
            deleteRequest.SetRequestHeader("apikey", supabaseKey);
            deleteRequest.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            deleteRequest.SetRequestHeader("Prefer", "return=minimal");

            yield return deleteRequest.SendWebRequest();

            if (deleteRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Failed to delete old buildings: {deleteRequest.error}");
            }
        }

        // Добавляем новые здания
        foreach (var building in buildings)
        {
            var supabaseBuilding = new SupabaseBuilding
            {
                building_name = building.prefabName,
                health = 100,
                max_health = 100,
                position_x = Mathf.RoundToInt(building.position.x),
                position_y = Mathf.RoundToInt(building.position.z)
            };

            string jsonData = JsonUtility.ToJson(supabaseBuilding);
            string url = $"{supabaseUrl}Building";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("apikey", supabaseKey);
                request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
                request.SetRequestHeader("Prefer", "return=representation");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Building save failed: {request.error}");
                    Debug.Log($"Data: {jsonData}");
                }
                else
                {
                    Debug.Log($"Building {building.prefabName} saved to Supabase");
                }
            }
        }

        Debug.Log($"Saved {buildings.Count} buildings to Supabase");
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
        if (coutUnits != null) coutUnits.text = units.ToString();
        if (coutFoods != null) coutFoods.text = food.ToString();
        if (coutWoods != null) coutWoods.text = wood.ToString();
        if (coutRocks != null) coutRocks.text = rock.ToString();
    }

    // Метод для принудительного сохранения
    public void ForceSave()
    {
        SaveAllGame();
    }

    // Метод для загрузки зданий из Supabase
    IEnumerator LoadBuildingsFromSupabase()
    {
        string url = $"{supabaseUrl}Building";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"Buildings loaded: {jsonResponse}");
                // Здесь можно добавить парсинг JSON и создание зданий в игре
            }
            else
            {
                Debug.LogError($"Failed to load buildings: {request.error}");
            }
        }
    }
}

public class BuildingIdentifier : MonoBehaviour
{
    public GameObject prefab;
}