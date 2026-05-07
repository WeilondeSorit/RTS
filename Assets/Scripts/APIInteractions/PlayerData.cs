using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance { get; private set; }

    // События для оповещения UI
    public event Action OnResourcesChanged;
    public event Action<string> OnQuestTextChanged;

    [HideInInspector] public bool isGameActive = false;

    // Сохранённые данные из Redis (для восстановления)
    public int savedVillagers;
    public int savedArchers;
    public int savedEnemies;
    public int savedPlayerBaseHp;
    public int savedEnemyBaseHp;

    // ===== ДАННЫЕ С СЕРВЕРА АККАУНТОВ =====
    public string playerId;
    public string playerName;
    public string authToken;
    public int experience;
    public int currency;
    public int wins;
    public int losses;
    public List<int> purchasedItems = new List<int>();
    public Dictionary<string, int> unitUpgrades = new Dictionary<string, int>();

    // ===== ЛОКАЛЬНЫЕ РЕСУРСЫ =====
    [Header("Resources")]
    public int units;
    public int food = 500;
    public int wood = 300;
    public int rock = 200;          // на сервере это поле называется "stone"

    [Header("Unit Consumption")]
    [SerializeField] private float foodConsumptionPerUnitPerSecond = 1f;
    private Coroutine consumptionCoroutine;

    [Header("Servers")]
    [SerializeField] private string accountServerUrl = "http://localhost:8080";
    [SerializeField] private string sessionServerUrl = "http://localhost:8082";
    [SerializeField] private float autoSaveInterval = 30f;

    // ===== ДАННЫЕ СЕССИИ =====
    private string currentSessionId = null;
    private float lastSaveTime = 0f;
    private bool isInitialized = false;

    public AchievementSystem achievementSystem;

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
            return;
        }

        achievementSystem = GetComponent<AchievementSystem>();
        if (achievementSystem == null)
            achievementSystem = gameObject.AddComponent<AchievementSystem>();
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey("PlayerId"))
        {
            LoadSession();
            LoadPlayerData();
            isInitialized = true;

            if (consumptionCoroutine == null)
                consumptionCoroutine = StartCoroutine(FoodConsumptionRoutine());
        }
        else
        {
            Debug.LogWarning("⚠️ Player not authenticated. Redirecting to login...");
        }
    }

    private void Update()
    {
        if (isInitialized && isGameActive && Time.time - lastSaveTime >= autoSaveInterval)
        {
            SaveGameStateToServer();
            lastSaveTime = Time.time;
        }
    }

    // ===== АВТОРИЗАЦИЯ =====
    public void SetAuthToken(string token, string userId, string username)
    {
        authToken = token;
        playerId = userId;
        playerName = username;

        PlayerPrefs.SetString("AuthToken", token);
        PlayerPrefs.SetString("PlayerId", userId);
        PlayerPrefs.SetString("PlayerName", username);
        PlayerPrefs.Save();

        achievementSystem.Initialize(this);
        Debug.Log($"✅ Authenticated: {username} (ID: {playerId})");
    }

    public void SetAuthToken(string token, int userId, string username)
    {
        SetAuthToken(token, userId.ToString(), username);
    }

    private void LoadSession()
    {
        authToken = PlayerPrefs.GetString("AuthToken");
        playerId = PlayerPrefs.GetString("PlayerId", "");
        playerName = PlayerPrefs.GetString("PlayerName", "Player");
        Debug.Log($"🔄 Loaded session for: {playerName} (ID: {playerId})");
    }

    public void UpdateFromServer(PlayerDataResponse serverData)
    {
        experience = serverData.experience;
        currency = serverData.currency;
        wins = serverData.wins;
        losses = serverData.losses;
        purchasedItems = serverData.purchasedItems ?? new List<int>();
        unitUpgrades = serverData.unitUpgrades ?? new Dictionary<string, int>();
        SavePlayerData();
    }

    public void LoadPlayerData()
    {
        if (PlayerPrefs.HasKey($"Player_{playerId}_Data"))
        {
            try
            {
                string json = PlayerPrefs.GetString($"Player_{playerId}_Data");
                var data = JsonUtility.FromJson<PlayerSaveData>(json);
                units = data.units;
                food = data.food;
                wood = data.wood;
                rock = data.rock;
                experience = data.experience;
                currency = data.currency;
                wins = data.wins;
                losses = data.losses;
                if (data.purchasedItems != null) purchasedItems = data.purchasedItems;
                if (data.unitUpgrades != null) unitUpgrades = data.unitUpgrades;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Load error: {ex.Message}");
            }
        }
        OnResourcesChanged?.Invoke();
    }

    public void SavePlayerData()
    {
        var data = new PlayerSaveData
        {
            playerId = playerId,
            playerName = playerName,
            units = units,
            food = food,
            wood = wood,
            rock = rock,
            experience = experience,
            currency = currency,
            wins = wins,
            losses = losses,
            purchasedItems = purchasedItems,
            unitUpgrades = unitUpgrades,
            lastSaved = DateTime.UtcNow.ToString("o")
        };
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString($"Player_{playerId}_Data", json);
        PlayerPrefs.Save();
    }

    // ===== ИГРОВЫЕ СОБЫТИЯ =====
    public void OnEnemyUnitKilled(string unitType)
    {
        achievementSystem?.OnEnemyUnitKilled(unitType);
        food += 5;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
    }

    public void OnResourceCollected(string resourceType, int amount)
    {
        switch (resourceType.ToLower())
        {
            case "food": food += amount; break;
            case "wood": wood += amount; break;
            case "rock": rock += amount; break;
            default: return;
        }
        OnResourcesChanged?.Invoke();
        SavePlayerData();
    }

    // ===== УПРАВЛЕНИЕ ЮНИТАМИ =====
    public bool TryAddUnits(int count)
    {
        if (BuildingManager.Instance == null)
        {
            Debug.LogError("BuildingManager.Instance missing!");
            return false;
        }
        int totalCapacity = BuildingManager.Instance.GetTotalCapacity() + 10; // базовая башня
        if (units + count > totalCapacity)
        {
            Debug.Log($"Not enough housing! Capacity: {totalCapacity}, units: {units}");
            return false;
        }
        units += count;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
        return true;
    }

    public void ForceSetUnits(int newUnits)
    {
        if (newUnits < 0) newUnits = 0;
        units = newUnits;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
    }

    public void AddUnitsIgnoreCapacity(int count)
    {
        if (count < 0) return;
        units += count;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
    }

    // ===== РАСХОД ЕДЫ =====
    public bool TryConsumeFood(int amount)
    {
        if (food < amount) return false;
        food -= amount;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
        return true;
    }

    public void AddFood(int amount)
    {
        food += amount;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
    }

    public void AddWood(int amount)
    {
        wood += amount;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
    }

    public void AddRock(int amount)
    {
        rock += amount;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
    }

    public void SpendResources(int woodCost, int rockCost)
    {
        wood -= woodCost;
        rock -= rockCost;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
    }

    public void AddResources(int woodAmount, int rockAmount)
    {
        wood += woodAmount;
        rock += rockAmount;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
    }

    private IEnumerator FoodConsumptionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (!isGameActive) continue;
            if (units > 0)
            {
                int consumption = Mathf.CeilToInt(units * foodConsumptionPerUnitPerSecond);
                food = Mathf.Max(0, food - consumption);
                OnResourcesChanged?.Invoke();
                SavePlayerData();
                if (food <= 0) Debug.LogWarning("Food depleted!");
            }
        }
    }

    public void UpdateQuestDisplay(string text)
    {
        OnQuestTextChanged?.Invoke(text);
    }

    // ===== ОТПРАВКА РЕЗУЛЬТАТА БОЯ НА ГЛАВНЫЙ СЕРВЕР =====
    public void SendBattleResult(bool isWin, Action<bool, string> onComplete = null)
    {
        if (string.IsNullOrEmpty(authToken))
        {
            onComplete?.Invoke(false, "Not authenticated");
            return;
        }
        StartCoroutine(SendBattleResultCoroutine(isWin, onComplete));
    }

    private IEnumerator SendBattleResultCoroutine(bool isWin, Action<bool, string> onComplete)
    {
        var requestData = new BattleResultRequest { isWin = isWin };
        string jsonData = JsonUtility.ToJson(requestData);
        string url = $"{accountServerUrl}/player/{playerId}/battle-result";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {authToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<BattleResultResponse>(request.downloadHandler.text);
                experience = response.experience;
                currency = response.currency;
                wins = response.wins;
                losses = response.losses;
                SavePlayerData();
                OnResourcesChanged?.Invoke();
                onComplete?.Invoke(true, null);
            }
            else
            {
                onComplete?.Invoke(false, request.error);
            }
        }
    }

    // ===== ИНТЕГРАЦИЯ С СЕРВЕРОМ СЕССИЙ (REDIS) =====
    public void StartServerSession(Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            onComplete?.Invoke(false);
            return;
        }
        StartCoroutine(StartSessionCoroutine(onComplete));
    }

    private IEnumerator StartSessionCoroutine(Action<bool> onComplete)
    {
        string url = $"{sessionServerUrl}/session/start";
        var req = new StartSessionRequest { playerId = playerId };
        string json = JsonUtility.ToJson(req);
        Debug.Log($"[Session] POST {url} -> {json}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var resp = JsonUtility.FromJson<StartSessionResponse>(request.downloadHandler.text);
                currentSessionId = resp.sessionId;
                Debug.Log($"✅ Session created: {currentSessionId}");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"❌ Failed to create session: {request.error} | Response: {request.downloadHandler.text}");
                onComplete?.Invoke(false);
            }
        }
    }

    public void LoadGameStateFromServer(Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            onComplete?.Invoke(false);
            return;
        }
        StartCoroutine(LoadStateCoroutine(onComplete));
    }

    private IEnumerator LoadStateCoroutine(Action<bool> onComplete)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            Debug.LogError("LoadStateCoroutine: currentSessionId is null or empty");
            onComplete?.Invoke(false);
            yield break;
        }

        string url = $"{sessionServerUrl}/session/{currentSessionId}/load";
        Debug.Log($"[Session] GET {url}");
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // ▼ ИСПРАВЛЕНО: имена полей camelCase ▼
                var state = JsonUtility.FromJson<GameState>(request.downloadHandler.text);
                wood = state.wood;
                rock = state.stone;   // на клиенте поле называется rock
                food = state.food;
                savedVillagers = state.villagers;
                savedArchers = state.archers;
                savedEnemies = state.enemies;
                savedPlayerBaseHp = state.playerBaseHp;
                savedEnemyBaseHp = state.enemyBaseHp;
                // ▲ конец исправлений ▲

                OnResourcesChanged?.Invoke();
                SavePlayerData();
                Debug.Log($"✅ Game state loaded: Wood={wood}, Stone={rock}, Food={food}, Villagers={savedVillagers}, Archers={savedArchers}, Enemies={savedEnemies}");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"❌ Load state error: {request.error} | Code: {request.responseCode} | URL: {url}");
                // обработка 404 – сессия не найдена
                if (request.responseCode == 404)
                {
                    Debug.LogWarning("Session not found on server, will restart session");
                    StartServerSession(success =>
                    {
                        if (success)
                            LoadGameStateFromServer(onComplete);
                        else
                            onComplete?.Invoke(false);
                    });
                }
                else
                {
                    onComplete?.Invoke(false);
                }
            }
        }
    }

    public void SaveGameStateToServer(Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            onComplete?.Invoke(false);
            return;
        }

        int villagers = CountUnitsByTag("Villager");
        int archers = CountUnitsByTag("Archer");
        int enemies = CountUnitsByTag("Enemy");
        int playerBaseHp = GetBaseHp("Base");
        int enemyBaseHp = GetBaseHp("EnemyBase");

        StartCoroutine(SaveStateCoroutine(villagers, archers, enemies, playerBaseHp, enemyBaseHp, onComplete));
    }

    private IEnumerator SaveStateCoroutine(int villagers, int archers, int enemies, int playerBaseHp, int enemyBaseHp, Action<bool> onComplete)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            Debug.LogError("SaveStateCoroutine: currentSessionId is null or empty");
            onComplete?.Invoke(false);
            yield break;
        }

        // ▼ ИСПРАВЛЕНО: имена полей camelCase ▼
        var state = new SaveStateRequest
        {
            wood = wood,
            stone = rock,            // на клиенте ресурс называется rock
            food = food,
            villagers = villagers,
            archers = archers,
            enemies = enemies,
            playerBaseHp = playerBaseHp,
            enemyBaseHp = enemyBaseHp
        };


        string json = JsonUtility.ToJson(state);
        string url = $"{sessionServerUrl}/session/{currentSessionId}/save";
        Debug.Log($"[Session] POST {url} -> {json}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Game state saved: V={villagers} A={archers} E={enemies}, Bases HP={playerBaseHp}/{enemyBaseHp}");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"❌ Save state error: {request.error} | Code: {request.responseCode} | URL: {url}");
                onComplete?.Invoke(false);
            }
        }
    }

    public void ResetSession()
    {
        currentSessionId = null;
        Debug.Log("[PlayerData] Session ID reset");
    }

    public void EndServerSession(bool isWin, Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            onComplete?.Invoke(false);
            return;
        }
        StartCoroutine(EndSessionCoroutine(isWin, onComplete));
    }

    private IEnumerator EndSessionCoroutine(bool isWin, Action<bool> onComplete)
    {
        var req = new EndSessionRequest { isWin = isWin };
        string json = JsonUtility.ToJson(req);
        string url = $"{sessionServerUrl}/session/{currentSessionId}/end";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Session ended, win={isWin}");
                currentSessionId = null;
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"End session error: {request.error}");
                onComplete?.Invoke(false);
            }
        }
    }

    // Вспомогательные методы для сбора данных со сцены
    private int CountUnitsByTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        return objects.Length;
    }

    private int GetBaseHp(string tag)
    {
        GameObject baseObj = GameObject.FindGameObjectWithTag(tag);
        if (baseObj != null)
        {
            Health health = baseObj.GetComponent<Health>();
            if (health != null) return health.health;
        }
        return 1000;
    }

    // ===== ВНУТРЕННИЕ КЛАССЫ ДЛЯ СЕРИАЛИЗАЦИИ =====
    [System.Serializable]
    private class BattleResultRequest { public bool isWin; }
    [System.Serializable]
    private class BattleResultResponse { public int experience, currency, wins, losses, expGain, currencyGain; public string message; }

    [System.Serializable]
    private class StartSessionRequest { public string playerId; }
    [System.Serializable]
    private class StartSessionResponse { public string sessionId; }

    [System.Serializable]
    private class SaveStateRequest
    {
        // ▼ ИСПРАВЛЕНО: имена полей camelCase ▼
        public int wood, stone, food, villagers, archers, enemies, playerBaseHp, enemyBaseHp;
    }

    [System.Serializable]
    private class GameState
    {
        // ▼ ИСПРАВЛЕНО: имена полей camelCase ▼
        public int wood, stone, food, villagers, archers, enemies, playerBaseHp, enemyBaseHp;
    }

    [System.Serializable]
    private class EndSessionRequest { public bool isWin; }

    [System.Serializable]
    public class PlayerSaveData
    {
        public string playerId, playerName, lastSaved;
        public int units, food, wood, rock, experience, currency, wins, losses;
        public List<int> purchasedItems;
        public Dictionary<string, int> unitUpgrades;
    }
}

[System.Serializable]
public class PlayerDataResponse
{
    public int experience, currency, wins, losses;
    public List<int> purchasedItems;
    public Dictionary<string, int> unitUpgrades;
}