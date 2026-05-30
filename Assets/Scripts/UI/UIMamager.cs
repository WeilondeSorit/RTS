using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UIMamager : MonoBehaviour
{
    public GameObject Achiv;
    public GameObject Shop;

    public AudioSource audioSource;
    public AudioClip clickSound;

    private void Awake()
    {

    }

    private void PlayClick()
    {
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound);
    }

    public void OpenShopMenu()
    {
        PlayClick();
        Shop.SetActive(true);
    }

    public void CloseShopMenu()
    {
        PlayClick();
        Shop.SetActive(false);
    }

    public void OpenAchiv()
    {
        PlayClick();
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
        PlayClick();
        Achiv.SetActive(false);
    }
}
