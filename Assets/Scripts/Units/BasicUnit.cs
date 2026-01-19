using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class BasicUnit : Health
{
    public string unitName;
    protected NavMeshAgent unitAgent;
    protected PlayerData playerData;

    protected virtual void Awake()
    {
        InitializeNavMeshAgent();
    }

    private void InitializeNavMeshAgent()
    {
        unitAgent = GetComponent<NavMeshAgent>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerData = playerObject.GetComponent<PlayerData>();
            if (playerData == null)
                Debug.LogError("PlayerData component not found on the object with tag 'Player'");
        }
        else
        {
            Debug.LogError("No GameObject found with tag 'Player'");
        }

        if (unitAgent == null)
            Debug.LogError($"Ошибка: NavMeshAgent отсутствует на {gameObject.name}!");
    }

    public Vector3 FindClosest(string tag)
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

        return closest != null ? closest.transform.position : Vector3.zero;
    }

    public Vector3 FindClosestAndDestroy(string tag)
    {
        Vector3 targetPos = FindClosest(tag);
        GameObject closest = GetClosestObject(tag);

        if (closest != null)
            Destroy(closest);

        return targetPos;
    }

    private GameObject GetClosestObject(string tag)
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
        return closest;
    }

    public void GCnavMeshAgent()
    {
        unitAgent = GetComponent<NavMeshAgent>();
        if (unitAgent != null && !unitAgent.isOnNavMesh)
            unitAgent.Warp(transform.position);

        StartCoroutine(MoveToTarget());
    }

    private IEnumerator MoveToTarget()
    {
        // Логика выбора цели
        string tag = "Enemy";
        Vector3 target = Vector3.zero;

        if (CompareTag("Villager"))
        {
            tag = Random.Range(0, 2) == 1 ? "Tree" : "Rock";
            target = FindClosestAndDestroy(tag);
            Debug.Log($"Villager selected resource: {tag}");
        }
        else
        {
            target = FindClosest("Enemy");
            if (target == Vector3.zero) target = FindClosest("EnemyBase");
        }

        if (target == Vector3.zero)
        {
            Debug.LogError("No valid targets found!");
            yield break;
        }

        // Настройка движения
        float stopDistance = CompareTag("Archer") ? 3f : 1f;
        unitAgent.stoppingDistance = stopDistance;
        unitAgent.SetDestination(target);

        yield return new WaitUntil(() =>
            !unitAgent.pathPending &&
            unitAgent.remainingDistance <= unitAgent.stoppingDistance);

        Debug.Log($"{gameObject.name} достиг цели");

        // Логика только для villagers
        if (CompareTag("Villager"))
        {
            yield return new WaitForSeconds(5f);
            playerData.wood += 5;
            playerData.rock += 5;

            // Возвращение на базу
            Vector3 home = FindClosest("Base");
            if (home == Vector3.zero) home = transform.position;
            unitAgent.SetDestination(home);
        }
    }
}