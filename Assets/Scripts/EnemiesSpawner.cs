using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemiesSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;  // ������ �����
    public Vector2Int spawnPos = new Vector2Int(15, 15); // ���������� ������
    public float spawnInterval = 5f; // �������� ������ (�������)

    void Start()
    {
        StartCoroutine(SpawnEnemies());
    }
    public Vector3 FindClosest(string tag) //closenest tree
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        GameObject closest = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (GameObject obj in objects)
        {
            float distance = Vector3.Distance(obj.transform.position, currentPos);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = obj;
            }
        }

        return closest != null ? closest.transform.position : currentPos;
    }
    IEnumerator SpawnEnemies()
    {
        while (true)
        {
            Vector3 spawnPosition = new Vector3(spawnPos.x, 0, spawnPos.y); // ����������� � Vector3
                Vector3 basePosition = FindClosest("Base");

            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            agent.stoppingDistance = 1f;
            if (agent != null)
            {

                agent.SetDestination(basePosition);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
