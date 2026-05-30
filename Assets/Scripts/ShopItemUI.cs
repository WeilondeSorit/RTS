using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShopItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;

    private int itemId;
    private bool isPurchased;

    public event Action<int> OnBuyClicked;

    public void Setup(ShopItemData data, bool purchased, Sprite icon, Action<int> buyCallback)
    {
        itemId = data.id;
        isPurchased = purchased;

        if (iconImage != null && icon != null)
            iconImage.sprite = icon;
        if (nameText != null)
            nameText.text = data.name;
        if (descriptionText != null)
            descriptionText.text = GetDescriptionForItem(data.id);
        if (priceText != null)
            priceText.text = isPurchased ? "" : $"Цена: {data.price}";
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClick);

        OnBuyClicked = buyCallback;

        UpdatePurchasedState();
    }

    private void OnBuyButtonClick()
    {
        if (isPurchased) return;
        OnBuyClicked?.Invoke(itemId);
    }

    public void SetPurchased(bool purchased)
    {
        isPurchased = purchased;
        UpdatePurchasedState();
    }

    private void UpdatePurchasedState()
    {
        if (buyButton != null)
        {
            var canvasGroup = buyButton.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = buyButton.gameObject.AddComponent<CanvasGroup>();

            if (isPurchased)
            {
                canvasGroup.alpha = 0.5f;
                buyButton.interactable = false;
                if (buyButtonText != null)
                    buyButtonText.text = "Куплено";
                if (priceText != null)
                    priceText.text = "";
            }
            else
            {
                canvasGroup.alpha = 1f;
                buyButton.interactable = true;
                if (buyButtonText != null)
                    buyButtonText.text = "Купить";
            }
        }
    }

    // Возвращает описание предмета по ID (можно вынести в отдельный файл с данными)
    private string GetDescriptionForItem(int id)
    {
        return id switch
        {
            1 => "Повышает прочность и эффективность всех зданий.",
            2 => "Усиливает боевые характеристики всех ваших юнитов.",
            3 => "Воодушевляет войска, повышая их боевой дух.",
            4 => "Уникальный аватар для вашего профиля.",
            _ => "Неизвестный предмет."
        };
    }

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveAllListeners();
    }
}