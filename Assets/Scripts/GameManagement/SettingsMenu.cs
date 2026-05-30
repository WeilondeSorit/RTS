using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Гарантированно работающее меню настроек: разрешение экрана (Dropdown), звук (Toggle), громкость (Slider).
/// Все изменения сразу применяются и сохраняются через PlayerPrefs.
/// При старте значения восстанавливаются из сохранений.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Dropdown для выбора разрешения экрана")]
    public TMP_Dropdown resolutionDropdown;
    [Tooltip("Toggle для включения/выключения звука (mute)")]
    public Toggle soundToggle;
    [Tooltip("Slider для громкости (0..1)")]
    public Slider volumeSlider;

    // Приватные данные разрешений
    private List<Resolution> availableResolutions = new List<Resolution>();

    // Ключи PlayerPrefs
    private const string RESOLUTION_WIDTH_KEY = "ResolutionWidth";
    private const string RESOLUTION_HEIGHT_KEY = "ResolutionHeight";
    private const string RESOLUTION_REFRESH_KEY = "ResolutionRefreshRate";
    private const string VOLUME_KEY = "Volume";
    private const string MUTED_KEY = "Muted";

    private void Start()
    {
        // Заполняем список уникальных разрешений (ширина × высота @ частота)
        PopulateResolutions();

        // Загружаем сохранённые настройки
        LoadSettings();

        // Подписываемся на события UI (на случай динамического добавления)
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    /// <summary>
    /// Заполняет выпадающий список уникальными разрешениями.
    /// </summary>
    private void PopulateResolutions()
    {
        resolutionDropdown.ClearOptions();
        availableResolutions.Clear();

        // Собираем все разрешения, которые поддерживает монитор
        Resolution[] allResolutions = Screen.resolutions;

        // Для каждого уникального сочетания ширины и высоты берём максимальную частоту
        Dictionary<(int, int), Resolution> unique = new Dictionary<(int, int), Resolution>();
        foreach (Resolution res in allResolutions)
        {
            var key = (res.width, res.height);
            if (!unique.ContainsKey(key) || res.refreshRate > unique[key].refreshRate)
                unique[key] = res;
        }

        // Сортируем для удобства: по ширине, потом по высоте
        List<Resolution> sorted = new List<Resolution>(unique.Values);
        sorted.Sort((a, b) => {
            int widthCompare = a.width.CompareTo(b.width);
            if (widthCompare != 0) return widthCompare;
            return a.height.CompareTo(b.height);
        });

        availableResolutions.AddRange(sorted);

        // Формируем строки для выпадающего списка
        List<string> options = new List<string>();
        foreach (Resolution res in availableResolutions)
        {
            options.Add($"{res.width}x{res.height} @ {res.refreshRate}Hz");
        }
        resolutionDropdown.AddOptions(options);
    }

    /// <summary>
    /// Загружает сохранённые настройки и применяет их.
    /// </summary>
    private void LoadSettings()
    {
        // --- Разрешение ---
        if (PlayerPrefs.HasKey(RESOLUTION_WIDTH_KEY))
        {
            int savedWidth = PlayerPrefs.GetInt(RESOLUTION_WIDTH_KEY);
            int savedHeight = PlayerPrefs.GetInt(RESOLUTION_HEIGHT_KEY);
            int savedRefresh = PlayerPrefs.GetInt(RESOLUTION_REFRESH_KEY);

            // Ищем индекс сохранённого разрешения в списке
            int index = availableResolutions.FindIndex(r =>
                r.width == savedWidth && r.height == savedHeight && r.refreshRate == savedRefresh);

            if (index >= 0)
            {
                resolutionDropdown.SetValueWithoutNotify(index);
                ApplyResolution(availableResolutions[index]);
            }
            else
            {
                // Если сохранённое разрешение больше не поддерживается, берём первое
                resolutionDropdown.SetValueWithoutNotify(0);
                ApplyResolution(availableResolutions[0]);
            }
        }
        else
        {
            // Нет сохранений – используем текущее разрешение экрана (или первое из списка)
            Resolution current = Screen.currentResolution;
            int index = availableResolutions.FindIndex(r =>
                r.width == current.width && r.height == current.height);

            if (index >= 0)
            {
                resolutionDropdown.SetValueWithoutNotify(index);
                ApplyResolution(availableResolutions[index]);
            }
            else
            {
                resolutionDropdown.SetValueWithoutNotify(0);
                ApplyResolution(availableResolutions[0]);
            }
        }

        // --- Громкость и Mute ---
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        bool isMuted = PlayerPrefs.GetInt(MUTED_KEY, 0) == 1;

        volumeSlider.SetValueWithoutNotify(savedVolume);
        soundToggle.SetIsOnWithoutNotify(!isMuted); // Toggle включен, если не в mute

        // Применяем громкость (учитывая mute)
        AudioListener.volume = isMuted ? 0f : savedVolume;
    }

    /// <summary>
    /// Вызывается при изменении выбора разрешения в Dropdown.
    /// </summary>
    public void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= availableResolutions.Count)
            return;

        Resolution selected = availableResolutions[index];
        ApplyResolution(selected);
        SaveResolution(selected);
    }

    /// <summary>
    /// Вызывается при переключении Toggle звука.
    /// </summary>
    public void OnSoundToggleChanged(bool isOn)
    {
        // isOn = true → звук включен (не muted)
        bool muted = !isOn;
        PlayerPrefs.SetInt(MUTED_KEY, muted ? 1 : 0);
        PlayerPrefs.Save();

        // Применяем: если muted – громкость 0, иначе то, что на слайдере
        AudioListener.volume = muted ? 0f : volumeSlider.value;
    }

    /// <summary>
    /// Вызывается при перемещении ползунка громкости.
    /// </summary>
    public void OnVolumeChanged(float volume)
    {
        PlayerPrefs.SetFloat(VOLUME_KEY, volume);
        PlayerPrefs.Save();

        // Если звук не в mute – сразу применяем новую громкость
        // (состояние mute хранится в PlayerPrefs, Toggle отражает его инвертированно)
        bool muted = PlayerPrefs.GetInt(MUTED_KEY, 0) == 1;
        AudioListener.volume = muted ? 0f : volume;
    }

    /// <summary>
    /// Применяет переданное разрешение (полноэкранный режим).
    /// </summary>
    private void ApplyResolution(Resolution res)
    {
        // true = полноэкранный режим (FullScreenMode.ExclusiveFullScreen в старых версиях,
        // в Unity 2022+ можно использовать FullScreenMode.FullScreenWindow для оконного без рамок)
        Screen.SetResolution(res.width, res.height, true);
    }

    /// <summary>
    /// Сохраняет разрешение в PlayerPrefs.
    /// </summary>
    private void SaveResolution(Resolution res)
    {
        PlayerPrefs.SetInt(RESOLUTION_WIDTH_KEY, res.width);
        PlayerPrefs.SetInt(RESOLUTION_HEIGHT_KEY, res.height);
        PlayerPrefs.SetInt(RESOLUTION_REFRESH_KEY, res.refreshRate);
        PlayerPrefs.Save();
    }

    // Опционально: если нужно освободить слушатели при уничтожении объекта
    private void OnDestroy()
    {
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (soundToggle != null)
            soundToggle.onValueChanged.RemoveListener(OnSoundToggleChanged);
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

public void SaveSettings()
{
    // Сохраняем разрешение (то, что сейчас выбрано в Dropdown)
    if (resolutionDropdown != null && availableResolutions.Count > 0)
    {
        int index = resolutionDropdown.value;
        if (index >= 0 && index < availableResolutions.Count)
        {
            Resolution res = availableResolutions[index];
            PlayerPrefs.SetInt(RESOLUTION_WIDTH_KEY, res.width);
            PlayerPrefs.SetInt(RESOLUTION_HEIGHT_KEY, res.height);
            PlayerPrefs.SetInt(RESOLUTION_REFRESH_KEY, res.refreshRate);
        }
    }

    // Сохраняем громкость
    if (volumeSlider != null)
    {
        PlayerPrefs.SetFloat(VOLUME_KEY, volumeSlider.value);
    }

    // Сохраняем состояние Mute (инвертируем Toggle)
    if (soundToggle != null)
    {
        bool muted = !soundToggle.isOn; // Toggle включен = звук есть, значит muted = false
        PlayerPrefs.SetInt(MUTED_KEY, muted ? 1 : 0);
    }

    // Записываем на диск
    PlayerPrefs.Save();

    // Опционально: применяем всё прямо сейчас (на случай, если автоматическое применение не сработало)
    bool isMuted = PlayerPrefs.GetInt(MUTED_KEY, 0) == 1;
    float vol = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
    AudioListener.volume = isMuted ? 0f : vol;

    Debug.Log("Settings saved and applied.");
}
}