using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinMap : MonoBehaviour
{
    public int width = 100;
    public int height = 100;
    public float scale = 20f;
    public float offsetX, offsetY;
    public GameObject tree;
    public GameObject rock;
    public GameObject basePrefab;

    [Header("Variation Settings")]
    public float minTreeScale = 0.8f;
    public float maxTreeScale = 1.2f;
    public float minRockScale = 0.7f;
    public float maxRockScale = 1.3f;

    [Header("Island Settings")]
    public int islandCount = 9;
    public float minIslandRadius = 8f;
    public float maxIslandRadius = 12f;

    private float[,] noiseMap;
    private bool[,] occupied;
    private Vector2Int playerBasePosition;
    private Vector2Int enemyBasePosition;
    private List<Vector2> islandCenters = new List<Vector2>();

    public UnityEngine.AI.NavMeshObstacle treeObstacle;
    public UnityEngine.AI.NavMeshObstacle rockObstacle;
    public UnityEngine.AI.NavMeshObstacle baseObstacle;

    void Update()
    {
        treeObstacle.carving = true;
        rockObstacle.carving = true;
        baseObstacle.carving = true;
    }

    void Awake()
    {
        offsetX = Random.Range(0f, 10000f);
        offsetY = Random.Range(0f, 10000f);
        noiseMap = GenerateNoiseMap(width, height, scale, offsetX, offsetY);
        occupied = new bool[width, height];

        GenerateIslands();
        PlaceBases();
        PlaceTree();
        PlaceRock();
    }

    float[,] GenerateNoiseMap(int width, int height, float scale, float offsetX, float offsetY)
    {
        float[,] map = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float sampleX = (x + offsetX) / scale;
                float sampleY = (y + offsetY) / scale;

                // Многооктавный шум для более естественного распределения
                float noiseValue = Mathf.PerlinNoise(sampleX, sampleY) * 0.6f;
                noiseValue += Mathf.PerlinNoise(sampleX * 2f, sampleY * 2f) * 0.3f;
                noiseValue += Mathf.PerlinNoise(sampleX * 4f, sampleY * 4f) * 0.1f;

                map[x, y] = noiseValue;
            }
        }
        return map;
    }

    void GenerateIslands()
    {
        islandCenters.Clear();

        for (int i = 0; i < islandCount; i++)
        {
            // Случайные позиции для островов с отступом от краев
            float posX = Random.Range(20f, width - 20f);
            float posY = Random.Range(20f, height - 20f);
            islandCenters.Add(new Vector2(posX, posY));
        }
    }

    bool IsInAnyIsland(Vector2 position, out float islandInfluence)
    {
        islandInfluence = 0f;

        foreach (Vector2 center in islandCenters)
        {
            float distance = Vector2.Distance(position, center);
            float maxRadius = maxIslandRadius;

            // Плавное уменьшение влияния от центра к краю острова
            float influence = Mathf.Clamp01(1f - distance / maxRadius);
            islandInfluence = Mathf.Max(islandInfluence, influence);
        }

        return islandInfluence > 0.1f; // Минимальный порог влияния
    }

    public void PlaceBases()
    {
        // Размещаем базы на свободных от островов позициях
        playerBasePosition = FindSuitableBasePosition(15f);
        enemyBasePosition = FindSuitableBasePosition(15f);

        // Убедимся, что базы достаточно далеко друг от друга
        while (Vector2.Distance(playerBasePosition, enemyBasePosition) < 50f)
        {
            enemyBasePosition = FindSuitableBasePosition(15f);
        }

        GameObject enemyBase = Instantiate(basePrefab, new Vector3(playerBasePosition.x, 0, playerBasePosition.y), Quaternion.identity);
        enemyBase.tag = "EnemyBase";
        Instantiate(basePrefab, new Vector3(enemyBasePosition.x, 0, enemyBasePosition.y), Quaternion.identity);

        ClearArea(playerBasePosition, 7);
        ClearArea(enemyBasePosition, 7);
    }

    Vector2Int FindSuitableBasePosition(float minDistanceFromIslands)
    {
        int attempts = 0;
        while (attempts < 100)
        {
            Vector2Int candidate = new Vector2Int(
                Random.Range(20, width - 20),
                Random.Range(20, height - 20)
            );

            // Проверяем, что позиция достаточно далеко от островов
            bool tooCloseToIsland = false;
            foreach (Vector2 center in islandCenters)
            {
                if (Vector2.Distance(candidate, center) < minDistanceFromIslands)
                {
                    tooCloseToIsland = true;
                    break;
                }
            }

            if (!tooCloseToIsland && !occupied[candidate.x, candidate.y])
            {
                return candidate;
            }

            attempts++;
        }

        // Если не нашли подходящую позицию, возвращаем позицию по умолчанию
        return new Vector2Int(20, 20);
    }

    public void PlaceTree()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (occupied[x, y]) continue;

                float islandInfluence;
                bool inIsland = IsInAnyIsland(new Vector2(x, y), out islandInfluence);

                if (inIsland)
                {
                    // Адаптируем порог в зависимости от положения в острове
                    float adaptiveThreshold = 0.4f + (1f - islandInfluence) * 0.3f;

                    if (noiseMap[x, y] > adaptiveThreshold &&
                        noiseMap[x, y] < 0.8f &&
                        !occupied[x, y])
                    {
                        // Проверяем, есть ли проходы вокруг
                        if (HasPathAround(x, y, 2))
                        {
                            GameObject newTree = Instantiate(tree, new Vector3(x, 0, y), Quaternion.identity);

                            float randomScale = Random.Range(minTreeScale, maxTreeScale);
                            newTree.transform.localScale *= randomScale;

                            float randomRotation = Random.Range(0f, 360f);
                            newTree.transform.rotation = Quaternion.Euler(0f, randomRotation, 0f);

                            occupied[x, y] = true;

                            // Занимаем только ближайшие клетки, оставляя проходы
                            MarkAreaOccupied(x, y, Mathf.CeilToInt(randomScale * 0.8f));
                        }
                    }
                }
            }
        }
    }

    public void PlaceRock()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (occupied[x, y]) continue;

                float islandInfluence;
                bool inIsland = IsInAnyIsland(new Vector2(x, y), out islandInfluence);

                if (inIsland)
                {
                    // Камни в другом диапазоне значений шума
                    float adaptiveThreshold = 0.3f + (1f - islandInfluence) * 0.2f;

                    if (noiseMap[x, y] > adaptiveThreshold &&
                        noiseMap[x, y] < 0.7f &&
                        !occupied[x, y])
                    {
                        // Проверяем проходимость
                        if (HasPathAround(x, y, 2))
                        {
                            GameObject newRock = Instantiate(rock, new Vector3(x, 0, y), Quaternion.identity);

                            float randomScale = Random.Range(minRockScale, maxRockScale);
                            newRock.transform.localScale *= randomScale;

                            float randomRotation = Random.Range(0f, 360f);
                            newRock.transform.rotation = Quaternion.Euler(0f, randomRotation, 0f);

                            float tilt = Random.Range(-5f, 5f);
                            newRock.transform.Rotate(tilt, 0f, tilt);

                            occupied[x, y] = true;

                            MarkAreaOccupied(x, y, Mathf.CeilToInt(randomScale * 0.6f));
                        }
                    }
                }
            }
        }
    }

    bool HasPathAround(int centerX, int centerY, int checkRadius)
    {
        int freeSpaces = 0;

        for (int x = -checkRadius; x <= checkRadius; x++)
        {
            for (int y = -checkRadius; y <= checkRadius; y++)
            {
                int checkX = centerX + x;
                int checkY = centerY + y;

                if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                {
                    if (!occupied[checkX, checkY])
                    {
                        freeSpaces++;
                    }
                }
            }
        }

        // Требуем определенное количество свободного пространства вокруг
        int requiredFreeSpaces = (checkRadius * 2 + 1) * (checkRadius * 2 + 1) / 2;
        return freeSpaces >= requiredFreeSpaces;
    }

    private void MarkAreaOccupied(int centerX, int centerY, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int checkX = centerX + x;
                int checkY = centerY + y;

                if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                {
                    // Занимаем только часть клеток для сохранения проходимости
                    float distance = Mathf.Sqrt(x * x + y * y);
                    if (distance <= radius * 0.5f || Random.Range(0f, 1f) > 0.7f)
                    {
                        occupied[checkX, checkY] = true;
                    }
                }
            }
        }
    }

    public void ClearArea(Vector2Int basePosition, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int checkX = basePosition.x + x;
                int checkY = basePosition.y + y;

                if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                {
                    noiseMap[checkX, checkY] = 0f;
                    occupied[checkX, checkY] = true;
                }
            }
        }
    }

    public Vector2Int GetPlayerBasePosition()
    {
        return playerBasePosition;
    }

    public Vector2Int GetEnemyBasePosition()
    {
        return enemyBasePosition;
    }
}