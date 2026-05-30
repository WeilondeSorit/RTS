using System.Collections;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Vector2Int spawnPos = new Vector2Int(85, 85);
    public float spawnInterval = 15f;
    public GameObject[] units = new GameObject[2];

    // Массив славянских имён
    private readonly string[] slavicNames = new string[]
    {
        "Владимир", "Святослав", "Добрыня", "Мирослав",
        "Радомир", "Ярослав", "Вячеслав", "Станислав", "Борислав",
        "Любомир", "Велимир", "Драгомир", "Ростислав", "Светозар"
    };

    // === НАЧАЛЬНАЯ ВМЕСТИМОСТЬ БАЗОВОЙ БАШНИ ===
    private const int BASE_TOWER_HOUSING = 10;

    void Start()
    {
        GameObject baseObject = GameObject.FindGameObjectWithTag("Base");
        if (baseObject == null)
        {
            Debug.LogError("UnitSpawner: нет объекта с тегом Base!");
            return;
        }

        StartCoroutine(SpawnUnits(baseObject));
    }

    /// <summary>
    /// Восстановление юнитов при загрузке игры.
    /// </summary>
    public void SpawnUnitByType(string type, int count)
    {
        if (count <= 0) return;

        GameObject baseObject = GameObject.FindWithTag("Base");
        if (baseObject == null)
        {
            Debug.LogError("No Base found for spawning units");
            return;
        }

        GameObject prefab = null;
        switch (type)
        {
            case "Villager":
                if (units.Length > 0) prefab = units[0];
                break;
            case "Archer":
                if (units.Length > 1) prefab = units[1];
                break;
            case "Enemy":
                prefab = enemyPrefab;
                break;
            default:
                Debug.LogWarning($"Unknown unit type: {type}");
                return;
        }

        if (prefab == null)
        {
            Debug.LogWarning($"Prefab for type {type} is missing");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(2f, 5f);
            Vector3 spawnOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
            Vector3 spawnPosition = baseObject.transform.position + spawnOffset;
            Instantiate(prefab, spawnPosition, Quaternion.identity);
        }
    }

    IEnumerator SpawnUnits(GameObject baseObject)
    {
        // Проверка наличия префабов
        if (units == null || units.Length == 0)
        {
            Debug.LogError("UnitSpawner: массив units пуст!");
            yield break;
        }

        while (true)
        {
            // Ждём заданный интервал перед каждой попыткой спавна
            yield return new WaitForSeconds(spawnInterval);

            // Проверяем все условия
            if (!HasFreeHousing())
            {
                Debug.Log("Нет свободного жилья – юнит не появился");
                continue;
            }

            if (!HasEnoughFood())
            {
                Debug.Log("Недостаточно еды – юнит не появился");
                continue;
            }

            if (!PlayerData.Instance.TryConsumeFood(5))
            {
                Debug.Log("Не удалось списать еду – юнит не появился");
                continue;
            }

            // Выбираем случайного юнита из массива (Villager или Archer)
            GameObject prefabToSpawn = units[Random.Range(0, units.Length)];
            if (prefabToSpawn == null)
            {
                Debug.LogError("UnitSpawner: один из префабов юнитов не назначен!");
                continue;
            }

            // Спавним юнита
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(5f, 10f);
            Vector3 spawnOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
            Vector3 spawnPosition = baseObject.transform.position + spawnOffset;

            GameObject newUnit = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

            // Присваиваем случайное славянское имя
            string randomName = slavicNames[Random.Range(0, slavicNames.Length)];
            newUnit.name = randomName;

            // Добавляем юнита в статистику
            PlayerData.Instance.TryAddUnits(1);
        }
    }

    private bool HasFreeHousing()
    {
        if (BuildingManager.Instance == null)
        {
            Debug.LogWarning("BuildingManager.Instance не найден!");
            return false;
        }

        int totalCapacity = BuildingManager.Instance.GetTotalCapacity() + BASE_TOWER_HOUSING;
        int currentUnits = PlayerData.Instance != null ? PlayerData.Instance.units : 0;
        return currentUnits < totalCapacity;
    }

    private bool HasEnoughFood()
    {
        return PlayerData.Instance != null && PlayerData.Instance.food >= 10;
    }
}