using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class StartGame : MonoBehaviour
{
    public GameObject startMenu;
    public TMP_Dropdown dropdown;

    
    public void OpenStartMenu()
    {
startMenu.SetActive(true);
dropdown.ClearOptions();
dropdown.AddOptions(new List<string> { "Лёгкая", "Средняя", "Кошмар" });
string option = dropdown.options[dropdown.value].text;
        PlayerPrefs.SetString("Difficulty", $"{option}");
        PlayerPrefs.Save();
    }
    public void SaveSettings()
    {
startMenu.SetActive(false);
    }

    public void Starting()
    {
        if (PlayerData.Instance != null)
            PlayerData.Instance.isGameActive = true;
        else
            Debug.LogError("PlayerData.Instance не найден!");

        Time.timeScale = 1.0f;
        SimpleLoadingManager.LoadSceneWithLoading("GameScene");
    }
}