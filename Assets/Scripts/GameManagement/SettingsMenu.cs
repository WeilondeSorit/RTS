using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
//пофиксить меню настроек
    [Header("UI Ýëåìåíòû")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle soundToggle;
    public Slider volumeSlider;

    private readonly Resolution[] availableResolutions =
    {
        new Resolution { width = 1280, height = 720 },
        new Resolution { width = 1366, height = 768 },
        new Resolution { width = 1600, height = 900 },
        new Resolution { width = 1920, height = 1080 },
    };

    private void Start()
    {
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            "1280 x 720",
            "1366 x 768",
            "1600 x 900",
            "1920 x 1080"
        });

        int savedResolutionIndex = PlayerPrefs.GetInt("resolutionIndex", 0);
        resolutionDropdown.value = savedResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        float savedVolume = PlayerPrefs.GetFloat("volume", 1f);
        volumeSlider.value = savedVolume;
        AudioListener.volume = savedVolume;

        int savedSoundState = PlayerPrefs.GetInt("soundOn", 1);
        bool isSoundOn = (savedSoundState == 1);
        soundToggle.isOn = isSoundOn;
        AudioListener.pause = !isSoundOn;
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution chosenRes = availableResolutions[resolutionIndex];
        Screen.SetResolution(chosenRes.width, chosenRes.height, Screen.fullScreen);
        PlayerPrefs.SetInt("resolutionIndex", resolutionIndex);
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("volume", volume);
    }

    public void SetSound(bool isOn)
    {
        AudioListener.pause = !isOn;
        PlayerPrefs.SetInt("soundOn", isOn ? 1 : 0);
    }

    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }
}