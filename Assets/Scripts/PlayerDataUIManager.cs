using UnityEngine;
using TMPro;

public class PlayerDataUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI unitsText;
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI rockText;
    public TextMeshProUGUI questDisplayText;

    private PlayerData playerData;

    private void Awake()
    {
        // Находим перенесённый объект PlayerData
        playerData = PlayerData.Instance;
        if (playerData == null)
        {
            Debug.LogError("PlayerData.Instance не найден! Убедитесь, что объект с PlayerData загружен и не уничтожается между сценами.");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (playerData == null) return;

        // Подписываемся на события
        playerData.OnResourcesChanged += UpdateAllResourcesUI;
        playerData.OnQuestTextChanged += UpdateQuestUIText;

        // Принудительно обновляем UI при появлении скрипта
        UpdateAllResourcesUI();
        // (текст квеста обновится сам, когда придёт событие, но можно запросить текущий текст, если нужно)
    }

    private void OnDisable()
    {
        if (playerData == null) return;

        playerData.OnResourcesChanged -= UpdateAllResourcesUI;
        playerData.OnQuestTextChanged -= UpdateQuestUIText;
    }

    private void UpdateAllResourcesUI()
    {
        if (unitsText != null) unitsText.text = playerData.units.ToString();
        if (foodText != null) foodText.text = playerData.food.ToString();
        if (woodText != null) woodText.text = playerData.wood.ToString();
        if (rockText != null) rockText.text = playerData.rock.ToString();
    }

    private void UpdateQuestUIText(string text)
    {
        if (questDisplayText != null) questDisplayText.text = text;
    }
}