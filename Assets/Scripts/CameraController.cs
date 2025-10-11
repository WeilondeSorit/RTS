using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float moveSensitivity = 7.0f;
    public float touchSensitivity = 0.1f;

    [Header("Platform Specific")]
    public bool useTouchControls = false;

    private Vector3 lastTouchPosition;
    private bool isDragging = false;

    private void Start()
    {
        // Автоматическое определение типа управления
        useTouchControls = Application.isMobilePlatform;
    }

    private void Update()
    {
        if (useTouchControls)
        {
            HandleTouchInput();
        }
        else
        {
            HandleKeyboardInput();
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