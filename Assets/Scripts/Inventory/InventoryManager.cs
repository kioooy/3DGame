using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton quản lý toàn bộ inventory của player
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
    [Header("Inventory Settings")]
    [Tooltip("Số lượng slots trong inventory")]
    [SerializeField] private int inventorySize = 20;
    
    private InventorySlot[] _slots;
    
    /// <summary>
    /// Event được gọi khi inventory thay đổi
    /// </summary>
    public event Action OnInventoryChanged;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Khởi tạo inventory
        InitializeInventory();
    }
    
    void InitializeInventory()
    {
        _slots = new InventorySlot[inventorySize];
        for (int i = 0; i < inventorySize; i++)
        {
            _slots[i] = new InventorySlot();
        }
    }
    
    /// <summary>
    /// Lấy tất cả slots
    /// </summary>
    public InventorySlot[] GetAllSlots() => _slots;
    
    /// <summary>
    /// Lấy slot tại index
    /// </summary>
    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= _slots.Length) return null;
        return _slots[index];
    }
    
    /// <summary>
    /// Thêm item vào inventory
    /// </summary>
    public bool AddItem(ItemData itemData, int quantity)
    {
        if (itemData == null || quantity <= 0)
        {
            Debug.LogWarning("Không thể thêm item null hoặc quantity <= 0");
            return false;
        }
        
        int remainingQty = quantity;
        
        // Bước 1: Thử stack vào các slot đã có item này
        if (itemData.IsStackable)
        {
            for (int i = 0; i < _slots.Length && remainingQty > 0; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].item == itemData && !_slots[i].IsFull)
                {
                    remainingQty = _slots[i].AddItem(itemData, remainingQty);
                }
            }
        }
        
        // Bước 2: Thêm vào các slot trống
        for (int i = 0; i < _slots.Length && remainingQty > 0; i++)
        {
            if (_slots[i].IsEmpty)
            {
                remainingQty = _slots[i].AddItem(itemData, remainingQty);
            }
        }
        
        // Notify UI update
        OnInventoryChanged?.Invoke();
        
        // Kiểm tra xem có thêm hết không
        if (remainingQty > 0)
        {
            Debug.LogWarning($"Inventory đầy! Không thể thêm {remainingQty} {itemData.itemName}");
            return false;
        }
        
        Debug.Log($"Đã thêm {quantity}x {itemData.itemName} vào inventory");
        return true;
    }
    
    /// <summary>
    /// Xóa item khỏi inventory
    /// </summary>
    public bool RemoveItem(ItemData itemData, int quantity)
    {
        if (itemData == null || quantity <= 0) return false;
        
        // Kiểm tra xem có đủ item không
        if (!HasItem(itemData, quantity))
        {
            Debug.LogWarning($"Không đủ {itemData.itemName} để xóa");
            return false;
        }
        
        int remainingQty = quantity;
        
        // Xóa từ các slots
        for (int i = 0; i < _slots.Length && remainingQty > 0; i++)
        {
            if (!_slots[i].IsEmpty && _slots[i].item == itemData)
            {
                int removed = _slots[i].RemoveItem(remainingQty);
                remainingQty -= removed;
            }
        }
        
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    /// <summary>
    /// Kiểm tra có item không
    /// </summary>
    public bool HasItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null) return false;
        
        int count = 0;
        foreach (var slot in _slots)
        {
            if (!slot.IsEmpty && slot.item == itemData)
            {
                count += slot.quantity;
                if (count >= quantity) return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Đếm số lượng item
    /// </summary>
    public int GetItemCount(ItemData itemData)
    {
        if (itemData == null) return 0;
        
        int count = 0;
        foreach (var slot in _slots)
        {
            if (!slot.IsEmpty && slot.item == itemData)
            {
                count += slot.quantity;
            }
        }
        
        return count;
    }
    
    /// <summary>
    /// Tìm slot trống đầu tiên
    /// </summary>
    public int GetFirstEmptySlotIndex()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].IsEmpty) return i;
        }
        return -1;
    }
    
    /// <summary>
    /// Swap 2 slots
    /// </summary>
    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= _slots.Length || indexB < 0 || indexB >= _slots.Length)
        {
            Debug.LogWarning("Invalid slot indices for swap");
            return;
        }
        
        InventorySlot temp = new InventorySlot();
        temp.CopyFrom(_slots[indexA]);
        _slots[indexA].CopyFrom(_slots[indexB]);
        _slots[indexB].CopyFrom(temp);
        
        OnInventoryChanged?.Invoke();
    }
    
    /// <summary>
    /// Merge slot A vào slot B (nếu cùng loại)
    /// </summary>
    public void MergeSlots(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _slots.Length || toIndex < 0 || toIndex >= _slots.Length)
            return;
        
        var fromSlot = _slots[fromIndex];
        var toSlot = _slots[toIndex];
        
        if (fromSlot.IsEmpty) return;
        
        // Nếu slot đích trống, chuyển hết sang
        if (toSlot.IsEmpty)
        {
            toSlot.CopyFrom(fromSlot);
            fromSlot.Clear();
        }
        // Nếu cùng loại item, merge
        else if (toSlot.item == fromSlot.item)
        {
            int remaining = toSlot.AddItem(fromSlot.item, fromSlot.quantity);
            fromSlot.quantity = remaining;
            if (remaining <= 0)
            {
                fromSlot.Clear();
            }
        }
        
        OnInventoryChanged?.Invoke();
    }
    
    /// <summary>
    /// Clear toàn bộ inventory
    /// </summary>
    public void ClearInventory()
    {
        foreach (var slot in _slots)
        {
            slot.Clear();
        }
        OnInventoryChanged?.Invoke();
    }
}
