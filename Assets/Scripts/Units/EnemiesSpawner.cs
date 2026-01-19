using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Wave
{
    [Tooltip("Количество врагов в волне")]
    public int enemyCount = 3;

    [Tooltip("Время волны в секундах")]
    public float waveDuration = 10f;

    [Tooltip("Интервал между врагами в волне")]
    public float spawnInterval = 1f;

    [Tooltip("Задержка перед следующей волной")]
    public float waveDelay = 2f;
}

public class EnemiesSpawner : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Настройки волн")]
    [SerializeField] private Wave[] waves;

    [Header("Автоувеличение сложности")]
    [SerializeField] private bool autoIncreaseDifficulty = true;
    [SerializeField] private float enemyCountMultiplier = 1.3f;
    [SerializeField] private float spawnIntervalMultiplier = 0.9f;

    private Transform playerBase;
    private int currentWaveIndex = 0;
    private bool isSpawning = false;
    private int enemiesSpawnedInCurrentWave = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Start()
    {
        InitializeReferences();
        StartCoroutine(WaveSpawnCycle());
    }

    private void InitializeReferences()
    {
        // Находим базу один раз
        GameObject baseObj = GameObject.FindGameObjectWithTag("Base");
        if (baseObj != null)
        {
            playerBase = baseObj.transform;
        }
        else
        {
            Debug.LogError("Не найдена база с тегом 'Base'!");
        }

        // Если нет точек спавна, используем текущую позицию
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoints = new Transform[] { transform };
        }
    }

    private IEnumerator WaveSpawnCycle()
    {
        while (true)
        {
            if (currentWaveIndex < waves.Length)
            {
                yield return StartCoroutine(SpawnWave(waves[currentWaveIndex]));

                // Ждем перед следующей волной
                yield return new WaitForSeconds(waves[currentWaveIndex].waveDelay);

                currentWaveIndex++;

                // Если включено автоувеличение сложности, создаем новую волну
                if (autoIncreaseDifficulty && currentWaveIndex >= waves.Length)
                {
                    CreateNextWave();
                }
            }
            else if (autoIncreaseDifficulty)
            {
                // Создаем новую волну автоматически
                CreateNextWave();
                yield return StartCoroutine(SpawnWave(waves[waves.Length - 1]));
                yield return new WaitForSeconds(waves[waves.Length - 1].waveDelay);
            }
            else
            {
                // Все волны пройдены, ждем
                Debug.Log("Все волны пройдены!");
                yield return new WaitForSeconds(5f);
            }
        }
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        isSpawning = true;
        enemiesSpawnedInCurrentWave = 0;

        Debug.Log($"Начало волны {currentWaveIndex + 1}: {wave.enemyCount} врагов за {wave.waveDuration} секунд");

        float timeBetweenSpawns = wave.waveDuration / wave.enemyCount;
        float actualSpawnInterval = wave.spawnInterval > 0 ? wave.spawnInterval : timeBetweenSpawns;

        for (int i = 0; i < wave.enemyCount; i++)
        {
            SpawnSingleEnemy();
            enemiesSpawnedInCurrentWave++;

            // Обновляем UI (если есть)
            UpdateWaveUI();

            yield return new WaitForSeconds(actualSpawnInterval);
        }

        isSpawning = false;
        Debug.Log($"Волна {currentWaveIndex + 1} завершена. Заспавнено: {enemiesSpawnedInCurrentWave} врагов");
    }

    private void SpawnSingleEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Префаб врага не назначен!");
            return;
        }

        // Выбираем случайную точку спавна
        Vector3 spawnPosition = spawnPoints[Random.Range(0, spawnPoints.Length)].position;

        // Создаем врага
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        activeEnemies.Add(enemy);

        // Настраиваем врага
        SetupEnemy(enemy);
    }

    private void SetupEnemy(GameObject enemy)
    {
        if (playerBase == null) return;

        // Добавляем навигацию
        UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.SetDestination(playerBase.position);

            // Увеличиваем скорость с каждой волной
            if (autoIncreaseDifficulty && currentWaveIndex > 0)
            {
                float speedMultiplier = 1 + (currentWaveIndex * 0.05f);
                agent.speed *= speedMultiplier;
            }
        }

        // Добавляем скрипт для удаления из списка при уничтожении
        EnemyHealth health = enemy.AddComponent<EnemyHealth>();
        health.Initialize(this);
    }

    private void CreateNextWave()
    {
        // Создаем новую волну на основе последней
        Wave lastWave = waves[waves.Length - 1];
        Wave newWave = new Wave
        {
            enemyCount = Mathf.RoundToInt(lastWave.enemyCount * enemyCountMultiplier),
            waveDuration = lastWave.waveDuration * 1.1f, // Увеличиваем длительность на 10%
            spawnInterval = lastWave.spawnInterval * spawnIntervalMultiplier,
            waveDelay = lastWave.waveDelay
        };

        // Добавляем новую волну
        System.Array.Resize(ref waves, waves.Length + 1);
        waves[waves.Length - 1] = newWave;

        Debug.Log($"Создана новая волна {waves.Length}: {newWave.enemyCount} врагов");
    }

    public void RemoveEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }

    private void UpdateWaveUI()
    {
        // Здесь можно обновлять UI, например:
        // UIManager.Instance.UpdateWaveInfo(currentWaveIndex + 1, enemiesSpawnedInCurrentWave, waves[currentWaveIndex].enemyCount);
    }

    public void StartNextWaveImmediately()
    {
        if (!isSpawning)
        {
            StopAllCoroutines();
            StartCoroutine(WaveSpawnCycle());
        }
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
        isSpawning = false;
    }

    public int GetActiveEnemiesCount()
    {
        return activeEnemies.Count;
    }

    public int GetCurrentWaveNumber()
    {
        return currentWaveIndex + 1;
    }

    public int GetTotalWaves()
    {
        return waves.Length;
    }

    private void OnValidate()
    {
        // В редакторе создаем начальные волны, если массив пуст
        if (waves == null || waves.Length == 0)
        {
            waves = new Wave[2];

            // Первая волна: 3 врага за 10 секунд
            waves[0] = new Wave
            {
                enemyCount = 3,
                waveDuration = 10f,
                spawnInterval = 3.33f, // 10 / 3 ≈ 3.33
                waveDelay = 2f
            };

            // Вторая волна: 5 врагов за 15 секунд
            waves[1] = new Wave
            {
                enemyCount = 5,
                waveDuration = 15f,
                spawnInterval = 3f, // 15 / 5 = 3
                waveDelay = 2f
            };
        }
    }
}

// Вспомогательный скрипт для отслеживания здоровья врага
public class EnemyHealth : MonoBehaviour
{
    private EnemiesSpawner spawner;

    public void Initialize(EnemiesSpawner waveSpawner)
    {
        spawner = waveSpawner;
    }

    private void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.RemoveEnemy(gameObject);
        }
    }
}

// Пример UI скрипта для отображения информации о волне
public class WaveInfoUI : MonoBehaviour
{
    [SerializeField] private EnemiesSpawner waveSpawner;
    [SerializeField] private UnityEngine.UI.Text waveText;
    [SerializeField] private UnityEngine.UI.Text enemiesText;
    [SerializeField] private UnityEngine.UI.Text timerText;

    private float waveTimer = 0f;
    private bool isWaveActive = false;

    private void Start()
    {
        if (waveSpawner == null)
        {
            waveSpawner = FindObjectOfType<EnemiesSpawner>();
        }
    }

    private void Update()
    {
        if (waveSpawner != null)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (waveText != null)
        {
            waveText.text = $"Волна: {waveSpawner.GetCurrentWaveNumber()}";
        }

        if (enemiesText != null)
        {
            enemiesText.text = $"Врагов: {waveSpawner.GetActiveEnemiesCount()}";
        }
    }
}