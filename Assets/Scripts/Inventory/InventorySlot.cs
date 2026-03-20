using System;

/// <summary>
/// Đại diện cho một slot trong inventory
/// </summary>
[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;
    
    public InventorySlot()
    {
        item = null;
        quantity = 0;
    }
    
    public InventorySlot(ItemData itemData, int qty)
    {
        item = itemData;
        quantity = qty;
    }
    
    /// <summary>
    /// Slot có trống không?
    /// </summary>
    public bool IsEmpty => item == null || quantity <= 0;
    
    /// <summary>
    /// Slot có đầy không?
    /// </summary>
    public bool IsFull => item != null && quantity >= item.maxStackSize;
    
    /// <summary>
    /// Có thể stack thêm item này không?
    /// </summary>
    public bool CanStack(ItemData itemData)
    {
        if (IsEmpty) return true;
        return item == itemData && !IsFull;
    }
    
    /// <summary>
    /// Thêm item vào slot
    /// </summary>
    /// <returns>Số lượng item còn thừa (không thêm được)</returns>
    public int AddItem(ItemData itemData, int qty)
    {
        if (itemData == null || qty <= 0) return qty;
        
        // Slot trống
        if (IsEmpty)
        {
            item = itemData;
            int amountToAdd = UnityEngine.Mathf.Min(qty, itemData.maxStackSize);
            quantity = amountToAdd;
            return qty - amountToAdd;
        }
        
        // Slot có item khác
        if (item != itemData) return qty;
        
        // Stack vào slot hiện tại
        int space = item.maxStackSize - quantity;
        int amountAdded = UnityEngine.Mathf.Min(space, qty);
        quantity += amountAdded;
        
        return qty - amountAdded;
    }
    
    /// <summary>
    /// Xóa item khỏi slot
    /// </summary>
    /// <returns>Số lượng item đã xóa thực tế</returns>
    public int RemoveItem(int qty)
    {
        if (IsEmpty) return 0;
        
        int amountToRemove = UnityEngine.Mathf.Min(quantity, qty);
        quantity -= amountToRemove;
        
        if (quantity <= 0)
        {
            Clear();
        }
        
        return amountToRemove;
    }
    
    /// <summary>
    /// Xóa toàn bộ slot
    /// </summary>
    public void Clear()
    {
        item = null;
        quantity = 0;
    }
    
    /// <summary>
    /// Copy data từ slot khác
    /// </summary>
    public void CopyFrom(InventorySlot other)
    {
        if (other == null)
        {
            Clear();
            return;
        }
        
        item = other.item;
        quantity = other.quantity;
    }
}
