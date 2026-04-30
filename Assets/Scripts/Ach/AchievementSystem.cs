using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

public class AchievementSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI questDisplayText;
    [SerializeField] private string questDisplayTag = "QuestText";

    private string serverUrl = "http://localhost:8080";
    private string playerId;
    private bool isReady = false;
    private Dictionary<string, int> keyToId = new Dictionary<string, int>();

    public void Initialize(PlayerData data)
    {
        playerId = data.playerId;
        Debug.Log($"🎮 AchievementSystem инициализирован для playerId: {playerId}");
        StartCoroutine(LoadAchievementsFromServer());
    }

    public void SetQuestDisplay(TextMeshProUGUI text)
    {
        questDisplayText = text;
    }

    private TextMeshProUGUI GetQuestDisplay()
    {
        if (questDisplayText != null && questDisplayText.gameObject.scene.isLoaded)
            return questDisplayText;

        if (string.IsNullOrEmpty(questDisplayTag))
            return null;

        GameObject go = GameObject.FindWithTag(questDisplayTag);
        if (go != null)
        {
            questDisplayText = go.GetComponent<TextMeshProUGUI>();
            if (questDisplayText == null)
                Debug.LogWarning($"Объект с тегом '{questDisplayTag}' не содержит TextMeshProUGUI");
        }
        return questDisplayText;
    }

    private IEnumerator LoadAchievementsFromServer()
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/achievements"))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                AchievementDTO[] achievements = JsonHelper.FromJson<AchievementDTO>(json);

                keyToId.Clear();
                foreach (var a in achievements)
                {
                    if (!string.IsNullOrEmpty(a.key))
                    {
                        keyToId[a.key] = a.id;
                        Debug.Log($"Загружено достижение: {a.key} -> ID {a.id}");
                    }
                }
                isReady = true;
                Debug.Log($"Всего загружено {keyToId.Count} достижений");
                StartCoroutine(RefreshQuest());
            }
            else
            {
                Debug.LogError($"Не удалось загрузить достижения: {request.error}");
            }
        }
    }

    // Триггеры событий
    public void OnEnemyUnitKilled() => TriggerProgress("first_blood", 1);
    public void OnEnemyUnitKilled(string unitType) => OnEnemyUnitKilled();
    public void OnResourceCollected(int amount) => TriggerProgress("resource_master", amount);
    public void OnUnitCreated() => TriggerProgress("unit_commander", 1);
    public void OnEnemyBaseDestroyed() => TriggerProgress("base_destroyer", 1);

    private void TriggerProgress(string key, int increment)
    {
        if (!isReady)
        {
            Debug.LogWarning("Достижения ещё не загружены");
            return;
        }
        if (!keyToId.TryGetValue(key, out int achId))
        {
            Debug.LogError($"Неизвестный ключ: {key}");
            return;
        }
        StartCoroutine(SendProgress(achId, increment));
    }

    private IEnumerator SendProgress(int achievementId, int increment)
    {
        string url = $"{serverUrl}/player/{playerId}/achievement/{achievementId}/progress";
        Debug.Log($"📡 Отправка прогресса: {url}");
        string json = $"{{\"increment\":{increment}}}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<ProgressResponse>(request.downloadHandler.text);
                if (response.completed)
                    Debug.Log("Достижение выполнено! Заберите награду в меню.");
                StartCoroutine(RefreshQuest());
            }
            else
            {
                Debug.LogError($"Ошибка: {request.error} - {request.downloadHandler.text}");
            }
        }
    }

    public IEnumerator RefreshQuest()
    {
        if (string.IsNullOrEmpty(playerId)) yield break;
        using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/player/{playerId}/quest"))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log($"[Quest] Ответ: {json}");
                if (json.Contains("Все достижения выполнены"))
                    UpdateQuestUI(null);
                else
                {
                    var quest = JsonUtility.FromJson<QuestResponse>(json);
                    if (quest != null && quest.id != 0)
                        UpdateQuestUI(quest);
                    else
                        Debug.LogWarning($"Не удалось распарсить квест: {json}");
                }
            }
        }
    }

    private void UpdateQuestUI(QuestResponse quest)
    {
        TextMeshProUGUI display = GetQuestDisplay();
        if (display == null)
        {
            if (quest != null && quest.id != 0)
                StartCoroutine(DelayedQuestUpdate(quest));
            return;
        }

        if (quest == null || quest.id == 0)
        {
            display.text = "<b>✅ Все достижения выполнены!</b>";
            display.color = Color.green;
            return;
        }

        float progress = (float)quest.currentProgress / quest.requiredValue;
        string bar = GetProgressBar(progress);
        display.text = $"<b>{quest.name}</b>\n{quest.description}\nПрогресс: {quest.currentProgress}/{quest.requiredValue}\n{bar}";
        display.color = (quest.currentProgress >= quest.requiredValue) ? Color.green : Color.white;
    }

    private IEnumerator DelayedQuestUpdate(QuestResponse quest)
    {
        yield return new WaitForSeconds(0.5f);
        UpdateQuestUI(quest);
    }

    private string GetProgressBar(float progress)
    {
        int total = 10;
        int filled = Mathf.FloorToInt(progress * total);
        return new string('█', filled) + new string('░', total - filled) + $" {Mathf.FloorToInt(progress * 100)}%";
    }

    // DTO с правильным регистром
    [Serializable] private class ProgressResponse { public int progress; public bool completed; public string message; }
    [Serializable] private class QuestResponse { public int id; public string name; public string description; public int requiredValue; public int currentProgress; public int rewardCurrency; public int rewardExperience; }
    [Serializable] private class AchievementDTO { public int id; public string key; public string name; public string description; public int requiredValue; public int rewardCurrency; public int rewardExperience; }

    private static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string wrapped = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper.array;
        }
        [Serializable] private class Wrapper<T> { public T[] array; }
    }
}