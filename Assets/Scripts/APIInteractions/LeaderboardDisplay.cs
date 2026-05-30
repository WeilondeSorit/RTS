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
            Debug.LogError($"[Leaderboard] Error: Array has {leaderboardTexts?.Length ?? 0} elements, but 4 are required!");
            return;
        }

        // Check that each text field is assigned and the GameObject is active
        for (int i = 0; i < leaderboardTexts.Length; i++)
        {
            if (leaderboardTexts[i] == null)
                Debug.LogError($"[Leaderboard] Element {i} in the array is null!");
            else
                Debug.Log($"[Leaderboard] Element {i}: assigned to text field '{leaderboardTexts[i].name}', GameObject active: {leaderboardTexts[i].gameObject.activeInHierarchy}");
        }

        StartCoroutine(RefreshLeaderboardCoroutine());
    }

    private IEnumerator RefreshLeaderboardCoroutine()
    {
        Debug.Log("[Leaderboard] Starting leaderboard refresh coroutine.");
        while (true)
        {
            yield return StartCoroutine(LoadLeaderboard());
            yield return new WaitForSeconds(refreshInterval);
        }
    }

    private IEnumerator LoadLeaderboard()
    {
        string url = $"{serverUrl}/leaderboard";
        Debug.Log($"[Leaderboard] Sending request to {url}...");
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            Debug.Log($"[Leaderboard] Request completed. Result: {request.result}, ResponseCode: {request.responseCode}");

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Leaderboard] Request error: {request.error}");
                SetAllTexts("Connection error");
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log($"[Leaderboard] Received JSON (length {json.Length}): {json}");

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("[Leaderboard] Received empty JSON!");
                SetAllTexts("No data");
                yield break;
            }

            List<LeaderboardEntry> entries;
            try
            {
                entries = JsonConvert.DeserializeObject<List<LeaderboardEntry>>(json);
                Debug.Log($"[Leaderboard] Deserialized {entries?.Count ?? 0} entries.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Leaderboard] JSON deserialization error: {ex.Message}");
                SetAllTexts("Data error");
                yield break;
            }

            if (entries == null) entries = new List<LeaderboardEntry>();

            for (int i = 0; i < leaderboardTexts.Length; i++)
            {
                if (leaderboardTexts[i] == null) continue;
                if (i < entries.Count)
                {
                    string newText = $"{i + 1}. {entries[i].login} (Wins: {entries[i].wins})";
                    Debug.Log($"[Leaderboard] Setting text for slot {i}: '{newText}'");
                    leaderboardTexts[i].text = newText;
                }
                else
                {
                    string emptyText = $"{i + 1}. ---";
                    Debug.Log($"[Leaderboard] Setting text for slot {i}: '{emptyText}' (no entry)");
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