using UnityEngine;
using UnityEngine.UI;

public class AvatarManager : MonoBehaviour
{
    public static AvatarManager Instance { get; private set; }

    [Header("Avatar Settings")]
    [SerializeField] private string specialAvatarPath = "Img/Special"; // путь к спрайту в папке Resources
    [SerializeField] private string playerImageTag = "PlayerImg";
    
    private Image playerImage;
    private Sprite defaultSprite;
    private Sprite specialSprite;
    
    // Ключ для сохранения в PlayerPrefs
    private const string SPECIAL_AVATAR_PREFS = "SpecialAvatarApplied";
    // ID особого аватара (должен совпадать с тем, что приходит с сервера)
    public int specialAvatarItemId = 1;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        FindPlayerImage();
        LoadSprites();
        
        // После инициализации PlayerData (можно подождать кадр для надёжности)
        Invoke(nameof(SyncWithDatabase), 0.1f);
    }

    // Поиск объекта Image по тегу
    private void FindPlayerImage()
    {
        GameObject imgObject = GameObject.FindGameObjectWithTag(playerImageTag);
        if (imgObject != null)
        {
            playerImage = imgObject.GetComponent<Image>();
            if (playerImage != null)
                defaultSprite = playerImage.sprite; // сохраняем оригинальный спрайт
            else
                Debug.LogError($"Объект с тегом '{playerImageTag}' не содержит компонента Image!");
        }
        else
        {
            Debug.LogError($"Не найден объект с тегом '{playerImageTag}' на сцене!");
        }
    }

    // Загрузка особого спрайта из Resources
    private void LoadSprites()
    {
        specialSprite = Resources.Load<Sprite>(specialAvatarPath);
        if (specialSprite == null)
            Debug.LogError($"Не удалось загрузить спрайт по пути Resources/{specialAvatarPath}");
    }

    /// <summary>
    /// Применяет особый аватар и сохраняет состояние в PlayerPrefs
    /// </summary>
    public void ApplySpecialAvatar()
    {
        if (playerImage == null || specialSprite == null)
        {
            Debug.LogWarning("Не удалось применить аватар: Image или спрайт не найден");
            return;
        }
        
        playerImage.sprite = specialSprite;
        PlayerPrefs.SetInt(SPECIAL_AVATAR_PREFS, 1);
        PlayerPrefs.Save();
        Debug.Log("Особый аватар применён и сохранён в PlayerPrefs");
    }

    /// <summary>
    /// Сбрасывает аватар на стандартный (если есть сохранённый оригинал)
    /// </summary>
    public void ResetToDefaultAvatar()
    {
        if (playerImage == null || defaultSprite == null)
        {
            Debug.LogWarning("Не удалось сбросить аватар: Image или стандартный спрайт не найден");
            return;
        }
        
        playerImage.sprite = defaultSprite;
        Debug.Log("Аватар сброшен на стандартный");
    }

    /// <summary>
    /// Тестовый метод: сбрасывает сохранение в PlayerPrefs (но не меняет текущий спрайт)
    /// </summary>
    public void ClearSpecialAvatarPrefs()
    {
        PlayerPrefs.DeleteKey(SPECIAL_AVATAR_PREFS);
        PlayerPrefs.Save();
        Debug.Log("Сохранение особого аватара в PlayerPrefs сброшено. Для восстановления требуется синхронизация с БД.");
    }

    /// <summary>
    /// Синхронизация с БД: если особый аватар куплен, применяет его (даже если PlayerPrefs сброшен)
    /// </summary>
    public void SyncWithDatabase()
    {
        if (PlayerData.Instance == null)
        {
            Debug.LogError("PlayerData.Instance недоступен для синхронизации аватара");
            return;
        }
        
        bool isPurchased = PlayerData.Instance.purchasedItems != null &&
                           PlayerData.Instance.purchasedItems.Contains(specialAvatarItemId);
        
        if (isPurchased)
        {
            // Если куплен, всегда применяем аватар (по требованию задания)
            ApplySpecialAvatar();
            Debug.Log("Синхронизация с БД: особый аватар куплен, применён.");
        }
        else
        {
            // Если не куплен, но в PlayerPrefs есть отметка – сбрасываем её (защита от рассинхрона)
            if (PlayerPrefs.HasKey(SPECIAL_AVATAR_PREFS))
            {
                PlayerPrefs.DeleteKey(SPECIAL_AVATAR_PREFS);
                PlayerPrefs.Save();
                ResetToDefaultAvatar();
                Debug.Log("Синхронизация с БД: аватар не куплен, сохранение сброшено, спрайт по умолчанию.");
            }
        }
    }

  
}