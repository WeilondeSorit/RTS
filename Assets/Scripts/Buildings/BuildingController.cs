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

    public BuildingData[] buildings;
    public Camera sceneCamera;
    public AudioClip audioBuild;
    public AudioSource audioSource;
    public PlayerData playerData;

    private GameObject currentBuilding;
    private int currentBuildingIndex = -1;
    private bool isPlacing = false;
    private Quaternion targetRotation;   // фиксированный поворот на время размещения

    void Update()
    {
        if (isPlacing && currentBuilding != null)
        {
            MoveBuildingToMouse();
            // Принудительно восстанавливаем поворот – даже если физика или что-то ещё его сбросили
            currentBuilding.transform.rotation = targetRotation;

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

        if (isPlacing)
        {
            Destroy(currentBuilding);
            isPlacing = false;
        }

        BuildingData data = buildings[index];

        if (playerData.wood >= data.woodCost && playerData.rock >= data.rockCost)
        {
            playerData.SpendResources(data.woodCost, data.rockCost);

            currentBuildingIndex = index;
            // Разворачиваем здание на 180° относительно его исходного поворота
            Quaternion baseRotation = data.prefab.transform.rotation;
            targetRotation = baseRotation * Quaternion.Euler(0, 180, 0);
            currentBuilding = Instantiate(data.prefab, Vector3.zero, targetRotation);

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
            pos.y = 0f;
            currentBuilding.transform.position = pos;
            // Поворот будет восстановлен в Update сразу после этого вызова
        }
    }

    bool CanPlaceBuilding()
    {
        if (currentBuilding == null) return false;

        Collider buildingCollider = currentBuilding.GetComponent<Collider>();
        if (buildingCollider == null) return true;

        Vector3 center = buildingCollider.bounds.center;
        Vector3 halfExtents = buildingCollider.bounds.extents;

        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, currentBuilding.transform.rotation);

        foreach (Collider col in hitColliders)
        {
            if (col == buildingCollider) continue;
            if (col.transform.IsChildOf(currentBuilding.transform)) continue;
            return false;
        }

        return true;
    }

    void PlaceBuilding()
    {
        isPlacing = false;

        // Окончательно фиксируем нужный поворот (на случай, если что‑то его изменило)
        currentBuilding.transform.rotation = targetRotation;

        ResidentialBuilding residential = currentBuilding.GetComponent<ResidentialBuilding>();
        if (residential != null && BuildingManager.Instance != null)
            BuildingManager.Instance.RegisterResidential(residential);

        Rigidbody rb = currentBuilding.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        currentBuilding = null;
        currentBuildingIndex = -1;
    }

    void CancelPlacement()
    {
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