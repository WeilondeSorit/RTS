using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float moveSensitivity = 7.0f;      // Для клавиатуры/мыши
    public float touchSensitivity = 0.1f;     // Базовая (устаревшая), см. ниже

    [Header("Touch Settings")]
    [Tooltip("Множитель скорости для тач-управления (не зависит от Time.deltaTime)")]
    public float touchMoveMultiplier = 0.4f;  // Рекомендуемо: 0.5 - 1.5

    [Tooltip("Инвертировать управление на тач-устройствах (как скролл на телефоне)")]
    public bool invertTouchControls = true;

    [Tooltip("Доп. ускорение при быстром свайпе (динамическое)")]
    public bool enableTouchAcceleration = true;
    [Tooltip("Множитель ускорения при быстром движении")]
    public float accelerationFactor = 1.5f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2.0f;
    public float minZoom = 2.0f;
    public float maxZoom = 20.0f;
    public bool usePerspectiveZoom = false;

    [Header("Map Boundaries - Auto")]
    [Tooltip("Объект карты для автоопределения границ. Если не задан — используются ручные настройки ниже.")]
    public Transform mapObject;

    [Tooltip("Дополнительный отступ от краёв карты (в единицах сцены)")]
    public float boundaryPadding = 5f;

    [Tooltip("Учитывать ли вращение карты при расчёте границ")]
    public bool accountForRotation = false;

    [Header("Map Boundaries - Manual Fallback")]
    [Tooltip("Используется, если mapObject не назначен")]
    public float mapSize = 100f;
    [Tooltip("Используется, если mapObject не назначен")]
    public Vector2 mapCenter = Vector2.zero;

    [Header("Platform Specific")]
    public bool useTouchControls = false;

    // Внутренние поля
    private Vector3 lastTouchPosition;
    private Vector2 lastTouchDelta; // Для акселерации
    private bool isDragging = false;
    private Camera cam;
    private float currentZoom;

    // Кэшированные границы
    private bool hasAutoBounds = false;
    private float autoMinX, autoMaxX, autoMinZ, autoMaxZ;

    private void Start()
    {
        useTouchControls = Application.isMobilePlatform;
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError("CameraController requires a Camera component!");
            return;
        }

        currentZoom = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
        TryCalculateAutoBounds();
        MoveCameraToBase();
        ClampCameraToBoundaries();
    }

    // ==================== AUTO BOUNDS (без изменений) ====================

    private void TryCalculateAutoBounds()
    {
        if (mapObject == null) { hasAutoBounds = false; return; }

        Bounds? bounds = null;
        var collider = mapObject.GetComponent<Collider>();
        if (collider != null && collider.enabled) bounds = collider.bounds;

        if (bounds == null)
        {
            var renderer = mapObject.GetComponent<Renderer>();
            if (renderer != null && renderer.enabled) bounds = renderer.bounds;
        }

        if (bounds == null) bounds = CalculateBoundsFromChildren(mapObject);

        if (bounds.HasValue)
        {
            float padding = boundaryPadding;
            if (accountForRotation && mapObject.rotation != Quaternion.identity)
            {
                var localBounds = GetLocalBounds(mapObject);
                autoMinX = mapObject.position.x +5 + localBounds.min.x - padding;
                autoMaxX = mapObject.position.x -5 + localBounds.max.x + padding;
                autoMinZ = mapObject.position.z + 15 + localBounds.min.z - padding;
                autoMaxZ = mapObject.position.z + 15 + localBounds.max.z + padding;
            }
            else
            {
                autoMinX = bounds.Value.min.x + 5 + padding;
                autoMaxX = bounds.Value.max.x -5 - padding;
                autoMinZ = bounds.Value.min.z +15 + padding;
                autoMaxZ = bounds.Value.max.z +15 - padding;
            }
            hasAutoBounds = true;
        }
        else hasAutoBounds = false;
    }

    private Bounds? CalculateBoundsFromChildren(Transform parent)
    {
        Bounds? combinedBounds = null;
        foreach (Transform child in parent)
        {
            Bounds? childBounds = child.GetComponent<Collider>()?.enabled == true
                ? child.GetComponent<Collider>().bounds
                : child.GetComponent<Renderer>()?.enabled == true
                    ? child.GetComponent<Renderer>().bounds : null;

            if (childBounds.HasValue)
            {
                combinedBounds = combinedBounds.HasValue ? CombineBounds(combinedBounds.Value, childBounds.Value) : childBounds;
            }
            var recursive = CalculateBoundsFromChildren(child);
            if (recursive.HasValue)
            {
                combinedBounds = combinedBounds.HasValue ? CombineBounds(combinedBounds.Value, recursive.Value) : recursive;
            }
        }
        return combinedBounds;
    }

    private Bounds CombineBounds(Bounds a, Bounds b) { a.Encapsulate(b); return a; }

    private Bounds GetLocalBounds(Transform obj)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null && renderer.enabled)
        {
            var worldBounds = renderer.bounds;
            Vector3 localCenter = obj.InverseTransformPoint(worldBounds.center);
            Bounds localBounds = new Bounds(localCenter, Vector3.zero);
            foreach (var corner in GetBoundsCorners(worldBounds))
                localBounds.Encapsulate(obj.InverseTransformPoint(corner));
            return localBounds;
        }
        return new Bounds(Vector3.zero, obj.localScale);
    }

    private Vector3[] GetBoundsCorners(Bounds bounds)
    {
        Vector3 c = bounds.center, e = bounds.extents;
        return new[]
        {
            new Vector3(c.x+e.x, c.y+e.y, c.z+e.z), new Vector3(c.x+e.x, c.y+e.y, c.z-e.z),
            new Vector3(c.x+e.x, c.y-e.y, c.z+e.z), new Vector3(c.x+e.x, c.y-e.y, c.z-e.z),
            new Vector3(c.x-e.x, c.y+e.y, c.z+e.z), new Vector3(c.x-e.x, c.y+e.y, c.z-e.z),
            new Vector3(c.x-e.x, c.y-e.y, c.z+e.z), new Vector3(c.x-e.x, c.y-e.y, c.z-e.z)
        };
    }

    // ==================== CAMERA MOVEMENT ====================

    private void MoveCameraToBase()
    {
        GameObject baseObject = GameObject.FindGameObjectWithTag("Base");
        Vector3 targetPos = transform.position;

        if (baseObject != null)
        {
            targetPos.x = baseObject.transform.position.x;
            targetPos.z = baseObject.transform.position.z;
        }
        else
        {
            targetPos.x = hasAutoBounds ? (autoMinX + autoMaxX) / 2 : mapCenter.x;
            targetPos.z = hasAutoBounds ? (autoMinZ + autoMaxZ) / 2 : mapCenter.y;
        }
        transform.position = ClampToBoundaries(targetPos);
    }

    private void Update()
    {
        if (useTouchControls) { HandleTouchInput(); HandlePinchZoom(); }
        else { HandleKeyboardInput(); HandleMouseWheelZoom(); }
    }

    // ==================== ZOOM (без изменений) ====================

    private void HandlePinchZoom()
    {
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0), t1 = Input.GetTouch(1);
            Vector2 p0 = t0.position - t0.deltaPosition, p1 = t1.position - t1.deltaPosition;
            float prevMag = (p0 - p1).magnitude, currMag = (t0.position - t1.position).magnitude;
            ApplyZoom((prevMag - currMag) * zoomSpeed * 0.01f);
        }
    }

    private void HandleMouseWheelZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0) ApplyZoom(scroll * zoomSpeed * (usePerspectiveZoom ? -1 : 1));
    }

    private void ApplyZoom(float zoomAmount)
    {
        if (cam.orthographic && !usePerspectiveZoom)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + zoomAmount, minZoom, maxZoom);
            currentZoom = cam.orthographicSize;
        }
        else
        {
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView + zoomAmount, minZoom, maxZoom);
            currentZoom = cam.fieldOfView;
        }
        ClampCameraToBoundaries();
    }

    // ==================== INPUT HANDLERS ====================

    private void HandleKeyboardInput()
    {
        float movX = Input.GetAxis("Horizontal") * Time.deltaTime * moveSensitivity;
        float movZ = Input.GetAxis("Vertical") * Time.deltaTime * moveSensitivity;
        Vector3 target = transform.position + new Vector3(-movX, 0, -movZ);
        transform.position = ClampToBoundaries(target);
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
                    lastTouchDelta = Vector2.zero;
                    isDragging = true;
                    break;
                case TouchPhase.Moved:
                    if (isDragging)
                    {
                        Vector2 delta = touch.position - (Vector2)lastTouchPosition;
                        MoveCameraTouch(delta);
                        lastTouchPosition = touch.position;
                        lastTouchDelta = delta; // Для акселерации
                    }
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isDragging = false;
                    lastTouchDelta = Vector2.zero;
                    break;
            }
        }
        else HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0)) { lastTouchPosition = Input.mousePosition; isDragging = true; }
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - (Vector2)lastTouchPosition;
            MoveCamera(delta); // Обычное движение для мыши
            lastTouchPosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0)) isDragging = false;
    }

    /// <summary>
    /// Движение камеры для МЫШИ (старое поведение, без инверсии)
    /// </summary>
    private void MoveCamera(Vector2 delta)
    {
        float movX = delta.x * touchSensitivity * Time.deltaTime;
        float movZ = delta.y * touchSensitivity * Time.deltaTime;
        Vector3 target = transform.position + new Vector3(-movX, 0, -movZ);
        transform.position = ClampToBoundaries(target);
    }

    /// <summary>
    /// Движение камеры для ТОЧА (быстрое + инверсное + с акселерацией)
    /// </summary>
    private void MoveCameraTouch(Vector2 delta)
    {
        // 🔁 Инверсия: если включена, меняем знак дельты
        if (invertTouchControls) delta = -delta;

        // ⚡ Ускорение: чем больше дельта за кадр, тем быстрее движение
        float speedMultiplier = touchMoveMultiplier;
        if (enableTouchAcceleration)
        {
            float deltaMagnitude = delta.magnitude;
            // Если свайп быстрый (>50 пикселей), ускоряем
            if (deltaMagnitude > 50f)
                speedMultiplier *= Mathf.Lerp(1f, accelerationFactor, Mathf.InverseLerp(50f, 200f, deltaMagnitude));
        }

        // 🎮 Применяем движение: 
        // - НЕ используем Time.deltaTime, т.к. Input.touch.deltaPosition уже в пикселях/кадр
        // - Инвертируем ось: свайп вверх (delta.y > 0) = камера вниз (-Z)
        float movX = delta.x * speedMultiplier;
        float movZ = delta.y * speedMultiplier;

        Vector3 target = transform.position + new Vector3(-movX, 0, -movZ);
        transform.position = ClampToBoundaries(target);
    }

    // ==================== BOUNDARIES (без изменений) ====================

    private float GetMinX() => hasAutoBounds ? autoMinX : mapCenter.x - mapSize / 2 + boundaryPadding;
    private float GetMaxX() => hasAutoBounds ? autoMaxX : mapCenter.x + mapSize / 2 - boundaryPadding;
    private float GetMinZ() => hasAutoBounds ? autoMinZ : mapCenter.y - mapSize / 2 + boundaryPadding;
    private float GetMaxZ() => hasAutoBounds ? autoMaxZ : mapCenter.y + mapSize / 2 - boundaryPadding;

    private float GetZoomOffset()
    {
        if (cam.orthographic && !usePerspectiveZoom)
            return cam.orthographicSize * cam.aspect * 0.5f;
        else
        {
            float height = Mathf.Abs(transform.position.y);
            return height * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * cam.aspect * 0.5f;
        }
    }

    private Vector3 ClampToBoundaries(Vector3 position)
    {
        float offset = GetZoomOffset();
        return new Vector3(
            Mathf.Clamp(position.x, GetMinX() + offset, GetMaxX() - offset),
            position.y,
            Mathf.Clamp(position.z, GetMinZ() + offset, GetMaxZ() - offset)
        );
    }

    public void ClampCameraToBoundaries() => transform.position = ClampToBoundaries(transform.position);
    public void RecalculateBounds() => TryCalculateAutoBounds();
    public void SetControlType(bool useTouch) => useTouchControls = useTouch;

    // ==================== DEBUG ====================

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        float minX = hasAutoBounds ? autoMinX : mapCenter.x - mapSize / 2 + boundaryPadding;
        float maxX = hasAutoBounds ? autoMaxX : mapCenter.x + mapSize / 2 - boundaryPadding;
        float minZ = hasAutoBounds ? autoMinZ : mapCenter.y - mapSize / 2 + boundaryPadding;
        float maxZ = hasAutoBounds ? autoMaxZ : mapCenter.y + mapSize / 2 - boundaryPadding;

        Gizmos.color = hasAutoBounds ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(new Vector3((minX + maxX) / 2, 0.1f, (minZ + maxZ) / 2), new Vector3(maxX - minX, 0.2f, maxZ - minZ));

        if (cam != null)
        {
            float offset = GetZoomOffset();
            Gizmos.color = new Color(1, 0.6f, 0, 0.4f);
            Gizmos.DrawWireCube(
                new Vector3((minX + maxX) / 2, 0.2f, (minZ + maxZ) / 2),
                new Vector3(Mathf.Max(0, maxX - minX - offset * 2), 0.4f, Mathf.Max(0, maxZ - minZ - offset * 2))
            );
        }
    }
}