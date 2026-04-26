using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HealthSlider : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider slider;
    [SerializeField] private string targetTag = "Base";
    [SerializeField] private bool findInStart = true;

    private Health targetHealth;
    private bool isInitialized = false;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        if (slider == null)
        {
            Debug.LogError("HealthSlider: Slider component not found on this GameObject");
            enabled = false;
            return;
        }

        slider.wholeNumbers = false;

        if (findInStart)
        {
            Initialize();
        }
    }

    private void Start()
    {
        if (!findInStart)
        {
            Initialize();
        }
    }

    public void Initialize()
    {
        if (isInitialized) return;

        GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);

        if (targetObject != null)
        {
            targetHealth = targetObject.GetComponent<Health>();
        }

        if (targetHealth == null)
        {
            Debug.LogWarning($"HealthSlider: Target with tag '{targetTag}' not found or has no Health component");
            return;
        }

        targetHealth.OnHealthChanged += UpdateSlider;
        UpdateSlider(targetHealth.HealthCurrent, targetHealth.MaxHealth);

        isInitialized = true;
    }

    private void UpdateSlider(int currentHealth, int maxHealth)
    {
        if (slider == null) return;

        slider.maxValue = maxHealth;
        slider.value = currentHealth;
    }

    public void SetTarget(Health newTarget)
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= UpdateSlider;
        }

        targetHealth = newTarget;
        isInitialized = false;

        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged += UpdateSlider;
            UpdateSlider(targetHealth.HealthCurrent, targetHealth.MaxHealth);
            isInitialized = true;
        }
    }

    private void OnDisable()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= UpdateSlider;
        }
    }

    private void OnDestroy()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= UpdateSlider;
        }
    }
}