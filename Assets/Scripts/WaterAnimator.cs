using UnityEngine;

/// <summary>
/// Простая анимация волн для водных плиток.
/// Прикрепите этот скрипт к префабу воды (waterPrefab).
/// </summary>
[RequireComponent(typeof(Renderer))]
public class WaterAnimator : MonoBehaviour
{
    [Header("Wave Parameters (auto-set by generator)")]
    [Tooltip("Сдвиг фазы для уникальности каждой волны")]
    public float phaseOffset = 0f;

    [Tooltip("Частота колебаний (0.5-2.0)")]
    [Range(0.5f, 2f)]
    public float frequency = 1f;

    [Tooltip("Амплитуда колебаний по высоте")]
    [Range(0.01f, 0.1f)]
    public float amplitude = 0.04f;

    [Tooltip("Множитель общей скорости анимации")]
    public float speedMultiplier = 1f;

    [Header("Visual Settings")]
    [Tooltip("Цвет воды в покое")]
    public Color baseColor = new Color(0.1f, 0.35f, 0.85f, 0.8f);

    [Tooltip("Цвет воды на гребне волны")]
    public Color highlightColor = new Color(0.3f, 0.6f, 1f, 0.9f);

    [Tooltip("Сила изменения прозрачности")]
    [Range(0f, 0.2f)]
    public float alphaVariation = 0.05f;

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;
    private Vector3 _originalPosition;
    private Vector3 _originalScale;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
        _originalPosition = transform.position;
        _originalScale = transform.localScale;

        // Начальная настройка материала
        if (_renderer.sharedMaterial.HasProperty("_Color"))
        {
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_Color", baseColor);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }

    /// <summary>
    /// Вызывайте этот метод из Update главного скрипта для анимации
    /// </summary>
    public void AnimateWave(float time)
    {
        // 1. Анимация позиции (вертикальные колебания)
        float waveY = Mathf.Sin(time * 2f * frequency * speedMultiplier + phaseOffset) * amplitude;
        transform.position = new Vector3(_originalPosition.x, _originalPosition.y + waveY, _originalPosition.z);

        // 2. Лёгкое "дыхание" масштаба
        float scalePulse = 1f + Mathf.Sin(time * 1.3f * frequency + phaseOffset) * 0.015f;
        transform.localScale = new Vector3(
            _originalScale.x * scalePulse,
            _originalScale.y,
            _originalScale.z * scalePulse
        );

        // 3. Анимация цвета (опционально, для красоты)
        if (_renderer != null)
        {
            float colorLerp = (Mathf.Sin(time * 1.8f * frequency + phaseOffset) + 1f) * 0.5f;
            Color currentColor = Color.Lerp(baseColor, highlightColor, colorLerp * 0.3f);

            // Вариация прозрачности
            currentColor.a = baseColor.a + Mathf.Sin(time * 0.7f + phaseOffset) * alphaVariation;

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_Color", currentColor);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}