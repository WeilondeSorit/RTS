using System.Collections;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;      // не используется
    public Vector2Int spawnPos = new Vector2Int(85, 85);
    public float spawnInterval = 15f;
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
        SpawnInitialUnits(baseObject, 30);

        // Запускаем обычный циклический спавн
        StartCoroutine(SpawnUnits(baseObject));
    }

    private void SpawnInitialUnits(GameObject baseObject, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(5f, 10f);
            Vector3 spawnOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
            Vector3 spawnPosition = baseObject.transform.position + spawnOffset;

            GameObject unitPrefab = units[Random.Range(0, units.Length)];
            Instantiate(unitPrefab, spawnPosition, Quaternion.identity);

            // Используем специальный метод для добавления юнитов без проверок
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

                    // Пытаемся списать еду через метод TryConsumeFood
                    if (!PlayerData.Instance.TryConsumeFood(5))
                    {
                        Debug.Log("Не удалось списать еду – юнит не появился");
                        yield return new WaitForSeconds(spawnInterval);
                        continue;
                    }

                    // Спавним юнита
                    Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(5f, 10f);
                    Vector3 spawnOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
                    Vector3 spawnPosition = baseObject.transform.position + spawnOffset;

                    Instantiate(units[Random.Range(0, units.Length)], spawnPosition, Quaternion.identity);

                    // Добавляем юнита в статистику (метод сам вызовет OnResourcesChanged)
                    PlayerData.Instance.TryAddUnits(1);

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
        // Проверяем, хватает ли еды (через публичное поле только для чтения)
        return PlayerData.Instance != null && PlayerData.Instance.food >= 10;
    }
}