using UnityEngine;
using TMPro;

public class QuestBinder : MonoBehaviour
{
    public TextMeshProUGUI questText; // перетащите сюда свой текст

    void Start()
    {
        if (PlayerData.Instance != null && PlayerData.Instance.achievementSystem != null)
        {
            PlayerData.Instance.achievementSystem.SetQuestDisplay(questText);
            PlayerData.Instance.achievementSystem.StartCoroutine(
                PlayerData.Instance.achievementSystem.RefreshQuest()
            );
        }
    }
}