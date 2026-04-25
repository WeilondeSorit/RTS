using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FogOfWarManager : MonoBehaviour
{
    [Header("Размеры карты")]
    public float mapWidth = 100f;
    public float mapHeight = 100f;
    public float originX = 0f;
    public float originZ = 0f;

    [Header("Размер ячейки")]
    public float cellSize = 5f;

    [Header("Радиус обзора (в единицах)")]
    public float visionRadius = 20f;

    [Header("Префаб тумана")]
    public GameObject fogPlanePrefab;

    [Header("Теги юнитов и базы")]
    public string[] unitTags = { "Villager", "Archer", "Knight", "PlayerUnit" };
    public string baseTag = "Base";

    [Header("Обновление")]
    public float updateInterval = 0.5f;
    public bool debugLog = true;

    private GameObject[,] fogCells;
    private int cellsX, cellsZ;

    void Start()
    {
        GenerateFogGrid();
        if (debugLog) Debug.Log($"Туман создан {cellsX}x{cellsZ}, область X:{originX}..{originX + mapWidth}, Z:{originZ}..{originZ + mapHeight}");
        StartCoroutine(UpdateFogCoroutine());
    }

    void GenerateFogGrid()
    {
        cellsX = Mathf.CeilToInt(mapWidth / cellSize);
        cellsZ = Mathf.CeilToInt(mapHeight / cellSize);
        fogCells = new GameObject[cellsX, cellsZ];

        for (int i = 0; i < cellsX; i++)
        {
            for (int j = 0; j < cellsZ; j++)
            {
                float centerX = originX + (i + 0.5f) * cellSize;
                float centerZ = originZ + (j + 0.5f) * cellSize;
                Vector3 pos = new Vector3(centerX, 10f, centerZ);
                GameObject tile = Instantiate(fogPlanePrefab, pos, Quaternion.identity);
                tile.transform.localScale = new Vector3(cellSize / 10f, 1f, cellSize / 10f);
                fogCells[i, j] = tile;
            }
        }
    }

    void RevealCell(int x, int z)
    {
        if (x < 0 || x >= cellsX || z < 0 || z >= cellsZ) return;
        if (fogCells[x, z] != null)
        {
            Destroy(fogCells[x, z]);
            fogCells[x, z] = null;
            if (debugLog) Debug.Log($"Открыта ячейка [{x},{z}]");
        }
    }

    // Основной метод: открывает ячейки, в которые попадает точка worldPos, и все ячейки в радиусе
    void RevealArea(Vector3 worldPos, float radius)
    {
        // Определяем индексы ячейки, в которой находится точка
        int cellX = Mathf.FloorToInt((worldPos.x - originX) / cellSize);
        int cellZ = Mathf.FloorToInt((worldPos.z - originZ) / cellSize);

        // Открываем центральную ячейку даже если radius = 0
        RevealCell(cellX, cellZ);

        // Радиус в ячейках
        int radiusCells = Mathf.CeilToInt(radius / cellSize);

        for (int dx = -radiusCells; dx <= radiusCells; dx++)
        {
            for (int dz = -radiusCells; dz <= radiusCells; dz++)
            {
                int nx = cellX + dx;
                int nz = cellZ + dz;
                if (nx < 0 || nx >= cellsX || nz < 0 || nz >= cellsZ) continue;

                // Проверяем расстояние от worldPos до центра ячейки (мировое, не индексное)
                Vector3 cellCenter = new Vector3(originX + (nx + 0.5f) * cellSize, worldPos.y, originZ + (nz + 0.5f) * cellSize);
                if (Vector3.Distance(worldPos, cellCenter) <= radius)
                {
                    RevealCell(nx, nz);
                }
            }
        }
    }

    System.Collections.IEnumerator UpdateFogCoroutine()
    {
        while (true)
        {
            List<Transform> revealers = new List<Transform>();

            // Ищем всех юнитов по тегам
            foreach (string tag in unitTags)
            {
                GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
                foreach (var obj in objs)
                {
                    if (obj != null) revealers.Add(obj.transform);
                }
            }

            // Ищем базу
            GameObject baseObj = GameObject.FindGameObjectWithTag(baseTag);
            if (baseObj != null) revealers.Add(baseObj.transform);

            if (debugLog && revealers.Count == 0)
                Debug.LogWarning("Не найдено ни одного объекта с заданными тегами юнитов или базы!");

            foreach (Transform t in revealers)
            {
                if (t != null)
                {
                    RevealArea(t.position, visionRadius);
                    if (debugLog)
                        Debug.Log($"Открываю туман вокруг {t.name} на позиции {t.position}");
                }
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }

    // Опционально: рисуем сетку в Scene View для отладки
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        // Рисуем границы всей зоны тумана
        Vector3 bottomLeft = new Vector3(originX, 0, originZ);
        Vector3 topRight = new Vector3(originX + mapWidth, 0, originZ + mapHeight);
        Gizmos.DrawWireCube((bottomLeft + topRight) / 2, new Vector3(mapWidth, 0.1f, mapHeight));

        // Рисуем сетку ячеек
        if (!Application.isPlaying) return;
        if (fogCells == null) return;
        Gizmos.color = Color.green;
        for (int i = 0; i <= cellsX; i++)
        {
            float x = originX + i * cellSize;
            Gizmos.DrawLine(new Vector3(x, 0, originZ), new Vector3(x, 0, originZ + mapHeight));
        }
        for (int j = 0; j <= cellsZ; j++)
        {
            float z = originZ + j * cellSize;
            Gizmos.DrawLine(new Vector3(originX, 0, z), new Vector3(originX + mapWidth, 0, z));
        }
    }
}