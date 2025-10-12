using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine;

public class GetDataFromSupabase : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    IEnumerator GetData()
    {
        string url = "https://mcp.supabase.com/mcp?project_ref=ceqdjafzolfhtqjjlvwg";
        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("apikey", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNlcWRqYWZ6b2xmaHRxampsdndnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjAyNTAyMzQsImV4cCI6MjA3NTgyNjIzNH0.N_RQNgbW0jx7mlyUI67sQMaZp38xqMzFR6fJjNN4338");
        www.SetRequestHeader("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNlcWRqYWZ6b2xmaHRxampsdndnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjAyNTAyMzQsImV4cCI6MjA3NTgyNjIzNH0.N_RQNgbW0jx7mlyUI67sQMaZp38xqMzFR6fJjNN4338");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
            Debug.Log(www.downloadHandler.text);
        else
            Debug.LogError(www.error);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
