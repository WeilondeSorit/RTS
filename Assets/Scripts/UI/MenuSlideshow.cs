using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuSlideshow : MonoBehaviour
{
    [Header("Настройки слайд-шоу")]
    [SerializeField] private List<Sprite> backgroundImages; // Список изображений для слайд-шоу
    [SerializeField] private float fadeDuration = 1.5f; // Длительность плавного перехода
    [SerializeField] private float displayDuration = 3f; // Время показа каждого изображения
    [SerializeField] private bool shuffleImages = false; // Перемешивать ли изображения

    [Header("Настройки UI")]
    [SerializeField] private Image currentImage; // Текущее изображение (UI Image)
    [SerializeField] private Image nextImage; // Следующее изображение (UI Image)

    private Queue<Sprite> imageQueue = new Queue<Sprite>();
    private List<Sprite> shuffledList = new List<Sprite>();
    private int currentIndex = 0;
    private bool isTransitioning = false;

    private void Start()
    {
        // Проверка наличия изображений
        if (backgroundImages == null || backgroundImages.Count == 0)
        {
            Debug.LogError("Список изображений пуст! Добавьте изображения в инспекторе.");
            return;
        }

        // Инициализация изображений
        if (currentImage == null || nextImage == null)
        {
            Debug.LogError("Назначьте оба UI Image компонента в инспекторе!");
            return;
        }

        // Настройка начального состояния
        currentImage.color = new Color(1, 1, 1, 1);
        nextImage.color = new Color(1, 1, 1, 0);

        // Подготовка очереди изображений
        PrepareImageQueue();

        // Установка первого изображения
        if (imageQueue.Count > 0)
        {
            currentImage.sprite = imageQueue.Dequeue();
        }

        // Запуск слайд-шоу
        StartCoroutine(SlideshowRoutine());
    }

    private void PrepareImageQueue()
    {
        if (shuffleImages)
        {
            // Создаем перемешанную копию списка
            shuffledList = new List<Sprite>(backgroundImages);
            ShuffleList(shuffledList);

            foreach (var sprite in shuffledList)
            {
                imageQueue.Enqueue(sprite);
            }
        }
        else
        {
            foreach (var sprite in backgroundImages)
            {
                imageQueue.Enqueue(sprite);
            }
        }
    }

    private System.Collections.IEnumerator SlideshowRoutine()
    {
        while (true)
        {
            // Ждем указанное время перед переходом
            yield return new WaitForSeconds(displayDuration);

            // Если уже в процессе перехода, ждем
            if (isTransitioning) continue;

            // Начинаем переход к следующему изображению
            yield return StartCoroutine(TransitionToNextImage());
        }
    }

    private System.Collections.IEnumerator TransitionToNextImage()
    {
        isTransitioning = true;

        // Подготавливаем следующее изображение
        if (imageQueue.Count == 0)
        {
            // Если очередь пуста, перезаполняем ее
            PrepareImageQueue();
        }

        nextImage.sprite = imageQueue.Dequeue();

        float elapsedTime = 0f;

        // Плавное затемнение текущего изображения и появление следующего
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Используем unscaledDeltaTime для работы в меню при паузе
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);

            // Текущее изображение исчезает
            currentImage.color = new Color(1, 1, 1, 1 - alpha);
            // Следующее изображение появляется
            nextImage.color = new Color(1, 1, 1, alpha);

            yield return null;
        }

        // Завершение перехода
        // Меняем изображения местами
        Image temp = currentImage;
        currentImage = nextImage;
        nextImage = temp;

        // Сбрасываем прозрачность
        currentImage.color = new Color(1, 1, 1, 1);
        nextImage.color = new Color(1, 1, 1, 0);

        isTransitioning = false;
    }

    // Метод для перемешивания списка
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // Метод для смены изображения по требованию (опционально)
    public void SkipToNextImage()
    {
        if (!isTransitioning)
        {
            StopAllCoroutines();
            StartCoroutine(TransitionToNextImage());
            StartCoroutine(SlideshowRoutine());
        }
    }

    // Метод для добавления изображений во время выполнения
    public void AddImage(Sprite newImage)
    {
        backgroundImages.Add(newImage);
        imageQueue.Enqueue(newImage);
    }

    // Метод для очистки всех изображений
    public void ClearAllImages()
    {
        StopAllCoroutines();
        backgroundImages.Clear();
        imageQueue.Clear();
        shuffledList.Clear();
    }
}