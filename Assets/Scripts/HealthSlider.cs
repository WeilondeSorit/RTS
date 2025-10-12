using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HealthSlider : MonoBehaviour
{
    public Slider slider;
    public string targetTag;
    private BasicBulding basicBulding;

    // Оптимизированное значение максимального хп (500)
    private float maxHealth = 500f;

    void Start()
    {
        // Находим объект с тегом "Base"
        GameObject baseObject = GameObject.FindGameObjectWithTag(targetTag);
        if (baseObject != null)
        {
            basicBulding = baseObject.GetComponent<BasicBulding>();
        }
        else
        {
            Debug.LogError("Объект с тегом 'Base' не найден!");
        }

        // Если слайдер не назначен через инспектор, пытаемся получить его с того же объекта
        if (slider == null)
        {
            slider = GetComponent<Slider>();
            if (slider == null)
            {
                Debug.LogError("Слайдер не найден!");
            }
        }

        // Задаем максимальное значение слайдера
        slider.maxValue = maxHealth;
    }

    void Update()
    {
        // Если компонент BasicBulding найден, обновляем значение слайдера
        if (basicBulding != null)
        {
            // Значение слайдера соответствует текущему здоровью здания (поле health)
            slider.value = basicBulding.health;
        }
    }
}