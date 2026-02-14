using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Tool hoàn chỉnh để setup Inventory UI với slot prefabs đẹp
/// Menu: Tools/Complete Inventory UI Setup
/// </summary>
public class CompleteInventoryUISetup : EditorWindow
{
    [MenuItem("Tools/Complete Inventory UI Setup")]
    static void ShowWindow()
    {
        var window = GetWindow<CompleteInventoryUISetup>("Inventory UI Setup");
        window.minSize = new Vector2(450, 400);
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.Label("Complete Inventory UI Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Tool này sẽ tạo HOÀN CHỈNH Inventory UI với:\n\n" +
            "✅ Slot prefabs có UIInventorySlot component\n" +
            "✅ Background và border rõ ràng cho mỗi slot\n" +
            "✅ Icon và quantity text\n" +
            "✅ Grid layout đẹp mắt\n\n" +
            "Nếu đã có InventoryUI, nó sẽ được recreate.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        // Check status
        var inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null)
        {
            GUI.color = Color.yellow;
            EditorGUILayout.HelpBox("⚠️ InventoryUI đã tồn tại. Sẽ được recreate.", MessageType.Warning);
            GUI.color = Color.white;
        }
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("🚀 Setup Complete Inventory UI", GUILayout.Height(50)))
        {
            SetupCompleteInventoryUI();
        }
    }
    
    void SetupCompleteInventoryUI()
    {
        if (!EditorUtility.DisplayDialog(
            "Setup Inventory UI",
            "Bạn có chắc muốn setup Inventory UI?\n\n" +
            "Nếu đã có InventoryUI, nó sẽ được xóa và tạo lại.",
            "Yes", "Cancel"))
        {
            return;
        }
        
        // Remove existing InventoryUI
        var existing = FindFirstObjectByType<InventoryUI>();
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
            Debug.Log("[Setup] Removed old InventoryUI");
        }
        
        // Ensure Canvas exists
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }
        
        // Create InventoryUI GameObject
        GameObject inventoryUIObj = new GameObject("InventoryUI");
        inventoryUIObj.transform.SetParent(canvas.transform, false);
        
        var inventoryUI = inventoryUIObj.AddComponent<InventoryUI>();
        
        // Create Inventory Panel
        GameObject panelObj = CreateInventoryPanel(inventoryUIObj.transform);
        
        // Create Slots Container
        GameObject slotsContainer = CreateSlotsContainer(panelObj.transform);
        
        // Create Slot Prefab
        GameObject slotPrefab = CreateSlotPrefab();
        
        // Assign references using SerializedObject
        SerializedObject serializedUI = new SerializedObject(inventoryUI);
        serializedUI.FindProperty("inventoryPanel").objectReferenceValue = panelObj;
        serializedUI.FindProperty("slotsContainer").objectReferenceValue = slotsContainer.transform;
        serializedUI.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
        serializedUI.ApplyModifiedProperties();
        
        // Save prefab to Assets
        string prefabPath = "Assets/Prefabs/UI/InventorySlot.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
        PrefabUtility.SaveAsPrefabAsset(slotPrefab, prefabPath);
        
        // Cleanup temp object
        DestroyImmediate(slotPrefab);
        
        // Load the saved prefab and assign it
        GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        serializedUI.FindProperty("slotPrefab").objectReferenceValue = savedPrefab;
        serializedUI.ApplyModifiedProperties();
        
        EditorUtility.SetDirty(inventoryUI);
        
        Debug.Log("✅ Inventory UI setup complete!");
        
        Selection.activeGameObject = inventoryUIObj;
        
        EditorUtility.DisplayDialog("Success",
            "✅ Inventory UI đã được setup thành công!\n\n" +
            "• Panel với background tối\n" +
            "• 32 slots với border rõ ràng\n" +
            "• Icon và quantity text\n\n" +
            "Hãy chạy game và test ngay!",
            "OK");
    }
    
    GameObject CreateInventoryPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("InventoryPanel");
        panelObj.transform.SetParent(parent, false);
        
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720, 560);
        
        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        
        // Add title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, 0);
        titleRect.sizeDelta = new Vector2(0, 50);
        
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "INVENTORY";
        titleText.fontSize = 28;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        
        return panelObj;
    }
    
    GameObject CreateSlotsContainer(Transform parent)
    {
        GameObject slotsContainer = new GameObject("SlotsContainer");
        slotsContainer.transform.SetParent(parent, false);
        
        var containerRect = slotsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = new Vector2(20, 20);
        containerRect.offsetMax = new Vector2(-20, -70); // Leave space for title
        
        var gridLayout = slotsContainer.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(80, 80);
        gridLayout.spacing = new Vector2(8, 8);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 8; // 8 columns
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        
        return slotsContainer;
    }
    
    GameObject CreateSlotPrefab()
    {
        GameObject slotObj = new GameObject("InventorySlot");
        
        var rectTransform = slotObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(80, 80);
        
        // Background
        var slotImage = slotObj.AddComponent<Image>();
        slotImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        slotImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        slotImage.type = Image.Type.Sliced;
        
        // Border/Outline
        var outline = slotObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        outline.effectDistance = new Vector2(2, -2);
        
        // Add UIInventorySlot component
        var uiSlot = slotObj.AddComponent<UIInventorySlot>();
        
        // Create Icon child
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform, false);
        
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(8, 8);
        iconRect.offsetMax = new Vector2(-8, -8);
        
        var iconImage = iconObj.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.enabled = false;
        iconImage.raycastTarget = false;
        
        // Create Quantity Text child
        GameObject quantityObj = new GameObject("Quantity");
        quantityObj.transform.SetParent(slotObj.transform, false);
        
        var quantityRect = quantityObj.AddComponent<RectTransform>();
        quantityRect.anchorMin = new Vector2(1, 0);
        quantityRect.anchorMax = new Vector2(1, 0);
        quantityRect.pivot = new Vector2(1, 0);
        quantityRect.anchoredPosition = new Vector2(-5, 5);
        quantityRect.sizeDelta = new Vector2(40, 25);
        
        var quantityText = quantityObj.AddComponent<TextMeshProUGUI>();
        quantityText.text = "99";
        quantityText.fontSize = 16;
        quantityText.alignment = TextAlignmentOptions.BottomRight;
        quantityText.color = Color.white;
        quantityText.fontStyle = FontStyles.Bold;
        quantityText.enabled = false;
        
        // Add shadow to quantity text
        var shadow = quantityObj.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(1, -1);
        
        // Create Highlight Image (for hover/selection)
        GameObject highlightObj = new GameObject("Highlight");
        highlightObj.transform.SetParent(slotObj.transform, false);
        
        var highlightRect = highlightObj.AddComponent<RectTransform>();
        highlightRect.anchorMin = Vector2.zero;
        highlightRect.anchorMax = Vector2.one;
        highlightRect.offsetMin = Vector2.zero;
        highlightRect.offsetMax = Vector2.zero;
        
        var highlightImage = highlightObj.AddComponent<Image>();
        highlightImage.color = new Color(1f, 1f, 1f, 0.2f);
        highlightImage.enabled = false;
        highlightImage.raycastTarget = false;
        
        // Assign to UIInventorySlot using SerializedObject
        SerializedObject serializedSlot = new SerializedObject(uiSlot);
        serializedSlot.FindProperty("itemIcon").objectReferenceValue = iconImage;
        serializedSlot.FindProperty("quantityText").objectReferenceValue = quantityText;
        serializedSlot.FindProperty("highlightImage").objectReferenceValue = highlightImage;
        serializedSlot.ApplyModifiedProperties();
        
        return slotObj;
    }
    
    Canvas CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create EventSystem if not exists
        var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
        }
        
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        
        Debug.Log("✅ Created Canvas");
        return canvas;
    }
}
