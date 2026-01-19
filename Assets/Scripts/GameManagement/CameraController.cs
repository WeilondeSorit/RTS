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
        // Автоматически определяем тип управления
        useTouchControls = Application.isMobilePlatform;
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError("CameraController requires a Camera component!");
            return;
        }

        // Перемещаем камеру над базой при старте
        MoveCameraToBase();
    }

    private void MoveCameraToBase()
    {
        GameObject baseObject = GameObject.FindGameObjectWithTag("Base");
        
        if (baseObject != null)
        {
            // Получаем позицию базы
            Vector3 basePosition = baseObject.transform.position;
            
            // Перемещаем камеру над базой, сохраняя текущую высоту
            transform.position = new Vector3(basePosition.x, transform.position.y, basePosition.z);
        }
        else
        {
            Debug.LogWarning("Base object with tag 'Base' not found! Camera will remain at default position.");
        }
    }

    private void Update()
    {
        if (useTouchControls)
        {
            HandleTouchInput();
            HandlePinchZoom();
        }
        else
        {
            HandleKeyboardInput();
            HandleMouseWheelZoom();
        }
    }

    private void HandlePinchZoom()
    {
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            ApplyZoom(deltaMagnitudeDiff * zoomSpeed * 0.01f);
        }
    }

    private void HandleMouseWheelZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            float zoomAmount = scroll * zoomSpeed;
            if (usePerspectiveZoom) zoomAmount *= -1;

            ApplyZoom(zoomAmount);
        }
    }

    private void ApplyZoom(float zoomAmount)
    {
        if (cam.orthographic && !usePerspectiveZoom)
        {
            cam.orthographicSize += zoomAmount;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
        else
        {
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

    public void SetControlType(bool useTouch)
    {
        useTouchControls = useTouch;
    }
}