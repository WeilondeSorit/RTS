using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class DatabaseTester : MonoBehaviour
{
    [Header("Supabase Configuration")]
    public string supabaseUrl = "https://ceqdjafzolfhtqjjlvwg.supabase.co/rest/v1/";
    public string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNlcWRqYWZ6b2xmaHRxampsdndnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjAyNTAyMzQsImV4cCI6MjA3NTgyNjIzNH0.N_RQNgbW0jx7mlyUI67sQMaZp38xqMzFR6fJjNN4338";

    [Header("UI Elements")]
    public TMP_Text outputText;
    
    [Header("Test Buttons")]
    public Button testAttackTriggerBtn;
    public Button testHealingTriggerBtn;
    public Button testConstructionTriggerBtn;
    public Button testTimestampTriggerBtn;
    public Button testGetPlayerStatsBtn;
    public Button testHealAllUnitsBtn;
    public Button testCollectResourcesBtn;
    public Button testCreatePlayerBtn;
    public Button testCreateTestDataBtn;
    public Button clearOutputBtn;

    [Header("Test Parameters")]
    public int testPlayerId = 1;
    public int testAttackerId = 4;
    public int testTargetId = 1;
    public int testDamage = 20;
    public int testHealAmount = 15;

    // Храним ID созданных объектов
    private int createdBuildingId = -1;
    private int createdUnitId = -1;

    private void Start()
    {
        SetupButtonListeners();
        LogMessage("✅ Database Tester инициализирован");
        LogMessage($"🔗 Supabase URL: {supabaseUrl}");
    }

    private void SetupButtonListeners()
    {
        testAttackTriggerBtn.onClick.AddListener(() => StartCoroutine(TestAttackTrigger()));
        testHealingTriggerBtn.onClick.AddListener(() => StartCoroutine(TestHealingTrigger()));
        testConstructionTriggerBtn.onClick.AddListener(() => StartCoroutine(TestConstructionTrigger()));
        testTimestampTriggerBtn.onClick.AddListener(() => StartCoroutine(TestTimestampTrigger()));
        testGetPlayerStatsBtn.onClick.AddListener(() => StartCoroutine(TestGetPlayerStats()));
        testHealAllUnitsBtn.onClick.AddListener(() => StartCoroutine(TestHealAllUnits()));
        testCollectResourcesBtn.onClick.AddListener(() => StartCoroutine(TestCollectResources()));
        testCreatePlayerBtn.onClick.AddListener(() => StartCoroutine(TestCreatePlayer()));
        testCreateTestDataBtn.onClick.AddListener(() => StartCoroutine(CreateTestData()));
        clearOutputBtn.onClick.AddListener(ClearOutput);
    }

    // Создание тестовых данных
    private IEnumerator CreateTestData()
    {
        LogMessage("🛠️ === СОЗДАНИЕ ТЕСТОВЫХ ДАННЫХ ===");

        // Создаем здание
        yield return StartCoroutine(CreateTestBuilding());
        
        // Создаем юнита
        yield return StartCoroutine(CreateTestUnit());

        LogMessage("✅ Тестовые данные созданы. Теперь можно запускать тесты.");
    }

    private IEnumerator CreateTestBuilding()
    {
        LogMessage("🏗️ Создание тестового здания...");
        
        string url = $"{supabaseUrl}building";
        string jsonData = "{\"player_id\": 1, \"building_type\": \"test_building\", \"coord_x\": 10, \"coord_y\": 10, \"current_health\": 100, \"max_health\": 100, \"level\": 1}";

        using (UnityWebRequest www = CreateWebRequest(url, "POST", jsonData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Парсим ответ чтобы получить ID созданного здания
                string response = www.downloadHandler.text;
                if (response.Contains("\"id\":"))
                {
                    int start = response.IndexOf("\"id\":") + 5;
                    int end = response.IndexOf(",", start);
                    string idStr = response.Substring(start, end - start).Trim();
                    if (int.TryParse(idStr, out createdBuildingId))
                    {
                        LogMessage($"✅ Тестовое здание создано с ID: {createdBuildingId}");
                    }
                    else
                    {
                        LogError("❌ Не удалось распарсить ID здания");
                    }
                }
            }
            else
            {
                LogError($"❌ Ошибка создания здания: {www.error}");
            }
        }
    }

    private IEnumerator CreateTestUnit()
    {
        LogMessage("⚔️ Создание тестового юнита...");
        
        string url = $"{supabaseUrl}unit";
        string jsonData = "{\"player_id\": 1, \"unit_type\": \"test_unit\", \"coord_x\": 5, \"coord_y\": 5, \"current_health\": 50, \"max_health\": 100}";

        using (UnityWebRequest www = CreateWebRequest(url, "POST", jsonData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text;
                if (response.Contains("\"id\":"))
                {
                    int start = response.IndexOf("\"id\":") + 5;
                    int end = response.IndexOf(",", start);
                    string idStr = response.Substring(start, end - start).Trim();
                    if (int.TryParse(idStr, out createdUnitId))
                    {
                        LogMessage($"✅ Тестовый юнит создан с ID: {createdUnitId}");
                    }
                    else
                    {
                        LogError("❌ Не удалось распарсить ID юнита");
                    }
                }
            }
            else
            {
                LogError($"❌ Ошибка создания юнита: {www.error}");
            }
        }
    }

    // 1. Тестирование триггера атаки
    private IEnumerator TestAttackTrigger()
    {
        LogMessage("⚔️ === ТЕСТИРОВАНИЕ ТРИГГЕРА АТАКИ ===");
        
        if (createdUnitId == -1)
        {
            LogError("❌ Сначала создайте тестовые данные!");
            yield break;
        }

        LogMessage($"🎯 Атакующий: {testAttackerId}, Цель: {createdUnitId}, Урон: {testDamage}");

        // Сначала получаем текущее здоровье цели
        yield return StartCoroutine(GetUnitHealth(createdUnitId, "ДО атаки"));

        // Выполняем атаку
        string url = $"{supabaseUrl}attack";
        string jsonData = $"{{\"attacker_unit_id\": {testAttackerId}, \"target_unit_id\": {createdUnitId}, \"damage_dealt\": {testDamage}, \"attack_type\": \"ranged\"}}";

        using (UnityWebRequest www = CreateWebRequest(url, "POST", jsonData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogMessage("✅ Атака зарегистрирована, триггер должен сработать");
                
                // Ждем срабатывания триггера
                yield return new WaitForSeconds(2f);
                
                // Проверяем здоровье после атаки
                yield return StartCoroutine(GetUnitHealth(createdUnitId, "ПОСЛЕ атаки"));
            }
            else
            {
                LogError($"❌ Ошибка атаки: {www.error}");
                if (!string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    LogError($"Response: {www.downloadHandler.text}");
                }
            }
        }
    }

    // 2. Тестирование триггера лечения
    private IEnumerator TestHealingTrigger()
    {
        LogMessage("❤️ === ТЕСТИРОВАНИЕ ТРИГГЕРА ЛЕЧЕНИЯ ===");
        
        if (createdUnitId == -1)
        {
            LogError("❌ Сначала создайте тестовые данные!");
            yield break;
        }

        // Получаем текущее здоровье
        yield return StartCoroutine(GetUnitHealth(createdUnitId, "ДО лечения"));

        // Выполняем лечение
        string url = $"{supabaseUrl}healing";
        string jsonData = $"{{\"healer_id\": 3, \"target_unit_id\": {createdUnitId}, \"heal_amount\": {testHealAmount}, \"heal_type\": \"single_heal\"}}";

        using (UnityWebRequest www = CreateWebRequest(url, "POST", jsonData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogMessage("✅ Лечение зарегистрировано, триггер должен сработать");
                
                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(GetUnitHealth(createdUnitId, "ПОСЛЕ лечения"));
            }
            else
            {
                LogError($"❌ Ошибка лечения: {www.error}");
                if (!string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    LogError($"Response: {www.downloadHandler.text}");
                }
            }
        }
    }

    // 3. Тестирование триггера строительства
    private IEnumerator TestConstructionTrigger()
    {
        LogMessage("🏗️ === ТЕСТИРОВАНИЕ ТРИГГЕРА СТРОИТЕЛЬСТВА ===");
        
        if (createdBuildingId == -1)
        {
            LogError("❌ Сначала создайте тестовые данные!");
            yield break;
        }
        
        string url = $"{supabaseUrl}construction";
        string jsonData = $"{{\"villager_id\": 2, \"building_id\": {createdBuildingId}, \"required_food\": 50, \"required_wood\": 30, \"required_rock\": 20}}";

        using (UnityWebRequest www = CreateWebRequest(url, "POST", jsonData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogMessage("✅ Строительство начато, триггер проверки ресурсов сработал");
            }
            else
            {
                LogError($"❌ Ошибка строительства: {www.error}");
                if (!string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    LogError($"Response: {www.downloadHandler.text}");
                }
            }
        }
    }

    // 4. Тестирование триггера временных меток
    private IEnumerator TestTimestampTrigger()
    {
        LogMessage("🕐 === ТЕСТИРОВАНИЕ ТРИГГЕРА ВРЕМЕННЫХ МЕТОК ===");
        
        // Сначала создаем запись player_data если её нет
        yield return StartCoroutine(CreatePlayerDataIfNotExists());

        // Получаем текущее время обновления
        yield return StartCoroutine(GetPlayerDataTimestamp(testPlayerId, "ДО обновления"));

        // Обновляем ресурсы (это вызовет триггер)
        string url = $"{supabaseUrl}player_data?player_id=eq.{testPlayerId}";
        string updateData = "{\"food\": 150}";

        using (UnityWebRequest www = CreateWebRequest(url, "PATCH", updateData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogMessage("✅ Ресурсы обновлены, триггер должен обновить timestamp");
                yield return new WaitForSeconds(1f);
                yield return StartCoroutine(GetPlayerDataTimestamp(testPlayerId, "ПОСЛЕ обновления"));
            }
            else
            {
                LogError($"❌ Ошибка обновления ресурсов: {www.error}");
                if (!string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    LogError($"Response: {www.downloadHandler.text}");
                }
            }
        }
    }

    private IEnumerator CreatePlayerDataIfNotExists()
    {
        string url = $"{supabaseUrl}player_data?player_id=eq.{testPlayerId}";
        
        using (UnityWebRequest www = CreateWebRequest(url, "GET"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text;
                if (response == "[]") // Нет данных
                {
                    // Создаем запись
                    string createUrl = $"{supabaseUrl}player_data";
                    string jsonData = $"{{\"player_id\": {testPlayerId}, \"food\": 100, \"wood\": 50, \"rock\": 25}}";
                    
                    using (UnityWebRequest createRequest = CreateWebRequest(createUrl, "POST", jsonData))
                    {
                        yield return createRequest.SendWebRequest();
                        if (createRequest.result == UnityWebRequest.Result.Success)
                        {
                            LogMessage("✅ Создана запись player_data");
                        }
                        else
                        {
                            LogError($"❌ Ошибка создания player_data: {createRequest.error}");
                        }
                    }
                }
            }
        }
    }

    // 5. Тестирование функции получения статистики игрока
    private IEnumerator TestGetPlayerStats()
    {
        LogMessage("📊 === ТЕСТИРОВАНИЕ ФУНКЦИИ GET_PLAYER_STATS ===");
        
        string url = $"{supabaseUrl}rpc/get_player_stats";
        string jsonData = $"{{\"player_id_param\": {testPlayerId}}}";

        using (UnityWebRequest www = CreateWebRequest(url, "POST", jsonData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogMessage($"✅ Статистика игрока получена: {www.downloadHandler.text}");
            }
            else
            {
                LogError($"❌ Ошибка получения статистики: {www.error}");
                if (!string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    LogError($"Response: {www.downloadHandler.text}");
                }
            }
        }
    }

    // 6. Тестирование функции лечения всех юнитов
    private IEnumerator TestHealAllUnits()
    {
        LogMessage("🏥 === ТЕСТИРОВАНИЕ ФУНКЦИИ HEAL_ALL_PLAYER_UNITS ===");
        
        string url = $"{supabaseUrl}rpc/heal_all_player_units";
        string jsonData = $"{{\"player_id_param\": {testPlayerId}, \"heal_amount_param\": {testHealAmount}}}";

        using (UnityWebRequest www = CreateWebRequest(url, "POST", jsonData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogMessage("✅ Процедура лечения выполнена успешно");
                
                // Проверяем результат лечения
                yield return StartCoroutine(CheckUnitsAfterHealing(testPlayerId));
            }
            else
            {
                LogError($"❌ Ошибка выполнения лечения: {www.error}");
                if (!string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    LogError($"Response: {www.downloadHandler.text}");
                }
            }
        }
    }

    // 7. Тестирование функции сбора ресурсов
    private IEnumerator TestCollectResources()
    {
        LogMessage("🌾 === ТЕСТИРОВАНИЕ ФУНКЦИИ COLLECT_FARM_RESOURCES ===");
        
        string url = $"{supabaseUrl}rpc/collect_farm_resources";
        string jsonData = $"{{\"player_id_param\": {testPlayerId}}}";

        using (UnityWebRequest www = CreateWebRequest(url, "POST", jsonData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogMessage($"✅ Ресурсы собраны: {www.downloadHandler.text}");
            }
            else
            {
                LogError($"❌ Ошибка сбора ресурсов: {www.error}");
                if (!string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    LogError($"Response: {www.downloadHandler.text}");
                }
            }
        }
    }

    // 8. Тестирование функции создания нового игрока
    private IEnumerator TestCreatePlayer()
    {
        LogMessage("👤 === ТЕСТИРОВАНИЕ ФУНКЦИИ CREATE_NEW_PLAYER ===");
        
        string url = $"{supabaseUrl}rpc/create_new_player";
        int randomSuffixInt = UnityEngine.Random.Range(1000, 9999);
        string randomSuffix = randomSuffixInt.ToString();
        string jsonData = $"{{\"p_login\": \"test_player_{randomSuffix}\", \"p_password\": \"test123\", \"p_sound_on\": true, \"p_volume\": 75.0}}";

        using (UnityWebRequest www = CreateWebRequest(url, "POST", jsonData))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogMessage($"✅ Новый игрок создан: {www.downloadHandler.text}");
            }
            else
            {
                LogError($"❌ Ошибка создания игрока: {www.error}");
                if (!string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    LogError($"Response: {www.downloadHandler.text}");
                }
            }
        }
    }

    // Вспомогательные методы
    private IEnumerator GetUnitHealth(int unitId, string context)
    {
        string url = $"{supabaseUrl}unit?id=eq.{unitId}";
        
        using (UnityWebRequest www = CreateWebRequest(url, "GET"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogMessage($"❤️ {context} - Здоровье юнита {unitId}: {www.downloadHandler.text}");
            }
            else
            {
                LogError($"❌ Ошибка получения здоровья: {www.error}");
            }
        }
    }

    private IEnumerator GetPlayerDataTimestamp(int playerId, string context)
    {
        string url = $"{supabaseUrl}player_data?player_id=eq.{playerId}&select=updated_at";
        
        using (UnityWebRequest www = CreateWebRequest(url, "GET"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogMessage($"🕐 {context} - Временная метка: {www.downloadHandler.text}");
            }
            else
            {
                LogError($"❌ Ошибка получения временной метки: {www.error}");
            }
        }
    }

    private IEnumerator CheckUnitsAfterHealing(int playerId)
    {
        string url = $"{supabaseUrl}unit?player_id=eq.{playerId}";
        
        using (UnityWebRequest www = CreateWebRequest(url, "GET"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                LogMessage($"❤️ Состояние юнитов после лечения: {www.downloadHandler.text}");
            }
            else
            {
                LogError($"❌ Ошибка проверки юнитов: {www.error}");
            }
        }
    }

    private UnityWebRequest CreateWebRequest(string url, string method, string jsonData = null)
    {
        UnityWebRequest www;
        
        if (method == "POST" || method == "PATCH")
        {
            www = new UnityWebRequest(url, method);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
        }
        else
        {
            www = UnityWebRequest.Get(url);
            www.downloadHandler = new DownloadHandlerBuffer();
        }

        www.SetRequestHeader("apikey", supabaseKey);
        www.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
        www.SetRequestHeader("Prefer", "return=representation");

        return www;
    }

    private void LogMessage(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string formattedMessage = $"\n<color=white>[{timestamp}] {message}</color>";
        
        Debug.Log(message);
        outputText.text += formattedMessage;
    }

    private void LogError(string error)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string formattedError = $"\n<color=red>[{timestamp}] {error}</color>";
        
        Debug.LogError(error);
        outputText.text += formattedError;
    }

    private void ClearOutput()
    {
        outputText.text = "Лог очищен\n";
        Debug.Log("Лог очищен");
    }
}