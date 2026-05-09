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
            descriptionText.text = GetDescriptionForItem(data.id);  // описание жёстко задаём по ID
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

    // Простейшая реализация описаний по ID товара (можно вынести в отдельный конфиг)
    private string GetDescriptionForItem(int id)
    {
        return id switch
        {
            1 => "Навсегда увеличивает прочность и производство всех зданий.",
            2 => "Навсегда усиливает здоровье и урон всех юнитов.",
            3 => "Ускоряет передвижение всех ваших юнитов.",
            4 => "Особый аватар для вашего профиля.",
            _ => "Описание отсутствует."
        };
    }

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveAllListeners();
    }
}