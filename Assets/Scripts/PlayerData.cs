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
    public string playerName;
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
    
    void Start()
    {
        // Сначала тестируем соединение
        StartCoroutine(TestServerConnection());
        
        // Затем загружаем игру
        Invoke(nameof(LoadGame), 2f);
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
            }
            else
            {
                Debug.LogError($"❌ Failed to load game: {request.error}");
                Debug.Log($"Response body: {request.downloadHandler?.text}");
            }
        }
    }
    
    public void TestSave()
    {
        StartCoroutine(TestSaveCoroutine());
    }
    
    private IEnumerator TestSaveCoroutine()
    {
        string url = $"{serverUrl}/api/game/save";
        Debug.Log($"💾 Testing save to: {url}");
        
        // Создаем тестовые данные
        var testData = new
        {
            playerId = playerId,
            playerName = "Test Player",
            units = 100,
            food = 50,
            wood = 75,
            rock = 25
        };
        
        string jsonData = JsonUtility.ToJson(testData);
        Debug.Log($"Sending JSON: {jsonData}");
        
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
                Debug.Log($"✅ Save successful!");
                Debug.Log($"Response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"❌ Save failed: {request.error}");
                Debug.Log($"Response: {request.downloadHandler?.text}");
            }
        }
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