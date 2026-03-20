using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool để validate scene setup cho Inventory System
/// </summary>
public class InventorySetupValidator : EditorWindow
{
    [MenuItem("Tools/Validate Inventory Setup")]
    public static void ShowWindow()
    {
        GetWindow<InventorySetupValidator>("Inventory Setup Validator");
    }

    private Vector2 scrollPosition;

    void OnGUI()
    {
        GUILayout.Label("Inventory System Setup Validator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // Check InventoryManager
        CheckComponent<InventoryManager>("InventoryManager", 
            "Manages the player's inventory data",
            "Create an empty GameObject named 'InventoryManager' and add the InventoryManager component");

        GUILayout.Space(5);

        // Check InventoryUI
        CheckComponent<InventoryUI>("InventoryUI",
            "Manages the inventory UI panel",
            "Create a Canvas with InventoryUI component. Assign inventoryPanel, slotsContainer, and slotPrefab in the Inspector");

        GUILayout.Space(5);

        // Check PickupPromptUI
        CheckComponent<PickupPromptUI>("PickupPromptUI",
            "Shows 'Press E to pickup' prompt",
            "Create a Canvas with PickupPromptUI component. Assign promptPanel and promptText in the Inspector");

        GUILayout.Space(5);

        // Check PlayerController
        CheckComponent<PlayerController>("PlayerController",
            "Handles player movement and item interaction",
            "Add PlayerController to your player GameObject. Assign cameraTransform and set itemLayer");

        GUILayout.Space(10);
        GUILayout.Label("Scene Objects Check", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // Check for PickableItems
        CheckPickableItems();

        GUILayout.Space(10);

        if (GUILayout.Button("Refresh", GUILayout.Height(30)))
        {
            Repaint();
        }

        GUILayout.EndScrollView();
    }

    void CheckComponent<T>(string componentName, string description, string fixInstructions) where T : MonoBehaviour
    {
        T component = FindFirstObjectByType<T>();
        
        if (component != null)
        {
            GUI.color = Color.green;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"✓ {componentName} - Found", EditorStyles.boldLabel);
            GUILayout.Label($"GameObject: {component.gameObject.name}");
            
            // Additional validation for specific components
            if (typeof(T) == typeof(InventoryUI))
            {
                ValidateInventoryUI(component as InventoryUI);
            }
            else if (typeof(T) == typeof(PickupPromptUI))
            {
                ValidatePickupPromptUI(component as PickupPromptUI);
            }
            else if (typeof(T) == typeof(PlayerController))
            {
                ValidatePlayerController(component as PlayerController);
            }
            
            GUILayout.EndVertical();
        }
        else
        {
            GUI.color = Color.red;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"✗ {componentName} - NOT FOUND", EditorStyles.boldLabel);
            GUILayout.Label($"Description: {description}");
            GUILayout.Label($"Fix: {fixInstructions}", EditorStyles.wordWrappedLabel);
            GUILayout.EndVertical();
        }
        
        GUI.color = Color.white;
    }

    void ValidateInventoryUI(InventoryUI inventoryUI)
    {
        var inventoryPanel = GetPrivateField<GameObject>(inventoryUI, "inventoryPanel");
        var slotsContainer = GetPrivateField<Transform>(inventoryUI, "slotsContainer");
        var slotPrefab = GetPrivateField<GameObject>(inventoryUI, "slotPrefab");

        if (inventoryPanel == null)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("⚠ inventoryPanel is not assigned!");
        }
        if (slotsContainer == null)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("⚠ slotsContainer is not assigned!");
        }
        if (slotPrefab == null)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("⚠ slotPrefab is not assigned!");
        }
        GUI.color = Color.white;
    }

    void ValidatePickupPromptUI(PickupPromptUI promptUI)
    {
        var promptPanel = GetPrivateField<GameObject>(promptUI, "promptPanel");
        var promptText = GetPrivateField<TMPro.TextMeshProUGUI>(promptUI, "promptText");

        if (promptPanel == null)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("⚠ promptPanel is not assigned!");
        }
        if (promptText == null)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("⚠ promptText is not assigned!");
        }
        GUI.color = Color.white;
    }

    void ValidatePlayerController(PlayerController player)
    {
        var cameraTransform = GetPrivateField<Transform>(player, "cameraTransform");
        
        if (cameraTransform == null)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("⚠ cameraTransform is not assigned!");
        }
        GUI.color = Color.white;
    }

    void CheckPickableItems()
    {
        PickableItem[] items = FindObjectsByType<PickableItem>(FindObjectsSortMode.None);
        
        if (items.Length == 0)
        {
            GUI.color = Color.yellow;
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("⚠ No PickableItems found in scene");
            GUILayout.Label("Add PickableItem component to objects you want to be pickable");
            GUILayout.EndVertical();
            GUI.color = Color.white;
            return;
        }

        GUI.color = Color.green;
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label($"✓ Found {items.Length} PickableItem(s) in scene", EditorStyles.boldLabel);
        
        // Check each item
        foreach (var item in items)
        {
            GUILayout.Space(3);
            GUILayout.BeginHorizontal();
            
            if (item.itemData == null)
            {
                GUI.color = Color.red;
                GUILayout.Label($"✗ {item.gameObject.name} - Missing ItemData!");
            }
            else
            {
                GUI.color = Color.green;
                GUILayout.Label($"✓ {item.gameObject.name} - {item.itemData.itemName}");
            }
            
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeGameObject = item.gameObject;
            }
            
            GUILayout.EndHorizontal();
            GUI.color = Color.white;
        }
        
        GUILayout.EndVertical();
        GUI.color = Color.white;
    }

    T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        return default(T);
    }
}
