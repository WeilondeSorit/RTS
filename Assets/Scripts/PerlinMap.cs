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

    private float[,] noiseMap;
    private bool[,] occupied;
    private Vector2Int playerBasePosition;
    private Vector2Int enemyBasePosition;
    public UnityEngine.AI.NavMeshObstacle treeObstacle;
    public UnityEngine.AI.NavMeshObstacle rockObstacle;
    public UnityEngine.AI.NavMeshObstacle baseObstacle;
    void Update()
    {
 treeObstacle.carving = true; // ������������ ��������� � NavMesh
        rockObstacle.carving = true;
        baseObstacle.carving = true;
    }
    void Awake()
    {
        offsetX = Random.Range(0f, 10000f);
        offsetY = Random.Range(0f, 10000f);
        noiseMap = GenerateNoiseMap(width, height, scale, offsetX, offsetY);
        occupied = new bool[width, height];

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
                map[x, y] = Mathf.PerlinNoise(sampleX, sampleY);
            }
        }
        return map;
    }

    public void PlaceBases()
    {
        // ������������� ������� ���: ����� ������ � ������ ������� ����
        playerBasePosition = new Vector2Int(10, 10);
        enemyBasePosition = new Vector2Int(width - 20, height - 20);
        GameObject enemyBase = Instantiate(basePrefab, new Vector3(playerBasePosition.x, 0, playerBasePosition.y), Quaternion.identity);
        enemyBase.tag = "EnemyBase";
        Instantiate(basePrefab, new Vector3(enemyBasePosition.x, 0, enemyBasePosition.y), Quaternion.identity);

        ClearArea(ref noiseMap, playerBasePosition, 10);
        ClearArea(ref noiseMap, enemyBasePosition, 10);
    }

    public void PlaceTree()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (noiseMap[x, y] > 0.6f && !occupied[x, y])
                {
                    Instantiate(tree, new Vector3(x, 0, y), Quaternion.identity);
                    occupied[x, y] = true;

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
                if (noiseMap[x, y] > 0.5f && !occupied[x, y])
                {
                    Instantiate(rock, new Vector3(x, 0, y), Quaternion.identity);
                    occupied[x, y] = true;
                }
            }
        }
    }

    public void ClearArea(ref float[,] noiseMap, Vector2Int basePosition, int radius)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

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