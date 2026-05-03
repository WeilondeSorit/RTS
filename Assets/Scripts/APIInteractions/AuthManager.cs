using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

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

    private bool isLoginInProgress = false;
    private bool isRegisterInProgress = false;

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
        switchToRegisterButton.onClick.AddListener(SwitchToRegisterPanel);
        switchToLoginButton.onClick.AddListener(SwitchToLoginPanel);

        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutClicked);

        // Показываем панель логина по умолчанию
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        allButtons.SetActive(false);

        // Попытка автовхода, только если все ключи на месте
        string savedToken = PlayerPrefs.GetString("AuthToken", "");
        string savedPlayerId = PlayerPrefs.GetString("PlayerId", "");
        string savedLogin = PlayerPrefs.GetString("PlayerLogin", "");

        if (!string.IsNullOrEmpty(savedToken) && !string.IsNullOrEmpty(savedPlayerId) && !string.IsNullOrEmpty(savedLogin))
        {
            loginUsernameInput.text = savedLogin;
            Debug.Log($"Автовход: {savedLogin} ({savedPlayerId})");
            StartCoroutine(GetPlayerDataAndLoad(savedPlayerId, savedLogin));
            allButtons.SetActive(true);
        }
        else
        {
            Debug.Log("Нет сохранённой сессии – требуется ручной вход.");
        }
    }

    private void OnLoginClicked()
    {
        if (isLoginInProgress) return;

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
        if (isRegisterInProgress) return;

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
        isLoginInProgress = true;
        loginButton.interactable = false;
        loginStatusText.text = "Подключение...";
        loginStatusText.color = Color.white;

        // Перед новым входом полностью стираем старые сохранения, чтобы не осталось следов
        ClearSessionData();

        string hashedPassword = HashPassword(password);
        var jsonBody = JsonConvert.SerializeObject(new { login, password = hashedPassword });

        using var request = UnityWebRequest.PostWwwForm($"{mainServerUrl}/auth/login", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        loginButton.interactable = true;
        isLoginInProgress = false;

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);
            string playerId = response.id;
            string token = response.token;

            // Сохраняем новые данные
            PlayerPrefs.SetString("PlayerId", playerId);
            PlayerPrefs.SetString("AuthToken", token);
            PlayerPrefs.SetString("PlayerLogin", login);
            PlayerPrefs.Save();

            if (PlayerData.Instance != null)
                PlayerData.Instance.SetAuthToken(token, playerId, login);

            Debug.Log($"Успешный вход. PlayerId: {playerId}");
            ShowLoginStatus("✅ Вход выполнен!");
            allButtons.SetActive(true);
            StartCoroutine(GetPlayerDataAndLoad(playerId, login));
        }
        else
        {
            Debug.LogError($"Login error: {request.error}, {request.downloadHandler.text}");
            ShowLoginError("❌ Неверный логин или пароль");
            // Возвращаем панель логина (на случай, если были очищены ключи)
            loginPanel.SetActive(true);
        }
    }

    private IEnumerator RegisterRequest(string login, string password)
    {
        isRegisterInProgress = true;
        registerButton.interactable = false;
        registerStatusText.text = "Регистрация...";
        registerStatusText.color = Color.white;

        // Очищаем старые данные перед регистрацией
        ClearSessionData();

        string hashedPassword = HashPassword(password);
        var jsonBody = JsonConvert.SerializeObject(new { login, password = hashedPassword });

        using var request = UnityWebRequest.PostWwwForm($"{mainServerUrl}/auth/register", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        registerButton.interactable = true;
        isRegisterInProgress = false;

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonConvert.DeserializeObject<RegisterResponse>(request.downloadHandler.text);
            ShowRegisterStatus("✅ Регистрация успешна! Выполняется вход...");
            loginPanel.SetActive(false);
            registerPanel.SetActive(false);

            // После успешной регистрации сразу логинимся (сохранит токен и загрузит данные)
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
    /// Загружает данные игрока с сервера, используя сохранённый токен.
    /// </summary>
    private IEnumerator GetPlayerDataAndLoad(string playerId, string login)
    {
        string token = PlayerPrefs.GetString("AuthToken", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Нет токена для запроса данных игрока");
            HandleSessionExpired();
            yield break;
        }

        using var request = UnityWebRequest.Get($"{mainServerUrl}/player/{playerId}");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var playerData = JsonConvert.DeserializeObject<PlayerDataResponse>(request.downloadHandler.text);
            if (PlayerData.Instance != null)
            {
                PlayerData.Instance.SetAuthToken(token, playerId, login);
                PlayerData.Instance.experience = playerData.experience;
                PlayerData.Instance.currency = playerData.currency;
                PlayerData.Instance.wins = playerData.wins;
                PlayerData.Instance.losses = playerData.losses;
            }
            else
            {
                Debug.LogError("PlayerData.Instance не найден на сцене!");
            }

            ShowLoginStatus("✅ Вход успешный!");
            loginPanel.SetActive(false);
            allButtons.SetActive(true);

            // Обновляем UI с информацией об игроке
            PlayerInfoDisplay display = FindObjectOfType<PlayerInfoDisplay>();
            if (display != null)
                display.RefreshDisplay();
        }
        else if (request.responseCode == 401)
        {
            Debug.LogWarning("Токен недействителен, требуется повторный вход.");
            HandleSessionExpired();
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
        Debug.Log("Выход из аккаунта...");
        ClearSessionData();

        // Дополнительно полностью сбрасываем PlayerData
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.experience = 0;
            PlayerData.Instance.currency = 0;
            PlayerData.Instance.wins = 0;
            PlayerData.Instance.losses = 0;
            PlayerData.Instance.SetAuthToken("", "", "");
        }

        // Останавливаем все активные корутины, чтобы избежать конфликтов
        StopAllCoroutines();

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

        // Снимаем блокировку, если она была
        isLoginInProgress = false;
        isRegisterInProgress = false;

        Debug.Log("Пользователь вышел из аккаунта");
    }

    /// <summary>
    /// Вызывается при любой ошибке авторизации (401) или принудительном сбросе.
    /// </summary>
    private void HandleSessionExpired()
    {
        ClearSessionData();
        if (PlayerData.Instance != null)
            PlayerData.Instance.SetAuthToken("", "", "");

        StopAllCoroutines();
        loginUsernameInput.text = "";
        loginPasswordInput.text = "";
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        allButtons.SetActive(false);
        loginStatusText.text = "Сессия истекла, войдите заново";
        loginStatusText.color = Color.red;
    }

    /// <summary>
    /// Полностью удаляет сохранённые ключи из PlayerPrefs, связанные с сессией.
    /// </summary>
    private void ClearSessionData()
    {
        PlayerPrefs.DeleteKey("PlayerId");
        PlayerPrefs.DeleteKey("PlayerLogin");
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.Save();
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

    private void SwitchToRegisterPanel()
    {
        loginPanel.SetActive(false);
        allButtons.SetActive(false);
        registerPanel.SetActive(true);
        loginStatusText.text = "";
    }

    private void SwitchToLoginPanel()
    {
        registerPanel.SetActive(false);
        allButtons.SetActive(false);
        loginPanel.SetActive(true);
        registerStatusText.text = "";
    }

    [Serializable] private class LoginResponse { public string id; public string token; }
    [Serializable] private class RegisterResponse { public string id; public string token; }
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