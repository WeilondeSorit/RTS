using UnityEngine;

public class BuildingController : MonoBehaviour
{
    [System.Serializable]
    public struct BuildingData
    {
        public string name;
        public GameObject prefab;
        public int woodCost;
        public int rockCost;
    }

    public BuildingData[] buildings;   // массив с данными о каждом здании
    public Camera sceneCamera;
    public AudioClip audioBuild;
    public AudioSource audioSource;
    public PlayerData playerData;

    private GameObject currentBuilding;
    private int currentBuildingIndex = -1;
    private bool isPlacing = false;

    void Update()
    {
        if (isPlacing && currentBuilding != null)
        {
            MoveBuildingToMouse();

            if (Input.GetMouseButtonDown(0) && CanPlaceBuilding())
            {
                PlaceBuilding();
                audioSource.PlayOneShot(audioBuild);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
            }
        }
    }

    /// <summary>Вызывается, когда игрок выбирает здание по индексу в массиве buildings</summary>
    public void StartPlacingBuilding(int index)
    {
        if (playerData == null)
        {
            playerData = PlayerData.Instance;
            if (playerData == null)
            {
                Debug.LogError("PlayerData.Instance не найден на сцене!");
                return;
            }
        }
        if (index < 0 || index >= buildings.Length)
        {
            Debug.LogError($"Индекс здания {index} вне допустимого диапазона!");
            return;
        }

        // Если уже было активное размещение – отменяем
        if (isPlacing)
        {
            Destroy(currentBuilding);
            isPlacing = false;
        }

        BuildingData data = buildings[index];

        // Проверка и списание ресурсов
        if (playerData.wood >= data.woodCost && playerData.rock >= data.rockCost)
        {
            playerData.SpendResources(data.woodCost, data.rockCost);

            currentBuildingIndex = index;
            currentBuilding = Instantiate(data.prefab);

            // Отключаем физику на время размещения (опционально)
            Rigidbody rb = currentBuilding.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            isPlacing = true;
        }
        else
        {
            Debug.Log($"Недостаточно ресурсов! Требуется: дерево {data.woodCost}, камень {data.rockCost}");
        }
    }

    void MoveBuildingToMouse()
    {
        Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 pos = hit.point;
            pos.y = 0f; // или можно оставить pos.y = hit.point.y, если нужна высота рельефа
            currentBuilding.transform.position = pos;
        }
    }

    bool CanPlaceBuilding()
    {
        if (currentBuilding == null) return false;

        Collider buildingCollider = currentBuilding.GetComponent<Collider>();
        if (buildingCollider == null) return true; // если коллайдера нет – разрешаем

        Vector3 center = buildingCollider.bounds.center;
        Vector3 halfExtents = buildingCollider.bounds.extents;

        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, currentBuilding.transform.rotation);

        foreach (Collider col in hitColliders)
        {
            if (col == buildingCollider) continue;                // игнорируем свой коллайдер
            if (col.transform.IsChildOf(currentBuilding.transform)) continue; // игнорируем дочерние
            return false; // нашли посторонний объект – место занято
        }

        return true;
    }

    void PlaceBuilding()
    {
        isPlacing = false;

        // Регистрация жилого здания, если это ResidentialBuilding
        ResidentialBuilding residential = currentBuilding.GetComponent<ResidentialBuilding>();
        if (residential != null && BuildingManager.Instance != null)
            BuildingManager.Instance.RegisterResidential(residential);

        // Если нужна физика после установки
        Rigidbody rb = currentBuilding.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        currentBuilding = null;
        currentBuildingIndex = -1;
    }

    void CancelPlacement()
    {
        // Возврат ресурсов
        if (currentBuildingIndex >= 0 && currentBuildingIndex < buildings.Length)
        {
            BuildingData data = buildings[currentBuildingIndex];
            playerData.AddResources(data.woodCost, data.rockCost);
        }

        Destroy(currentBuilding);
        isPlacing = false;
        currentBuilding = null;
        currentBuildingIndex = -1;
    }
}