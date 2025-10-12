using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GetDataFromSupabase : MonoBehaviour
{
    private string supabaseUrl = "https://ceqdjafzolfhtqjjlvwg.supabase.co/rest/v1/";
    private string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNlcWRqYWZ6b2xmaHRxampsdndnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjAyNTAyMzQsImV4cCI6MjA3NTgyNjIzNH0.N_RQNgbW0jx7mlyUI67sQMaZp38xqMzFR6fJjNN4338";

    void Start()
    {
        // Пример загрузки данных при старте
        StartCoroutine(GetPlayerData());
    }

    IEnumerator GetPlayerData()
    {
        string url = $"{supabaseUrl}Player?id=eq.1";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("apikey", supabaseKey);
            www.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            www.SetRequestHeader("Accept", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Player Data: " + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error: " + www.error);
            }
        }
    }

    IEnumerator GetBuildingsData()
    {
        string url = $"{supabaseUrl}Building?player_id=eq.1";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("apikey", supabaseKey);
            www.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            www.SetRequestHeader("Accept", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Buildings Data: " + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error: " + www.error);
            }
        }
    }

    // Вызов этих методов из UI или других скриптов
    public void LoadPlayerData()
    {
        StartCoroutine(GetPlayerData());
    }

    public void LoadBuildingsData()
    {
        StartCoroutine(GetBuildingsData());
    }
}