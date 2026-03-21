using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ArmWrestlingManager : MonoBehaviour
{
    public static ArmWrestlingManager Instance { get; private set; }

    [Header("Audio SFX")]
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip winSFX;
    public AudioClip loseSFX;
    public AudioClip bgmClip;
    private AudioSource _audioSource;
    private AudioSource _bgmSource;

    [Header("Story")]
    public Sprite[] storyImages;

    [Header("Health Settings")]
    private const int maxLives = 5;
    private int _currentLives = 5;

    [Header("Settings")]
    public float maxPower = 100f;
    public float targetKeysCount = 5; // Số phím mũi tên cần bấm trong 1 lượt
    public float roundTime = 3f;      // Thời gian cho mỗi lượt gõ
    
    // Gánh nặng của việc giữ cự ly với Dế Trũi
    public float drainPerSecond = 5f; 
    public float successPowerGain = 15f;
    public float failPowerLoss = 10f;

    private bool _isGameActive = false;
    private float _currentPower = 50f;
    private bool _isGameOver = false;
    private string _winnerText = "";
    
    // Hệ thống phím
    private List<KeyCode> _targetSequence = new List<KeyCode>();
    private int _currentKeyIndex = 0;
    private float _roundTimer = 0f;
    
    // Tham chiếu NPC để trả control khi kết thúc
    private INPCMinigame _currentNPC;

    // Dùng để tạo mảng ngẫu nhiên 4 mũi tên
    private KeyCode[] _arrowKeys = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };

    public bool IsGameActive => _isGameActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
        
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.spatialBlend = 0f;
        _bgmSource.loop = true;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
            _audioSource.PlayOneShot(clip);
    }

    private string GetNpcDisplayName()
    {
        if (_currentNPC == null) return "Đối thủ";
        string nameLower = _currentNPC.npcName.ToLower();
        if (nameLower.Contains("detrui")) return "Dế Trũi";
        if (nameLower.Contains("dechoat")) return "Dế Choắt";
        if (nameLower.Contains("xentoc")) return "Xén Tóc";
        return _currentNPC.npcName;
    }

    /// <summary>
    /// Bắt đầu game Vật Tay.
    /// </summary>
    public void StartGame(INPCMinigame npc, bool resetLives = true)
    {
        _currentNPC = npc;
        
        if (resetLives)
        {
            _currentLives = maxLives;
        }
        
        _currentPower = maxPower / 2f; // Bắt đầu ở mốc 50%
        _isGameOver = false;
        _winnerText = "";
        _isGameActive = true;
        _currentNPC.isMinigameActive = true;
        
        // Mở khoá chuột để có thể bấm nút Tắt
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tắt nhạc nền chung, bật nhạc Minigame
        if (BackgroundMusicManager.Instance != null) BackgroundMusicManager.Instance.PauseMusic();
        if (_bgmSource != null && bgmClip != null)
        {
            _bgmSource.clip = bgmClip;
            _bgmSource.volume = 0.8f; // Tăng âm lượng nhạc nền
            _bgmSource.Play();
        }

        GenerateNewSequence();
    }

    private void GenerateNewSequence()
    {
        _targetSequence.Clear();
        _currentKeyIndex = 0;
        _roundTimer = roundTime;

        for (int i = 0; i < targetKeysCount; i++)
        {
            _targetSequence.Add(_arrowKeys[Random.Range(0, _arrowKeys.Length)]);
        }
    }

    private void Update()
    {
        if (!_isGameActive) return;

        if (_isGameOver)
        {
            var kbUI = Keyboard.current;
            if (kbUI != null)
            {
                if (_currentLives > 0 && kbUI.digit1Key.wasPressedThisFrame)
                {
                    StartGame(_currentNPC, false);
                }
                else if (kbUI.digit2Key.wasPressedThisFrame)
                {
                    QuitGame();
                }
            }
            return;
        }

        // Trọng lực: Sức bền của Dế Trũi đẩy power về 0 (Thua) liên tục
        _currentPower -= drainPerSecond * Time.deltaTime;

        // Đếm ngược thời gian lượt gõ
        _roundTimer -= Time.deltaTime;
        
        if (_roundTimer <= 0)
        {
            // Hết giờ -> Fail lượt
            HandleTurnResult(false);
            return;
        }

        KeyCode pressedArrow = GetPressedArrowKey();

        if (pressedArrow != KeyCode.None)
        {
            if (pressedArrow == _targetSequence[_currentKeyIndex])
            {
                // Gõ đúng
                PlaySFX(correctSFX);
                _currentKeyIndex++;
                
                // Nếu gõ xong nguyên chuỗi
                if (_currentKeyIndex >= _targetSequence.Count)
                {
                    HandleTurnResult(true);
                }
            }
            else
            {
                // Gõ sai
                PlaySFX(wrongSFX);
                HandleTurnResult(false);
            }
        }

        // Check Win/Lose
        string npcNameUpper = GetNpcDisplayName().ToUpper();
        if (_currentPower >= maxPower)
        {
            _currentPower = maxPower;
            _isGameOver = true;
            _winnerText = $"SỨC MẠNH VÔ SONG! BẠN ĐÃ QUẬT NGÃ {npcNameUpper}!";
            PlaySFX(winSFX);
        }
        else if (_currentPower <= 0)
        {
            _currentPower = 0;
            _isGameOver = true;
            _currentLives--;
            
            if (_currentLives > 0)
            {
                _winnerText = $"YẾU XÌU! BẠN BỊ {npcNameUpper} NGHIỀN NÁT! (-1 Mạng)";
            }
            else
            {
                _winnerText = $"BẠN ĐÃ HẾT MẠNG! HÃY NGHỈ TAY RỒI QUAY LẠI SAU!";
            }
            PlaySFX(loseSFX);
        }
    }

    /// <summary>
    /// Xử lý hậu quả của việc Gõ đúng hoặc Gõ sai/Hết giờ
    /// </summary>
    private void HandleTurnResult(bool success)
    {
        if (success)
        {
            _currentPower += successPowerGain;
        }
        else
        {
            _currentPower -= failPowerLoss;
        }
        
        GenerateNewSequence();
    }

    private KeyCode GetPressedArrowKey()
    {
        var kb = Keyboard.current;
        if (kb == null) return KeyCode.None;
        
        if (kb.upArrowKey.wasPressedThisFrame) return KeyCode.UpArrow;
        if (kb.downArrowKey.wasPressedThisFrame) return KeyCode.DownArrow;
        if (kb.leftArrowKey.wasPressedThisFrame) return KeyCode.LeftArrow;
        if (kb.rightArrowKey.wasPressedThisFrame) return KeyCode.RightArrow;
        
        return KeyCode.None;
    }

    private string GetKeyArrowSymbol(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.UpArrow: return "↑";
            case KeyCode.DownArrow: return "↓";
            case KeyCode.LeftArrow: return "←";
            case KeyCode.RightArrow: return "→";
            default: return "?";
        }
    }

    private void OnGUI()
    {
        if (!_isGameActive) return;

        // Vẽ màn hình nền mờ
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Vẽ thanh máu (Trái tim) góc trái trên
        GUIStyle hpStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 32,
            fontStyle = FontStyle.Bold
        };
        hpStyle.normal.textColor = new Color(1f, 0.3f, 0.3f); // Đỏ nhạt
        string hearts = "Mạng: ";
        for (int m = 0; m < maxLives; m++)
        {
            hearts += (m < _currentLives) ? "♥" : "♡";
        }
        GUI.Label(new Rect(30, 30, 600, 50), hearts, hpStyle);

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        // Tiêu đề
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 40,
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState() { textColor = Color.yellow }
        };
        string npcNameUpper = GetNpcDisplayName().ToUpper();
        GUI.Label(new Rect(0, centerY - 150, Screen.width, 60), $"THI VẬT TAY CÙNG XÉN TÓC", titleStyle);

        // Vẽ Power Bar (Thanh Sức lực giữa 2 người)
        float barWidth = 400f;
        float barHeight = 30f;
        Rect bgBarRect = new Rect(centerX - barWidth / 2f, centerY - 70, barWidth, barHeight);
        
        // Vẽ Nền Xám của thanh Bar
        GUI.color = Color.gray;
        GUI.DrawTexture(bgBarRect, Texture2D.whiteTexture);

        // Vẽ Fill Màu của thanh Bar (Trắng tới Vàng/Đỏ tuỳ lực)
        float powerPercentage = _currentPower / maxPower;
        Rect fillBarRect = new Rect(bgBarRect.x, bgBarRect.y, barWidth * powerPercentage, barHeight);
        
        Color powerColor = Color.yellow;
        if (powerPercentage < 0.3f) powerColor = Color.red;
        if (powerPercentage > 0.7f) powerColor = Color.green;

        GUI.color = powerColor;
        GUI.DrawTexture(fillBarRect, Texture2D.whiteTexture);
        GUI.color = Color.white; // Reset color

        // Chú thích Bar
        GUIStyle textLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(bgBarRect.x, bgBarRect.y - 30, bgBarRect.width, 30), "Sức mạnh của bạn", textLabel);

        if (_isGameOver)
        {
            // --- GIAO DIỆN KHI KẾT THÚC ---
            GUIStyle resultStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 32,
                fontStyle = FontStyle.Bold
            };
            resultStyle.normal.textColor = _currentPower >= maxPower ? Color.green : Color.red;
            resultStyle.wordWrap = true;
            GUI.Label(new Rect(0, centerY + 20, Screen.width, 80), _winnerText, resultStyle);

            if (_currentLives > 0)
            {
                if (GUI.Button(new Rect(centerX - 120, centerY + 100, 100, 40), "[1] Chơi Lại"))
                {
                    StartGame(_currentNPC, false);
                }
            }
            
            if (GUI.Button(new Rect(centerX + 20, centerY + 100, 100, 40), "[2] Tạm Nghỉ"))
            {
                QuitGame();
            }
        }
        else
        {
            // --- GIAO DIỆN KHI ĐANG CHƠI (Hàng Phím Bấm) ---
            
            // Thanh Thời Gian Của Lược
            float timeRatio = _roundTimer / roundTime;
            Rect timerBar = new Rect(centerX - (barWidth/2 * timeRatio), bgBarRect.yMax + 10, barWidth * timeRatio, 10);
            GUI.color = Color.cyan;
            GUI.DrawTexture(timerBar, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Hiển thị các ô phím Mũi tên (Kiểu Audition)
            float keyBoxSize = 65f; // Làm phím bự hơn
            float spacing = 20f;
            int totalKeys = _targetSequence.Count;
            float startXKeys = centerX - (totalKeys * keyBoxSize + (totalKeys - 1) * spacing) / 2f;

            GUIStyle keyBoxNormal = new GUIStyle(GUI.skin.box) { fontSize = 45, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            GUIStyle keyBoxPassed = new GUIStyle(keyBoxNormal) { normal = new GUIStyleState() { background = Texture2D.whiteTexture, textColor = Color.black } };
            // Style cho phím đang chờ bấm (Pulse effect ngọc cam)
            GUIStyle keyBoxCurrent = new GUIStyle(keyBoxNormal) { normal = new GUIStyleState() { background = Texture2D.whiteTexture, textColor = Color.white } };

            for (int i = 0; i < totalKeys; i++)
            {
                Rect keyRect = new Rect(startXKeys + i * (keyBoxSize + spacing), centerY + 40, keyBoxSize, keyBoxSize);
                
                string symbol = GetKeyArrowSymbol(_targetSequence[i]);

                if (i < _currentKeyIndex)
                {
                    // Phím đã gõ trúng (Sáng màu xanh)
                    GUI.backgroundColor = new Color(0.2f, 1f, 0.2f);
                    GUI.Box(keyRect, symbol, keyBoxPassed);
                }
                else if (i == _currentKeyIndex)
                {
                    // Phím đang cần bấm (Flash màu cam/vàng liên tục)
                    float pulse = Mathf.PingPong(Time.time * 5f, 1f);
                    GUI.backgroundColor = Color.Lerp(new Color(1f, 0.5f, 0f), Color.yellow, pulse);
                    GUI.Box(keyRect, symbol, keyBoxCurrent);
                }
                else
                {
                    // Phím chờ gõ tiếp theo (Tối/Trắng mờ)
                    GUI.backgroundColor = new Color(1f, 1f, 1f, 0.4f);
                    GUI.Box(keyRect, symbol, keyBoxNormal);
                }
                GUI.backgroundColor = Color.white; // Reset
            }

            GUIStyle hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 24, fontStyle = FontStyle.Bold };
            hintStyle.normal.textColor = new Color(1f, 0.8f, 0.2f);
            GUI.Label(new Rect(0, centerY + 130, Screen.width, 30), "Múa nhanh các phím Mũi tên trên bàn phím trước khi hết giờ!", hintStyle);

            // Nút Thoát Ngang
            if (GUI.Button(new Rect(10, 10, 100, 30), "Đầu Hàng Ngay"))
            {
                QuitGame();
            }
        }
    }

    private void QuitGame()
    {
        _isGameActive = false;
        
        // Khóa lại chuột nếu cần
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Tắt nhạc Minigame, mở lại nhạc chung
        if (_bgmSource != null) _bgmSource.Stop();
        if (BackgroundMusicManager.Instance != null) BackgroundMusicManager.Instance.ResumeMusic();

        if (_currentNPC != null)
        {
            // Trả số điểm về xem Player thắng hay thua
            bool isWin = (_currentPower >= maxPower);
            _currentNPC.EndMinigame(isWin, false);

            // Hiện màn hình truyện nếu thắng và có dữ liệu bộ truyện
            if (isWin && storyImages != null && storyImages.Length > 0 && StoryViewerManager.Instance != null)
            {
                StoryViewerManager.Instance.ShowStory(storyImages);
            }
        }
    }
}
