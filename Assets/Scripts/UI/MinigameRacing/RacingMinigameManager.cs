using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class RacingMinigameManager : MonoBehaviour
{
    [Header("Racers")]
    public RacingPlayer playerRacer;
    public RacingNPC npcRacer;

    [Header("Track Elements")]
    public Transform playerStartPos;
    public Transform npcStartPos;
    public Transform finishLine;
    public GameObject raceArea;

    [Header("UI Elements")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI instructionsText;
    public GameObject endPanel;
    public GameObject resultsPanel; // Added: For the results panel

    [Header("Audio Settings")]
    public AudioClip bgmClip;
    public AudioClip winSFX;
    public AudioClip loseSFX;
    public AudioClip startSFX; // Âm thanh beep đếm ngược
    public AudioClip goSFX;    // Âm thanh BẮT ĐẦU!
    private AudioSource _audioSource;

    private bool _isRacing = false;
    private bool _isFinished = false;

    void Start()
    {
        // Đảm bảo AudioListener tồn tại và được bật trong minigame
        EnsureAudioListener();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = true;
        
        // Dừng nhạc nền chính nếu có
        if (BackgroundMusicManager.Instance != null)
            BackgroundMusicManager.Instance.PauseMusic();

        endPanel.SetActive(false);
        resultsPanel.SetActive(false); // Set results panel inactive
        countdownText.gameObject.SetActive(false); // Set countdown text inactive initially
        resultText.text = "";
        instructionsText.text = "BẤM LUÂN PHIÊN [Trái]/[Phải] HOẶC [A]/[D] ĐỂ CHẠY!\nNHẤN [SPACE] ĐỂ NHẢY QUA RÀO!";
        instructionsText.gameObject.SetActive(true); // Ensure instructions are visible at start

        if (raceArea != null) raceArea.SetActive(false); // Set race area inactive initially
        
        // Thêm offset Y để tránh việc nhân vật bị lún xuống đất
        playerRacer.transform.position = playerStartPos.position + new Vector3(0, 0.5f, 0);
        npcRacer.transform.position = npcStartPos.position + new Vector3(0, 0.5f, 0);

        playerRacer.EnableRunning(false);
        npcRacer.EnableRunning(false);

        // Tăng tốc độ của De Trui
        npcRacer.baseSpeed = 7.0f;
        npcRacer.speedVariation = 1.0f;

        // Start the minigame after initial setup
        StartMiniGame();
    }

    void EnsureAudioListener()
    {
        // Tìm bất kỳ AudioListener nào đang có
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        
        // Nếu không có listener nào, thêm 1 cái vào Camera chính của minigame
        if (listeners.Length == 0)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            
            if (cam != null)
            {
                cam.gameObject.AddComponent<AudioListener>();
                Debug.Log($"[RacingMinigameManager] Đã thêm AudioListener vào {cam.gameObject.name}");
            }
        }
        else
        {
            // Đảm bảo ít nhất 1 cái được bật
            bool anyEnabled = false;
            foreach (var l in listeners) if (l.enabled) anyEnabled = true;
            
            if (!anyEnabled && listeners.Length > 0)
            {
                listeners[0].enabled = true;
                Debug.Log($"[RacingMinigameManager] Đã bật AudioListener trên {listeners[0].gameObject.name}");
            }
        }
    }

    public void StartMiniGame()
    {
        _isRacing = false; // Reset racing state
        _isFinished = false; // Reset finished state
        resultsPanel.SetActive(false);
        endPanel.SetActive(false); // Ensure end panel is also off
        
        if (raceArea != null) raceArea.SetActive(true);
        
        // Disable controls initially
        playerRacer.EnableRunning(false);
        npcRacer.EnableRunning(false);
        
        // Teleport to start (re-position in case of restart)
        playerRacer.transform.position = playerStartPos != null ? playerStartPos.position + new Vector3(0, 0.5f, 0) : Vector3.zero;
        npcRacer.transform.position = npcStartPos != null ? npcStartPos.position + new Vector3(0, 0.5f, 0) : new Vector3(2, 0, 0);
        
        instructionsText.gameObject.SetActive(true); // Show instructions before countdown
        StartCoroutine(RaceCountdownRoutine());
    }

    private IEnumerator RaceCountdownRoutine()
    {
        // Dừng nhạc nền chính
        BackgroundMusicManager bmm = BackgroundMusicManager.Instance;
        if (bmm != null) bmm.PauseMusic();

        countdownText.gameObject.SetActive(true);
        
        string[] countdowns = { "3", "2", "1", "BẮT ĐẦU!" };
        foreach (string s in countdowns)
        {
            countdownText.text = s;
            // Hiệu ứng scale nhẹ
            countdownText.transform.localScale = Vector3.one * 1.5f;
            
            // Play a beep sound if available
            if (s == "BẮT ĐẦU!")
            {
                if (goSFX != null) _audioSource.PlayOneShot(goSFX, 0.5f);
            }
            else
            {
                if (startSFX != null) _audioSource.PlayOneShot(startSFX, 0.4f);
            }

            float timer = 0f;
            while (timer < 1f)
            {
                timer += Time.deltaTime;
                countdownText.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, timer);
                yield return null;
            }
        }
        
        countdownText.gameObject.SetActive(false);
        _isRacing = true; // Start racing state
        
        // Bắt đầu nhạc đua
        if (bgmClip != null)
        {
            _audioSource.clip = bgmClip;
            _audioSource.volume = 0.6f;
            _audioSource.Play();
        }

        playerRacer.EnableRunning(true);
        npcRacer.EnableRunning(true);

        // Hide instructions after countdown
        instructionsText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_isRacing || _isFinished) return;

        // Check if anyone passed the finish line Z coordinate
        if (playerRacer.transform.position.z >= finishLine.position.z)
        {
            EndRace(true); // Player Wins
        }
        else if (npcRacer.transform.position.z >= finishLine.position.z)
        {
            EndRace(false); // NPC Wins
        }
    }

    private void EndRace(bool playerWon)
    {
        _isFinished = true;
        _isRacing = false;

        playerRacer.EnableRunning(false);
        npcRacer.EnableRunning(false);

        // Dừng nhạc đua
        if (_audioSource != null) _audioSource.Stop();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        endPanel.SetActive(true);

        if (playerWon)
        {
            resultText.text = "CHIẾN THẮNG!";
            resultText.color = Color.green;
            if (winSFX != null && _audioSource != null) _audioSource.PlayOneShot(winSFX);
        }
        else
        {
            resultText.text = "THUA CUỘC!";
            resultText.color = Color.red;
            if (loseSFX != null && _audioSource != null) _audioSource.PlayOneShot(loseSFX);
        }
    }

    // Gắn vào hàm OnClick của nút Chơi Lại
    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    // Gắn vào hàm OnClick của nút Kết Thúc
    public void ReturnToMainScene()
    {
        // Tiếp tục nhạc nền chính
        if (BackgroundMusicManager.Instance != null)
            BackgroundMusicManager.Instance.ResumeMusic();

        // Kiểm tra xem Scene cũ tên gì, mặc định là SampleScene
        string previousScene = PlayerPrefs.GetString("PreviousScene", "SampleScene");
        
        // Cắm cờ báo cho người chơi biết là vừa mới đi đua về
        PlayerPrefs.SetInt("ReturnedFromRace", 1);
        
        SceneManager.LoadScene(previousScene);
    }
}
