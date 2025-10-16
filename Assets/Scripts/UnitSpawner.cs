using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Vector2Int spawnPos = new Vector2Int(85, 85);
    public float spawnInterval = 5f;
    public GameObject[] units = new GameObject[2];

    void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        GameObject baseObject = GameObject.FindGameObjectWithTag("Base");
        if (baseObject == null) yield break;

        while (true)
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(5f, 10f);
                    Vector3 spawnOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
                    Vector3 spawnPosition = baseObject.transform.position + spawnOffset;

                    GameObject enemy = Instantiate(units[Random.Range(0, 2)], spawnPosition, Quaternion.identity);
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
        }
    }
}