using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UIMamager : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject profile;
    public GameObject buildMenu;
    public void OpenBuildMenu()
    {
        buildMenu.SetActive(true);
    }

    public void CloseBuildMenu()
    {
        buildMenu.SetActive(false);
    }

    public void OpenPofile()
    {
        profile.SetActive(true);
    }

    public void ClosePofile()
    {
        profile.SetActive(false);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
