using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Displays player info using TextMeshPro: login, experience, wins, losses.
/// Data is read from PlayerData.Instance.
/// Automatically updates when PlayerData is ready. Call RefreshDisplay() from AuthManager if needed.
/// </summary>
public class PlayerInfoDisplay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text infoText;

    [Header("Settings")]
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
        // Wait until PlayerData.Instance is initialized
        while (PlayerData.Instance == null)
        {
            yield return null;
        }

        // Wait one extra frame to allow SetAuthToken to be called (if needed)
        yield return null;

        RefreshDisplay();
    }

    /// <summary>
    /// Updates the displayed information from PlayerData.Instance.
    /// </summary>
    public void RefreshDisplay()
    {
        if (infoText == null)
        {
            Debug.LogError("PlayerInfoDisplay: infoText is not assigned!");
            return;
        }

        if (PlayerData.Instance == null)
        {
            infoText.text = "Player data not loaded.";
            Debug.LogWarning("PlayerInfoDisplay: PlayerData.Instance is null");
            return;
        }

        // Get login and statistics from PlayerData (adjust property names as needed)
        string login = PlayerData.Instance.playerName;   // Could be Login or GetLogin()
        int exp = PlayerData.Instance.experience;
        int wins = PlayerData.Instance.wins;
        int losses = PlayerData.Instance.losses;

        Debug.Log($"PlayerInfoDisplay: Updating UI -> Login={login}, Exp={exp}, Wins={wins}, Losses={losses}");

        infoText.text = $"Имя: {login}\nОпыт: {exp}\nПобеды: {wins}\nПоражения: {losses}";
    }
}