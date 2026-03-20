using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hotbar UI - Quick slots giống Minecraft
/// Hiển thị 9 slots đầu của inventory
/// </summary>
public class HotbarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab;
    
    [Header("Settings")]
    [SerializeField] private int hotbarSize = 9;
    [SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
    
    private HotbarSlot[] _hotbarSlots;
    private int _selectedSlotIndex = 0;
    
    void Start()
    {
        CreateHotbarSlots();
        
        // Subscribe to inventory changes
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshHotbar;
        }
        
        RefreshHotbar();
        UpdateSelectedSlot();
    }
    
    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshHotbar;
        }
    }
    
    void CreateHotbarSlots()
    {
        if (slotsContainer == null || slotPrefab == null)
        {
            Debug.LogError("[HotbarUI] Missing references!");
            return;
        }
        
        _hotbarSlots = new HotbarSlot[hotbarSize];
        
        for (int i = 0; i < hotbarSize; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            HotbarSlot slot = slotObj.GetComponent<HotbarSlot>();
            
            if (slot != null)
            {
                slot.Setup(i);
                _hotbarSlots[i] = slot;
            }
        }
        
        Debug.Log($"[HotbarUI] Created {hotbarSize} hotbar slots");
    }
    
    void RefreshHotbar()
    {
        if (InventoryManager.Instance == null || _hotbarSlots == null) return;
        
        for (int i = 0; i < hotbarSize; i++)
        {
            var inventorySlot = InventoryManager.Instance.GetSlot(i);
            if (_hotbarSlots[i] != null)
            {
                _hotbarSlots[i].UpdateSlot(inventorySlot);
            }
        }
    }
    
    /// <summary>
    /// Select slot by index (0-8)
    /// </summary>
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= hotbarSize) return;
        
        _selectedSlotIndex = index;
        UpdateSelectedSlot();
        
        // Equip item từ slot này
        var slot = InventoryManager.Instance?.GetSlot(index);
        if (slot != null && !slot.IsEmpty)
        {
            var playerEquipment = FindFirstObjectByType<PlayerEquipment>();
            if (playerEquipment != null)
            {
                playerEquipment.EquipItem(slot.item, index);
            }
        }
    }
    
    void UpdateSelectedSlot()
    {
        if (_hotbarSlots == null) return;
        
        for (int i = 0; i < _hotbarSlots.Length; i++)
        {
            if (_hotbarSlots[i] != null)
            {
                _hotbarSlots[i].SetSelected(i == _selectedSlotIndex);
            }
        }
    }
    
    public int SelectedSlotIndex => _selectedSlotIndex;
}
