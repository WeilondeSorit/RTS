using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ClickHandler : MonoBehaviour
{
    [Header("References")]
    public Camera sceneCamera;
    public GameObject unitMenu;
    public TextMeshProUGUI nameUnit;
    public Image unitIcon;
    public Slider sliderHealth;
    public TextMeshProUGUI textHealth;
    public AudioClip audioMov;
    public AudioSource audioSource;

    [Header("Icons")]
    public Sprite villagerIcon;
    public Sprite archerIcon;
    public Sprite buldIcon;

    private BasicUnit basicUnit;
    private Enemy enemyHealth;
    private BasicBulding basicBulding;

    void Update()
    {
        if (!IsClickStarted()) return;
        if (IsPointerOverUI()) return;

        HandleClick();
    }

    private bool IsClickStarted()
    {
        // Работает для PC и Android
        return Input.GetMouseButtonDown(0);
    }

    private bool IsPointerOverUI()
    {
        // Проверка для PC и Android
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        // Дополнительная проверка для мобильных устройств
        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId))
                return true;
        }

        return false;
    }

    private void HandleClick()
    {
        Ray ray = sceneCamera.ScreenPointToRay(GetClickPosition());
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            CloseMenuPanel();
            return;
        }

        ProcessHitObject(hit);
    }

    private Vector3 GetClickPosition()
    {
        // Возвращает позицию клика/тапа
        if (Input.touchCount > 0)
            return Input.touches[0].position;
        else
            return Input.mousePosition;
    }

    private void ProcessHitObject(RaycastHit hit)
    {
        basicUnit = hit.collider.GetComponent<BasicUnit>();
        enemyHealth = hit.collider.GetComponent<Enemy>();
        basicBulding = hit.collider.GetComponent<BasicBulding>();

        if (basicUnit == null && enemyHealth == null && basicBulding == null)
        {
            CloseMenuPanel();
            return;
        }

        OpenMenuPanel();
        UpdateUI(hit);
    }

    private void UpdateUI(RaycastHit hit)
    {
        nameUnit.text = hit.collider.gameObject.name;

        // Обновление здоровья
        if (enemyHealth != null)
            UpdateHealth(enemyHealth.health, enemyHealth.maxHealth, 0.01f);
        else if (basicUnit != null)
            UpdateHealth(basicUnit.health, basicUnit.maxHealth, 0.01f);
        else if (basicBulding != null)
            UpdateHealth(basicBulding.health, basicBulding.maxHealth, 0.005f);

        // Обновление иконки
        UpdateIcon(hit.collider.tag);
    }

    private void UpdateHealth(float currentHealth, float maxHealth, float multiplier)
    {
        sliderHealth.value = currentHealth * multiplier;
        textHealth.text = $"{currentHealth}/{maxHealth}";
    }

    private void UpdateIcon(string objectTag)
    {
        unitIcon.sprite = objectTag switch
        {
            "Villager" => villagerIcon,
            "Archer" => archerIcon,
            "Enemy" => archerIcon,
            "Base" => buldIcon,
            _ => unitIcon.sprite
        };
    }

    public void MoveTo()
    {
        if (basicUnit == null) return;

        basicUnit.GCnavMeshAgent();
        audioSource.PlayOneShot(audioMov);
    }

    public void OpenMenuPanel() => unitMenu.SetActive(true);
    public void CloseMenuPanel() => unitMenu.SetActive(false);
}