using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

public class PerlinMap : MonoBehaviour
{
    public enum Season { Spring, Summer, Autumn, Winter }

    [System.Serializable]
    public class SeasonData
    {
        public GameObject treePrefab;
        public GameObject rockPrefab;
        public Material groundMaterial;
    }

    private struct PlacementCandidate
    {
        public int x, y;
        public bool preferTree;
        public PlacementCandidate(int x, int y, bool preferTree)
        {
            this.x = x;
            this.y = y;
            this.preferTree = preferTree;
        }
    }

    [Header("Season")]
    public bool randomSeason = true;
    public Season selectedSeason;
    public SeasonData[] seasonData = new SeasonData[4];

    [Header("Map Size")]
    public int width = 100;
    public int height = 100;
    public float scale = 20f;
    public MeshRenderer groundRenderer;

    [Header("Object Balance")]
    [Range(0f, 0.3f)] public float targetDensity = 0.08f;
    [Range(0f, 1f)] public float treeRockRatio = 0.5f;
    public float noiseThreshold = 0.5f;

    [Header("Optimization")]
    [Range(1f, 10f)] public float minSpacing = 3f;
    public bool useCircularSpacing = true;

    [Header("Bases")]
    public GameObject basePrefab;
    public int baseClearRadius = 7;
    public float minDistanceBetweenBases = 70f;

    [Header("NavMesh")]
    public bool rebuildNavMesh = true;

    private NavMeshSurface navMeshSurface;
    private float[,] noiseMap;
    private bool[,] occupied;
    private Transform environmentParent;
    private int spacingCells;
    private float minSpacingSqr;

    void Start()
    {
        Season currentSeason = randomSeason ? (Season)Random.Range(0, 4) : selectedSeason;
        SeasonData data = seasonData[(int)currentSeason];

        if (data.treePrefab == null || data.rockPrefab == null)
        {
            Debug.LogError("Не назначены префабы для сезона " + currentSeason);
            return;
        }

        if (groundRenderer != null && data.groundMaterial != null)
            groundRenderer.material = data.groundMaterial;

        environmentParent = new GameObject("Environment").transform;

        spacingCells = Mathf.Max(1, Mathf.CeilToInt(minSpacing));
        minSpacingSqr = minSpacing * minSpacing;

        GenerateNoiseMap();
        occupied = new bool[width, height];

        Vector2Int playerPos = FindBasePosition();
        Vector2Int enemyPos = FindBasePosition();
        while (Vector2.Distance(playerPos, enemyPos) < minDistanceBetweenBases)
            enemyPos = FindBasePosition();

        PlaceBase(playerPos, "Base");
        PlaceBase(enemyPos, "EnemyBase");
        ClearArea(playerPos, baseClearRadius);
        ClearArea(enemyPos, baseClearRadius);

        PlaceBalancedObjects(data.treePrefab, data.rockPrefab);

        if (rebuildNavMesh)
        {
            navMeshSurface = FindAnyObjectByType<NavMeshSurface>();
            if (navMeshSurface != null)
                navMeshSurface.BuildNavMesh();
        }

        Debug.Log($"Готово! Деревья: {CountByTag("Tree")}, Камни: {CountByTag("Rock")}");
    }

    void GenerateNoiseMap()
    {
        float offsetX = Random.Range(0f, 10000f);
        float offsetY = Random.Range(0f, 10000f);
        noiseMap = new float[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float sx = (x + offsetX) / scale;
                float sy = (y + offsetY) / scale;
                noiseMap[x, y] = Mathf.PerlinNoise(sx, sy);
            }
    }

    void PlaceBalancedObjects(GameObject treePrefab, GameObject rockPrefab)
    {
        int totalCells = width * height;

        // Исходное количество объектов по старым настройкам
        int baseTotal = Mathf.RoundToInt(totalCells * targetDensity);
        int baseTrees = Mathf.RoundToInt(baseTotal * treeRockRatio);
        int baseRocks = baseTotal - baseTrees;

        // Уменьшаем на 1/3 (оставляем 2/3)
        int targetTrees = Mathf.Max(0, Mathf.RoundToInt(baseTrees * 2f / 3f));
        int targetRocks = Mathf.Max(0, Mathf.RoundToInt(baseRocks * 2f / 3f));
        int targetTotal = targetTrees + targetRocks;

        // Если ничего размещать не нужно – выходим
        if (targetTotal <= 0)
        {
            Debug.Log("Целевое количество объектов равно нулю, пропускаем размещение.");
            return;
        }

        int placedTrees = 0, placedRocks = 0;

        // Собираем только столько кандидатов, сколько можем обработать (оптимизация памяти)
        // Предварительное выделение списка с запасом
        var candidates = new List<PlacementCandidate>(targetTotal * 3);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (occupied[x, y]) continue;
                bool preferTree = noiseMap[x, y] > noiseThreshold;
                candidates.Add(new PlacementCandidate(x, y, preferTree));
            }
        }

        // Перемешиваем кандидатов (Fisher–Yates на списке)
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = temp;
        }

        foreach (var pos in candidates)
        {
            if (placedTrees >= targetTrees && placedRocks >= targetRocks)
                break;

            if (occupied[pos.x, pos.y] || IsTooClose(pos.x, pos.y))
                continue;

            GameObject prefab = null;
            string tag = "";

            if (pos.preferTree)
            {
                if (placedTrees < targetTrees && treePrefab != null)
                { prefab = treePrefab; tag = "Tree"; }
                else if (placedRocks < targetRocks && rockPrefab != null)
                { prefab = rockPrefab; tag = "Rock"; }
            }
            else
            {
                if (placedRocks < targetRocks && rockPrefab != null)
                { prefab = rockPrefab; tag = "Rock"; }
                else if (placedTrees < targetTrees && treePrefab != null)
                { prefab = treePrefab; tag = "Tree"; }
            }

            if (prefab == null) continue;

            var obj = Instantiate(prefab, new Vector3(pos.x, 0, pos.y),
                Quaternion.Euler(0, Random.Range(0, 360), 0), environmentParent);
            obj.tag = tag;

            // Препятствия: добавляем коллайдер и NavMeshObstacle
            if (obj.GetComponent<Collider>() == null)
                obj.AddComponent<BoxCollider>();

            NavMeshObstacle obstacle = obj.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;

            if (obj.TryGetComponent<Rigidbody>(out var rb))
                rb.isKinematic = true;

            occupied[pos.x, pos.y] = true;

            if (tag == "Tree") placedTrees++;
            else placedRocks++;
        }
    }

    bool IsTooClose(int x, int y)
    {
        int minX = Mathf.Max(0, x - spacingCells);
        int maxX = Mathf.Min(width - 1, x + spacingCells);
        int minY = Mathf.Max(0, y - spacingCells);
        int maxY = Mathf.Min(height - 1, y + spacingCells);

        for (int cx = minX; cx <= maxX; cx++)
        {
            for (int cy = minY; cy <= maxY; cy++)
            {
                if (!occupied[cx, cy]) continue;
                if (useCircularSpacing)
                {
                    float dx = cx - x;
                    float dy = cy - y;
                    if (dx * dx + dy * dy < minSpacingSqr) return true;
                }
                else
                {
                    return true;
                }
            }
        }
        return false;
    }

    Vector2Int FindBasePosition()
    {
        int margin = 10;
        for (int i = 0; i < 200; i++)
        {
            int x = Random.Range(margin, width - margin);
            int y = Random.Range(margin, height - margin);
            if (!occupied[x, y]) return new Vector2Int(x, y);
        }
        return new Vector2Int(width / 2, height / 2);
    }

    void PlaceBase(Vector2Int pos, string tag)
    {
        var baseObj = Instantiate(basePrefab, new Vector3(pos.x, 0, pos.y), Quaternion.identity);
        baseObj.tag = tag;
        baseObj.name = "Главная крепость";
        occupied[pos.x, pos.y] = true;
    }

    void ClearArea(Vector2Int center, int radius)
    {
        int minX = Mathf.Max(0, center.x - radius);
        int maxX = Mathf.Min(width - 1, center.x + radius);
        int minY = Mathf.Max(0, center.y - radius);
        int maxY = Mathf.Min(height - 1, center.y + radius);

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                occupied[x, y] = true;
    }

    int CountByTag(string tag)
    {
        int count = 0;
        foreach (Transform child in environmentParent)
            if (child.CompareTag(tag)) count++;
        return count;
    }
}