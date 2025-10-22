using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Text;

//этот файл идет на диплом
[System.Serializable]
public class SupabasePlayerData
{
    public string player_name;
    public int units;
    public int food;
    public int wood;
    public int rock;
}

[System.Serializable]
public class SupabaseBuildingList
{
    public SupabaseBuilding[] buildings;
}

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

// Классы для взаимодействия с Supabase
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

        // Локальное сохранение
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Game saved locally");

        // Сохранение в Supabase
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
        // Удаляем старые здания
        string deleteUrl = $"{supabaseUrl}building";
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

        // Сохраняем новые здания
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
            string url = $"{supabaseUrl}building";

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

    public void ClearAllBuildingsData()
    {
        StartCoroutine(ClearAllBuildingsFromSupabase());
        ClearLocalBuildings();
    }

    IEnumerator ClearAllBuildingsFromSupabase()
    {
        // Удаляем все здания из Supabase
        string deleteUrl = $"{supabaseUrl}building";
        using (UnityWebRequest deleteRequest = UnityWebRequest.Delete(deleteUrl))
        {
            deleteRequest.SetRequestHeader("apikey", supabaseKey);
            deleteRequest.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            deleteRequest.SetRequestHeader("Prefer", "return=minimal");

            yield return deleteRequest.SendWebRequest();

            if (deleteRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Failed to delete buildings from Supabase: {deleteRequest.error}");
            }
            else
            {
                Debug.Log("All buildings deleted from Supabase");
            }
        }

        // Сбрасываем ресурсы игрока в Supabase
        yield return StartCoroutine(ResetPlayerDataInSupabase());
    }

    IEnumerator ResetPlayerDataInSupabase()
    {
        var resetData = new SupabasePlayerData
        {
            player_name = "Player",
            units = 0,
            food = 0,
            wood = 0,
            rock = 0
        };

        string jsonData = JsonUtility.ToJson(resetData);
        string url = $"{supabaseUrl}player_data?player_id=eq.1";

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            request.SetRequestHeader("Prefer", "return=minimal");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to reset player data in Supabase: {request.error}");
            }
            else
            {
                Debug.Log("Player data reset in Supabase");
            }
        }
    }

    void ClearLocalBuildings()
    {
        // Очищаем локальные здания
        foreach (var building in placedBuildings)
        {
            if (building != null)
            {
                Destroy(building);
            }
        }
        placedBuildings.Clear();

        // Сбрасываем ресурсы
        units = 0;
        food = 0;
        wood = 0;
        rock = 0;
        UpdateUI();
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
        StartCoroutine(LoadGameCoroutine());
    }

    private IEnumerator LoadGameCoroutine()
    {
        bool databaseLoadSuccessful = false;

        // Пытаемся загрузить из базы данных
        yield return StartCoroutine(LoadFromDatabase((success) => {
            databaseLoadSuccessful = success;
        }));

        // Если загрузка из базы не удалась, загружаем из JSON
        if (!databaseLoadSuccessful)
        {
            LoadFromJSON();
            Debug.Log("Database load failed. Loaded from JSON backup.");
        }
        else
        {
            Debug.Log("Game successfully loaded from database.");
        }

        UpdateUI();
    }

    private IEnumerator LoadFromDatabase(System.Action<bool> onComplete)
    {
        bool success = false;

        // Загружаем данные игрока из базы данных
        bool playerDataSuccess = false;
        yield return StartCoroutine(LoadPlayerDataFromDatabase((result) => {
            playerDataSuccess = result;
        }));

        // Загружаем здания из базы данных
        bool buildingsSuccess = false;
        if (playerDataSuccess)
        {
            yield return StartCoroutine(LoadBuildingsFromDatabase((result) => {
                buildingsSuccess = result;
            }));
        }

        success = playerDataSuccess && buildingsSuccess;
        onComplete?.Invoke(success);
    }

    private IEnumerator LoadPlayerDataFromDatabase(System.Action<bool> onComplete)
    {
        string url = $"{supabaseUrl}player_data?player_id=eq.1";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;

                // Обрабатываем JSON ответ, так как Supabase возвращает массив
                if (jsonResponse.StartsWith("[") && jsonResponse.EndsWith("]"))
                {
                    jsonResponse = jsonResponse.Substring(1, jsonResponse.Length - 2);
                }

                if (!string.IsNullOrEmpty(jsonResponse) && jsonResponse != "[]")
                {
                    try
                    {
                        var playerData = JsonUtility.FromJson<SupabasePlayerData>(jsonResponse);
                        if (playerData != null)
                        {
                            playerName = playerData.player_name;
                            units = playerData.units;
                            food = playerData.food;
                            wood = playerData.wood;
                            rock = playerData.rock;
                            onComplete?.Invoke(true);
                            yield break;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error parsing player data: {e.Message}");
                    }
                }
            }
            else
            {
                Debug.LogError($"Failed to load player data from database: {request.error}");
            }

            onComplete?.Invoke(false);
        }
    }

    private IEnumerator LoadBuildingsFromDatabase(System.Action<bool> onComplete)
    {
        string url = $"{supabaseUrl}building";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;

                try
                {
                    // Оборачиваем JSON в объект для десериализации
                    string wrappedJson = "{\"buildings\":" + jsonResponse + "}";
                    var buildingList = JsonUtility.FromJson<SupabaseBuildingList>(wrappedJson);

                    if (buildingList != null && buildingList.buildings != null && buildingList.buildings.Length > 0)
                    {
                        ClearExistingBuildings();

                        foreach (var building in buildingList.buildings)
                        {
                            GameObject prefab = System.Array.Find(buildingPrefabs,
                                p => p != null && p.name == building.building_name);

                            if (prefab != null)
                            {
                                Vector3 position = new Vector3(building.position_x, 0, building.position_y);
                                var newBuilding = Instantiate(prefab, position, Quaternion.identity);

                                if (newBuilding != null)
                                {
                                    placedBuildings.Add(newBuilding);
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"Prefab {building.building_name} not found!");
                            }
                        }
                        onComplete?.Invoke(true);
                        yield break;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing buildings data: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Failed to load buildings from database: {request.error}");
            }

            onComplete?.Invoke(false);
        }
    }

    private void LoadFromJSON()
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
        string url = $"{supabaseUrl}building";
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
                // Здесь можно добавить обработку JSON и создание зданий в игре
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