using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool để setup Inventory UI nhanh chóng
/// Window > Inventory > Setup Inventory UI
/// </summary>
public class InventorySetupTool : EditorWindow
{
    [MenuItem("Window/Inventory/Setup Inventory UI")]
    static void ShowWindow()
    {
        GetWindow<InventorySetupTool>("Inventory Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Inventory System Setup", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Tool này sẽ tạo Inventory UI Canvas với tất cả components cần thiết.", MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Create Inventory UI Canvas", GUILayout.Height(40)))
        {
            CreateInventoryUI();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Create Inventory Manager", GUILayout.Height(30)))
        {
            CreateInventoryManager();
        }
        
        if (GUILayout.Button("Create Pickup Prompt UI", GUILayout.Height(30)))
        {
            CreatePickupPromptUI();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Sau khi tạo UI, nhớ:\n1. Assign Slot Prefab vào InventoryUI\n2. Setup Layer 'Item' cho pickable items\n3. Tạo ItemData ScriptableObjects", MessageType.Warning);
    }

    void CreateInventoryUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("InventoryCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create Inventory Panel
        GameObject panelObj = new GameObject("InventoryPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(800, 600);
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        // Create Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(-40, 50);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "INVENTORY";
        titleText.fontSize = 36;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        
        // Create Slots Container
        GameObject containerObj = new GameObject("SlotsContainer");
        containerObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.offsetMin = new Vector2(20, 20);
        containerRect.offsetMax = new Vector2(-20, -80);
        
        GridLayoutGroup grid = containerObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(80, 80);
        grid.spacing = new Vector2(10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        
        // Add InventoryUI component
        InventoryUI inventoryUI = canvasObj.AddComponent<InventoryUI>();
        
        // Use reflection to set private fields
        var panelField = typeof(InventoryUI).GetField("inventoryPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var containerField = typeof(InventoryUI).GetField("slotsContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        panelField?.SetValue(inventoryUI, panelObj);
        containerField?.SetValue(inventoryUI, containerObj.transform);
        
        // Create Slot Prefab
        CreateSlotPrefab();
        
        Debug.Log("✅ Đã tạo Inventory UI Canvas!");
        Selection.activeGameObject = canvasObj;
    }

    void CreateSlotPrefab()
    {
        // Create slot prefab
        GameObject slotObj = new GameObject("InventorySlot");
        
        RectTransform slotRect = slotObj.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(80, 80);
        
        Image slotBg = slotObj.AddComponent<Image>();
        slotBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform, false);
        
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(5, 5);
        iconRect.offsetMax = new Vector2(-5, -5);
        
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.enabled = false;
        
        // Quantity Text
        GameObject qtyObj = new GameObject("QuantityText");
        qtyObj.transform.SetParent(slotObj.transform, false);
        
        RectTransform qtyRect = qtyObj.AddComponent<RectTransform>();
        qtyRect.anchorMin = new Vector2(1, 0);
        qtyRect.anchorMax = new Vector2(1, 0);
        qtyRect.pivot = new Vector2(1, 0);
        qtyRect.anchoredPosition = new Vector2(-5, 5);
        qtyRect.sizeDelta = new Vector2(40, 25);
        
        TextMeshProUGUI qtyText = qtyObj.AddComponent<TextMeshProUGUI>();
        qtyText.fontSize = 16;
        qtyText.alignment = TextAlignmentOptions.BottomRight;
        qtyText.color = Color.white;
        qtyText.fontStyle = FontStyles.Bold;
        
        // Highlight
        GameObject highlightObj = new GameObject("Highlight");
        highlightObj.transform.SetParent(slotObj.transform, false);
        
        RectTransform highlightRect = highlightObj.AddComponent<RectTransform>();
        highlightRect.anchorMin = Vector2.zero;
        highlightRect.anchorMax = Vector2.one;
        highlightRect.offsetMin = Vector2.zero;
        highlightRect.offsetMax = Vector2.zero;
        
        Image highlightImage = highlightObj.AddComponent<Image>();
        highlightImage.color = new Color(1f, 1f, 0.5f, 0.3f);
        highlightImage.enabled = false;
        
        // Add components
        UIInventorySlot uiSlot = slotObj.AddComponent<UIInventorySlot>();
        slotObj.AddComponent<ItemDragHandler>();
        slotObj.AddComponent<ItemTooltip>();
        
        // Set references using reflection
        var iconField = typeof(UIInventorySlot).GetField("itemIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var qtyField = typeof(UIInventorySlot).GetField("quantityText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var highlightField = typeof(UIInventorySlot).GetField("highlightImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        iconField?.SetValue(uiSlot, iconImage);
        qtyField?.SetValue(uiSlot, qtyText);
        highlightField?.SetValue(uiSlot, highlightImage);
        
        // Save as prefab
        string path = "Assets/Prefabs/UI/InventorySlot.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
        PrefabUtility.SaveAsPrefabAsset(slotObj, path);
        
        DestroyImmediate(slotObj);
        Debug.Log($"✅ Đã tạo Slot Prefab tại: {path}");
    }

    void CreateInventoryManager()
    {
        GameObject managerObj = new GameObject("InventoryManager");
        managerObj.AddComponent<InventoryManager>();
        
        Debug.Log("✅ Đã tạo Inventory Manager!");
        Selection.activeGameObject = managerObj;
    }

    void CreatePickupPromptUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Không tìm thấy Canvas! Tạo Inventory UI trước.", "OK");
            return;
        }
        
        GameObject promptObj = new GameObject("PickupPromptUI");
        promptObj.transform.SetParent(canvas.transform, false);
        
        RectTransform promptRect = promptObj.AddComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.3f);
        promptRect.anchorMax = new Vector2(0.5f, 0.3f);
        promptRect.sizeDelta = new Vector2(400, 60);
        
        CanvasGroup cg = promptObj.AddComponent<CanvasGroup>();
        cg.alpha = 0;
        
        Image bg = promptObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(promptObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Nhấn [E] để nhặt Item";
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        
        PickupPromptUI promptUI = promptObj.AddComponent<PickupPromptUI>();
        
        var panelField = typeof(PickupPromptUI).GetField("promptPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var textField = typeof(PickupPromptUI).GetField("promptText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        panelField?.SetValue(promptUI, promptObj);
        textField?.SetValue(promptUI, text);
        
        Debug.Log("✅ Đã tạo Pickup Prompt UI!");
        Selection.activeGameObject = promptObj;
    }
}
