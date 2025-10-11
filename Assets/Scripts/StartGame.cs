using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    //Дописать скрипт сохранения данных настроек в JSON
    public void Starting()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("GameScene");
    }
}
