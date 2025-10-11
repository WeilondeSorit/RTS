using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;  // ������ �����
    public Vector2Int spawnPos = new Vector2Int(85, 85); // ���������� ������
    public float spawnInterval = 5f; // �������� ������ (�������)
    public GameObject[] units = new GameObject[2];
    void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {

        while (true)
        {
             for(int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    Vector3 spawnPosition = new Vector3(spawnPos.x - i, 0, spawnPos.y-j); // ����������� � Vector3
                    GameObject enemy = Instantiate(units[Random.Range(0, 2)], spawnPosition, Quaternion.identity);
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
        }
    }
}
