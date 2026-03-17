using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Camera dành riêng cho Minimap có hỗ trợ phóng to toàn màn hình và đặt Waypoint.
/// </summary>
public class MinimapCamera : MonoBehaviour
{
    [Header("Target & Camera")]
    public Transform target;
    public float height = 90f;
    public bool rotateWithTarget = false;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.M;
    public float holdTimeToHide = 0.5f;

    [Header("UI References")]
    public GameObject minimapUI;
    private RectTransform minimapRect;
    private RawImage mapRawImage;

    [Header("Waypoint Settings")]
    private GameObject currentWaypoint;
    public float waypointClearDistance = 3f;

    // Trạng thái Minimap
    private enum MapState { Small, Fullscreen, Hidden }
    private MapState currentState = MapState.Small;

    // Dữ liệu cho Input
    private float keyPressTime = 0f;
    private bool isKeyHeld = false;
    private bool stateChangedByHold = false;

    // Dữ liệu UI Small map
    private Vector2 smallSize;
    private Vector2 smallAnchorMin;
    private Vector2 smallAnchorMax;
    private Vector2 smallPivot;
    private Vector2 smallPosition;

    [Header("Panning Settings")]
    public float panSpeed = 50f;
    private Vector3 panOffset = Vector3.zero;
    private bool isPanning = false;
    private Vector2 lastMousePosition;

    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 20f;
    public float maxZoom = 250f;
    private float defaultOrthoSize = 40f;

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (minimapUI == null)
        {
            minimapUI = GameObject.Find("MinimapUI");
        }

        if (minimapUI != null)
        {
            minimapRect = minimapUI.GetComponent<RectTransform>();
            Transform mapObj = minimapUI.transform.Find("BorderMask/MapImage");
            if (mapObj != null) mapRawImage = mapObj.GetComponent<RawImage>();

            // Lưu cài đặt lúc đầu (Small)
            smallSize = minimapRect.sizeDelta;
            smallAnchorMin = minimapRect.anchorMin;
            smallAnchorMax = minimapRect.anchorMax;
            smallPivot = minimapRect.pivot;
            smallPosition = minimapRect.anchoredPosition;
        }

        // Culling Mask Setup
        Camera mainCam = Camera.main;
        Camera miniCam = GetComponent<Camera>();

        int minimapLayer = LayerMask.NameToLayer("MinimapIcon");
        if (minimapLayer == -1) minimapLayer = 8; // fallback

        if (mainCam != null) mainCam.cullingMask &= ~(1 << minimapLayer);
        if (miniCam != null)
        {
            miniCam.cullingMask |= (1 << minimapLayer);
            defaultOrthoSize = miniCam.orthographicSize;
        }
    }

    void Update()
    {
        HandleInput();
        HandleWaypointLogic();
        HandlePanningLogic();
        HandleZoomLogic();
        CheckWaypointDistance();
        HandlePlayerIconFix();
    }

    // Tự động fix kích thước và MÀU SẮC của player icon trên minimap
    void HandlePlayerIconFix()
    {
        if (minimapUI != null)
        {
            Transform pIcon = minimapUI.transform.Find("BorderMask/PlayerIcon");
            if (pIcon != null)
            {
                // Ẩn khi mở bản đồ lớn
                if (currentState == MapState.Fullscreen)
                {
                    pIcon.gameObject.SetActive(false);
                }
                else
                {
                    pIcon.gameObject.SetActive(true);
                    
                    RectTransform pRect = pIcon.GetComponent<RectTransform>();
                    if (pRect.sizeDelta.x > 50f || pRect.sizeDelta.y > 50f)
                    {
                        pRect.sizeDelta = new Vector2(30f, 30f); 
                    }
                    
                    // Ép màu luôn là XANH LÁ để tránh bị đổi sang xanh dương
                    Image pImg = pIcon.GetComponent<Image>();
                    if (pImg != null && pImg.color != Color.green)
                    {
                        pImg.color = Color.green;
                    }
                }
            }
        }
    }

    void HandleInput()
    {
        bool keyDown = false;
        bool keyUp = false;
        bool keyHold = false;

#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            keyDown = kb.mKey.wasPressedThisFrame;
            keyUp = kb.mKey.wasReleasedThisFrame;
            keyHold = kb.mKey.isPressed;
        }
#else
        keyDown = Input.GetKeyDown(toggleKey);
        keyUp = Input.GetKeyUp(toggleKey);
        keyHold = Input.GetKey(toggleKey);
#endif

        if (keyDown)
        {
            keyPressTime = Time.time;
            isKeyHeld = true;
            stateChangedByHold = false;
        }

        if (keyHold && isKeyHeld)
        {
            if (!stateChangedByHold && (Time.time - keyPressTime) >= holdTimeToHide)
            {
                stateChangedByHold = true;
                if (currentState == MapState.Hidden)
                    SetMapState(MapState.Small);
                else
                    SetMapState(MapState.Hidden);
            }
        }

        bool escDown = false;
#if ENABLE_INPUT_SYSTEM
        var kbCurrent = UnityEngine.InputSystem.Keyboard.current;
        if (kbCurrent != null) escDown = kbCurrent.escapeKey.wasPressedThisFrame;
#else
        escDown = Input.GetKeyDown(KeyCode.Escape);
#endif

        if (escDown && currentState == MapState.Fullscreen)
        {
            SetMapState(MapState.Small);
        }

        if (keyUp)
        {
            isKeyHeld = false;
            // Bấm nhanh -> Đổi giữa Small và Fullscreen
            if (!stateChangedByHold)
            {
                if (currentState == MapState.Small || currentState == MapState.Hidden)
                {
                    SetMapState(MapState.Fullscreen);
                }
                else if (currentState == MapState.Fullscreen)
                {
                    SetMapState(MapState.Small);
                }
            }
        }
    }

    void SetMapState(MapState newState)
    {
        currentState = newState;
        if (minimapUI == null || minimapRect == null) return;

        switch (currentState)
        {
            case MapState.Hidden:
                minimapUI.SetActive(false);
                break;
            case MapState.Small:
                minimapUI.SetActive(true);
                minimapRect.anchorMin = smallAnchorMin;
                minimapRect.anchorMax = smallAnchorMax;
                minimapRect.pivot = smallPivot;
                minimapRect.anchoredPosition = smallPosition;
                minimapRect.sizeDelta = smallSize;
                panOffset = Vector3.zero; // Reset vị trí pan
                Camera mCam = GetComponent<Camera>();
                if (mCam != null) mCam.orthographicSize = defaultOrthoSize;
                SetCursorState(false);
                break;
            case MapState.Fullscreen:
                minimapUI.SetActive(true);
                minimapRect.anchorMin = new Vector2(0.5f, 0.5f);
                minimapRect.anchorMax = new Vector2(0.5f, 0.5f);
                minimapRect.pivot = new Vector2(0.5f, 0.5f);
                minimapRect.anchoredPosition = Vector2.zero;
                // Lấy kích thước màn hình để tạo hình vuông lớn
                float size = Mathf.Min(Screen.width, Screen.height) * 0.85f;
                minimapRect.sizeDelta = new Vector2(size, size);
                SetCursorState(true);
                break;
        }
    }

    void SetCursorState(bool isMapOpen)
    {
        if (isMapOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void HandleWaypointLogic()
    {
        // Phải đang mở Fullscreen map thì mới cho click điểm
        if (currentState != MapState.Fullscreen || mapRawImage == null) return;

        bool mouseClicked = false;
        Vector2 mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            mouseClicked = true;
            mousePos = mouse.position.ReadValue();
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            mouseClicked = true;
            mousePos = Input.mousePosition;
        }
#endif

        if (mouseClicked)
        {
            // Kiểm tra click có nằm trong khung render Map không
            RectTransform mapRT = mapRawImage.rectTransform;
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRT, mousePos, null, out localPoint))
            {
                if (mapRT.rect.Contains(localPoint))
                {
                    // Tính toán tọa độ chuẩn hóa (0.0 -> 1.0) từ góc dưới bên trái
                    float normalizedX = (localPoint.x - mapRT.rect.xMin) / mapRT.rect.width;
                    float normalizedY = (localPoint.y - mapRT.rect.yMin) / mapRT.rect.height;

                    PlaceWaypoint(normalizedX, normalizedY);
                }
            }
        }
    }

    void PlaceWaypoint(float normX, float normY)
    {
        Camera miniCam = GetComponent<Camera>();
        
        // Tạo tia ray từ Viewport (0 -> 1) 
        Vector3 viewportPoint = new Vector3(normX, normY, 0f);
        Ray ray = miniCam.ViewportPointToRay(viewportPoint);
        
        // Giao cắt với mặt đất World để tìm toạ độ thực
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, target != null ? target.position.y : 0, 0));
        
        Vector3 worldPos = Vector3.zero;
        if (groundPlane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
        }
        else
        {
            worldPos = transform.position + ray.direction * height;
        }

        // Xoá cột cũ và tạo cột mới (đảm bảo màu luôn đúng)
        if (currentWaypoint != null)
        {
            Destroy(currentWaypoint);
            currentWaypoint = null;
        }
        currentWaypoint = CreateWaypointPillar();
        currentWaypoint.SetActive(true);
        currentWaypoint.transform.position = worldPos;
    }

    private GameObject CreateWaypointPillar()
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "ActiveWaypoint";
        Destroy(pillar.GetComponent<Collider>());
        
        // Cột cao gấp 4 lần so với trước (20 -> 80), mảnh hơn một chút
        pillar.transform.localScale = new Vector3(1.2f, 80f, 1.2f);
        
        Material mat = new Material(Shader.Find("Standard"));
        // Màu vàng nhạt ấm (hơi ngả trắng), trong suốt nhẹ
        Color lightYellow = new Color(1f, 0.95f, 0.6f, 0.45f);
        mat.color = lightYellow;
        
        // Bật Transparent mode
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        
        // Bật Emission để cột phát sáng vàng, dễ thấy từ xa
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.92f, 0.5f, 1f) * 1.5f);
        
        pillar.GetComponent<MeshRenderer>().sharedMaterial = mat;

        return pillar;
    }

    void CheckWaypointDistance()
    {
        if (currentWaypoint != null && currentWaypoint.activeSelf && target != null)
        {
            // Tính toán khoảng cách trên mặt phẳng 2D (trục X,Z) để bỏ qua độ cao
            Vector2 p1 = new Vector2(target.position.x, target.position.z);
            Vector2 p2 = new Vector2(currentWaypoint.transform.position.x, currentWaypoint.transform.position.z);
            
            if (Vector2.Distance(p1, p2) <= waypointClearDistance)
            {
                // Người chơi đã tới nơi -> xóa cột sáng
                currentWaypoint.SetActive(false);
            }
        }
    }

    void HandlePanningLogic()
    {
        if (currentState != MapState.Fullscreen) return;

        bool rightMouseDown = false;
        bool rightMouseHold = false;
        bool rightMouseUp = false;
        Vector2 mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            rightMouseDown = mouse.rightButton.wasPressedThisFrame;
            rightMouseHold = mouse.rightButton.isPressed;
            rightMouseUp = mouse.rightButton.wasReleasedThisFrame;
            mousePos = mouse.position.ReadValue();
        }
#else
        rightMouseDown = Input.GetMouseButtonDown(1);
        rightMouseHold = Input.GetMouseButton(1);
        rightMouseUp = Input.GetMouseButtonUp(1);
        mousePos = Input.mousePosition;
#endif

        if (rightMouseDown)
        {
            isPanning = true;
            lastMousePosition = mousePos;
        }
        
        if (rightMouseHold && isPanning)
        {
            Vector2 delta = mousePos - lastMousePosition;
            lastMousePosition = mousePos;

            // Tính tỷ lệ di chuyển dựa trên kích thước màn hình
            float panFactorX = delta.x / Screen.width;
            float panFactorY = delta.y / Screen.height;

            Camera miniCam = GetComponent<Camera>();
            float camOrthoSize = miniCam.orthographicSize;
            float camAspect = miniCam.aspect;

            // Di chuyển camera ngược chiều chuột (kéo bản đồ)
            float moveX = -panFactorX * (camOrthoSize * 2f * camAspect) * panSpeed;
            float moveZ = -panFactorY * (camOrthoSize * 2f) * panSpeed;

            panOffset += new Vector3(moveX, 0, moveZ);
        }
        
        if (rightMouseUp)
        {
            isPanning = false;
        }
    }

    void HandleZoomLogic()
    {
        if (currentState != MapState.Fullscreen) return;

        float scrollDelta = 0f;

#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            float y = mouse.scroll.ReadValue().y;
            if (y > 0) scrollDelta = 1f;
            else if (y < 0) scrollDelta = -1f;
        }
#else
        float rawScroll = Input.GetAxis("Mouse ScrollWheel");
        if (rawScroll > 0) scrollDelta = 1f;
        else if (rawScroll < 0) scrollDelta = -1f;
#endif

        if (scrollDelta != 0)
        {
            Camera miniCam = GetComponent<Camera>();
            if (miniCam != null && miniCam.orthographic)
            {
                // Lăn chuột lên (scrollDelta = 1) -> Giảm orthoSize -> Phóng to
                // Lăn chuột xuống (scrollDelta = -1) -> Tăng orthoSize -> Thu nhỏ
                miniCam.orthographicSize -= scrollDelta * zoomSpeed;
                miniCam.orthographicSize = Mathf.Clamp(miniCam.orthographicSize, minZoom, maxZoom);
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 newPosition = target.position;
        newPosition.y += height; // Thêm độ cao camera
        
        if (currentState == MapState.Fullscreen)
        {
            // Áp dụng độ lệch khi kéo bản đồ Fullscreen
            newPosition += panOffset;
        }

        transform.position = newPosition;

        if (rotateWithTarget && currentState != MapState.Fullscreen)
        {
            transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
        }
        else
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
