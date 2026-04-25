using UnityEngine;

public class BuildingIdentifier : MonoBehaviour
{
    [Header("Building Data")]
    public string id;
    public GameObject prefab;
    public int currentHealth = 100;
    public int maxHealth = 100;
    public int level = 1;
    
    void Start()
    {
        // Генерируем ID если его нет
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
        }
    }
}