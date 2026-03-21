using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Màn hình giao diện full screen của Sổ tay Côn trùng.
/// Khi ở trong game, nhấn N hoặc B để bật/tắt sổ tay.
/// Thiết kế 2 nửa: Trái là danh sách, Phải là chi tiết.
/// </summary>
public class EncyclopediaUI : MonoBehaviour
{
    public static EncyclopediaUI Instance { get; private set; }

    [Header("Main Panel")]
    public GameObject mainPanel;

    [Header("List View")]
    public Transform listContent;
    public GameObject insectBtnPrefab;

    [Header("Detail View")]
    public Image detailImage;
    public TextMeshProUGUI detailNameTxt;
    public TextMeshProUGUI detailDescTxt;
    public TextMeshProUGUI detailDangerTxt;
    public TextMeshProUGUI detailFactTxt;

    [Header("Buttons")]
    public Button closeBtn;

    private bool _isOpen = false;
    private List<GameObject> _spawnedBtns = new List<GameObject>();
    private CursorLockMode _prevCursorLockMode;
    private bool _prevCursorVisible;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (closeBtn) closeBtn.onClick.AddListener(Close);
    }

    void Update()
    {
        // Không thao tác nếu TimeScale = 0 ngoại trừ lúc đang mở chính Sổ tay này
        if (Time.timeScale == 0f && !_isOpen && !PauseMenuManager.IsPaused) return;

        bool togglePressed = false;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && (kb.bKey.wasPressedThisFrame || kb.nKey.wasPressedThisFrame))
            togglePressed = true;
#else
        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.B))
            togglePressed = true;
#endif

        if (togglePressed)
        {
            if (_isOpen) Close();
            else Open();
        }
    }

    public void Open()
    {
        if (_isOpen) return;

        // Nếu Pause Menu đang mở thì không cho mở Sổ
        if (PauseMenuManager.IsPaused) return; 

        _isOpen = true;
        mainPanel.SetActive(true);
        
        _prevCursorLockMode = Cursor.lockState;
        _prevCursorVisible = Cursor.visible;

        Time.timeScale = 0f; // Dừng thời gian game

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (UIAudioFeedback.Instance != null) UIAudioFeedback.Play(UIAudioFeedback.SoundType.Tab);

        RefreshList();
    }

    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        mainPanel.SetActive(false);
        Time.timeScale = 1f;

        // Khôi phục lại trạng thái chuột trước đó (hoặc ép khóa chuột nếu không có UI khác đang mở)
        bool shouldLock = true;
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) shouldLock = false;
        if (SettingsUI.Instance != null && SettingsUI.Instance.settingsPanel.activeSelf) shouldLock = false;

        if (shouldLock)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = _prevCursorVisible;
            Cursor.lockState = _prevCursorLockMode;
        }

        if (UIAudioFeedback.Instance != null) UIAudioFeedback.Play(UIAudioFeedback.SoundType.Hover);
    }

    private void RefreshList()
    {
        // Xóa nút củ
        foreach (var b in _spawnedBtns) Destroy(b);
        _spawnedBtns.Clear();

        if (EncyclopediaManager.Instance == null) return;

        var allInsects = EncyclopediaManager.Instance.GetAllInsects();
        if (allInsects.Count > 0)
        {
            ShowDetail(allInsects[0]); // Mặc định show con đầu tiên
        }

        foreach (var insect in allInsects)
        {
            GameObject btnObj = Instantiate(insectBtnPrefab, listContent);
            btnObj.SetActive(true); // <--- QUAN TRỌNG: Hiện nút lên vì prefab đang bị ẩn
            _spawnedBtns.Add(btnObj);

            bool isUnlocked = EncyclopediaManager.Instance.IsUnlocked(insect.insectID);

            Transform iconMask = btnObj.transform.Find("IconMask");
            Image iconImg = iconMask != null ? iconMask.Find("IconImg").GetComponent<Image>() : btnObj.transform.Find("Icon").GetComponent<Image>();
            TextMeshProUGUI nameTxt = btnObj.transform.Find("Name").GetComponent<TextMeshProUGUI>();

            if (isUnlocked)
            {
                if (insect.unlockedSprite != null) iconImg.sprite = insect.unlockedSprite;
                iconImg.color = Color.white;
                nameTxt.text = insect.insectName;
            }
            else
            {
                if (insect.lockedSprite != null) 
                    iconImg.sprite = insect.lockedSprite;
                else if (insect.unlockedSprite != null) 
                    iconImg.sprite = insect.unlockedSprite;

                iconImg.color = new Color(0, 0, 0, 0.9f); // Đen thui (Cái bóng)
                nameTxt.text = "???";
            }

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (UIAudioFeedback.Instance != null) UIAudioFeedback.Play(UIAudioFeedback.SoundType.Click);
                ShowDetail(insect);
            });
        }
    }

    private void ShowDetail(InsectData insect)
    {
        bool isUnlocked = EncyclopediaManager.Instance != null && EncyclopediaManager.Instance.IsUnlocked(insect.insectID);

        if (isUnlocked)
        {
            if (insect.unlockedSprite != null) detailImage.sprite = insect.unlockedSprite;
            detailImage.color = Color.white;
            detailNameTxt.text = insect.insectName;
            detailDescTxt.text = insect.description;
            detailDangerTxt.text = "Mức độ: " + GetDangerString(insect.dangerLevel);
            detailFactTxt.text = "Sự thật thú vị:\n" + insect.funFact;
        }
        else
        {
            if (insect.lockedSprite != null) 
                detailImage.sprite = insect.lockedSprite;
            else if (insect.unlockedSprite != null) 
                detailImage.sprite = insect.unlockedSprite;

            detailImage.color = new Color(0, 0, 0, 0.9f);
            detailNameTxt.text = "???";
            detailDescTxt.text = "Bạn chưa khám phá ra sinh vật này. Hãy năng nổ phiêu lưu, nói chuyện và khám phá thế giới xung quanh nhé!";
            detailDangerTxt.text = "Mức độ: <color=#808080>Chưa phân loại</color>";
            detailFactTxt.text = "Sự thật thú vị:\n???";
        }
    }

    private string GetDangerString(InsectDangerLevel lvl)
    {
        switch (lvl)
        {
            case InsectDangerLevel.HienLanh: return "<color=#4CAF50>Hiền lành</color>"; // Xanh lá
            case InsectDangerLevel.NguyHiem: return "<color=#FF9800>Nguy hiểm</color>"; // Cam
            case InsectDangerLevel.CucDoc: return "<color=#F44336>Cực độc</color>"; // Đỏ
            default: return "Chưa rõ";
        }
    }
}
