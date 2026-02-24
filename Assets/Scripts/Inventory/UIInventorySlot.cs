using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component cho một slot trong inventory
/// </summary>
public class UIInventorySlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image highlightImage;
    [SerializeField] private GameObject emptySlotIndicator;
    
    private InventorySlot _slot;
    private int _slotIndex;
    
    public InventorySlot Slot => _slot;
    public int SlotIndex => _slotIndex;
    
    /// <summary>
    /// Setup slot với data
    /// </summary>
    public void Setup(InventorySlot slot, int index)
    {
        _slot = slot;
        _slotIndex = index;
        Debug.Log($"[UIInventorySlot] Slot {index} setup complete");
        UpdateUI();
    }
    
    /// <summary>
    /// Update UI hiển thị
    /// </summary>
    public void UpdateUI()
    {
        if (_slot == null)
        {
            Debug.LogWarning($"[UIInventorySlot] Slot {_slotIndex}: _slot is NULL!");
            return;
        }
        
        bool hasItem = !_slot.IsEmpty;
        
        if (hasItem)
        {
            Debug.Log($"[UIInventorySlot] Slot {_slotIndex}: Updating with item {_slot.item.itemName} x{_slot.quantity}");
        }
        
        // Icon
        if (itemIcon != null)
        {
            itemIcon.enabled = hasItem;
            if (hasItem && _slot.item.itemIcon != null)
            {
                itemIcon.sprite = _slot.item.itemIcon;
                Debug.Log($"[UIInventorySlot] Slot {_slotIndex}: Set icon sprite");
            }
            else if (hasItem && _slot.item.itemIcon == null)
            {
                Debug.LogWarning($"[UIInventorySlot] Slot {_slotIndex}: Item {_slot.item.itemName} has NO ICON!");
            }
        }
        else
        {
            Debug.LogWarning($"[UIInventorySlot] Slot {_slotIndex}: itemIcon reference is NULL!");
        }
        
        // Quantity text
        if (quantityText != null)
        {
            quantityText.enabled = hasItem && _slot.quantity > 1;
            if (hasItem)
            {
                quantityText.text = _slot.quantity.ToString();
            }
        }
        
        // Empty indicator
        if (emptySlotIndicator != null)
        {
            emptySlotIndicator.SetActive(!hasItem);
        }
    }
    
    /// <summary>
    /// Highlight slot
    /// </summary>
    public void SetHighlight(bool enabled)
    {
        if (highlightImage != null)
        {
            highlightImage.enabled = enabled;
        }
    }
    
    /// <summary>
    /// Lấy item name để hiển thị tooltip
    /// </summary>
    public string GetItemName()
    {
        if (_slot == null || _slot.IsEmpty) return "";
        return _slot.item.itemName;
    }
    
    /// <summary>
    /// Lấy item description
    /// </summary>
    public string GetItemDescription()
    {
        if (_slot == null || _slot.IsEmpty) return "";
        return _slot.item.description;
    }
}
