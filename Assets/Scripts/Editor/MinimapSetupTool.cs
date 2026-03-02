#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Tool dùng để tạo nhanh Minimap trong Unity Editor.
/// Bấm vào Tools -> Thêm Minimap để tự động sinh ra UI và Camera.
/// </summary>
public class MinimapSetupTool : EditorWindow
{
    [MenuItem("Tools/Thêm Minimap Tự Động")]
    public static void ShowWindow()
    {
        GetWindow<MinimapSetupTool>("Minimap Setup").Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Công Cụ Tạo Minimap", EditorStyles.boldLabel);
        GUILayout.Space(10);
        if (GUILayout.Button("Tạo Minimap Mới", GUILayout.Height(40)))
        {
            SetupMinimap();
        }
    }

    void SetupMinimap()
    {
        // 1. Tạo Render Texture
        string rtPath = "Assets/MinimapRT.renderTexture";
        RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (rt == null)
        {
            rt = new RenderTexture(512, 512, 16);
            rt.name = "MinimapRT";
            AssetDatabase.CreateAsset(rt, rtPath);
            AssetDatabase.SaveAssets();
        }

        // 2. Tạo Minimap Camera
        var existingCam = GameObject.Find("MinimapCamera");
        if (existingCam != null) DestroyImmediate(existingCam);

        GameObject camObj = new GameObject("MinimapCamera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 40f; // Vùng nhìn 40x40
        cam.targetTexture = rt;
        
        // Loại bỏ lớp UI khỏi Camera này để tránh lỗi hiển thị UI lên Minimap
        cam.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));

        var mmc = camObj.AddComponent<MinimapCamera>();
        mmc.height = 90f;

        Undo.RegisterCreatedObjectUndo(camObj, "Create Minimap Camera");

        // 3. Xử lý UI Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }

        // 4. Tạo Minimap UI (Góc trên bên phải)
        var existingUI = GameObject.Find("MinimapUI");
        if (existingUI != null) DestroyImmediate(existingUI);

        GameObject miniUI = new GameObject("MinimapUI", typeof(RectTransform));
        miniUI.transform.SetParent(canvas.transform, false);
        var miniRT = miniUI.GetComponent<RectTransform>();
        miniRT.anchorMin = new Vector2(1, 1);
        miniRT.anchorMax = new Vector2(1, 1);
        miniRT.pivot = new Vector2(1, 1);
        miniRT.anchoredPosition = new Vector2(-20, -20);
        miniRT.sizeDelta = new Vector2(200, 200);

        // Nền ngoài (Mặt nạ cắt hình tròn)
        var borderObj = new GameObject("BorderMask", typeof(RectTransform));
        borderObj.transform.SetParent(miniUI.transform, false);
        var bRT = borderObj.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.sizeDelta = Vector2.zero;
        
        var imgMask = borderObj.AddComponent<Image>();
        imgMask.color = new Color(0, 0, 0, 0.4f); // Nền đen mờ 
        var mask = borderObj.AddComponent<Mask>();
        mask.showMaskGraphic = true; 

        // Màng kết xuất Render Texture
        var mapObj = new GameObject("MapImage", typeof(RectTransform));
        mapObj.transform.SetParent(borderObj.transform, false);
        var mRT = mapObj.GetComponent<RectTransform>();
        mRT.anchorMin = Vector2.zero; mRT.anchorMax = Vector2.one;
        mRT.sizeDelta = new Vector2(-4, -4); // Cắt vào một tí tạo khoảng trống
        var mapRaw = mapObj.AddComponent<RawImage>();
        mapRaw.texture = rt;

        // Player Icon ở giữa
        var pIconObj = new GameObject("PlayerIcon", typeof(RectTransform));
        pIconObj.transform.SetParent(borderObj.transform, false);
        var pIRT = pIconObj.GetComponent<RectTransform>();
        pIRT.anchorMin = new Vector2(0.5f, 0.5f);
        pIRT.anchorMax = new Vector2(0.5f, 0.5f);
        pIRT.sizeDelta = new Vector2(16, 16);
        var pImg = pIconObj.AddComponent<Image>();
        pImg.color = Color.green; // Chấm xanh lá

        // Vòng viền trang trí bo bên ngoài cùng (Outline)
        var ringObj = new GameObject("OuterRing", typeof(RectTransform));
        ringObj.transform.SetParent(miniUI.transform, false);
        var ringRT = ringObj.GetComponent<RectTransform>();
        ringRT.anchorMin = Vector2.zero; ringRT.anchorMax = Vector2.one;
        ringRT.sizeDelta = new Vector2(6, 6); // To hơn tí xíu để bọc ngoài
        
        var ringImg = ringObj.AddComponent<Image>();
        ringImg.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Màu viền neon xanh dương (Hex #38BDF8)
        
        // Thêm Component Outline để tạo viền rỗng (viền có độ dày 3px)
        // Lưu ý: Outline UI Component dùng chính độ Alpha của Image để kẻ viền
        ringImg.color = new Color(1,1,1,0); // Tàng hình ruột
        var outline = ringObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0.22f, 0.74f, 0.97f, 1f); // Màu viền ngoài
        outline.effectDistance = new Vector2(3, -3);

        Undo.RegisterCreatedObjectUndo(miniUI, "Create Minimap UI");

        Debug.Log("Đã tạo xong Minimap! Vào Canvas -> MinimapUI để kiểm tra.");
    }
}
#endif
