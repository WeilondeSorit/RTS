using System.Collections;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;      // не используется
    public Vector2Int spawnPos = new Vector2Int(85, 85);
    public float spawnInterval = 5f;
    public GameObject[] units = new GameObject[2];

    void Start()
    {
        GameObject baseObject = GameObject.FindGameObjectWithTag("Base");
        if (baseObject == null)
        {
            Debug.LogError("UnitSpawner: нет объекта с тегом Base!");
            return;
        }

        // Спавним 5 стартовых юнитов (игнорируя ресурсы и жильё)
        SpawnInitialUnits(baseObject, 5);

        // Запускаем обычный циклический спавн
        StartCoroutine(SpawnUnits(baseObject));
    }

    /// <summary>
    /// Спавнит указанное количество юнитов принудительно (без проверок еды/жилья)
    /// </summary>
    private void SpawnInitialUnits(GameObject baseObject, int count)
    {
        for (int i = 0; i < count; i++)
        {
            // Случайная позиция вокруг базы
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(5f, 10f);
            Vector3 spawnOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
            Vector3 spawnPosition = baseObject.transform.position + spawnOffset;

            // Случайный юнит из массива
            GameObject unitPrefab = units[Random.Range(0, units.Length)];
            Instantiate(unitPrefab, spawnPosition, Quaternion.identity);

            // Увеличиваем счётчик юнитов в PlayerData (без проверок)
            if (PlayerData.Instance != null)
                PlayerData.Instance.AddUnitsIgnoreCapacity(1);
        }
    }

    IEnumerator SpawnUnits(GameObject baseObject)
    {
        while (true)
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    // Проверки перед спавном
                    if (!HasFreeHousing())
                    {
                        Debug.Log("Нет свободного жилья – юнит не появился");
                        yield return new WaitForSeconds(spawnInterval);
                        continue;
                    }

                    if (!HasEnoughFood())
                    {
                        Debug.Log("Недостаточно еды – юнит не появился");
                        yield return new WaitForSeconds(spawnInterval);
                        continue;
                    }

                    Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(5f, 10f);
                    Vector3 spawnOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
                    Vector3 spawnPosition = baseObject.transform.position + spawnOffset;

                    GameObject newUnit = Instantiate(units[Random.Range(0, units.Length)], spawnPosition, Quaternion.identity);

                    // Тратим еду и добавляем юнита в статистику
                    PlayerData.Instance.food -= 10;
                    PlayerData.Instance.TryAddUnits(1);
                    PlayerData.Instance.UpdateUI();

                    yield return new WaitForSeconds(spawnInterval);
                }
            }
        }
    }

    private bool HasFreeHousing()
    {
        if (BuildingManager.Instance == null) return false;
        int totalCapacity = BuildingManager.Instance.GetTotalCapacity();
        int currentUnits = PlayerData.Instance != null ? PlayerData.Instance.units : 0;
        return currentUnits < totalCapacity;
    }

    private bool HasEnoughFood()
    {
        return PlayerData.Instance != null && PlayerData.Instance.food >= 10;
    }
}