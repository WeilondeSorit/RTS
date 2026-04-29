using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Linq;

public class LeaderboardDisplay : MonoBehaviour
{
    [Header("UI Text Fields for Top 4 Players")]
    [SerializeField] private TMP_Text[] leaderboardTexts;

    [Header("API Settings")]
    [SerializeField] private string serverUrl = "http://localhost:8081";
    [SerializeField] private float refreshInterval = 10f;

    private void Start()
    {
        Debug.Log($"[Leaderboard] Start called. Object active: {gameObject.activeInHierarchy}, enabled: {enabled}");

        if (leaderboardTexts == null || leaderboardTexts.Length != 4)
        {
            Debug.LogError($"[Leaderboard] Ошибка: массив содержит {leaderboardTexts?.Length ?? 0} элементов вместо 4!");
            return;
        }

        // Проверим, что все ссылки не null и GameObject активны
        for (int i = 0; i < leaderboardTexts.Length; i++)
        {
            if (leaderboardTexts[i] == null)
                Debug.LogError($"[Leaderboard] Элемент {i} в массиве равен null!");
            else
                Debug.Log($"[Leaderboard] Элемент {i}: привязан к объекту '{leaderboardTexts[i].name}', активен в иерархии: {leaderboardTexts[i].gameObject.activeInHierarchy}");
        }

        StartCoroutine(RefreshLeaderboardCoroutine());
    }

    private IEnumerator RefreshLeaderboardCoroutine()
    {
        Debug.Log("[Leaderboard] Корутина обновления запущена.");
        while (true)
        {
            yield return StartCoroutine(LoadLeaderboard());
            yield return new WaitForSeconds(refreshInterval);
        }
    }

    private IEnumerator LoadLeaderboard()
    {
        string url = $"{serverUrl}/leaderboard";
        Debug.Log($"[Leaderboard] Отправляю запрос к {url}...");
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            Debug.Log($"[Leaderboard] Запрос завершён. Result: {request.result}, ResponseCode: {request.responseCode}");

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Leaderboard] Ошибка сети: {request.error}");
                SetAllTexts("Ошибка сети");
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log($"[Leaderboard] Получен JSON (длина {json.Length}): {json}");

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("[Leaderboard] Ответ сервера пуст!");
                SetAllTexts("Нет данных");
                yield break;
            }

            List<LeaderboardEntry> entries;
            try
            {
                entries = JsonConvert.DeserializeObject<List<LeaderboardEntry>>(json);
                Debug.Log($"[Leaderboard] Десериализовано {entries?.Count ?? 0} записей.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Leaderboard] Ошибка десериализации JSON: {ex.Message}");
                SetAllTexts("Ошибка формата");
                yield break;
            }

            if (entries == null) entries = new List<LeaderboardEntry>();

            for (int i = 0; i < leaderboardTexts.Length; i++)
            {
                if (leaderboardTexts[i] == null) continue;
                if (i < entries.Count)
                {
                    string newText = $"{i + 1}. {entries[i].login} (побед: {entries[i].wins})";
                    Debug.Log($"[Leaderboard] Назначаю текст в элемент {i}: '{newText}'");
                    leaderboardTexts[i].text = newText;
                }
                else
                {
                    string emptyText = $"{i + 1}. ---";
                    Debug.Log($"[Leaderboard] Назначаю текст в элемент {i}: '{emptyText}' (нет записи)");
                    leaderboardTexts[i].text = emptyText;
                }
            }
        }
    }

    private void SetAllTexts(string message)
    {
        for (int i = 0; i < leaderboardTexts.Length; i++)
        {
            if (leaderboardTexts[i] != null)
                leaderboardTexts[i].text = message;
        }
    }

    [System.Serializable]
    private class LeaderboardEntry
    {
        public string login;
        public int wins;
        public int experience;
    }
}