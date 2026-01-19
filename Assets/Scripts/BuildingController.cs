using UnityEngine;

public class BuildingController : MonoBehaviour
{
    public GameObject[] buildingPrefabs; // ������ �������� ������
    private GameObject currentBuilding;
    public Camera sceneCamera;
    public PlayerData playerData;

    public AudioClip audioBuld;
    public AudioSource audioSource;
    private bool isPlacing = false;
    private int currentBuildingIndex; // ���� ��� �������� �������� �������

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

        // ��������� ������
        int[] woodCost = { 10, 0, 20, 10, 30, 0, 15, 50, 5, 20 };  // ������ ������
        int[] rockCost = { 0, 15, 0, 10, 0, 20, 5, 50, 10, 10 };    // ������ �����

        // ���������, ������� �� ��������
        if (playerData.wood >= woodCost[index] && playerData.rock >= rockCost[index])
        {
            playerData.wood -= woodCost[index];
            playerData.rock -= rockCost[index];

            currentBuildingIndex = index; // ��������� ������
            currentBuilding = Instantiate(buildingPrefabs[index]);

            // ��������� ��������� BuildingIdentifier, ���� ��� ���
            if (!currentBuilding.GetComponent<BuildingIdentifier>())
            {
                BuildingIdentifier identifier = currentBuilding.AddComponent<BuildingIdentifier>();
                identifier.prefab = buildingPrefabs[index]; // ��������� prefab �����
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
            pos.y = 0f; // ��������� Y �� ������ �����
            currentBuilding.transform.position = pos;
        }
    }

    bool CanPlaceBuilding()
    {
        Collider[] colliders = Physics.OverlapBox(
            currentBuilding.transform.position,
            currentBuilding.transform.localScale / 2
        );
        return colliders.Length <= 1; // ������ ��� ������
    }

    void PlaceBuilding()
    {
        isPlacing = false;
        BuildingIdentifier identifier = currentBuilding.GetComponent<BuildingIdentifier>();

        if (identifier != null)
        {
           // playerData.placedBuildings.Add(currentBuilding); // �������� � ������
        }
        else
        {
            Debug.LogError("��������� BuildingIdentifier �� ������ �� ������.");
        }

        currentBuilding = null;
    }

    void CancelPlacement()
    {
        Destroy(currentBuilding);
        isPlacing = false;
    }
}
