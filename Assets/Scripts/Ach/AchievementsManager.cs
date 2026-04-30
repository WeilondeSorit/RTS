using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AchievementsManager : MonoBehaviour
{
    public Transform achievementsContainer;
    public GameObject achievementPrefab;

    private string serverUrl = "http://localhost:8080";
    private string playerId;

    public void Initialize(PlayerData data)
    {
        playerId = data.playerId;
        Debug.Log($"[AchievementsManager] Инициализация для {playerId}");
        StartCoroutine(LoadAchievements());
    }

    private IEnumerator LoadAchievements()
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/player/{playerId}/achievements"))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log($"[Mgr] Ответ: {json}");
                AchievementDTO[] achievements = JsonHelper.FromJson<AchievementDTO>(json);
                PopulateUI(achievements);
            }
            else
            {
                Debug.LogError($"[Mgr] Ошибка: {request.error}");
            }
        }
    }

    private void PopulateUI(AchievementDTO[] achievements)
    {
        // Очистка контейнера
        foreach (Transform child in achievementsContainer)
            Destroy(child.gameObject);
        float yOffset = 0;
        float spacing = 100f;
        foreach (var ach in achievements)
        {
            GameObject item = Instantiate(achievementPrefab, achievementsContainer);
            RectTransform rt = item.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -yOffset);
            yOffset += spacing;
            // Поиск текста (поддерживает оба типа)
            Text textComp = item.GetComponentInChildren<Text>();
            if (textComp != null)
                textComp.text = $"{ach.name}\n{ach.description}\nПрогресс: {ach.progress}/{ach.requiredValue}";
            else
            {
                TextMeshProUGUI tmp = item.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                    tmp.text = $"{ach.name}\n{ach.description}\nПрогресс: {ach.progress}/{ach.requiredValue}";
            }

            Button claimButton = item.GetComponentInChildren<Button>();
            if (claimButton != null)
            {
                bool canClaim = ach.isCompleted && !ach.isRewardClaimed;
                claimButton.interactable = canClaim;
                if (canClaim)
                {
                    int id = ach.id;
                    claimButton.onClick.AddListener(() => StartCoroutine(ClaimReward(id, claimButton)));
                }
            }
        }
    }

    private void EnsureLayout()
    {
        if (achievementsContainer.GetComponent<VerticalLayoutGroup>() == null)
        {
            var vlg = achievementsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }
        if (achievementsContainer.GetComponent<ContentSizeFitter>() == null)
        {
            var csf = achievementsContainer.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
    private IEnumerator ClaimReward(int achievementId, Button button)
    {
        using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/player/{playerId}/achievement/{achievementId}/claim", "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Награда получена!");
                button.interactable = false;
                StartCoroutine(LoadAchievements());
            }
        }
    }

    [Serializable]
    private class AchievementDTO
    {
        public int id;
        public string name;
        public string description;
        public int requiredValue;
        public int progress;
        public bool isCompleted;
        public bool isRewardClaimed;
        public int rewardCurrency;
        public int rewardExperience;
    }

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