using UnityEngine;

public class StartGame : MonoBehaviour
{
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