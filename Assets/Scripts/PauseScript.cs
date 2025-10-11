using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseScript : MonoBehaviour
{
    public GameObject setActivatePanel;
    //Скрипт паузы, открывает меню паузы. Логично.
public void PauseGame()
    {
        Time.timeScale = 0.0f;
        setActivatePanel.SetActive(true);
        Debug.Log("Game is paused");

    }
    public void ReturnInGame()
    {
        Time.timeScale = 1.0f;
        setActivatePanel.SetActive(false);
        Debug.Log("Game is resumed");
    }
}
