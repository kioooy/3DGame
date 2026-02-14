using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component cho mỗi hotbar slot
/// </summary>
public class HotbarSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI numberText; // Số thứ tự (1-9)
    
    private int _slotIndex;
    private InventorySlot _inventorySlot;
    
    public void Setup(int index)
    {
        _slotIndex = index;
        
        // Set number text (1-9)
        if (numberText != null)
        {
            numberText.text = (index + 1).ToString();
        }
    }
    
    public void UpdateSlot(InventorySlot inventorySlot)
    {
        _inventorySlot = inventorySlot;
        
        bool hasItem = inventorySlot != null && !inventorySlot.IsEmpty;
        
        // Update icon
        if (itemIcon != null)
        {
            itemIcon.enabled = hasItem;
            if (hasItem && inventorySlot.item.itemIcon != null)
            {
                itemIcon.sprite = inventorySlot.item.itemIcon;
            }
        }
        
        // Update quantity
        if (quantityText != null)
        {
            quantityText.enabled = hasItem && inventorySlot.quantity > 1;
            if (hasItem)
            {
                quantityText.text = inventorySlot.quantity.ToString();
            }
        }
    }
    
    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? 
                new Color(1f, 1f, 1f, 0.8f) : 
                new Color(0.5f, 0.5f, 0.5f, 0.6f);
        }
    }
}
