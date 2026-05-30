using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemiesSpawner : MonoBehaviour
{
    [Header("Основные настройки")]
    public GameObject enemyPrefab;              // Префаб врага
    public Vector3 spawnPos;                    // Позиция спавна
    public float waveIntervalMin = 20f;         // Мин. время между волнами (сек)
    public float waveIntervalMax = 25f;         // Макс. время между волнами (сек)

    [Header("Настройки волн")]
    public int[] enemiesPerWave = { 2, 5, 10, 15, 20 }; // Врагов в волнах 1,2,3...
    public float spawnDelayWithinWave = 0.3f;   // Задержка между спавном врагов в одной волне
    public float spawnOffset = 1.5f;            // Радиус случайного смещения спавна

    [Header("Информация (только просмотр)")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int enemiesInCurrentWave = 2;

    private int currentWaveIndex = 0;

    void Start()
    {
        
        spawnPos = FindClosest("EnemyBase");
        StartCoroutine(SpawnWaves());
    }

    /// <summary>
    /// Находит ближайший объект с указанным тегом
    /// </summary>
    public Vector3 FindClosest(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        GameObject closest = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (GameObject obj in objects)
        {
            float distance = Vector3.Distance(obj.transform.position, currentPos);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = obj;
            }
        }

        return closest != null ? closest.transform.position : currentPos;
    }

    /// <summary>
    /// Основной цикл спавна волн
    /// </summary>
    IEnumerator SpawnWaves()
    {
        while (true)
        {
            // Получаем количество врагов для текущей волны
            enemiesInCurrentWave = GetEnemiesForCurrentWave();

            // Запускаем спавн волны
            yield return StartCoroutine(SpawnWave(enemiesInCurrentWave));

            // Ждём до следующей волны (20-25 секунд с рандомизацией)
            float waitTime = Random.Range(waveIntervalMin, waveIntervalMax);
            yield return new WaitForSeconds(waitTime);

            // Переходим к следующей волне
            currentWaveIndex++;
            currentWave = currentWaveIndex + 1;
        }
    }

    /// <summary>
    /// Возвращает количество врагов для текущей волны с прогрессией
    /// </summary>
    int GetEnemiesForCurrentWave()
    {
        // Если есть значение в массиве - используем его
        if (currentWaveIndex < enemiesPerWave.Length)
        {
            return enemiesPerWave[currentWaveIndex];
        }
        // После исчерпания массива - продолжаем увеличивать на 5 врагов за волну
        else
        {
            int lastPreset = enemiesPerWave[enemiesPerWave.Length - 1];
            int extraWaves = currentWaveIndex - enemiesPerWave.Length + 1;
            return lastPreset + extraWaves * 5;
        }
    }

    /// <summary>
    /// Спавнит врагов одной волной с небольшой задержкой между ними
    /// </summary>
    IEnumerator SpawnWave(int enemyCount)
    {
        Vector3 basePosition = FindClosest("Base");

        for (int i = 0; i < enemyCount; i++)
        {
            // Добавляем случайное смещение, чтобы враги не спавнились в одной точке
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnOffset, spawnOffset),
                0,
                Random.Range(-spawnOffset, spawnOffset)
            );
            Vector3 spawnPosition = spawnPos + randomOffset;

            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();

            if (agent != null)
            {
                agent.stoppingDistance = 1f;
                agent.SetDestination(basePosition);
            }

            // Небольшая пауза между спавном врагов в одной волне (кроме последнего)
            if (i < enemyCount - 1)
            {
                yield return new WaitForSeconds(spawnDelayWithinWave);
            }
        }

        Debug.Log($"[Wave {currentWave}] Spawned {enemyCount} enemies");
    }

    // Опционально: метод для получения текущей волны из других скриптов
    public int GetCurrentWaveNumber() => currentWave;
    public int GetEnemiesInCurrentWave() => enemiesInCurrentWave;
}