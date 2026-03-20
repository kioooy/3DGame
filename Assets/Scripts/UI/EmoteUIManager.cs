using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class EmoteUIManager : MonoBehaviour
{
    public static EmoteUIManager Instance { get; private set; }

    [Header("Emote Settings")]
    [SerializeField] private float radius = 150f;
    [SerializeField] private float centerDeadZone = 30f; // Bán kính vùng chết ko chọn gì
    // Danh sách tên Trigger trong Animator.
    // Index: 0=Right, 1=Top, 2=Left, 3=Bottom
    [SerializeField] private List<string> emoteTriggers = new List<string> { "Emote1", "Emote2", "Emote3", "Emote4" };
    // Tên hiển thị tương ứng lên UI
    [SerializeField] private List<string> emoteDisplayNames = new List<string> { "Dance", "Girl Skin", "Waving", "None" };

    public static bool IsEmoteMenuOpen { get; private set; }

    private Vector2 _screenCenter;
    private int _selectedIndex = -1;
    private bool _wasCursorVisible;
    private CursorLockMode _previousLockMode;
    
    private string _lastEmoteTrigger = ""; // Lưu lại biểu cảm cuối cùng
    private bool _isPlayingEmote = false;
    private int _defaultStateHash; // Lưu mã Hash của Node Nhảy thường (Idle/Walk)
    
    // Tham chiếu Animator nhân vật
    private Animator _playerAnimator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        // Tìm animator từ player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerAnimator = player.GetComponentInChildren<Animator>();
        }

        // Delay 1 chút ở frame đầu tiên để Animator có đủ thời gian thiết lập trạng thái gốc
        Invoke(nameof(SaveDefaultAnimatorState), 0.5f);
    }

    private void SaveDefaultAnimatorState()
    {
        if (_playerAnimator != null)
        {
            // Lưu lại thông tin state mặc định (như Idle / Locomotion) để ép trả về khi CancelEmote
            _defaultStateHash = _playerAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        }
    }

    public void OpenRadialMenu()
    {
        if (IsEmoteMenuOpen) return;

        IsEmoteMenuOpen = true;
        _selectedIndex = -1;
        
        // Cập nhật center đề phòng resize màn hình
        _screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // Lưu trạng thái chuột hiện tại
        _wasCursorVisible = Cursor.visible;
        _previousLockMode = Cursor.lockState;

        // Unlock và show con trỏ chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Di chuyển chuột ảo về giữa màn hình (InputSystem)
        Mouse.current?.WarpCursorPosition(_screenCenter);
    }

    public void CloseRadialMenu()
    {
        if (!IsEmoteMenuOpen) return;
        
        IsEmoteMenuOpen = false;

        // Phục hồi chuột
        Cursor.visible = _wasCursorVisible;
        Cursor.lockState = _previousLockMode;

        // Thực thi Emote nếu có chọn
        if (_selectedIndex >= 0 && _selectedIndex < emoteTriggers.Count)
        {
            PlayEmote(emoteTriggers[_selectedIndex]);
        }
    }

    private void Update()
    {
        if (!IsEmoteMenuOpen) return;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            Vector2 dir = mousePos - _screenCenter;
            
            float distance = dir.magnitude;
            if (distance < centerDeadZone)
            {
                _selectedIndex = -1; // Chuột ở quá gần tâm -> ko chọn
            }
            else
            {
                // Tính góc (-180 tới 180). Lật trục Y vì tọa độ màn render GUI (0,0) nằm góc trên trái
                // Tuy nhiên mouse.position.ReadValue() có (0,0) ở góc dưới trái. 
                // Ta phải tinh chỉnh cho logic dễ.
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                
                // Quy về 0-360
                if (angle < 0) angle += 360f;

                // Chia 4 vùng (Mỗi vùng 90 độ)
                // -45 đến +45 (315-360 và 0-45): Right (index 0)
                // 45 đến 135: Top (index 1)
                // 135 đến 225: Left (index 2)
                // 225 đến 315: Bottom (index 3)
                
                int segment = Mathf.FloorToInt((angle + 45f) / 90f) % 4;
                _selectedIndex = segment;
            }
        }
    }

    private void PlayEmote(string triggerName)
    {
        if (_playerAnimator != null && !string.IsNullOrEmpty(triggerName) && triggerName != "None")
        {
            // Reset các trigger cũ tránh xếp chồng
            foreach(var trig in emoteTriggers) {
                _playerAnimator.ResetTrigger(trig);
            }
            
            _playerAnimator.SetTrigger(triggerName);
            _lastEmoteTrigger = triggerName;
            _isPlayingEmote = true;
            Debug.Log($"[Emote] Playing: {triggerName}");
        }
    }

    /// <summary>
    /// Chơi lại hành động trước đó nếu nhấn phím một lần
    /// </summary>
    public void PlayLastEmote()
    {
        if (!string.IsNullOrEmpty(_lastEmoteTrigger))
        {
            PlayEmote(_lastEmoteTrigger);
        }
    }

    /// <summary>
    /// Ngắt hành động đang chơi (chuyển về Idle/Walk)
    /// </summary>
    public void CancelEmote()
    {
        if (_isPlayingEmote && _playerAnimator != null)
        {
            // Tắt các trigger cũ
            foreach(var trig in emoteTriggers) {
                _playerAnimator.ResetTrigger(trig);
            }

            // Gọi Trigger ngắt (Sẽ kích hoạt Transition cancelTr đã cài trong file Animator, nếu người dùng có Update Tool)
            _playerAnimator.SetTrigger("CancelEmote");

            // --- PHƯƠNG ÁN CHẮC KÈO 100% ---
            // Ép Animator Crossfade mượt 0.1s về Node Di Chuyển Gốc mà ta đã lưu lúc Start.
            // Phương án này bypass luôn cả việc người dùng đã làm đủ các bước cài Animator Editor hay chưa.
            if (_defaultStateHash != 0)
            {
                _playerAnimator.CrossFade(_defaultStateHash, 0.1f, 0);
            }

            _isPlayingEmote = false;
        }
    }

    private void OnGUI()
    {
        if (!IsEmoteMenuOpen) return;

        // Đổ bóng background / Filter che mờ màn hình
        GUI.color = new Color(0, 0, 0, 0.4f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        
        GUIStyle baseStyle = new GUIStyle(GUI.skin.box);
        baseStyle.alignment = TextAnchor.MiddleCenter;
        baseStyle.fontStyle = FontStyle.Bold;
        baseStyle.normal.textColor = Color.white;
        
        // Vẽ 4 ô (Tạm giả lập UI cho lẹ)
        DrawEmoteSlot(1, "Top", new Vector2(_screenCenter.x, Screen.height - _screenCenter.y - radius));
        DrawEmoteSlot(3, "Bottom", new Vector2(_screenCenter.x, Screen.height - _screenCenter.y + radius));
        DrawEmoteSlot(2, "Left", new Vector2(_screenCenter.x - radius, Screen.height - _screenCenter.y));
        DrawEmoteSlot(0, "Right", new Vector2(_screenCenter.x + radius, Screen.height - _screenCenter.y));
        
        // Center node
        GUI.color = _selectedIndex == -1 ? Color.yellow : Color.gray;
        GUI.Box(new Rect(_screenCenter.x - 20, Screen.height - _screenCenter.y - 20, 40, 40), "X", baseStyle);
    }

    private void DrawEmoteSlot(int index, string label, Vector2 pos)
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleCenter;
        
        if (_selectedIndex == index) 
        {
            GUI.color = Color.green; // Highlight
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
        }
        else 
        {
            GUI.color = Color.gray;
            style.fontSize = 14;
            style.normal.textColor = Color.lightGray;
        }

        float s = _selectedIndex == index ? 80f : 60f;
        Rect rect = new Rect(pos.x - s/2, pos.y - s/2, s, s);
        
        string triggerName = index < emoteTriggers.Count ? emoteTriggers[index] : "Empty";
        GUI.Box(rect, triggerName, style);
        
        // Vẽ thêm tên thân thiện ở bên dưới ô Box
        string displayName = index < emoteDisplayNames.Count ? emoteDisplayNames[index] : triggerName;
        Rect labelRect = new Rect(pos.x - 50, pos.y + s/2 + 5, 100, 25);
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = _selectedIndex == index ? Color.green : Color.white;
        
        GUI.Label(labelRect, displayName, labelStyle);
    }
}
