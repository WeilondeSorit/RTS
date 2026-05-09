using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private ShopItemUI shopItemPrefab;
    [SerializeField] private TextMeshProUGUI currencyText;

    [Header("Settings")]
    [SerializeField] private string serverUrl = "http://localhost:8080";
    [SerializeField] private string shopIconsPath = "ShopIcons/";

    private List<ShopItemData> shopItems = new List<ShopItemData>();
    private List<ShopItemUI> spawnedItems = new List<ShopItemUI>();

    private void Start()
    {
        if (PlayerData.Instance == null)
        {
            Debug.LogError("PlayerData.Instance not found!");
            return;
        }

        PlayerData.Instance.OnResourcesChanged += UpdateCurrencyDisplay;
        UpdateCurrencyDisplay();
    }

    private void OnEnable()
    {
        if (shopItems.Count == 0)
            StartCoroutine(LoadShopItems());
    }

    public void ToggleShop()
    {
        if (shopPanel != null)
        {
            bool active = !shopPanel.activeSelf;
            shopPanel.SetActive(active);
            if (active)
                StartCoroutine(LoadShopItems());
        }
    }

    private IEnumerator LoadShopItems()
    {
        string url = $"{serverUrl}/shop";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load shop items: {request.error}");
                yield break;
            }

            string json = request.downloadHandler.text;
            var items = JsonUtility.FromJson<ShopItemList>("{\"items\":" + json + "}");
            if (items != null && items.items != null)
            {
                shopItems = new List<ShopItemData>(items.items);
                PopulateShop();
            }
        }
    }

    private void PopulateShop()
    {
        foreach (var spawned in spawnedItems)
        {
            if (spawned != null) Destroy(spawned.gameObject);
        }
        spawnedItems.Clear();

        List<int> purchasedIds = PlayerData.Instance.purchasedItems ?? new List<int>();

        foreach (var item in shopItems)
        {
            ShopItemUI uiItem = Instantiate(shopItemPrefab, itemsContainer);
            bool purchased = purchasedIds.Contains(item.id);
            Sprite icon = Resources.Load<Sprite>($"{shopIconsPath}{item.imagePath}");
            uiItem.Setup(item, purchased, icon, OnBuyClicked);
            spawnedItems.Add(uiItem);
        }
    }

    private void OnBuyClicked(int itemId)
    {
        if (PlayerData.Instance.purchasedItems.Contains(itemId))
        {
            Debug.Log("Already purchased");
            return;
        }
        StartCoroutine(BuyItem(itemId));
    }

    private IEnumerator BuyItem(int itemId)
    {
        string url = $"{serverUrl}/player/{PlayerData.Instance.playerId}/buy";
        var requestData = new BuyRequest { itemId = itemId };
        string jsonData = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {PlayerData.Instance.authToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<BuyResponse>(request.downloadHandler.text);
                PlayerData.Instance.currency = response.currency;
                PlayerData.Instance.purchasedItems = response.purchasedItems;
                PlayerData.Instance.SavePlayerData();
                PlayerData.Instance.NotifyResourcesChanged();
                RefreshPurchasedStates();
            }
            else
            {
                Debug.LogError($"Purchase failed: {request.downloadHandler.text}");
            }
        }
    }

    private void RefreshPurchasedStates()
    {
        var purchasedIds = PlayerData.Instance.purchasedItems;
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (i < shopItems.Count)
            {
                int id = shopItems[i].id;
                spawnedItems[i].SetPurchased(purchasedIds.Contains(id));
            }
        }
    }

    private void UpdateCurrencyDisplay()
    {
        if (currencyText != null && PlayerData.Instance != null)
        {
            currencyText.text = $"Валюта: {PlayerData.Instance.currency}";
        }
    }

    private void OnDestroy()
    {
        if (PlayerData.Instance != null)
            PlayerData.Instance.OnResourcesChanged -= UpdateCurrencyDisplay;
    }

    // Вспомогательные приватные классы для десериализации
    [System.Serializable]
    private class ShopItemList
    {
        public List<ShopItemData> items;
    }

    [System.Serializable]
    private class BuyRequest
    {
        public int itemId;
    }

    [System.Serializable]
    private class BuyResponse
    {
        public int currency;
        public List<int> purchasedItems;
    }
}