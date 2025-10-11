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

    // Имя файла сохранения
    private string saveFileName = "save.json";

    void Start()
    {
        // Поиск баз по тегам
        playerBase = GameObject.FindWithTag("Base");
        enemyBase = GameObject.FindWithTag("EnemyBase");

        if (playerBase == null)
            Debug.LogError("Не найдена база игрока! Тег: 'Base'");
        if (enemyBase == null)
            Debug.LogError("Не найдена база врага! Тег: 'EnemyBase'");

        // Разрешаем проверку через 5 секунд
        Invoke(nameof(AllowCheck), 5f);
    }

    void Update()
    {
        if (gameEnded) return;

        // Если проверка разрешена, отслеживаем существование баз
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
        // Удаление файла сохранения при открытии меню результатов
        DeleteSaveFile();

        menu.SetActive(true);
        results.text = "You've lost";
        audioSource.PlayOneShot(audioLoose);
        Time.timeScale = 0f; // Остановка игрового времени
    }

    void YouWin()
    {
        // Удаление файла сохранения при открытии меню результатов
        DeleteSaveFile();

        menu.SetActive(true);
        results.text = "You've won";
        audioSource.PlayOneShot(audioWin);
        Time.timeScale = 0f; // Остановка игрового времени
    }

    void AllowCheck()
    {
        checkAllowed = true;
    }

    public void GoBack()
    {
        Time.timeScale = 1f; // Восстановление времени
        SceneManager.LoadScene("SampleScene");
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
}