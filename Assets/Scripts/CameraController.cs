using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float moveSensitivity = 7.0f;
    public float touchSensitivity = 0.1f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2.0f;
    public float minZoom = 2.0f;
    public float maxZoom = 20.0f;
    public bool usePerspectiveZoom = false;

    [Header("Platform Specific")]
    public bool useTouchControls = false;

    private Vector3 lastTouchPosition;
    private bool isDragging = false;
    private Camera cam;

    private void Start()
    {
        // Автоматическое определение типа управления
        useTouchControls = Application.isMobilePlatform;
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError("CameraController requires a Camera component!");
        }
    }

    private void Update()
    {
        if (useTouchControls)
        {
            HandleTouchInput();
            HandlePinchZoom(); // Обработка зума щипком
        }
        else
        {
            HandleKeyboardInput();
            HandleMouseWheelZoom(); // Обработка зума колесиком мыши
        }
    }

    private void HandlePinchZoom()
    {
        // Для зума щипком нужно два касания:cite[2]:cite[8]
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Получаем позиции касаний в предыдущем кадре
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Вычисляем разницу расстояний между касаниями
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Разница в расстоянии (отрицательная = zoom in, положительная = zoom out)
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            // Применяем зум:cite[8]
            ApplyZoom(deltaMagnitudeDiff * zoomSpeed * 0.01f);
        }
    }

    private void HandleMouseWheelZoom()
    {
        // Input.GetAxis("Mouse ScrollWheel") возвращает значение прокрутки колесика:cite[3]:cite[4]
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            // Для перспективной камеры инвертируем направление:cite[3]
            float zoomAmount = scroll * zoomSpeed;
            if (usePerspectiveZoom) zoomAmount *= -1;

            ApplyZoom(zoomAmount);
        }
    }

    private void ApplyZoom(float zoomAmount)
    {
        if (cam.orthographic && !usePerspectiveZoom)
        {
            // Для ортографической камеры изменяем orthographicSize:cite[4]:cite[8]
            cam.orthographicSize += zoomAmount;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
        else
        {
            // Для перспективной камеры изменяем fieldOfView:cite[3]:cite[4]:cite[8]
            cam.fieldOfView += zoomAmount;
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minZoom, maxZoom);
        }
    }


    private void HandleKeyboardInput()
    {
        float movX = Input.GetAxis("Horizontal") * Time.deltaTime * moveSensitivity;
        float movZ = Input.GetAxis("Vertical") * Time.deltaTime * moveSensitivity;

        transform.position += new Vector3(-movX, 0, -movZ);
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    lastTouchPosition = touch.position;
                    isDragging = true;
                    break;

                case TouchPhase.Moved:
                    if (isDragging)
                    {
                        Vector2 delta = touch.position - (Vector2)lastTouchPosition;
                        MoveCamera(delta);
                        lastTouchPosition = touch.position;
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isDragging = false;
                    break;
            }
        }
        else
        {
            // Альтернативная реализация для мыши в редакторе/тестировании
            HandleMouseInput();
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastTouchPosition = Input.mousePosition;
            isDragging = true;
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - (Vector2)lastTouchPosition;
            MoveCamera(delta);
            lastTouchPosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private void MoveCamera(Vector2 delta)
    {
        float movX = delta.x * touchSensitivity * Time.deltaTime;
        float movZ = delta.y * touchSensitivity * Time.deltaTime;

        transform.position += new Vector3(-movX, 0, -movZ);
    }

    // Метод для принудительного переключения типа управления
    public void SetControlType(bool useTouch)
    {
        useTouchControls = useTouch;
    }
}