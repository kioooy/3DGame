using UnityEngine;

/// <summary>
/// Debug tool để kiểm tra inventory system
/// Attach vào một GameObject trong scene và check Console
/// </summary>
public class InventoryDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool debugOnStart = true;
    [SerializeField] private bool debugEveryFrame = false;
    
    void Start()
    {
        if (debugOnStart)
        {
            Invoke(nameof(DebugInventorySystem), 1f); // Delay 1s để các systems khởi tạo
        }
    }
    
    void Update()
    {
        if (debugEveryFrame && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f1Key.wasPressedThisFrame)
        {
            DebugInventorySystem();
        }
    }
    
    void DebugInventorySystem()
    {
        Debug.Log("=== INVENTORY SYSTEM DEBUG ===");
        
        // Check InventoryManager
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("❌ InventoryManager.Instance is NULL! Inventory system không hoạt động!");
            Debug.LogError("   → Hãy đảm bảo có GameObject với InventoryManager component trong scene");
            return;
        }
        else
        {
            Debug.Log("✅ InventoryManager.Instance exists");
        }
        
        // Check slots
        var slots = InventoryManager.Instance.GetAllSlots();
        if (slots == null || slots.Length == 0)
        {
            Debug.LogError("❌ Inventory slots is NULL or empty!");
            return;
        }
        else
        {
            Debug.Log($"✅ Inventory has {slots.Length} slots");
        }
        
        // Count items
        int itemCount = 0;
        int emptySlots = 0;
        
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                Debug.LogError($"❌ Slot {i} is NULL!");
                continue;
            }
            
            if (slots[i].IsEmpty)
            {
                emptySlots++;
            }
            else
            {
                itemCount++;
                Debug.Log($"   Slot {i}: {slots[i].item.itemName} x{slots[i].quantity}");
            }
        }
        
        Debug.Log($"📦 Total items in inventory: {itemCount}");
        Debug.Log($"📭 Empty slots: {emptySlots}");
        
        // Check InventoryUI
        if (InventoryUI.Instance == null)
        {
            Debug.LogWarning("⚠️ InventoryUI.Instance is NULL! UI sẽ không hiển thị");
            Debug.LogWarning("   → Hãy đảm bảo có GameObject với InventoryUI component trong scene");
        }
        else
        {
            Debug.Log("✅ InventoryUI.Instance exists");
            Debug.Log($"   Inventory is open: {InventoryUI.Instance.IsOpen}");
        }
        
        // Check PickupPromptUI
        if (PickupPromptUI.Instance == null)
        {
            Debug.LogWarning("⚠️ PickupPromptUI.Instance is NULL! Pickup prompt sẽ không hiển thị");
        }
        else
        {
            Debug.Log("✅ PickupPromptUI.Instance exists");
        }
        
        Debug.Log("=== END DEBUG ===");
    }
    
    /// <summary>
    /// Test thêm item vào inventory
    /// </summary>
    [ContextMenu("Test Add Random Item")]
    void TestAddItem()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager.Instance is NULL!");
            return;
        }
        
        // Load một item từ Resources
        ItemData testItem = Resources.Load<ItemData>("Items/Stone");
        
        if (testItem == null)
        {
            Debug.LogError("Không tìm thấy Stone item trong Resources/Items/");
            return;
        }
        
        bool success = InventoryManager.Instance.AddItem(testItem, 1);
        
        if (success)
        {
            Debug.Log($"✅ Successfully added {testItem.itemName} to inventory");
        }
        else
        {
            Debug.LogError($"❌ Failed to add {testItem.itemName} to inventory");
        }
    }
}
