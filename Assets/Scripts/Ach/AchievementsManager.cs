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
    private string authToken;

    public void Initialize(PlayerData data)
    {
        playerId = data.playerId;
        authToken = data.authToken;
        Debug.Log($"[AchievementsManager] Инициализация для {playerId}");
        StartCoroutine(LoadAchievements());
    }

    private IEnumerator LoadAchievements()
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/player/{playerId}/achievements"))
        {
            if (!string.IsNullOrEmpty(authToken))
                request.SetRequestHeader("Authorization", $"Bearer {authToken}");

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
                Debug.LogError($"[Mgr] Ошибка: {request.error} - Код: {request.responseCode}");
                if (request.responseCode == 401)
                    Debug.LogError("Не авторизован. Проверьте токен.");
            }
        }
    }

    private void PopulateUI(AchievementDTO[] achievements)
    {
        // Очистка контейнера
        foreach (Transform child in achievementsContainer)
            Destroy(child.gameObject);

        // Настраиваем компоновку один раз (контейнер)
        SetupContainerLayout();

        foreach (var ach in achievements)
        {
            GameObject item = Instantiate(achievementPrefab, achievementsContainer);

            // Заполняем текстом (размеры берутся из префаба, ничего не трогаем)
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
                    claimButton.onClick.RemoveAllListeners();
                    claimButton.onClick.AddListener(() => StartCoroutine(ClaimReward(id, claimButton)));
                }
            }
        }
    }

    private void SetupContainerLayout()
    {
        // VerticalLayoutGroup – отвечает за позиционирование
        VerticalLayoutGroup vlg = achievementsContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
            vlg = achievementsContainer.gameObject.AddComponent<VerticalLayoutGroup>();

        // Настройки, которые не ломают размеры префаба
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;   // ширина элемента = ширина контейнера (чтобы текст не вылезал)
        vlg.childControlHeight = false;  // высота берётся из префаба
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // ContentSizeFitter – чтобы контейнер подстраивался под суммарную высоту элементов
        ContentSizeFitter csf = achievementsContainer.GetComponent<ContentSizeFitter>();
        if (csf == null)
            csf = achievementsContainer.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // ширина фиксирована (берём от родителя)
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;   // высота по содержимому
    }

    private IEnumerator ClaimReward(int achievementId, Button button)
    {
        using (UnityWebRequest request = new UnityWebRequest($"{serverUrl}/player/{playerId}/achievement/{achievementId}/claim", "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {authToken}");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Награда получена!");
                button.interactable = false;
                StartCoroutine(LoadAchievements());
                PlayerData.Instance?.LoadPlayerData();
            }
            else
            {
                Debug.LogError($"Ошибка при получении награды: {request.error}");
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