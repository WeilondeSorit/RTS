using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

public class GameManager : MonoBehaviour
{
    public GameObject menu;
    public TextMeshProUGUI results;
    public AudioClip audioLoose;
    public AudioClip audioWin;
    public AudioSource audioSource;

    private GameObject playerBase;
    private GameObject enemyBase;
    private bool gameEnded = false;
    private bool checkAllowed = false;

    // Путь к файлу сохранения
    private string saveFileName = "save.json";
    private PlayerData playerData;

    void Start()
    {
        // Находим базы по тегам
        playerBase = GameObject.FindWithTag("Base");
        enemyBase = GameObject.FindWithTag("EnemyBase");
        playerData = FindObjectOfType<PlayerData>();

        if (playerBase == null)
            Debug.LogError("Не найдена база игрока! Тег: 'Base'");
        if (enemyBase == null)
            Debug.LogError("Не найдена база врага! Тег: 'EnemyBase'");
        if (playerData == null)
            Debug.LogError("PlayerData не найден!");

        // Разрешаем проверку через 5 секунд
        Invoke(nameof(AllowCheck), 5f);
    }

    void Update()
    {
        if (gameEnded) return;

        // Если проверка разрешена, проверяем условия победы/проигрыша
        if (checkAllowed)
        {
            if (playerBase == null)
            {
                YouLoose();
                gameEnded = true;
            }
            else if (enemyBase == null)
            {
                YouWin();
                gameEnded = true;
            }
        }
    }

    void YouLoose()
    {
        if (PlayerData.Instance != null)
            PlayerData.Instance.isGameActive = false;

        DeleteSaveFile();
        menu.SetActive(true);
        results.text = "Вы проиграли";
        audioSource.PlayOneShot(audioLoose);
        Time.timeScale = 0f;
    }

    void YouWin()
    {
        if (PlayerData.Instance != null)
            PlayerData.Instance.isGameActive = false;

        DeleteSaveFile();
        menu.SetActive(true);
        results.text = "Вы выиграли!";
        audioSource.PlayOneShot(audioWin);
        Time.timeScale = 0f;
    }

    public void GoBack()
    {
        if (PlayerData.Instance != null)
            PlayerData.Instance.isGameActive = false;

        Time.timeScale = 1f;
        SimpleLoadingManager.LoadSceneWithLoading("SampleScene");
    }

    void AllowCheck()
    {
        checkAllowed = true;
    }

  

    void DeleteSaveFile()
    {
        string savePath = Path.Combine(Application.persistentDataPath, saveFileName);
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Файл сохранения удален.");
        }
        else
        {
            Debug.Log("Файл сохранения не найден для удаления.");
        }
    }

    // Сохранение игры (вызывайте этот метод при необходимости)
    public void SaveGame()
    {
        if (playerData != null)
        {
           // playerData.SaveGame();
        }
    }

    // Загрузка игры
    public void LoadGame()
    {
        if (playerData != null)
        {
           // playerData.LoadGame();
        }
    }
}