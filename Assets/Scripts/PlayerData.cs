using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Text;
using System.IO;

public class PlayerData : MonoBehaviour
{
    [Header("Game Data")]
    public string playerId = "player_1";
    public string playerName = "Player";
    public int units;
    public int food;
    public int wood;
    public int rock;

    [Header("References")]
    public TextMeshProUGUI coutUnits;
    public TextMeshProUGUI coutFoods;
    public TextMeshProUGUI coutWoods;
    public TextMeshProUGUI coutRocks;

    [Header("Server Configuration")]
    [SerializeField] private string serverUrl = "http://localhost:5000";

    [Header("Prefabs for Loading")]
    public GameObject[] buildingPrefabs;
    public GameObject[] unitPrefabs;
    public GameObject treePrefab;
    public GameObject rockPrefab;

    void Start()
    {
        // Сначала тестируем соединение
        StartCoroutine(TestServerConnection());

        // Затем загружаем игру (если нужно)
        // Invoke(nameof(LoadGame), 2f);
    }

    IEnumerator TestServerConnection()
    {
        string url = $"{serverUrl}/api/game/test";
        Debug.Log($"🔍 Testing connection to: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            Debug.Log($"Status Code: {request.responseCode}");
            Debug.Log($"Error: {request.error}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Server connection successful!");
                Debug.Log($"Response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"❌ Server connection failed!");
                Debug.LogError($"Error details: {request.error}");
                Debug.Log($"Response: {request.downloadHandler?.text}");

                // Проверяем другие возможные порты
                yield return StartCoroutine(TestAlternativePorts());
            }
        }
    }

    IEnumerator TestAlternativePorts()
    {
        string[] ports = { "5000", "5001", "8080", "8081" };

        foreach (var port in ports)
        {
            string url = $"http://localhost:{port}/api/game/test";
            Debug.Log($"Trying port {port}: {url}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 3;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"✅ Found server on port {port}!");
                    Debug.Log($"Response: {request.downloadHandler.text}");
                    serverUrl = $"http://localhost:{port}";
                    yield break;
                }
            }
        }

        Debug.LogError("❌ Could not find server on any port!");
    }

    public void LoadGame()
    {
        StartCoroutine(LoadGameCoroutine());
    }

    private IEnumerator LoadGameCoroutine()
    {
        string url = $"{serverUrl}/api/game/load/{playerId}";
        Debug.Log($"📥 Loading game from: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            Debug.Log($"Status: {request.responseCode}");
            Debug.Log($"Error: {request.error}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Game loaded!");
                Debug.Log($"Response: {request.downloadHandler.text}");

                // Десериализуем ответ
                GameState gameState = JsonUtility.FromJson<GameState>(request.downloadHandler.text);
                ApplyGameState(gameState);
            }
            else
            {
                Debug.LogError($"❌ Failed to load game: {request.error}");
                Debug.Log($"Response body: {request.downloadHandler?.text}");
            }
        }
    }

    // Новый метод для сохранения полного состояния игры
    public void SaveGame()
    {
        StartCoroutine(SaveGameCoroutine());
    }

    private IEnumerator SaveGameCoroutine()
    {
        // Собираем все данные игры
        GameState gameState = new GameState
        {
            PlayerData = new PlayerDataEntity
            {
                PlayerId = playerId,
                PlayerName = playerName,
                Units = units,
                Food = food,
                Wood = wood,
                Rock = rock
            },
            Units = DataCollector.CollectAllUnits(),
            Buildings = DataCollector.CollectAllBuildings(),
            Resources = DataCollector.CollectResources()
        };

        string url = $"{serverUrl}/api/game/save/{playerId}";
        string jsonData = JsonUtility.ToJson(gameState, true);
        Debug.Log($"Saving game state...");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            Debug.Log($"Save Status: {request.responseCode}");
            Debug.Log($"Error: {request.error}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Game state saved!");
                Debug.Log($"Response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"❌ Save failed: {request.error}");
                Debug.Log($"Response: {request.downloadHandler?.text}");
            }
        }
    }

    // Новый метод для применения загруженного состояния
    private void ApplyGameState(GameState gameState)
    {
        if (gameState == null || gameState.PlayerData == null)
        {
            Debug.LogError("Invalid game state received");
            return;
        }

        // Применяем данные игрока
        playerName = gameState.PlayerData.PlayerName;
        units = gameState.PlayerData.Units;
        food = gameState.PlayerData.Food;
        wood = gameState.PlayerData.Wood;
        rock = gameState.PlayerData.Rock;

        Debug.Log($"Applied player data: {playerName}, Units: {units}, Food: {food}, Wood: {wood}, Rock: {rock}");
        UpdateUI();

        // Создаем здания
        if (gameState.Buildings != null)
        {
            ApplyBuildings(gameState.Buildings);
        }

        // Создаем юнитов
        if (gameState.Units != null)
        {
            ApplyUnits(gameState.Units);
        }

        // Создаем ресурсы
        if (gameState.Resources != null)
        {
            ApplyResources(gameState.Resources);
        }
    }

    private void ApplyBuildings(List<BuildingEntity> buildings)
    {
        foreach (BuildingEntity building in buildings)
        {
            // Найти префаб по типу здания
            GameObject prefab = GetBuildingPrefab(building.BuildingType);
            if (prefab != null)
            {
                GameObject buildingObj = Instantiate(prefab,
                    new Vector3(building.CoordX, 0, building.CoordY),
                    Quaternion.identity);

                // Устанавливаем здоровье
                Health health = buildingObj.GetComponent<Health>();
                if (health != null)
                {
                    health.health = building.CurrentHealth;
                    health.maxHealth = building.MaxHealth;
                }

                // Устанавливаем тег
                if (building.BuildingType == "MainBuilding")
                    buildingObj.tag = "Base";
                else
                    buildingObj.tag = "Building";

                Debug.Log($"Created building: {building.BuildingType} at ({building.CoordX}, {building.CoordY})");
            }
        }
    }

    private void ApplyUnits(List<UnitEntity> units)
    {
        foreach (UnitEntity unit in units)
        {
            GameObject prefab = GetUnitPrefab(unit.UnitType);
            if (prefab != null)
            {
                GameObject unitObj = Instantiate(prefab,
                    new Vector3(unit.CoordX, 0, unit.CoordY),
                    Quaternion.identity);

                // Устанавливаем здоровье
                Health health = unitObj.GetComponent<Health>();
                if (health != null)
                {
                    health.health = unit.CurrentHealth;
                    health.maxHealth = unit.MaxHealth;
                }

                unitObj.tag = unit.UnitType;
                Debug.Log($"Created unit: {unit.UnitType} at ({unit.CoordX}, {unit.CoordY})");
            }
        }
    }

    private void ApplyResources(List<ResourceEntity> resources)
    {
        foreach (ResourceEntity resource in resources)
        {
            GameObject prefab = resource.Type == "Tree" ? treePrefab : rockPrefab;
            if (prefab != null)
            {
                GameObject resourceObj = Instantiate(prefab,
                    new Vector3(resource.CoordX, 0, resource.CoordY),
                    Quaternion.identity);

                resourceObj.tag = resource.Type;
                Debug.Log($"Created resource: {resource.Type} at ({resource.CoordX}, {resource.CoordY})");
            }
        }
    }

    private GameObject GetBuildingPrefab(string buildingType)
    {
        foreach (GameObject prefab in buildingPrefabs)
        {
            if (prefab.name.Contains(buildingType))
                return prefab;
        }
        return null;
    }

    private GameObject GetUnitPrefab(string unitType)
    {
        foreach (GameObject prefab in unitPrefabs)
        {
            if (prefab.name.Contains(unitType))
                return prefab;
        }
        return null;
    }

    // Метод для удаления игры с сервера
    public IEnumerator DeleteGameCoroutine()
    {
        string url = $"{serverUrl}/api/game/delete/{playerId}";
        Debug.Log($"🗑️ Deleting game: {url}");

        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Game deleted from server!");
            }
            else
            {
                Debug.LogError($"❌ Delete failed: {request.error}");
            }
        }
    }

    // Публичный метод для удаления (вызывается из GameManager)
    public void SendDeleteRequest()
    {
        StartCoroutine(DeleteGameCoroutine());
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
}

[System.Serializable]
public class PlayerDataEntity
{
    public string PlayerId;
    public string PlayerName;
    public int Units;
    public int Food;
    public int Wood;
    public int Rock;
}