using UnityEngine;
using TMPro;

/// <summary>
/// Hiển thị prompt "Nhấn E để nhặt [Item]" khi nhìn vào item
/// </summary>
public class PickupPromptUI : MonoBehaviour
{
    public static PickupPromptUI Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string promptFormat = "Nhấn [E] để nhặt {0}";
    
    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 5f;
    
    private CanvasGroup _canvasGroup;
    private bool _isVisible;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        _canvasGroup = promptPanel?.GetComponent<CanvasGroup>();
        if (_canvasGroup == null && promptPanel != null)
        {
            _canvasGroup = promptPanel.AddComponent<CanvasGroup>();
        }
        
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
        
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }
    
    void Update()
    {
        if (_canvasGroup == null) return;
        
        // Smooth fade in/out
        float targetAlpha = _isVisible ? 1f : 0f;
        _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        
        // Ẩn panel khi alpha gần 0
        if (_canvasGroup.alpha < 0.01f && promptPanel != null && promptPanel.activeSelf)
        {
            promptPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Hiển thị pickup prompt
    /// </summary>
    public void ShowPrompt(string itemName)
    {
        if (promptPanel != null && !promptPanel.activeSelf)
            promptPanel.SetActive(true);
        
        if (promptText != null)
        {
            promptText.text = string.Format(promptFormat, itemName);
        }
        
        _isVisible = true;
    }
    
    /// <summary>
    /// Ẩn pickup prompt
    /// </summary>
    public void HidePrompt()
    {
        _isVisible = false;
    }
}
