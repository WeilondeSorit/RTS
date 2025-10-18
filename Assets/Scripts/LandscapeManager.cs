using UnityEngine;

public class LandscapeManager : MonoBehaviour
{
    void Start()
    {
        SetLandscapeOrientation();
    }

    void SetLandscapeOrientation()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
    }

    void Update()
    {
        // На случай, если пользователь перевернет устройство
        if (Screen.orientation != ScreenOrientation.LandscapeLeft &&
            Screen.orientation != ScreenOrientation.LandscapeRight)
        {
            SetLandscapeOrientation();
        }
    }
}