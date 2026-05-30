using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SimpleLoadingManager : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text loadingText;

    private static string sceneToLoad = "";
    private static bool shouldLoad = false;

    void Start()
    {
        // Если ничего не нужно загружать - выходим
        if (!shouldLoad || string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("[Loading] Нет сцены для загрузки!");
            if (loadingText != null) loadingText.text = "Ошибка загрузки";
            return;
        }

        // Сбрасываем флаги
        string targetScene = sceneToLoad;
        sceneToLoad = "";
        shouldLoad = false;

        // Запускаем загрузку
        StartCoroutine(LoadYourScene(targetScene));
    }

    IEnumerator LoadYourScene(string sceneName)
    {
        Debug.Log("[Loading] Начинаем загрузку: " + sceneName);

        // Ждем 2 кадра для инициализации UI
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // Обновляем UI
        if (loadingText != null) loadingText.text = "Загрузка...";
        if (progressBar != null) progressBar.value = 0;

        // Загружаем сцену
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = true; // Разрешаем активацию сразу

        // Ждем завершения
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Min(asyncLoad.progress / 0.9f, 1f);

            if (progressBar != null) progressBar.value = progress;
            if (loadingText != null)
                loadingText.text = "Загрузка: " + Mathf.RoundToInt(progress * 100) + "%";

            yield return null;
        }

        Debug.Log("[Loading] Завершено!");
    }

    // ПУБЛИЧНЫЙ МЕТОД
    public static void LoadSceneWithLoading(string sceneName)
    {
        Debug.Log("[Loading] Запрос загрузки: " + sceneName);
        sceneToLoad = sceneName;
        shouldLoad = true;
        SceneManager.LoadScene("LoadingScene");
    }
}