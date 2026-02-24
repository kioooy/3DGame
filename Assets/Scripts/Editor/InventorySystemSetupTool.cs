using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool để setup toàn bộ Inventory System trong scene
/// Menu: Tools/Setup Inventory System
/// </summary>
public class InventorySystemSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup Inventory System")]
    static void ShowWindow()
    {
        var window = GetWindow<InventorySystemSetupTool>("Inventory Setup");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }
    
    private Vector2 scrollPos;
    
    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        GUILayout.Label("Inventory System Setup Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Tool này sẽ tự động setup toàn bộ Inventory System trong scene hiện tại.\n\n" +
            "Bao gồm:\n" +
            "• InventoryManager (quản lý data)\n" +
            "• InventoryUI (hiển thị UI)\n" +
            "• PickupPromptUI (hiển thị prompt khi nhặt)\n" +
            "• Canvas và EventSystem nếu chưa có",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        // Check current status
        DrawStatusSection();
        
        GUILayout.Space(20);
        
        // Setup buttons
        if (GUILayout.Button("🚀 Setup Complete Inventory System", GUILayout.Height(40)))
        {
            SetupCompleteSystem();
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("Or setup individual components:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Setup InventoryManager Only"))
        {
            SetupInventoryManager();
        }
        
        if (GUILayout.Button("Setup InventoryUI Only"))
        {
            SetupInventoryUI();
        }
        
        if (GUILayout.Button("Setup PickupPromptUI Only"))
        {
            SetupPickupPromptUI();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    void DrawStatusSection()
    {
        EditorGUILayout.LabelField("Current Scene Status:", EditorStyles.boldLabel);
        
        var inventoryManager = FindFirstObjectByType<InventoryManager>();
        DrawStatus("InventoryManager", inventoryManager != null);
        
        var inventoryUI = FindFirstObjectByType<InventoryUI>();
        DrawStatus("InventoryUI", inventoryUI != null);
        
        var pickupPrompt = FindFirstObjectByType<PickupPromptUI>();
        DrawStatus("PickupPromptUI", pickupPrompt != null);
        
        var canvas = FindFirstObjectByType<Canvas>();
        DrawStatus("Canvas", canvas != null);
        
        var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        DrawStatus("EventSystem", eventSystem != null);
    }
    
    void DrawStatus(string name, bool exists)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(name, GUILayout.Width(150));
        
        if (exists)
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField("✅ Exists", EditorStyles.boldLabel);
        }
        else
        {
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("❌ Missing", EditorStyles.boldLabel);
        }
        
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
    }
    
    void SetupCompleteSystem()
    {
        if (!EditorUtility.DisplayDialog(
            "Setup Complete Inventory System",
            "Bạn có chắc muốn setup toàn bộ Inventory System?\n\n" +
            "Nếu đã có components, chúng sẽ không bị thay thế.",
            "Yes", "Cancel"))
        {
            return;
        }
        
        SetupInventoryManager();
        SetupInventoryUI();
        SetupPickupPromptUI();
        
        Debug.Log("✅ Inventory System setup complete!");
        EditorUtility.DisplayDialog("Success", "Inventory System đã được setup thành công!", "OK");
    }
    
    GameObject SetupInventoryManager()
    {
        var existing = FindFirstObjectByType<InventoryManager>();
        if (existing != null)
        {
            Debug.Log("InventoryManager already exists in scene");
            Selection.activeGameObject = existing.gameObject;
            return existing.gameObject;
        }
        
        GameObject managerObj = new GameObject("InventoryManager");
        managerObj.AddComponent<InventoryManager>();
        
        Undo.RegisterCreatedObjectUndo(managerObj, "Create InventoryManager");
        
        Debug.Log("✅ Created InventoryManager");
        Selection.activeGameObject = managerObj;
        return managerObj;
    }
    
    GameObject SetupInventoryUI()
    {
        var existing = FindFirstObjectByType<InventoryUI>();
        if (existing != null)
        {
            Debug.Log("InventoryUI already exists in scene");
            Selection.activeGameObject = existing.gameObject;
            return existing.gameObject;
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
        GameObject panelObj = new GameObject("InventoryPanel");
        panelObj.transform.SetParent(inventoryUIObj.transform, false);
        
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600, 400);
        
        var panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        // Create Slots Container
        GameObject slotsContainer = new GameObject("SlotsContainer");
        slotsContainer.transform.SetParent(panelObj.transform, false);
        
        var containerRect = slotsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = new Vector2(10, 10);
        containerRect.offsetMax = new Vector2(-10, -10);
        
        var gridLayout = slotsContainer.AddComponent<UnityEngine.UI.GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(80, 80);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 5;
        
        // Create Slot Prefab (we'll need to create this manually or load from resources)
        GameObject slotPrefab = CreateSlotPrefab();
        
        // Assign references via reflection (since fields are private)
        var inventoryUIType = typeof(InventoryUI);
        var inventoryPanelField = inventoryUIType.GetField("inventoryPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var slotsContainerField = inventoryUIType.GetField("slotsContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var slotPrefabField = inventoryUIType.GetField("slotPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        inventoryPanelField?.SetValue(inventoryUI, panelObj);
        slotsContainerField?.SetValue(inventoryUI, slotsContainer.transform);
        slotPrefabField?.SetValue(inventoryUI, slotPrefab);
        
        Undo.RegisterCreatedObjectUndo(inventoryUIObj, "Create InventoryUI");
        
        Debug.Log("✅ Created InventoryUI with Panel and Slots Container");
        Selection.activeGameObject = inventoryUIObj;
        
        EditorUtility.SetDirty(inventoryUI);
        
        return inventoryUIObj;
    }
    
    GameObject CreateSlotPrefab()
    {
        GameObject slotObj = new GameObject("InventorySlot");
        
        var rectTransform = slotObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(80, 80);
        
        var slotImage = slotObj.AddComponent<UnityEngine.UI.Image>();
        slotImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        var uiSlot = slotObj.AddComponent<UIInventorySlot>();
        
        // Create Icon child
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform, false);
        
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(5, 5);
        iconRect.offsetMax = new Vector2(-5, -5);
        
        var iconImage = iconObj.AddComponent<UnityEngine.UI.Image>();
        iconImage.preserveAspect = true;
        iconImage.enabled = false;
        
        // Create Quantity Text child
        GameObject quantityObj = new GameObject("Quantity");
        quantityObj.transform.SetParent(slotObj.transform, false);
        
        var quantityRect = quantityObj.AddComponent<RectTransform>();
        quantityRect.anchorMin = new Vector2(1, 0);
        quantityRect.anchorMax = new Vector2(1, 0);
        quantityRect.pivot = new Vector2(1, 0);
        quantityRect.anchoredPosition = new Vector2(-5, 5);
        quantityRect.sizeDelta = new Vector2(30, 20);
        
        var quantityText = quantityObj.AddComponent<TMPro.TextMeshProUGUI>();
        quantityText.text = "99";
        quantityText.fontSize = 14;
        quantityText.alignment = TMPro.TextAlignmentOptions.BottomRight;
        quantityText.color = Color.white;
        quantityText.enabled = false;
        
        // Assign to UIInventorySlot via reflection
        var uiSlotType = typeof(UIInventorySlot);
        var itemIconField = uiSlotType.GetField("itemIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var quantityTextField = uiSlotType.GetField("quantityText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        itemIconField?.SetValue(uiSlot, iconImage);
        quantityTextField?.SetValue(uiSlot, quantityText);
        
        return slotObj;
    }
    
    GameObject SetupPickupPromptUI()
    {
        var existing = FindFirstObjectByType<PickupPromptUI>();
        if (existing != null)
        {
            Debug.Log("PickupPromptUI already exists in scene");
            Selection.activeGameObject = existing.gameObject;
            return existing.gameObject;
        }
        
        // Ensure Canvas exists
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }
        
        GameObject promptObj = new GameObject("PickupPromptUI");
        promptObj.transform.SetParent(canvas.transform, false);
        
        var promptUI = promptObj.AddComponent<PickupPromptUI>();
        
        // Create prompt panel
        GameObject panelObj = new GameObject("PromptPanel");
        panelObj.transform.SetParent(promptObj.transform, false);
        
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0, -200);
        panelRect.sizeDelta = new Vector2(300, 60);
        
        var panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);
        
        // Create text
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
        
        var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "Press [E] to pickup";
        text.fontSize = 18;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = Color.white;
        
        // Assign via reflection
        var promptUIType = typeof(PickupPromptUI);
        var promptPanelField = promptUIType.GetField("promptPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var promptTextField = promptUIType.GetField("promptText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        promptPanelField?.SetValue(promptUI, panelObj);
        promptTextField?.SetValue(promptUI, text);
        
        Undo.RegisterCreatedObjectUndo(promptObj, "Create PickupPromptUI");
        
        Debug.Log("✅ Created PickupPromptUI");
        Selection.activeGameObject = promptObj;
        
        EditorUtility.SetDirty(promptUI);
        
        return promptObj;
    }
    
    Canvas CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
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
