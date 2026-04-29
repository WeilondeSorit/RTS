using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Выводит в TextMeshPro информацию о текущем игроке: логин, опыт, победы, поражения.
/// Данные берутся из PlayerData.Instance.
/// Обновление происходит либо при появлении PlayerData, либо по вызову RefreshDisplay() из AuthManager.
/// </summary>
public class PlayerInfoDisplay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text infoText;

    [Header("Настройки")]
    [SerializeField] private bool waitForInstance = true;

    private void OnEnable()
    {
        if (waitForInstance)
            StartCoroutine(WaitForPlayerDataAndDisplay());
        else
            RefreshDisplay();
    }

    private IEnumerator WaitForPlayerDataAndDisplay()
    {
        // Ждём, пока PlayerData.Instance появится
        while (PlayerData.Instance == null)
        {
            yield return null;
        }

        // Небольшая задержка, чтобы SetAuthToken успел отработать (если нужно)
        yield return null;

        RefreshDisplay();
    }

    /// <summary>
    /// Принудительно обновляет текстовое поле текущими данными из PlayerData.Instance.
    /// </summary>
    public void RefreshDisplay()
    {
        if (infoText == null)
        {
            Debug.LogError("PlayerInfoDisplay: infoText не назначен!");
            return;
        }

        if (PlayerData.Instance == null)
        {
            infoText.text = "Данные игрока не загружены.";
            Debug.LogWarning("PlayerInfoDisplay: PlayerData.Instance = null");
            return;
        }

        // Получаем логин – используем поле playerName (если у вас другое – замените)
        string login = PlayerData.Instance.playerName;   // может быть Login или GetLogin()
        int exp = PlayerData.Instance.experience;
        int wins = PlayerData.Instance.wins;
        int losses = PlayerData.Instance.losses;

        Debug.Log($"PlayerInfoDisplay: обновление UI -> логин={login}, опыт={exp}, победы={wins}, поражения={losses}");

        infoText.text = $"Логин: {login}\nОпыт: {exp}\nПобед: {wins}\nПоражений: {losses}";
    }
}