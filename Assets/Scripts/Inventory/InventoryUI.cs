using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý inventory UI panel
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab;
    
    [Header("Settings")]
    [SerializeField] private bool pauseGameWhenOpen = false;
    
    private UIInventorySlot[] _uiSlots;
    private bool _isOpen = false;
    private Animator _animator;
    
    public bool IsOpen => _isOpen;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        _animator = inventoryPanel?.GetComponent<Animator>();
        
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }
    
    void Start()
    {
        // Subscribe to inventory changes
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshUI;
        }
        
        // Create UI slots
        CreateSlots();
        RefreshUI();
    }
    
    void OnDestroy()
    {
        // Unsubscribe
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
        }
    }
    
    void Update()
    {
        // Close với ESC (Tab được xử lý trong PlayerController)
        var kb = Keyboard.current;
        if (_isOpen && kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            CloseInventory();
        }
    }
    
    /// <summary>
    /// Tạo các UI slots
    /// </summary>
    void CreateSlots()
    {
        if (InventoryManager.Instance == null || slotsContainer == null || slotPrefab == null)
        {
            Debug.LogWarning("InventoryUI: Missing references!");
            return;
        }
        
        var slots = InventoryManager.Instance.GetAllSlots();
        _uiSlots = new UIInventorySlot[slots.Length];
        
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            UIInventorySlot uiSlot = slotObj.GetComponent<UIInventorySlot>();
            
            if (uiSlot != null)
            {
                uiSlot.Setup(slots[i], i);
                _uiSlots[i] = uiSlot;
            }
        }
    }
    
    /// <summary>
    /// Refresh tất cả UI slots
    /// </summary>
    void RefreshUI()
    {
        if (_uiSlots == null) return;
        
        foreach (var uiSlot in _uiSlots)
        {
            if (uiSlot != null)
                uiSlot.UpdateUI();
        }
    }
    
    /// <summary>
    /// Toggle inventory
    /// </summary>
    public void ToggleInventory()
    {
        if (_isOpen)
            CloseInventory();
        else
            OpenInventory();
    }
    
    /// <summary>
    /// Mở inventory
    /// </summary>
    public void OpenInventory()
    {
        if (_isOpen) return;
        
        _isOpen = true;
        
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
        
        // Play animation
        if (_animator != null)
            _animator.SetTrigger("Open");
        
        // Pause game
        if (pauseGameWhenOpen)
            Time.timeScale = 0f;
        
        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        RefreshUI();
    }
    
    /// <summary>
    /// Đóng inventory
    /// </summary>
    public void CloseInventory()
    {
        if (!_isOpen) return;
        
        _isOpen = false;
        
        // Play animation
        if (_animator != null)
            _animator.SetTrigger("Close");
        
        // Delay disable panel để animation chạy
        if (inventoryPanel != null)
        {
            Invoke(nameof(DisablePanel), 0.2f);
        }
        
        // Resume game
        if (pauseGameWhenOpen)
            Time.timeScale = 1f;
        
        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void DisablePanel()
    {
        if (inventoryPanel != null && !_isOpen)
            inventoryPanel.SetActive(false);
    }
}
