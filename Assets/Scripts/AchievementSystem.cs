using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AchievementSystem : MonoBehaviour
{
    private PlayerData playerData;
    private Dictionary<string, Achievement> achievements = new Dictionary<string, Achievement>();
    private List<Achievement> activeQuests = new List<Achievement>();

    // UI элемент для отображения квестов
    [Header("UI References")]
    public TextMeshProUGUI questDisplayText; // Прямая ссылка на текст квеста
    public GameObject questPanel;            // Панель квестов (опционально)
    public Color completedColor = new Color(0.4f, 1f, 0.4f);  // Зеленый для завершенных
    public Color progressColor = new Color(1f, 1f, 1f);       // Белый для прогресса
    public Color rewardColor = new Color(1f, 0.84f, 0f);      // Золотой для наград

    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string title;
        public string description;
        public int targetValue;
        public int currentValue;
        public bool isCompleted;
        public Reward reward;

        public string GetProgressText()
        {
            return isCompleted
                ? $"✅ {title}"
                : $"{title}\n<size=70%>{description}</size>\nПрогресс: {currentValue}/{targetValue}";
        }
    }

    [System.Serializable]
    public class Reward
    {
        public int gold = 0;
        public int gems = 0;
        public int xp = 0;

        public string GetRewardText()
        {
            List<string> rewards = new List<string>();
            if (gold > 0) rewards.Add($"<color=#FFD700>{gold} золота</color>");
            if (gems > 0) rewards.Add($"<color=#00FFFF>{gems} камней</color>");
            if (xp > 0) rewards.Add($"<color=#FFA500>{xp} опыта</color>");
            return string.Join(", ", rewards);
        }
    }

    public void Initialize(PlayerData data)
    {
        playerData = data;
        SetupAchievements();
        LoadAchievements();
    }

    private void SetupAchievements()
    {
        // 1. Первая кровь
        achievements["first_blood"] = new Achievement
        {
            id = "first_blood",
            title = "Первая кровь",
            description = "Уничтожьте первого вражеского юнита",
            targetValue = 1,
            currentValue = 0,
            isCompleted = false,
            reward = new Reward { gold = 100, xp = 50 }
        };

        // 2. Мастер ресурсов
        achievements["resource_master"] = new Achievement
        {
            id = "resource_master",
            title = "Мастер ресурсов",
            description = "Соберите 500 единиц ресурсов",
            targetValue = 500,
            currentValue = 0,
            isCompleted = false,
            reward = new Reward { gold = 200, gems = 10 }
        };

        // 3. Командир армии
        achievements["unit_commander"] = new Achievement
        {
            id = "unit_commander",
            title = "Командир армии",
            description = "Наберите 20 юнитов в армии",
            targetValue = 20,
            currentValue = 0,
            isCompleted = false,
            reward = new Reward { gold = 150, xp = 100 }
        };

        // 4. Разрушитель баз
        achievements["base_destroyer"] = new Achievement
        {
            id = "base_destroyer",
            title = "Разрушитель баз",
            description = "Уничтожьте 3 вражеских базы",
            targetValue = 3,
            currentValue = 0,
            isCompleted = false,
            reward = new Reward { gold = 300, gems = 20, xp = 150 }
        };

        // Активируем первый квест по умолчанию
        activeQuests.Add(achievements["first_blood"]);
        UpdateQuestDisplay();
    }

    public void LoadAchievements()
    {
        foreach (var kvp in achievements)
        {
            string key = $"Achievement_{playerData.playerId}_{kvp.Key}";
            if (PlayerPrefs.HasKey(key))
            {
                try
                {
                    string json = PlayerPrefs.GetString(key);
                    AchievementSave save = JsonUtility.FromJson<AchievementSave>(json);

                    kvp.Value.currentValue = save.currentValue;
                    kvp.Value.isCompleted = save.isCompleted;

                    // Если квест завершен, убираем из активных
                    if (kvp.Value.isCompleted && activeQuests.Contains(kvp.Value))
                    {
                        activeQuests.Remove(kvp.Value);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Не удалось загрузить достижение {kvp.Key}: {ex.Message}");
                }
            }
        }

        // Активируем следующий квест в цепочке
        ActivateNextQuest();

        UpdateQuestDisplay();
        Debug.Log($"✅ Загружено {achievements.Count} достижений");
    }

    private void ActivateNextQuest()
    {
        // Цепочка квестов: first_blood → resource_master → unit_commander → base_destroyer
        if (achievements["first_blood"].isCompleted &&
            !achievements["resource_master"].isCompleted &&
            !activeQuests.Contains(achievements["resource_master"]))
        {
            activeQuests.Add(achievements["resource_master"]);
        }
        else if (achievements["resource_master"].isCompleted &&
                 !achievements["unit_commander"].isCompleted &&
                 !activeQuests.Contains(achievements["unit_commander"]))
        {
            activeQuests.Add(achievements["unit_commander"]);
        }
        else if (achievements["unit_commander"].isCompleted &&
                 !achievements["base_destroyer"].isCompleted &&
                 !activeQuests.Contains(achievements["base_destroyer"]))
        {
            activeQuests.Add(achievements["base_destroyer"]);
        }
    }

    public void SaveAchievement(string achievementId)
    {
        if (achievements.TryGetValue(achievementId, out var achievement))
        {
            AchievementSave save = new AchievementSave
            {
                id = achievementId,
                currentValue = achievement.currentValue,
                isCompleted = achievement.isCompleted
            };

            string json = JsonUtility.ToJson(save);
            PlayerPrefs.SetString($"Achievement_{playerData.playerId}_{achievementId}", json);
            PlayerPrefs.Save();
            Debug.Log($"💾 Сохранено достижение: {achievementId} ({achievement.currentValue}/{achievement.targetValue})");
        }
    }

    // ===== ТРИГГЕРЫ ДОСТИЖЕНИЙ =====
    public void OnEnemyUnitKilled(string unitType)
    {
        ProgressAchievement("first_blood", 1);
    }

    public void OnResourceCollected(string resourceType, int amount)
    {
        ProgressAchievement("resource_master", amount);
    }

    public void OnUnitCreated(string unitType)
    {
        ProgressAchievement("unit_commander", 1);
    }

    public void OnEnemyBaseDestroyed()
    {
        ProgressAchievement("base_destroyer", 1);
    }

    private void ProgressAchievement(string achievementId, int value)
    {
        if (achievements.TryGetValue(achievementId, out var achievement) && !achievement.isCompleted)
        {
            achievement.currentValue += value;
            SaveAchievement(achievementId);

            // Проверяем завершение
            if (achievement.currentValue >= achievement.targetValue)
            {
                CompleteAchievement(achievement);
            }
            else
            {
                UpdateQuestDisplay();
            }
        }
    }

    private void CompleteAchievement(Achievement achievement)
    {
        achievement.isCompleted = true;
        achievement.currentValue = achievement.targetValue;
        SaveAchievement(achievement.id);

        // Начисляем награду
        int gold = PlayerPrefs.GetInt("PlayerGold", 0) + achievement.reward.gold;
        int gems = PlayerPrefs.GetInt("PlayerGems", 0) + achievement.reward.gems;
        int xp = PlayerPrefs.GetInt("PlayerXP", 0) + achievement.reward.xp;

        PlayerPrefs.SetInt("PlayerGold", gold);
        PlayerPrefs.SetInt("PlayerGems", gems);
        PlayerPrefs.SetInt("PlayerXP", xp);
        PlayerPrefs.Save();

        // Убираем из активных квестов
        activeQuests.Remove(achievement);

        // Активируем следующий квест
        ActivateNextQuest();

        // Показываем уведомление с анимацией
        StartCoroutine(ShowCompletionNotification(achievement));

        Debug.Log($"✅ Достижение завершено: {achievement.title}. Награда: {achievement.reward.GetRewardText()}");
    }

    private IEnumerator ShowCompletionNotification(Achievement achievement)
    {
        // Создаем красивое уведомление о завершении
        string rewardText = achievement.reward.GetRewardText();

        if (questDisplayText != null)
        {
            // Анимация: показываем завершение
            questDisplayText.color = completedColor;
            questDisplayText.text = $"<size=110%><b> {achievement.title} выполнено!</b></size>\n<size=80%>Награда: {rewardText}</size>";

            // Мерцание для привлечения внимания
            for (int i = 0; i < 3; i++)
            {
                questDisplayText.color = completedColor;
                yield return new WaitForSeconds(0.2f);
                questDisplayText.color = Color.white;
                yield return new WaitForSeconds(0.2f);
            }

            yield return new WaitForSeconds(2f);

            // Возвращаем обычный текст
            UpdateQuestDisplay();
        }
    }

    public void UpdateQuestDisplay()
    {
        // Если нет прямой ссылки на текст, используем PlayerData
        if (questDisplayText == null && playerData != null && playerData.questDisplayText != null)
        {
            questDisplayText = playerData.questDisplayText;
        }

        if (questDisplayText != null)
        {
            if (activeQuests.Count > 0)
            {
                var quest = activeQuests[0];

                if (quest.isCompleted)
                {
                    questDisplayText.color = completedColor;
                    questDisplayText.text = $"<b>{quest.GetProgressText()}</b>";
                }
                else
                {
                    questDisplayText.color = progressColor;
                    questDisplayText.text = quest.GetProgressText();

                    // Добавляем прогресс-бар в текст (визуальная индикация)
                    float progress = (float)quest.currentValue / quest.targetValue;
                    string progressBar = GetProgressBar(progress);
                    questDisplayText.text += $"\n{progressBar}";
                }
            }
            else
            {
                // Проверяем, есть ли завершенные достижения
                bool hasCompleted = false;
                foreach (var achievement in achievements.Values)
                {
                    if (achievement.isCompleted)
                    {
                        hasCompleted = true;
                        break;
                    }
                }

                if (hasCompleted)
                {
                    questDisplayText.color = completedColor;
                    questDisplayText.text = "<b>✅ Все доступные квесты выполнены!</b>\n<size=70%>Ожидайте новых заданий...</size>";
                }
                else
                {
                    questDisplayText.color = Color.gray;
                    questDisplayText.text = "<i>Нет активных квестов</i>";
                }
            }

            // Показываем/скрываем панель
            if (questPanel != null)
            {
                questPanel.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("⚠️ QuestDisplayText не назначен! Присвойте ссылку на TextMeshPro в инспекторе.");
        }
    }

    // Вспомогательный метод для создания прогресс-бара в тексте
    private string GetProgressBar(float progress)
    {
        int totalBlocks = 10;
        int filledBlocks = Mathf.FloorToInt(progress * totalBlocks);
        int emptyBlocks = totalBlocks - filledBlocks;

        string filled = new string('*', filledBlocks);
        string empty = new string('_', emptyBlocks);

        int percent = Mathf.FloorToInt(progress * 100);

        return $"<color=#4CAF50>{filled}</color><color=#555555>{empty}</color> {percent}%";
    }

    // ===== ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ =====
    [System.Serializable]
    private class AchievementSave
    {
        public string id;
        public int currentValue;
        public bool isCompleted;
    }
}