using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinMap : MonoBehaviour
{
    [Header("Map Settings")]
    public int width = 100;
    public int height = 100;
    public float scale = 20f;
    public float offsetX, offsetY;

    [Header("Objects")]
    public GameObject tree;
    public GameObject rock;
    public GameObject basePrefab;
    public GameObject waterPrefab;  // Префаб воды с компонентом WaterAnimator

    [Header("Variation Settings")]
    public float minTreeScale = 0.8f;
    public float maxTreeScale = 1.2f;
    public float minRockScale = 0.7f;
    public float maxRockScale = 1.3f;

    [Header("Island Settings")]
    public int islandCount = 9;
    public float minIslandRadius = 8f;
    public float maxIslandRadius = 12f;

    [Header("Sea Settings")]
    [Tooltip("Ширина непроходимой морской границы по краям карты")]
    public float seaBorderWidth = 8f;
    [Tooltip("Количество проходов через море (2-4 рекомендуется)")]
    [Range(1, 4)]
    public int seaPassageCount = 3;
    [Tooltip("Ширина каждого прохода в клетках")]
    [Range(2, 8)]
    public int passageWidth = 4;
    [Tooltip("Множитель скорости анимации волн")]
    public float waveSpeedMultiplier = 1f;

    private float[,] noiseMap;
    private bool[,] occupied;
    private Vector2Int playerBasePosition;
    private Vector2Int enemyBasePosition;
    private List<Vector2> islandCenters = new List<Vector2>();
    private List<GameObject> waterTiles = new List<GameObject>(); // Для управления анимацией

    public UnityEngine.AI.NavMeshObstacle treeObstacle;
    public UnityEngine.AI.NavMeshObstacle rockObstacle;
    public UnityEngine.AI.NavMeshObstacle baseObstacle;

    void Update()
    {
        treeObstacle.carving = true;
        rockObstacle.carving = true;
        baseObstacle.carving = true;

        // Обновляем анимацию волн для всех водных плиток
        float time = Time.time;
        foreach (var water in waterTiles)
        {
            var animator = water?.GetComponent<WaterAnimator>();
            animator?.AnimateWave(time);
        }
    }

    void Awake()
    {
        offsetX = Random.Range(0f, 10000f);
        offsetY = Random.Range(0f, 10000f);
        noiseMap = GenerateNoiseMap(width, height, scale, offsetX, offsetY);
        occupied = new bool[width, height];

        GenerateSeaBorder();      // ← Сначала море с проходами
        GenerateIslands();        // ← Потом острова внутри карты
        PlaceBases();             // ← Базы в безопасных зонах
        PlaceTree();              // ← Деревья на островах
        PlaceRock();              // ← Камни на островах
    }

    // ============================================================
    // 🌊 ГЕНЕРАЦИЯ МОРЯ И ВОЛН
    // ============================================================

    void GenerateSeaBorder()
    {
        waterTiles.Clear();

        // 1. Помечаем края карты как море
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isSeaBorder = x < seaBorderWidth || x >= width - seaBorderWidth ||
                                  y < seaBorderWidth || y >= height - seaBorderWidth;

                if (isSeaBorder)
                {
                    occupied[x, y] = true; // Непроходимо
                    CreateWaterTile(x, y);
                }
            }
        }

    }

    void CreateWaterTile(int x, int y)
    {
        if (waterPrefab != null)
        {
            Quaternion waterRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject water = Instantiate(waterPrefab, new Vector3(x, 0.1f, y), waterRotation);
            water.name = $"Water_{x}_{y}";

            if (water.GetComponent<WaterAnimator>() == null)
            {
                water.AddComponent<WaterAnimator>();
            }

            var animator = water.GetComponent<WaterAnimator>();
            animator.phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            animator.frequency = Random.Range(0.8f, 1.2f);
            animator.amplitude = Random.Range(0.03f, 0.06f);
            animator.speedMultiplier = waveSpeedMultiplier;

            waterTiles.Add(water);
        }
        else
        {
            // Заглушка: создаём правильный Quad программно
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Quad);
            water.name = $"WaterPlaceholder_{x}_{y}";

            // 🔧 Правильная позиция и ориентация
            water.transform.position = new Vector3(x, 0f, y);
            water.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // ← Ключевое исправление!
            water.transform.localScale = new Vector3(1f, 1f, 1f);

            // Настройка материала для прозрачности
            var renderer = water.GetComponent<Renderer>();
            renderer.material.color = new Color(0.1f, 0.35f, 0.85f, 0.8f);
            renderer.material.SetFloat("_Mode", 3);
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.renderQueue = 3000;

            if (water.GetComponent<WaterAnimator>() == null)
            {
                water.AddComponent<WaterAnimator>();
            }
            var animator = water.GetComponent<WaterAnimator>();
            animator.phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            animator.frequency = Random.Range(0.8f, 1.2f);
            animator.amplitude = Random.Range(0.03f, 0.06f);
            animator.speedMultiplier = waveSpeedMultiplier;

            waterTiles.Add(water);
        }
    }



    void RemoveWaterAt(int x, int y)
    {
        for (int i = waterTiles.Count - 1; i >= 0; i--)
        {
            Vector3 pos = waterTiles[i].transform.position;
            if (Mathf.Approximately(pos.x, x) && Mathf.Approximately(pos.z, y))
            {
                Destroy(waterTiles[i]);
                waterTiles.RemoveAt(i);
                break;
            }
        }
    }

    void UpdateWaveAnimation()
    {
        float time = Time.time;

        foreach (GameObject water in waterTiles)
        {
            var animator = water.GetComponent<WaterAnimator>();
            if (animator != null && water != null)
            {
                // Сине-волновая анимация по высоте
                float waveOffset = Mathf.Sin(time * 2f * animator.frequency * animator.speedMultiplier + animator.phaseOffset)
                                 * animator.amplitude;

                Vector3 pos = water.transform.position;
                water.transform.position = new Vector3(pos.x, -0.3f + waveOffset, pos.z);

                // Лёгкое покачивание по масштабу для эффекта "набухания"
                float scaleWave = 1f + Mathf.Sin(time * 1.5f * animator.frequency + animator.phaseOffset) * 0.02f;
                Vector3 originalScale = water.transform.localScale;
                water.transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.5f + waveOffset * 2f, originalScale.z);
            }
        }
    }

    // ============================================================
    // 🗺️ ГЕНЕРАЦИЯ ШУМА И ОСТРОВОВ
    // ============================================================

    float[,] GenerateNoiseMap(int width, int height, float scale, float offsetX, float offsetY)
    {
        float[,] map = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float sampleX = (x + offsetX) / scale;
                float sampleY = (y + offsetY) / scale;

                // Многооктавный шум для естественности
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
            // Острова только внутри "сухой" зоны карты
            float posX = Random.Range(20f + seaBorderWidth, width - 20f - seaBorderWidth);
            float posY = Random.Range(20f + seaBorderWidth, height - 20f - seaBorderWidth);
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
            float influence = Mathf.Clamp01(1f - distance / maxRadius);
            islandInfluence = Mathf.Max(islandInfluence, influence);
        }

        return islandInfluence > 0.1f;
    }

    // ============================================================
    // 🏠 РАЗМЕЩЕНИЕ БАЗ
    // ============================================================

    public void PlaceBases()
    {
        playerBasePosition = FindSuitableBasePosition(15f);
        enemyBasePosition = FindSuitableBasePosition(15f);

        while (Vector2.Distance(playerBasePosition, enemyBasePosition) < 50f)
        {
            enemyBasePosition = FindSuitableBasePosition(15f);
        }

        GameObject playerBase = Instantiate(basePrefab, new Vector3(playerBasePosition.x, 0, playerBasePosition.y), Quaternion.identity);
        playerBase.tag = "PlayerBase";

        GameObject enemyBase = Instantiate(basePrefab, new Vector3(enemyBasePosition.x, 0, enemyBasePosition.y), Quaternion.identity);
        enemyBase.tag = "EnemyBase";

        ClearArea(playerBasePosition, 7);
        ClearArea(enemyBasePosition, 7);
    }

    Vector2Int FindSuitableBasePosition(float minDistanceFromIslands)
    {
        int attempts = 0;
        int safeMargin = Mathf.CeilToInt(seaBorderWidth) + 5;

        while (attempts < 150)
        {
            Vector2Int candidate = new Vector2Int(
                Random.Range(safeMargin, width - safeMargin),
                Random.Range(safeMargin, height - safeMargin)
            );

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

        return new Vector2Int(safeMargin, safeMargin);
    }

    // ============================================================
    // 🌳 РАЗМЕЩЕНИЕ ОБЪЕКТОВ
    // ============================================================

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
                    float adaptiveThreshold = 0.4f + (1f - islandInfluence) * 0.3f;

                    if (noiseMap[x, y] > adaptiveThreshold &&
                        noiseMap[x, y] < 0.8f &&
                        !occupied[x, y] &&
                        HasPathAround(x, y, 2))
                    {
                        GameObject newTree = Instantiate(tree, new Vector3(x, 0, y), Quaternion.identity);
                        float randomScale = Random.Range(minTreeScale, maxTreeScale);
                        newTree.transform.localScale *= randomScale;
                        newTree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                        occupied[x, y] = true;
                        MarkAreaOccupied(x, y, Mathf.CeilToInt(randomScale * 0.8f));
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
                    float adaptiveThreshold = 0.3f + (1f - islandInfluence) * 0.2f;

                    if (noiseMap[x, y] > adaptiveThreshold &&
                        noiseMap[x, y] < 0.7f &&
                        !occupied[x, y] &&
                        HasPathAround(x, y, 2))
                    {
                        GameObject newRock = Instantiate(rock, new Vector3(x, 0, y), Quaternion.identity);
                        float randomScale = Random.Range(minRockScale, maxRockScale);
                        newRock.transform.localScale *= randomScale;
                        newRock.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        newRock.transform.Rotate(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));

                        occupied[x, y] = true;
                        MarkAreaOccupied(x, y, Mathf.CeilToInt(randomScale * 0.6f));
                    }
                }
            }
        }
    }

    // ============================================================
    // 🔍 ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

    bool HasPathAround(int centerX, int centerY, int checkRadius)
    {
        int freeSpaces = 0;
        int totalSpaces = 0;

        for (int x = -checkRadius; x <= checkRadius; x++)
        {
            for (int y = -checkRadius; y <= checkRadius; y++)
            {
                int checkX = centerX + x;
                int checkY = centerY + y;

                if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                {
                    totalSpaces++;
                    if (!occupied[checkX, checkY]) freeSpaces++;
                }
            }
        }

        return freeSpaces >= totalSpaces / 2;
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
                    RemoveWaterAt(checkX, checkY);
                }
            }
        }
    }

    public Vector2Int GetPlayerBasePosition() => playerBasePosition;
    public Vector2Int GetEnemyBasePosition() => enemyBasePosition;
}