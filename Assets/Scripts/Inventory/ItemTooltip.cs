using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Hiển thị tooltip khi hover vào item
/// </summary>
public class ItemTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip Settings")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private float showDelay = 0.5f;
    
    private UIInventorySlot _uiSlot;
    private float _hoverTime;
    private bool _isHovering;
    
    void Awake()
    {
        _uiSlot = GetComponent<UIInventorySlot>();
        
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
    
    void Update()
    {
        if (_isHovering)
        {
            _hoverTime += Time.unscaledDeltaTime;
            
            if (_hoverTime >= showDelay && tooltipPanel != null && !tooltipPanel.activeSelf)
            {
                ShowTooltip();
            }
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        _hoverTime = 0f;
        
        // Highlight slot
        if (_uiSlot != null)
            _uiSlot.SetHighlight(true);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        _hoverTime = 0f;
        HideTooltip();
        
        // Remove highlight
        if (_uiSlot != null)
            _uiSlot.SetHighlight(false);
    }
    
    void ShowTooltip()
    {
        if (_uiSlot == null || _uiSlot.Slot == null || _uiSlot.Slot.IsEmpty)
            return;
        
        if (tooltipPanel == null) return;
        
        // Set text
        if (itemNameText != null)
            itemNameText.text = _uiSlot.GetItemName();
        
        if (itemDescriptionText != null)
            itemDescriptionText.text = _uiSlot.GetItemDescription();
        
        tooltipPanel.SetActive(true);
        
        // Position tooltip near mouse
        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero;
        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        if (tooltipRect != null)
        {
            tooltipRect.position = mousePos + new Vector2(20, -20);
        }
    }
    
    void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
    
    void OnDisable()
    {
        HideTooltip();
    }
}
