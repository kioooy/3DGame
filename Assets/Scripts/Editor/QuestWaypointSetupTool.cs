using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class QuestWaypointSetupTool : EditorWindow
{
    [MenuItem("Window/Quest System/Generate Waypoint UI")]
    public static void ShowWindow()
    {
        GetWindow<QuestWaypointSetupTool>("Quest Waypoint Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tạo hệ thống Mũi tên Chỉ đường (Waypoint)", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Tự động tạo UI", GUILayout.Height(40)))
        {
            CreateWaypointUI();
        }
    }

    public static void CreateWaypointUI()
    {
        QuestWaypointManager existingManager = Object.FindFirstObjectByType<QuestWaypointManager>();
        if (existingManager != null && existingManager.pointerUI != null)
        {
            Debug.Log("Waypoint UI đã có sẵn!");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MainCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Tạo Manager
        GameObject managerObj = existingManager != null ? existingManager.gameObject : new GameObject("QuestWaypointManager");
        QuestWaypointManager manager = managerObj.GetComponent<QuestWaypointManager>();
        if (manager == null) manager = managerObj.AddComponent<QuestWaypointManager>();

        // Tạo Nút Parent (Pointer UI)
        GameObject pointerObj = new GameObject("WaypointPointer");
        pointerObj.transform.SetParent(canvas.transform, false);
        RectTransform pointerRect = pointerObj.AddComponent<RectTransform>();
        pointerRect.sizeDelta = new Vector2(60, 60);

        // Chữ "M" (Marker) hoặc dùng Mũi Tên giả làm Icon
        GameObject iconObj = new GameObject("ArrowIcon");
        iconObj.transform.SetParent(pointerObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(40, 40);
        
        // Dùng TextMeshPro làm mũi tên "V"
        TextMeshProUGUI arrowText = iconObj.AddComponent<TextMeshProUGUI>();
        arrowText.text = "►"; // Ký tự mũi tên
        arrowText.fontSize = 50;
        arrowText.alignment = TextAlignmentOptions.Center;
        arrowText.color = new Color(0.2f, 0.8f, 1f); // Xanh cyan
        arrowText.textWrappingMode = TextWrappingModes.NoWrap;

        // Chữ khoảng cách
        GameObject distObj = new GameObject("DistanceText");
        distObj.transform.SetParent(pointerObj.transform, false);
        RectTransform distRect = distObj.AddComponent<RectTransform>();
        distRect.anchoredPosition = new Vector2(0, -35); // Nằm dưới Icon
        distRect.sizeDelta = new Vector2(100, 30);
        
        TextMeshProUGUI distText = distObj.AddComponent<TextMeshProUGUI>();
        distText.text = "10m";
        distText.fontSize = 18;
        distText.alignment = TextAlignmentOptions.Center;
        distText.color = Color.white;
        distText.fontStyle = FontStyles.Bold;
        
        // Thêm outline đen cho chữ khoảng cách dễ nhìn
        Outline outline = distObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);

        // Gắn references
        manager.pointerUI = pointerRect;
        manager.arrowIcon = iconRect;
        manager.distanceText = distText;

        pointerObj.SetActive(false);

        Debug.Log("Waypoint UI đã được tạo thành công!");
    }
}
