using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Добавлено для работы сценами

public class ShopEffectManager : MonoBehaviour
{
    public static ShopEffectManager Instance { get; private set; }

    [Header("Параметры улучшений")]
    [SerializeField] private float buildingHealthMult = 1.5f;
    [SerializeField] private float buildingProductionMult = 1.5f;
    [SerializeField] private float unitHealthMult = 1.3f;
    [SerializeField] private float unitDamageMult = 1.3f;
    [SerializeField] private float unitSpeedMult = 1.2f;

    [Header("Особый аватар")]
    [SerializeField] private string specialAvatarResourcePath = "Img/Special";
    [SerializeField] private string playerImageTag = "PlayerImg";

    private const int BUILDING_UPGRADE = 1;
    private const int UNIT_UPGRADE = 2;
    private const int BATTLE_STANDARD = 3;
    private const int AVATAR = 4;
    private const string SPECIAL_AVATAR_PREFS = "SpecialAvatarApplied";

    public bool IsBuildingUpgraded { get; private set; }
    public bool IsUnitUpgraded { get; private set; }
    public bool IsSpeedBoosted { get; private set; }
    public bool IsAvatarPurchased { get; private set; }

    private Sprite specialAvatarSprite;
    private Sprite defaultAvatarSprite;
    private Image playerImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        specialAvatarSprite = Resources.Load<Sprite>(specialAvatarResourcePath);
        if (specialAvatarSprite == null)
            Debug.LogError($"Не удалось загрузить спрайт по пути Resources/{specialAvatarResourcePath}");

        // FIX: подписываемся на событие загрузки сцены
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        FindPlayerImageAndStoreDefault();
        ApplyAllPurchasedEffects();
    }

    private void OnDestroy()
    {
        // Отписываемся, чтобы избежать утечек
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // FIX: этот метод вызывается при загрузке любой сцены
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ждём один кадр, чтобы UI-объекты успели создаться
        StartCoroutine(ApplyAvatarAfterSceneLoad());
    }

    private System.Collections.IEnumerator ApplyAvatarAfterSceneLoad()
    {
        yield return null; // ждём один кадр
        FindPlayerImageAndStoreDefault(); // переищем аватар на новой сцене
        SyncAvatarWithDatabase(); // применим аватар, если куплен
    }

    private void FindPlayerImageAndStoreDefault()
    {
        GameObject imgObj = GameObject.FindGameObjectWithTag(playerImageTag);
        if (imgObj != null)
        {
            playerImage = imgObj.GetComponent<Image>();
            if (playerImage != null)
                defaultAvatarSprite = playerImage.sprite;
            else
                Debug.LogError($"Объект с тегом '{playerImageTag}' не имеет компонента Image!");
        }
        else
        {
            // Не ошибка — возможно, на текущей сцене нет аватара
            playerImage = null;
        }
    }

    private void ApplyAllPurchasedEffects()
    {
        if (PlayerData.Instance == null) return;

        foreach (int id in PlayerData.Instance.purchasedItems)
        {
            ApplyEffectInternal(id, false);
        }

        ApplyToAllExisting();
        SyncAvatarWithDatabase();
    }

    public void ApplyEffect(int itemId)
    {
        ApplyEffectInternal(itemId, true);
    }

    private void ApplyEffectInternal(int itemId, bool applyToExisting)
    {
        switch (itemId)
        {
            case BUILDING_UPGRADE:
                if (!IsBuildingUpgraded)
                {
                    IsBuildingUpgraded = true;
                    if (applyToExisting) ApplyBuildingUpgradeToAll();
                }
                break;
            case UNIT_UPGRADE:
                if (!IsUnitUpgraded)
                {
                    IsUnitUpgraded = true;
                    if (applyToExisting) ApplyUnitUpgradeToAll();
                }
                break;
            case BATTLE_STANDARD:
                if (!IsSpeedBoosted)
                {
                    IsSpeedBoosted = true;
                    if (applyToExisting) ApplySpeedBoostToAll();
                }
                break;
            case AVATAR:
                if (!IsAvatarPurchased)
                {
                    IsAvatarPurchased = true;
                    ApplyAvatarChange();
                }
                break;
        }
    }

    private void ApplyToAllExisting()
    {
        ApplyBuildingUpgradeToAll();
        ApplyUnitUpgradeToAll();
        ApplySpeedBoostToAll();
    }

    private void ApplyBuildingUpgradeToAll()
    {
        if (!IsBuildingUpgraded) return;
        var buildings = FindObjectsOfType<BasicBulding>(true);
        foreach (var b in buildings) ApplyBuildingUpgrade(b);
    }

    private void ApplyUnitUpgradeToAll()
    {
        if (!IsUnitUpgraded) return;
        var units = FindObjectsOfType<BasicUnit>(true);
        foreach (var u in units) ApplyUnitUpgrade(u);
    }

    private void ApplySpeedBoostToAll()
    {
        if (!IsSpeedBoosted) return;
        var units = FindObjectsOfType<BasicUnit>(true);
        foreach (var u in units) ApplySpeedBoost(u);
    }

    public void ApplyBuildingUpgrade(BasicBulding b)
    {
        if (!IsBuildingUpgraded || b == null) return;
        b.maxHealth = Mathf.RoundToInt(b.maxHealth * buildingHealthMult);
        b.health = b.maxHealth;
        if (b is Farm farm) farm.foodPerCycle = Mathf.RoundToInt(farm.foodPerCycle * buildingProductionMult);
        else if (b is Mill mill) mill.foodPerCycle = Mathf.RoundToInt(mill.foodPerCycle * buildingProductionMult);
        else if (b is DefenseBuilding def) def.damage = Mathf.RoundToInt(def.damage * buildingProductionMult);
    }

    public void ApplyUnitUpgrade(BasicUnit u)
    {
        if (!IsUnitUpgraded || u == null) return;
        u.maxHealth = Mathf.RoundToInt(u.maxHealth * unitHealthMult);
        u.health = u.maxHealth;
        if (u is Archer archer) archer.damage = Mathf.RoundToInt(archer.damage * unitDamageMult);
    }

    public void ApplySpeedBoost(BasicUnit u)
    {
        if (!IsSpeedBoosted || u == null) return;
        var agent = u.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.speed *= unitSpeedMult;
    }

    public void ApplyAvatarChange()
    {
        if (!IsAvatarPurchased) return;
        if (playerImage == null) FindPlayerImageAndStoreDefault();
        if (playerImage != null && specialAvatarSprite != null)
        {
            playerImage.sprite = specialAvatarSprite;
            PlayerPrefs.SetInt(SPECIAL_AVATAR_PREFS, 1);
            PlayerPrefs.Save();
            Debug.Log("Особый аватар применён и сохранён в PlayerPrefs");
        }
    }

    public void SyncAvatarWithDatabase()
    {
        if (PlayerData.Instance == null)
        {
            Debug.LogError("PlayerData.Instance недоступен для синхронизации аватара");
            return;
        }

        bool isPurchased = PlayerData.Instance.purchasedItems != null &&
                           PlayerData.Instance.purchasedItems.Contains(AVATAR);

        if (isPurchased)
        {
            if (!IsAvatarPurchased) IsAvatarPurchased = true;
            ApplyAvatarChange();
            Debug.Log("Синхронизация с БД: особый аватар куплен, применён.");
        }
        else
        {
            if (playerImage != null && defaultAvatarSprite != null)
                playerImage.sprite = defaultAvatarSprite;
            if (PlayerPrefs.HasKey(SPECIAL_AVATAR_PREFS))
            {
                PlayerPrefs.DeleteKey(SPECIAL_AVATAR_PREFS);
                PlayerPrefs.Save();
            }
            IsAvatarPurchased = false;
            Debug.Log("Синхронизация с БД: аватар не куплен, стандартный спрайт восстановлен.");
        }
    }

    public void ClearAvatarPrefs()
    {
        PlayerPrefs.DeleteKey(SPECIAL_AVATAR_PREFS);
        PlayerPrefs.Save();
        Debug.Log("Сохранение особого аватара в PlayerPrefs сброшено.");
    }

    public Sprite GetSpecialAvatarSprite() => specialAvatarSprite;
}