using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    public string resourceType; // Тип ресурса (например, "Дерево")
    public int resourceAmount;  // Количество ресурса

    public int Collect(int amount)
    {
        int collected = Mathf.Min(amount, resourceAmount);
        resourceAmount -= collected;
        Debug.Log($"Собрано {collected} единиц {resourceType}");

        // Если ресурсы закончились — уничтожаем объект
        if (resourceAmount <= 0)
        {
            Destroy(gameObject);
        }

        return collected;
    }
}

