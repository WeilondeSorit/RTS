using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance { get; private set; }

    // ===== ДАННЫЕ С СЕРВЕРА (MainServer) =====
    public string playerId;          // GUID как строка
    public string playerName;
    public string authToken;
    public int experience;
    public int currency;             // валюта
    public int wins;
    public int losses;
    public List<int> purchasedItems = new List<int>();
    public Dictionary<string, int> unitUpgrades = new Dictionary<string, int>();

    // ===== ЛОКАЛЬНЫЕ РЕСУРСЫ ДЛЯ ТЕКУЩЕЙ СЕССИИ =====
    public int units;
    public int food = 500;
    public int wood = 300;
    public int rock = 200;

    // ===== UI =====
    public TextMeshProUGUI unitsText;
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI rockText;
    public TextMeshProUGUI questDisplayText;

    // ===== КОНФИГУРАЦИЯ =====
    [SerializeField] private bool useServerSync = false;
    [SerializeField] public string playerServiceUrl = "http://localhost:8082";
    [SerializeField] private float autoSaveInterval = 30f;

    private AchievementSystem achievementSystem;
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

        achievementSystem = gameObject.AddComponent<AchievementSystem>();
        achievementSystem.Initialize(this);
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey("PlayerId"))
        {
            LoadSession();
            LoadPlayerData();
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning("⚠️ Player not authenticated. Redirecting to login...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
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

    // ===== АВТОРИЗАЦИЯ (принимает строку GUID) =====
    public void SetAuthToken(string token, string userId, string username)
    {
        authToken = token;
        playerId = userId;
        playerName = username;

        PlayerPrefs.SetString("AuthToken", token);
        PlayerPrefs.SetString("PlayerId", userId);
        PlayerPrefs.SetString("PlayerName", username);
        PlayerPrefs.Save();

        Debug.Log($"✅ Authenticated: {username} (ID: {playerId})");
    }

    // Перегрузка для обратной совместимости (если где-то передаётся int)
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

    // Обновление данных с сервера
    public void UpdateFromServer(PlayerDataResponse serverData)
    {
        experience = serverData.experience;
        currency = serverData.currency;
        wins = serverData.wins;
        losses = serverData.losses;
        purchasedItems = serverData.purchasedItems ?? new List<int>();
        unitUpgrades = serverData.unitUpgrades ?? new Dictionary<string, int>();

        SavePlayerData();
        Debug.Log($"✅ Updated from server: XP={experience}, Currency={currency}, Wins={wins}, Losses={losses}");
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
                Debug.Log($"✅ Loaded local data: Units={units}, Food={food}, Wood={wood}, Rock={rock}");
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

        UpdateUI();
        if (achievementSystem != null)
        {
            achievementSystem.questDisplayText = GameObject.FindWithTag("QuestText")?.GetComponent<TextMeshProUGUI>();
            achievementSystem.LoadAchievements();
        }
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
        UpdateUI();
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
        UpdateUI();
        SavePlayerData();
    }

    // ===== UI =====
    private void UpdateUI()
    {
        if (unitsText != null) unitsText.text = units.ToString();
        if (foodText != null) foodText.text = food.ToString();
        if (woodText != null) woodText.text = wood.ToString();
        if (rockText != null) rockText.text = rock.ToString();
    }

    public void UpdateQuestDisplay(string text)
    {
        if (questDisplayText != null) questDisplayText.text = text;
    }

    // ===== КЛАССЫ ДЛЯ СОХРАНЕНИЯ =====
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

// Класс ответа от сервера (должен совпадать с тем, что в AuthManager)
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