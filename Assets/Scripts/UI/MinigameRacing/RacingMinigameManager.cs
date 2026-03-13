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

    [Header("UI Elements")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI instructionsText;
    public GameObject endPanel;

    private bool _isRacing = false;
    private bool _isFinished = false;

    void Start()
    {
        endPanel.SetActive(false);
        resultText.text = "";
        instructionsText.text = "BẤM LUÂN PHIÊN [Trái]/[Phải] HOẶC [A]/[D] ĐỂ CHẠY!\nNHẤN [SPACE] ĐỂ NHẢY QUA RÀO!";
        
        // Thêm offset Y để tránh việc nhân vật bị lún xuống đất
        playerRacer.transform.position = playerStartPos.position + new Vector3(0, 0.5f, 0);
        npcRacer.transform.position = npcStartPos.position + new Vector3(0, 0.5f, 0);

        playerRacer.EnableRunning(false);
        npcRacer.EnableRunning(false);

        // Tăng tốc độ của De Trui
        npcRacer.baseSpeed = 7.0f;
        npcRacer.speedVariation = 1.0f;

        StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        countdownText.gameObject.SetActive(true);
        
        countdownText.text = "3";
        yield return new WaitForSeconds(1f);
        
        countdownText.text = "2";
        yield return new WaitForSeconds(1f);
        
        countdownText.text = "1";
        yield return new WaitForSeconds(1f);
        
        countdownText.text = "BẮT ĐẦU!";
        _isRacing = true;
        playerRacer.EnableRunning(true);
        npcRacer.EnableRunning(true);

        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);
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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        endPanel.SetActive(true);

        if (playerWon)
        {
            resultText.text = "CHIẾN THẮNG!";
            resultText.color = Color.green;
        }
        else
        {
            resultText.text = "THUA CUỘC!";
            resultText.color = Color.red;
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
        // Kiểm tra xem Scene cũ tên gì, mặc định là SampleScene
        string previousScene = PlayerPrefs.GetString("PreviousScene", "SampleScene");
        
        // Cắm cờ báo cho người chơi biết là vừa mới đi đua về
        PlayerPrefs.SetInt("ReturnedFromRace", 1);
        
        SceneManager.LoadScene(previousScene);
    }
}
