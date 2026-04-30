using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UIMamager : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject Achiv;
    public GameObject Shop;
    public void OpenShopMenu()
    {
        Shop.SetActive(true);
    }

    public void CloseShopMenu()
    {
        Shop.SetActive(false);
    }

    public void OpenAchiv()
    {
        Achiv.SetActive(true);
        // »щем AchievementsManager на самой панели, а не на текущем объекте
        var achManager = Achiv.GetComponent<AchievementsManager>();
        if (achManager != null && PlayerData.Instance != null)
            achManager.Initialize(PlayerData.Instance);
        else
            Debug.LogError("AchievementsManager не найден на панели Achiv!");
    }
    public void CloseAchiv()
    {
        Achiv.SetActive(false);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
