using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class PlayerData : MonoBehaviour
{
    [HideInInspector] public bool isGameActive = false;  // Активна ли игровая сцена
    public static PlayerData Instance { get; private set; }

    // События для оповещения UI
    public event Action OnResourcesChanged;   // вызывается при изменении юнитов, еды, дерева, камня
    public event Action<string> OnQuestTextChanged; // вызывается при изменении текста квеста

    // ===== ДАННЫЕ С СЕРВЕРА =====
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
    public int rock = 200;

    [Header("Unit Consumption")]
    [SerializeField] private float foodConsumptionPerUnitPerSecond = 1f;
    private Coroutine consumptionCoroutine;

    // ===== КОНФИГУРАЦИЯ =====
    [SerializeField] private bool useServerSync = false;
    [SerializeField] public string playerServiceUrl = "http://localhost:8082";
    [SerializeField] private float autoSaveInterval = 30f;

    public AchievementSystem achievementSystem;
    private float lastSaveTime = 0f;
    private bool isInitialized = false;

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

        // Добавляем AchievementSystem, если его ещё нет (но пока не инициализируем)
        achievementSystem = GetComponent<AchievementSystem>();
        if (achievementSystem == null)
            achievementSystem = gameObject.AddComponent<AchievementSystem>();

        // Не вызываем Initialize здесь – playerId ещё не известен
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
        if (isInitialized && Time.time - lastSaveTime >= autoSaveInterval)
        {
            SavePlayerData();
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

        // Теперь playerId известен – можно инициализировать систему достижений
        if (achievementSystem != null)
            achievementSystem.Initialize(this);
        else
            Debug.LogError("AchievementSystem not found!");

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
        Debug.Log($"✅ Updated from server: XP={experience}, Currency={currency}");
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
                Debug.Log($"✅ Loaded local data: Units={units}, Food={food}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Load error: {ex.Message}. Using defaults.");
            }
        }
        else
        {
            Debug.Log("🆕 New player - using default resources");
        }

        // Оповещаем UI об изменении ресурсов
        OnResourcesChanged?.Invoke();

        // ❌ УДАЛИТЬ СТРОКУ НИЖЕ:
        // achievementSystem.LoadAchievements();
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

        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString($"Player_{playerId}_Data", json);
        PlayerPrefs.Save();
        Debug.Log($"💾 Saved player data locally (ID: {playerId})");
    }

    // ===== ИГРОВЫЕ СОБЫТИЯ =====
    public void OnEnemyUnitKilled(string unitType)
    {
        achievementSystem?.OnEnemyUnitKilled(unitType);
        food += 5;
        OnResourcesChanged?.Invoke();  // было UpdateUI()
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
        OnResourcesChanged?.Invoke();  // было UpdateUI()
        SavePlayerData();
    }

    // ===== УПРАВЛЕНИЕ ЮНИТАМИ =====
    public bool TryAddUnits(int count)
    {
        if (BuildingManager.Instance == null)
        {
            Debug.LogError("BuildingManager.Instance отсутствует!");
            return false;
        }

        int totalCapacity = BuildingManager.Instance.GetTotalCapacity();
        if (units + count > totalCapacity)
        {
            Debug.Log($"Недостаточно жилых зданий! Вместимость: {totalCapacity}, юнитов: {units}");
            return false;
        }

        units += count;
        OnResourcesChanged?.Invoke();  // было UpdateUI()
        SavePlayerData();
        return true;
    }

    public void ForceSetUnits(int newUnits)
    {
        if (newUnits < 0) newUnits = 0;
        units = newUnits;
        OnResourcesChanged?.Invoke();  // было UpdateUI()
        SavePlayerData();
        Debug.Log($"Количество юнитов принудительно изменено на {units}");
    }

    public void AddUnitsIgnoreCapacity(int count)
    {
        if (count < 0) return;
        units += count;
        OnResourcesChanged?.Invoke();  // было UpdateUI()
        SavePlayerData();
        Debug.Log($"Добавлено {count} юнитов (игнорируя лимиты). Всего юнитов: {units}");
    }

    // ===== ПОСТОЯННЫЙ РАСХОД ЕДЫ =====
    // Проверяет, хватает ли еды, и если да — списывает её
    public bool TryConsumeFood(int amount)
    {
        if (food < amount) return false;
        food -= amount;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
        return true;
    }

    // Добавляет еду (например, при сборе ресурсов)
    public void AddFood(int amount)
    {
        food += amount;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
    }

    // Аналогично для дерева и камня (по желанию)
    public void AddWood(int amount) { wood += amount; OnResourcesChanged?.Invoke(); SavePlayerData(); }
    public void AddRock(int amount) { rock += amount; OnResourcesChanged?.Invoke(); SavePlayerData(); }
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

                if (food <= 0)
                {
                    Debug.LogWarning("⚠️ Еда закончилась! Юниты голодают.");
                }
            }
        }
    }

    // Оповещение об изменении текста квеста (вызывается из AchievementSystem)
    public void UpdateQuestDisplay(string text)
    {
        OnQuestTextChanged?.Invoke(text);
    }
    // Добавьте в PlayerData следующие методы (остальной код остаётся как есть)
    public void SpendResources(int woodCost, int rockCost)
    {
        wood -= woodCost;
        rock -= rockCost;
        OnResourcesChanged?.Invoke();   // теперь вызов внутри самого класса – ошибки не будет
        SavePlayerData();
    }

    public void AddResources(int woodAmount, int rockAmount)
    {
        wood += woodAmount;
        rock += rockAmount;
        OnResourcesChanged?.Invoke();
        SavePlayerData();
    }

    // ===== СОХРАНЯЕМЫЕ ДАННЫЕ =====
    [System.Serializable]
    public class PlayerSaveData
    {
        public string playerId;
        public string playerName;
        public int units;
        public int food;
        public int wood;
        public int rock;
        public int experience;
        public int currency;
        public int wins;
        public int losses;
        public List<int> purchasedItems;
        public Dictionary<string, int> unitUpgrades;
        public string lastSaved;
    }
}

[System.Serializable]
public class PlayerDataResponse
{
    public int experience;
    public int currency;
    public int wins;
    public int losses;
    public List<int> purchasedItems;
    public Dictionary<string, int> unitUpgrades;
}