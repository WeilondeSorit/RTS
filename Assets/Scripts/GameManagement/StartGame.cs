using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    //�������� ������ ���������� ������ �������� � JSON
    public void Starting()
    {
        Time.timeScale = 1.0f;
        SimpleLoadingManager.LoadSceneWithLoading("GameScene");
    }
}
