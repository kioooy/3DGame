using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Tool tự động tạo PickupPromptUI trong scene
/// </summary>
public class PickupPromptSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup Pickup Prompt UI")]
    public static void ShowWindow()
    {
        GetWindow<PickupPromptSetupTool>("Pickup Prompt Setup");
    }

    private string promptFormat = "Nhấn [E] để nhặt {0}";
    private int fontSize = 28;
    private Color textColor = Color.white;
    private float fadeSpeed = 5f;

    void OnGUI()
    {
        GUILayout.Label("Pickup Prompt UI Setup Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Check if already exists
        PickupPromptUI existing = FindFirstObjectByType<PickupPromptUI>();
        if (existing != null)
        {
            EditorGUILayout.HelpBox($"PickupPromptUI đã tồn tại trên GameObject: {existing.gameObject.name}", MessageType.Info);
            
            if (GUILayout.Button("Select Existing PickupPromptUI"))
            {
                Selection.activeGameObject = existing.gameObject;
            }
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Nếu muốn tạo mới, hãy xóa component cũ trước.", MessageType.Warning);
            return;
        }

        EditorGUILayout.HelpBox("Tool này sẽ tự động tạo:\n• Canvas cho Pickup Prompt\n• Panel với CanvasGroup\n• TextMeshProUGUI\n• Component PickupPromptUI", MessageType.Info);
        
        GUILayout.Space(10);
        
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        promptFormat = EditorGUILayout.TextField("Prompt Format", promptFormat);
        fontSize = EditorGUILayout.IntField("Font Size", fontSize);
        textColor = EditorGUILayout.ColorField("Text Color", textColor);
        fadeSpeed = EditorGUILayout.FloatField("Fade Speed", fadeSpeed);
        
        GUILayout.Space(20);

        if (GUILayout.Button("Create Pickup Prompt UI", GUILayout.Height(40)))
        {
            CreatePickupPromptUI();
        }
    }

    void CreatePickupPromptUI()
    {
        // 1. Tìm hoặc tạo Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        GameObject canvasObj;
        
        if (canvas == null)
        {
            canvasObj = new GameObject("PickupPromptCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("Created new Canvas: PickupPromptCanvas");
        }
        else
        {
            canvasObj = canvas.gameObject;
            Debug.Log($"Using existing Canvas: {canvasObj.name}");
        }

        // 2. Tạo GameObject chính cho PickupPromptUI
        GameObject promptUIObj = new GameObject("PickupPromptUI");
        promptUIObj.transform.SetParent(canvasObj.transform, false);
        
        // Add PickupPromptUI component
        PickupPromptUI promptUI = promptUIObj.AddComponent<PickupPromptUI>();

        // 3. Tạo Panel
        GameObject panelObj = new GameObject("PickupPromptPanel");
        panelObj.transform.SetParent(promptUIObj.transform, false);
        
        // Add RectTransform và setup
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 120f);
        panelRect.sizeDelta = new Vector2(400f, 80f);

        // Add Image component (optional background)
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.5f); // Semi-transparent black

        // Add CanvasGroup for fading
        CanvasGroup canvasGroup = panelObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // 4. Tạo Text
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        // Add TextMeshProUGUI
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Nhấn [E] để nhặt";
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        // 5. Assign references to PickupPromptUI
        SerializedObject serializedPromptUI = new SerializedObject(promptUI);
        serializedPromptUI.FindProperty("promptPanel").objectReferenceValue = panelObj;
        serializedPromptUI.FindProperty("promptText").objectReferenceValue = text;
        serializedPromptUI.FindProperty("promptFormat").stringValue = promptFormat;
        serializedPromptUI.FindProperty("fadeSpeed").floatValue = fadeSpeed;
        serializedPromptUI.ApplyModifiedProperties();

        // 6. Disable panel by default
        panelObj.SetActive(false);

        // 7. Select the created object
        Selection.activeGameObject = promptUIObj;

        Debug.Log("✓ PickupPromptUI đã được tạo thành công!");
        EditorUtility.DisplayDialog("Success", 
            "PickupPromptUI đã được tạo thành công!\n\n" +
            "GameObject: PickupPromptUI\n" +
            "Location: Canvas/PickupPromptUI\n\n" +
            "Bạn có thể chỉnh sửa vị trí và style trong Inspector.", 
            "OK");
    }
}
