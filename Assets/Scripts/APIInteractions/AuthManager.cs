using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

public class AuthManager : MonoBehaviour
{
    [Header("Login Panel")]
    public GameObject loginPanel;
    public GameObject allButtons;
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;
    public Button loginButton;
    public Button switchToRegisterButton;
    public TMP_Text loginStatusText;

    [Header("Register Panel")]
    public GameObject registerPanel;
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerPasswordInput;
    public Button registerButton;
    public Button switchToLoginButton;
    public TMP_Text registerStatusText;

    [Header("Logout")]
    public Button logoutButton;

    [Header("Scene Management")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("API Settings")]
    [SerializeField] private string mainServerUrl = "http://localhost:8080";

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
        switchToRegisterButton.onClick.AddListener(SwitchToRegisterPanel);
        switchToLoginButton.onClick.AddListener(SwitchToLoginPanel);

        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutClicked);

        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        allButtons.SetActive(false);

        // --- Автовход по сохранённым данным ---
        if (PlayerPrefs.HasKey("PlayerId"))
        {
            string playerId = PlayerPrefs.GetString("PlayerId");
            string savedLogin = PlayerPrefs.GetString("PlayerLogin", "");
            if (!string.IsNullOrEmpty(savedLogin))
            {
                loginUsernameInput.text = savedLogin;   // показываем логин в поле
                Debug.Log($"Автовход: {savedLogin} ({playerId})");
                StartCoroutine(GetPlayerDataAndLoad(playerId, savedLogin));
                allButtons.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Найден PlayerId, но нет сохранённого логина – требуется ручной вход.");
            }
        }
    }

    private void OnLoginClicked()
    {
        string login = loginUsernameInput.text.Trim();
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            ShowLoginError("Заполните все поля");
            return;
        }

        StartCoroutine(LoginRequest(login, password));
    }

    private void OnRegisterClicked()
    {
        string login = registerUsernameInput.text.Trim();
        string password = registerPasswordInput.text;

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            ShowRegisterError("Заполните все поля");
            return;
        }

        if (login.Length < 3)
        {
            ShowRegisterError("Логин не менее 3 символов");
            return;
        }

        if (password.Length < 6)
        {
            ShowRegisterError("Пароль не менее 6 символов");
            return;
        }

        StartCoroutine(RegisterRequest(login, password));
    }

    private IEnumerator LoginRequest(string login, string password)
    {
        loginButton.interactable = false;
        loginStatusText.text = "Подключение...";
        loginStatusText.color = Color.white;

        string hashedPassword = HashPassword(password);
        var jsonBody = JsonConvert.SerializeObject(new { login, password = hashedPassword });

        using var request = UnityWebRequest.PostWwwForm($"{mainServerUrl}/auth/login", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        loginButton.interactable = true;

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);
            string playerId = response.id;

            // Сохраняем и ID, и логин
            PlayerPrefs.SetString("PlayerId", playerId);
            PlayerPrefs.SetString("PlayerLogin", login);
            PlayerPrefs.Save();

            Debug.Log($"Успешный вход. PlayerId: {playerId}, логин: {login}");
            ShowLoginStatus("✅ Вход выполнен!");
            allButtons.SetActive(true);
            StartCoroutine(GetPlayerDataAndLoad(playerId, login));
        }
        else
        {
            Debug.LogError($"Login error: {request.error}, {request.downloadHandler.text}");
            ShowLoginError("❌ Неверный логин или пароль");
        }
    }

    private IEnumerator RegisterRequest(string login, string password)
    {
        registerButton.interactable = false;
        registerStatusText.text = "Регистрация...";
        registerStatusText.color = Color.white;

        string hashedPassword = HashPassword(password);
        var jsonBody = JsonConvert.SerializeObject(new { login, password = hashedPassword });

        using var request = UnityWebRequest.PostWwwForm($"{mainServerUrl}/auth/register", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        registerButton.interactable = true;

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonConvert.DeserializeObject<RegisterResponse>(request.downloadHandler.text);
            string playerId = response.id;
            ShowRegisterStatus("✅ Регистрация успешна! Вход...");
            loginPanel.SetActive(false);
            registerPanel.SetActive(false);
            Debug.Log($"Зарегистрирован новый игрок: {playerId}");

            // После регистрации выполняем вход (сохранит логин/пароль и получит данные)
            StartCoroutine(LoginRequest(login, password));
        }
        else
        {
            Debug.LogError($"Register error: {request.error}, {request.downloadHandler.text}");
            if (request.downloadHandler.text.Contains("Login exists"))
                ShowRegisterError("❌ Логин уже занят");
            else
                ShowRegisterError("❌ Ошибка регистрации");
        }
    }

    /// <summary>
    /// Загружает данные игрока с сервера (опыт, победы, поражения) и записывает в PlayerData.Instance.
    /// </summary>
    /// <param name="playerId">GUID игрока</param>
    /// <param name="login">логин (сервер его не возвращает, поэтому передаём отдельно)</param>
    private IEnumerator GetPlayerDataAndLoad(string playerId, string login)
    {
        using var request = UnityWebRequest.Get($"{mainServerUrl}/player/{playerId}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var playerData = JsonConvert.DeserializeObject<PlayerDataResponse>(request.downloadHandler.text);
            if (PlayerData.Instance != null)
            {
                // Записываем логин и ID
                PlayerData.Instance.SetAuthToken("", playerId, login);
                // Записываем игровую статистику
                PlayerData.Instance.experience = playerData.experience;
                PlayerData.Instance.currency = playerData.currency;
                PlayerData.Instance.wins = playerData.wins;
                PlayerData.Instance.losses = playerData.losses;

                // Обновляем UI с информацией об игроке (если существует на сцене)
                PlayerInfoDisplay display = FindObjectOfType<PlayerInfoDisplay>();
                if (display != null)
                    display.RefreshDisplay();
            }
            else
            {
                Debug.LogError("PlayerData.Instance не найден на сцене!");
            }

            ShowLoginStatus("Вход успешный!");
            loginPanel.SetActive(false);
        }
        else
        {
            Debug.LogError($"Failed to load player data: {request.error}");
            ShowLoginError("Ошибка загрузки данных игрока");
            allButtons.SetActive(false);
        }
    }

    public void OnLogoutClicked()
    {
        // Очищаем сохранённые данные
        if (PlayerPrefs.HasKey("PlayerId"))
            PlayerPrefs.DeleteKey("PlayerId");
        if (PlayerPrefs.HasKey("PlayerLogin"))
            PlayerPrefs.DeleteKey("PlayerLogin");
        PlayerPrefs.Save();

        // Очищаем данные в синглтоне
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.experience = 0;
            PlayerData.Instance.currency = 0;
            PlayerData.Instance.wins = 0;
            PlayerData.Instance.losses = 0;
            PlayerData.Instance.SetAuthToken("", "", "");
        }

        // Сбрасываем UI
        loginUsernameInput.text = "";
        loginPasswordInput.text = "";
        registerUsernameInput.text = "";
        registerPasswordInput.text = "";
        loginStatusText.text = "";
        registerStatusText.text = "";

        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        allButtons.SetActive(false);

        StopAllCoroutines();

        Debug.Log("Пользователь вышел из аккаунта");
    }

    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = sha256.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    private void ShowLoginStatus(string message) { loginStatusText.text = message; loginStatusText.color = Color.white; }
    private void ShowLoginError(string message) { loginStatusText.text = message; loginStatusText.color = Color.red; }
    private void ShowRegisterStatus(string message) { registerStatusText.text = message; registerStatusText.color = Color.white; }
    private void ShowRegisterError(string message) { registerStatusText.text = message; registerStatusText.color = Color.red; }

    private void SwitchToRegisterPanel() { loginPanel.SetActive(false); allButtons.SetActive(false); registerPanel.SetActive(true); loginStatusText.text = ""; }
    private void SwitchToLoginPanel() { registerPanel.SetActive(false); allButtons.SetActive(false); loginPanel.SetActive(true); registerStatusText.text = ""; }

    [Serializable] private class LoginResponse { public string id; }
    [Serializable] private class RegisterResponse { public string id; }
    [Serializable]
    private class PlayerDataResponse
    {
        public int experience;
        public int currency;
        public int wins;
        public int losses;
        public List<int> purchasedItems;
        public Dictionary<string, int> unitUpgrades;
    }
}