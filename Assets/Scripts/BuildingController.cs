using UnityEngine;

public class BuildingController : MonoBehaviour
{
    public GameObject[] buildingPrefabs; // Список префабов зданий
    private GameObject currentBuilding;
    public Camera sceneCamera;
    public PlayerData playerData;

    public AudioClip audioBuld;
    public AudioSource audioSource;
    private bool isPlacing = false;
    private int currentBuildingIndex; // Поле для хранения текущего индекса

    void Update()
    {
        if (isPlacing && currentBuilding != null)
        {
            MoveBuildingToMouse();

            if (Input.GetMouseButtonDown(0) && CanPlaceBuilding())
            {
                PlaceBuilding();
                audioSource.PlayOneShot(audioBuld);
            }
            if (Input.GetMouseButtonDown(1))
                CancelPlacement();
        }
    }

    public void StartPlacingBuilding(int index)
    {
        if (isPlacing) Destroy(currentBuilding);

        // Стоимость зданий
        int[] woodCost = { 10, 0, 20, 10, 30, 0, 15, 50, 5, 20 };  // Расход дерева
        int[] rockCost = { 0, 15, 0, 10, 0, 20, 5, 50, 10, 10 };    // Расход камня

        // Проверяем, хватает ли ресурсов
        if (playerData.wood >= woodCost[index] && playerData.rock >= rockCost[index])
        {
            playerData.wood -= woodCost[index];
            playerData.rock -= rockCost[index];

            currentBuildingIndex = index; // Сохраняем индекс
            currentBuilding = Instantiate(buildingPrefabs[index]);

            // Добавляем компонент BuildingIdentifier, если его нет
            if (!currentBuilding.GetComponent<BuildingIdentifier>())
            {
                BuildingIdentifier identifier = currentBuilding.AddComponent<BuildingIdentifier>();
                identifier.prefab = buildingPrefabs[index]; // Установка prefab сразу
            }

            isPlacing = true;
        }
    }

    void MoveBuildingToMouse()
    {
        Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 pos = hit.point;
            pos.y = 0f; // Фиксируем Y на уровне земли
            currentBuilding.transform.position = pos;
        }
    }

    bool CanPlaceBuilding()
    {
        Collider[] colliders = Physics.OverlapBox(
            currentBuilding.transform.position,
            currentBuilding.transform.localScale / 2
        );
        return colliders.Length <= 1; // Только сам объект
    }

    void PlaceBuilding()
    {
        isPlacing = false;
        BuildingIdentifier identifier = currentBuilding.GetComponent<BuildingIdentifier>();

        if (identifier != null)
        {
            playerData.placedBuildings.Add(currentBuilding); // Добавьте в список
        }
        else
        {
            Debug.LogError("Компонент BuildingIdentifier не найден на здании.");
        }

        currentBuilding = null;
    }

    void CancelPlacement()
    {
        Destroy(currentBuilding);
        isPlacing = false;
    }
}
