using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SimpleLoadingManager : MonoBehaviour
{
    public Slider progressBar; // Привяжи в инспекторе
    public Text loadingText;   // Привяжи в инспекторе

    // Статическая переменная для хранения имени цели
    public static string targetSceneName;

    void Start()
    {
        StartCoroutine(LoadSceneCoroutine());
    }

    IEnumerator LoadSceneCoroutine()
    {
        // Даем кадру отрисоваться, чтобы показать UI
        yield return null;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = false; // Не активируем сразу

        while (!asyncLoad.isDone)
        {
            // Прогресс от 0 до 0.9
            float progress = asyncLoad.progress / 0.9f;
            progressBar.value = progress;
            loadingText.text = "Загрузка: " + (int)(progress * 100) + "%";

            // Когда загрузка завершена
            if (asyncLoad.progress >= 0.9f)
            {
                loadingText.text = "Завершение...";
                // АВТОМАТИЧЕСКАЯ активация через 0.5 секунды
                yield return new WaitForSeconds(0.5f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    // Статический метод для вызова перехода из ЛЮБОГО места
    public static void LoadSceneWithLoading(string sceneName)
    {
        targetSceneName = sceneName; // Сохраняем имя цели
        SceneManager.LoadScene("LoadingScene"); // Сразу грузим сцену загрузки
    }
}