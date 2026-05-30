using System.Collections;
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
    private bool isFinalizing = false;
    private PlayerData playerData;

    void Start()
    {
        // Используем FindFirstObjectByType вместо устаревшего FindObjectOfType
        playerData = FindFirstObjectByType<PlayerData>();
        playerData.ResetSession();
        if (playerData == null)
        {
            Debug.LogError("PlayerData not found!");
            return;
        }

        playerBase = GameObject.FindWithTag("Base");
        enemyBase = GameObject.FindWithTag("EnemyBase");

        if (playerBase == null) Debug.LogError("Base not found!");
        if (enemyBase == null) Debug.LogError("EnemyBase not found!");

        // 1. Создаём сессию на сервере 8082 (Redis)
        playerData.StartServerSession(success =>
        {
            if (success)
            {
                // 2. Загружаем сохранённое состояние
                playerData.LoadGameStateFromServer(loaded =>
                {
                    if (loaded)
                        RestoreGameState();   // восстановить юнитов и HP баз
                    playerData.isGameActive = true;
                    Invoke(nameof(AllowCheck), 5f);
                });
            }
            else
            {
                Debug.LogWarning("Could not create session – playing without saving");
                playerData.isGameActive = true;
                Invoke(nameof(AllowCheck), 5f);
            }
        });
    }

    // Восстановление юнитов и здоровья баз из сохранённых данных
    private void RestoreGameState()
    {
        // 1. Удаляем всех существующих юнитов, кроме баз
        string[] unitTags = { "Villager", "Archer", "Enemy" };
        foreach (string tag in unitTags)
        {
            GameObject[] units = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject unit in units)
                Destroy(unit);
        }

        // 2. Восстанавливаем здоровье баз
        GameObject playerBaseObj = GameObject.FindWithTag("Base");
        if (playerBaseObj != null && playerBaseObj.TryGetComponent<Health>(out var playerHealth))
            playerHealth.health = playerData.savedPlayerBaseHp;

        GameObject enemyBaseObj = GameObject.FindWithTag("EnemyBase");
        if (enemyBaseObj != null && enemyBaseObj.TryGetComponent<Health>(out var enemyHealth))
            enemyHealth.health = playerData.savedEnemyBaseHp;

        // 3. Спавним юнитов через UnitSpawner с игнорированием лимита жилья
        UnitSpawner spawner = FindFirstObjectByType<UnitSpawner>();
        if (spawner != null)
        {
            spawner.SpawnUnitByType("Villager", playerData.savedVillagers);
            spawner.SpawnUnitByType("Archer", playerData.savedArchers);
            spawner.SpawnUnitByType("Enemy", playerData.savedEnemies);
        }
        else
        {
            Debug.LogWarning("UnitSpawner not found – units cannot be restored");
        }

        // 4. Обновляем общее количество юнитов в PlayerData
        playerData.units = playerData.savedVillagers + playerData.savedArchers;

        Debug.Log($"Game state restored: Villagers={playerData.savedVillagers}, Archers={playerData.savedArchers}, " +
                  $"Enemies={playerData.savedEnemies}, PlayerBaseHP={playerData.savedPlayerBaseHp}, EnemyBaseHP={playerData.savedEnemyBaseHp}");
    }

    void Update()
    {
        if (gameEnded || isFinalizing) return;
        if (checkAllowed)
        {
            if (playerBase == null)
                YouLoose();
            else if (enemyBase == null)
                YouWin();
        }
    }

    void YouLoose()
    {
        if (isFinalizing) return;
        isFinalizing = true;
        if (playerData != null) playerData.isGameActive = false;

        // Сначала сохраняем состояние игры
        playerData.SaveGameStateToServer(success =>
        {
            // Затем отправляем результат боя на account-сервер
            playerData.SendBattleResult(false, (battleSuccess, error) =>
            {
                if (!battleSuccess)
                    Debug.LogError($"SendBattleResult failed: {error}");

                // Завершаем сессию в Redis
                playerData.EndServerSession(false, _ =>
                {
                    FinalizeGame(false);
                });
            });
        });
    }

    void YouWin()
    {
        if (isFinalizing) return;
        isFinalizing = true;
        if (playerData != null) playerData.isGameActive = false;

        playerData.SaveGameStateToServer(success =>
        {
            playerData.SendBattleResult(true, (battleSuccess, error) =>
            {
                if (!battleSuccess)
                    Debug.LogError($"SendBattleResult failed: {error}");

                playerData.EndServerSession(true, _ =>
                {
                    FinalizeGame(true);
                });
            });
        });
    }

    private void FinalizeGame(bool isWin)
    {
        DeleteSaveFile();
        menu.SetActive(true);
        results.text = isWin ? "Вы выиграли!" : "Вы проиграли";
        if (isWin && audioWin != null) audioSource.PlayOneShot(audioWin);
        else if (!isWin && audioLoose != null) audioSource.PlayOneShot(audioLoose);
        Time.timeScale = 0f;
    }

    public void GoBack()
    {
        if (playerData != null) playerData.isGameActive = false;
        Time.timeScale = 1f;
        SimpleLoadingManager.LoadSceneWithLoading("SampleScene");
    }

    private void AllowCheck()
    {
        checkAllowed = true;
    }

    private void DeleteSaveFile()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "save.json");
        if (File.Exists(savePath)) File.Delete(savePath);
    }
}