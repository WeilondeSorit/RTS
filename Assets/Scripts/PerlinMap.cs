using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PerlinMap : MonoBehaviour
{
    // ---------- НОВЫЙ БЛОК: ВРЕМЕНА ГОДА ----------
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    [System.Serializable]
    public class SeasonData
    {
        public GameObject treePrefab;
        public GameObject rockPrefab;
        public Material groundMaterial;
    }

    [Header("Season Settings")]
    public bool randomSeason = true;
    public Season selectedSeason;
    public SeasonData[] seasonData = new SeasonData[4];

    private Season currentSeason;
    private GameObject currentTreePrefab;
    private GameObject currentRockPrefab;

    [Header("Map Settings")]
    public int width = 100;
    public int height = 100;
    public float scale = 20f;
    public float offsetX, offsetY;

    [Header("Base Placement")]
    public int baseEdgeMargin = 10;      // Минимальное расстояние от края карты для баз

    public MeshRenderer groundRenderer;
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

    [Header("Placement Density")]
    [Range(0f, 1f)]
    public float treeDensity = 0.6f;
    [Range(0f, 1f)]
    public float rockDensity = 0.6f;

    [Header("Spacing Settings")]
    public float treeSpacingMultiplier = 1.5f;
    public float rockSpacingMultiplier = 1.2f;

    public NavMeshObstacle treeObstacle;
    public NavMeshObstacle rockObstacle;
    public NavMeshObstacle baseObstacle;

    private float[,] noiseMap;
    private bool[,] occupied;
    private Vector2Int playerBasePosition;
    private Vector2Int enemyBasePosition;
    private readonly List<Vector2> islandCenters = new();

    void Awake()
    {
        ApplyRandomSeason();

        offsetX = Random.Range(0f, 10000f);
        offsetY = Random.Range(0f, 10000f);
        noiseMap = GenerateNoiseMap(width, height, scale, offsetX, offsetY);
        occupied = new bool[width, height];

        GenerateIslands();
        PlaceBases();
        PlaceTree();
        PlaceRock();
    }

    void Update()
    {
        if (treeObstacle != null) treeObstacle.carving = true;
        if (rockObstacle != null) rockObstacle.carving = true;
        if (baseObstacle != null) baseObstacle.carving = true;
    }

    private void ApplyRandomSeason()
    {
        if (randomSeason)
        {
            int seasonIndex = Random.Range(0, 4);
            currentSeason = (Season)seasonIndex;
        }
        else
        {
            currentSeason = selectedSeason;
        }

        int idx = (int)currentSeason;
        if (seasonData == null || idx >= seasonData.Length || seasonData[idx] == null)
        {
            Debug.LogError($"Нет данных для сезона {currentSeason}! Проверьте настройки SeasonData в инспекторе.");
            return;
        }

        SeasonData data = seasonData[idx];
        currentTreePrefab = data.treePrefab;
        currentRockPrefab = data.rockPrefab;

        if (groundRenderer == null)
            groundRenderer = GetComponent<MeshRenderer>();

        if (groundRenderer != null && data.groundMaterial != null)
            groundRenderer.material = data.groundMaterial;
        else
            Debug.LogWarning("Не удалось назначить материал земли: отсутствует MeshRenderer или материал в данных сезона.");
    }

    float[,] GenerateNoiseMap(int w, int h, float s, float ox, float oy)
    {
        float[,] map = new float[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                float sx = (x + ox) / s, sy = (y + oy) / s;
                map[x, y] = Mathf.PerlinNoise(sx, sy) * 0.6f +
                           Mathf.PerlinNoise(sx * 2f, sy * 2f) * 0.3f +
                           Mathf.PerlinNoise(sx * 4f, sy * 4f) * 0.1f;
            }
        return map;
    }

    void GenerateIslands()
    {
        islandCenters.Clear();
        // Острова тоже не будут генерироваться слишком близко к краю (отступ 20 уже есть)
        for (int i = 0; i < islandCount; i++)
        {
            float px = Random.Range(20f, width - 20f);
            float py = Random.Range(20f, height - 20f);
            islandCenters.Add(new Vector2(px, py));
        }
    }

    bool IsInAnyIsland(Vector2 pos, out float influence)
    {
        influence = 0f;
        foreach (var center in islandCenters)
        {
            float dist = Vector2.Distance(pos, center);
            influence = Mathf.Max(influence, Mathf.Clamp01(1f - dist / maxIslandRadius));
        }
        return influence > 0.1f;
    }

    public void PlaceBases()
    {
        playerBasePosition = FindSuitableBasePosition(15f);
        enemyBasePosition = FindSuitableBasePosition(15f);
        while (Vector2.Distance(playerBasePosition, enemyBasePosition) < 70f)
            enemyBasePosition = FindSuitableBasePosition(15f);

        var pBase = Instantiate(basePrefab, new Vector3(playerBasePosition.x, 0, playerBasePosition.y), Quaternion.identity);
        pBase.tag = "Base";
        var eBase = Instantiate(basePrefab, new Vector3(enemyBasePosition.x, 0, enemyBasePosition.y), Quaternion.identity);
        eBase.tag = "EnemyBase";

        ClearArea(playerBasePosition, 7);
        ClearArea(enemyBasePosition, 7);
    }

    Vector2Int FindSuitableBasePosition(float minDist)
    {
        int attempts = 0;
        // Увеличил максимальное количество попыток, так как отступ увеличен
        while (attempts < 200)
        {
            var cand = new Vector2Int(
                Random.Range(baseEdgeMargin, width - baseEdgeMargin),
                Random.Range(baseEdgeMargin, height - baseEdgeMargin)
            );
            bool tooClose = false;
            foreach (var c in islandCenters)
                if (Vector2.Distance(cand, c) < minDist) { tooClose = true; break; }

            if (!tooClose && !occupied[cand.x, cand.y]) return cand;
            attempts++;
        }
        // fallback – центр карты (если ничего не нашли)
        return new Vector2Int(width / 2, height / 2);
    }

    public void PlaceTree()
    {
        if (currentTreePrefab == null) return;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (occupied[x, y]) continue;
                if (IsInAnyIsland(new Vector2(x, y), out float inf) &&
                    noiseMap[x, y] > 0.4f + (1f - inf) * 0.3f &&
                    noiseMap[x, y] < 0.8f &&
                    HasPathAround(x, y, 2) &&
                    Random.value <= treeDensity)
                {
                    var treeObj = Instantiate(currentTreePrefab, new Vector3(x, 0, y), Quaternion.identity);
                    float s = Random.Range(minTreeScale, maxTreeScale);
                    treeObj.transform.localScale *= s;
                    treeObj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    var obs = treeObj.AddComponent<NavMeshObstacle>();
                    obs.carving = true;
                    treeObj.tag = "Tree";

                    occupied[x, y] = true;
                    MarkAreaOccupied(x, y, Mathf.CeilToInt(s * treeSpacingMultiplier), true);
                }
            }
        }
    }

    public void PlaceRock()
    {
        if (currentRockPrefab == null) return;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (occupied[x, y]) continue;
                if (IsInAnyIsland(new Vector2(x, y), out float inf) &&
                    noiseMap[x, y] > 0.3f + (1f - inf) * 0.2f &&
                    noiseMap[x, y] < 0.7f &&
                    HasPathAround(x, y, 2) &&
                    Random.value <= rockDensity)
                {
                    var rockObj = Instantiate(currentRockPrefab, new Vector3(x, 0, y), Quaternion.identity);
                    float s = Random.Range(minRockScale, maxRockScale);
                    rockObj.transform.localScale *= s;
                    rockObj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    rockObj.transform.Rotate(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));

                    var obs = rockObj.AddComponent<NavMeshObstacle>();
                    obs.carving = true;
                    rockObj.tag = "Rock";

                    occupied[x, y] = true;
                    MarkAreaOccupied(x, y, Mathf.CeilToInt(s * rockSpacingMultiplier), true);
                }
            }
        }
    }

    bool HasPathAround(int cx, int cy, int r)
    {
        int free = 0, total = 0;
        for (int x = -r; x <= r; x++)
            for (int y = -r; y <= r; y++)
            {
                int nx = cx + x, ny = cy + y;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    total++;
                    if (!occupied[nx, ny]) free++;
                }
            }
        return free >= total / 2;
    }

    void MarkAreaOccupied(int cx, int cy, int radius, bool hard)
    {
        for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
            {
                int nx = cx + x, ny = cy + y;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    float d = Mathf.Sqrt(x * x + y * y);
                    if (hard ? d <= radius : (d <= radius * 0.5f || Random.value > 0.7f))
                        occupied[nx, ny] = true;
                }
            }
    }

    public void ClearArea(Vector2Int pos, int radius)
    {
        for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
            {
                int cx = pos.x + x, cy = pos.y + y;
                if (cx >= 0 && cx < width && cy >= 0 && cy < height)
                {
                    noiseMap[cx, cy] = 0f;
                    occupied[cx, cy] = true;
                }
            }
    }

    public Vector2Int GetPlayerBasePosition() => playerBasePosition;
    public Vector2Int GetEnemyBasePosition() => enemyBasePosition;
}