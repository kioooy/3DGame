using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CaroGameManager : MonoBehaviour
{
    public static CaroGameManager Instance { get; private set; }

    [Header("UI Settings")]
    public int cellSize = 90; // Kích thước mỗi ô (5x5 = 450x450 vừa màn hình)

    private bool _isGameActive = false;
    public bool IsGameActive => _isGameActive;

    [Header("Audio SFX")]
    public AudioClip clickSFX;
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

    private int[,] _board = new int[5, 5]; // 0: Trống, 1: Player (X), 2: Dế Trũi (O)
    private bool _isPlayerTurn = true;
    private bool _isGameOver = false;
    private string _winnerText = "";
    
    // Lưu tham chiếu npc để trả lại control khi kết thúc
    private INPCMinigame _currentNPC;

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
    /// Bắt đầu một ván cờ Caro 5x5 với NPC.
    /// </summary>
    public void StartGame(INPCMinigame npc, bool resetLives = true)
    {
        _currentNPC = npc;
        
        if (resetLives)
        {
            _currentLives = maxLives;
        }
        
        // Reset Bàn cờ
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                _board[i, j] = 0;
            }
        }
        
        _isPlayerTurn = true;
        _isGameOver = false;
        _winnerText = "";
        _isGameActive = true;
        _currentNPC.isMinigameActive = true;
        
        // Mở khoá con trỏ chuột để bấm cờ
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tắt nhạc nền chung, bật nhạc Minigame
        if (BackgroundMusicManager.Instance != null) BackgroundMusicManager.Instance.PauseMusic();
        if (_bgmSource != null && bgmClip != null)
        {
            _bgmSource.clip = bgmClip;
            _bgmSource.volume = 0.5f;
            _bgmSource.Play();
        }
    }

    private void OnGUI()
    {
        if (!_isGameActive) return;

        // Vẽ màn chắn nền đen mờ
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

        // Căn giữa bàn cờ
        float startX = (Screen.width - (cellSize * 5)) / 2f;
        float startY = (Screen.height - (cellSize * 5)) / 2f;

        // Tiêu đề
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 40,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(0, startY - 80, Screen.width, 60), "Cờ Caro Sinh Tử (5x5)", titleStyle);
        
        // Lượt đi
        GUIStyle turnStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24
        };
        string npcName = GetNpcDisplayName();
        string turnText = _isGameOver ? _winnerText : (_isPlayerTurn ? "Lượt của BẠN (X)" : $"{npcName} đang suy nghĩ (O)...");
        turnStyle.normal.textColor = _isGameOver ? Color.yellow : (_isPlayerTurn ? Color.green : Color.red);
        turnStyle.wordWrap = true;
        GUI.Label(new Rect(0, startY - 40, Screen.width, 40), turnText, turnStyle);

        // Vẽ Khung Lưới Bàn Cờ
        GUIStyle cellStyle = new GUIStyle(GUI.skin.button) { fontSize = 36, fontStyle = FontStyle.Bold };

        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                Rect cellRect = new Rect(startX + x * cellSize, startY + y * cellSize, cellSize, cellSize);
                string cellText = "";
                
                // Tô màu X, O
                GUI.backgroundColor = Color.white;
                if (_board[x, y] == 1) 
                {
                    cellText = "X";
                    GUI.backgroundColor = new Color(0.4f, 1f, 0.4f); // Xanh lá nhạt
                }
                else if (_board[x, y] == 2) 
                {
                    cellText = "O";
                    GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); // Đỏ nhạt
                }

                // Hiệu ứng Hover khi đưa chuột vào (nếu là ô trống và tới lượt)
                if (_board[x, y] == 0 && _isPlayerTurn && !_isGameOver && cellRect.Contains(Event.current.mousePosition))
                {
                    GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
                }

                if (GUI.Button(cellRect, cellText, cellStyle))
                {
                    // Lượt của Player và ô chưa đánh
                    if (_isPlayerTurn && !_isGameOver && _board[x, y] == 0)
                    {
                        PlaySFX(clickSFX);
                        PlayerMove(x, y);
                    }
                }
                GUI.backgroundColor = Color.white; // Phục hồi màu cũ
            }
        }
        if (_isGameOver)
        {
            if (_currentLives > 0)
            {
                if (GUI.Button(new Rect(Screen.width / 2f - 110, startY + cellSize * 5 + 20, 100, 40), "Chơi Lại"))
                {
                    StartGame(_currentNPC, false); // Không reset máu khi chơi lại
                }
            }
            
            if (GUI.Button(new Rect(Screen.width / 2f + 10, startY + cellSize * 5 + 20, 100, 40), "Nghỉ Tay"))
            {
                QuitGame();
            }
        }
    }

    private void PlayerMove(int x, int y)
    {
        _board[x, y] = 1; // 1 = X
        CheckWinCondition(x, y, 1);

        if (!_isGameOver)
        {
            _isPlayerTurn = false;
            StartCoroutine(NpcMoveCoroutine());
        }
    }

    private IEnumerator NpcMoveCoroutine()
    {
        yield return new WaitForSeconds(0.6f); // NPC suy nghĩ...
        
        if (!_isGameOver)
        {
            int[] bestMove = FindBestMove();
            if (bestMove[0] != -1)
            {
                PlaySFX(clickSFX);
                _board[bestMove[0], bestMove[1]] = 2; // 2 = O
                CheckWinCondition(bestMove[0], bestMove[1], 2);
            }
            else 
            {
                // Nếu không còn nước nào (hoà)
                CheckWinCondition(0,0,0);
            }

            if (!_isGameOver)
            {
                _isPlayerTurn = true;
            }
        }
    }

    private void CheckWinCondition(int lastX, int lastY, int playerValue)
    {
        if (playerValue != 0 && Check4InARow(lastX, lastY, playerValue)) // Game 5x5 mình có thể set 4 con thắng để game mượt, hoặc 5 con
        {
            _isGameOver = true;
            string npcNameUpper = GetNpcDisplayName().ToUpper();
            if (playerValue == 1)
            {
                _winnerText = $"XUẤT SẮC! BẠN ĐÃ CHIẾN THẮNG {npcNameUpper}!";
                PlaySFX(winSFX);
            }
            else
            {
                _currentLives--;
                if (_currentLives > 0)
                {
                    _winnerText = $"GÀ! {npcNameUpper} ĐÃ CHIẾN THẮNG! (-1 Mạng)";
                }
                else
                {
                    _winnerText = $"BẠN ĐÃ HẾT MẠNG! HÃY NGHỈ TAY RỒI QUAY LẠI SAU!";
                }
                PlaySFX(loseSFX);
            }
            return;
        }

        if (!IsMovesLeft())
        {
            _isGameOver = true;
            _winnerText = "BẤT PHÂN THẮNG BẠI! HOÀ RỒI!";
            PlaySFX(loseSFX);
        }
    }

    private bool Check4InARow(int x, int y, int player)
    {
        // 4 hướng: ngang, dọc, chéo /, chéo \
        int[][] dirs = new int[][]
        {
            new int[] {1, 0},
            new int[] {0, 1},
            new int[] {1, 1},
            new int[] {1, -1}
        };

        foreach (var dir in dirs)
        {
            int count = 1;
            // Tiến lên
            for (int i = 1; i < 4; i++) // Thay đổi thành ăn 4 hoặc ăn 5 tuỳ ý. Trong lưới 5x5 ăn 4 cho dễ thắng.
            {
                int nx = x + dir[0] * i;
                int ny = y + dir[1] * i;
                if (nx >= 0 && nx < 5 && ny >= 0 && ny < 5 && _board[nx, ny] == player)
                    count++;
                else break;
            }
            // Lùi lại
            for (int i = 1; i < 4; i++)
            {
                int nx = x - dir[0] * i;
                int ny = y - dir[1] * i;
                if (nx >= 0 && nx < 5 && ny >= 0 && ny < 5 && _board[nx, ny] == player)
                    count++;
                else break;
            }

            // Với 5x5: Đủ 4 con là Thắng
            if (count >= 4) return true;
        }

        return false;
    }

    private bool IsMovesLeft()
    {
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                if (_board[i, j] == 0) return true;
        return false;
    }

    /// <summary>
    /// Thuật toán Heuristic cho AI: Đánh giá điểm Tấn Công và Phòng Thủ từng ô.
    /// </summary>
    private int[] FindBestMove()
    {
        long maxScore = -1;
        List<int[]> bestMoves = new List<int[]>();

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (_board[i, j] == 0)
                {
                    long attackScore = EvaluatePoint(i, j, 2); // Điểm tấn công của Bot (2)
                    long defenseScore = EvaluatePoint(i, j, 1); // Điểm phòng thủ Player (1)
                    
                    long score = attackScore + defenseScore;
                    if (attackScore >= 1000000) score += attackScore; // Ưu tiên win
                    else if (defenseScore >= 1000000) score += defenseScore; // Chặn lép góc địch

                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestMoves.Clear();
                        bestMoves.Add(new int[] { i, j });
                    }
                    else if (score == maxScore)
                    {
                        bestMoves.Add(new int[] { i, j });
                    }
                }
            }
        }

        if (bestMoves.Count > 0)
        {
            return bestMoves[Random.Range(0, bestMoves.Count)];
        }

        return new int[] { Random.Range(0, 5), Random.Range(0, 5) };
    }

    // Các mảng điểm Heuristic
    private long[] attackScores = new long[]  { 0, 9, 54, 50000, 10000000 };
    private long[] defenseScores = new long[] { 0, 3, 27, 20000,  5000000 };

    private long EvaluatePoint(int r, int c, int player)
    {
        long totalScore = 0;
        int opponent = (player == 1) ? 2 : 1;

        int[][] dirs = new int[][]
        {
            new int[] {1, 0},
            new int[] {0, 1},
            new int[] {1, 1},
            new int[] {1, -1}
        };

        foreach (var dir in dirs)
        {
            int consecutive = 0;
            int openEnds = 0;
            
            // Hướng tới
            int tr = r + dir[0];
            int tc = c + dir[1];
            while (tr >= 0 && tr < 5 && tc >= 0 && tc < 5 && _board[tr, tc] == player)
            {
                consecutive++;
                tr += dir[0];
                tc += dir[1];
            }
            if (tr >= 0 && tr < 5 && tc >= 0 && tc < 5 && _board[tr, tc] == 0) openEnds++;

            // Hướng tới ngược lại
            tr = r - dir[0];
            tc = c - dir[1];
            while (tr >= 0 && tr < 5 && tc >= 0 && tc < 5 && _board[tr, tc] == player)
            {
                consecutive++;
                tr -= dir[0];
                tc -= dir[1];
            }
            if (tr >= 0 && tr < 5 && tc >= 0 && tc < 5 && _board[tr, tc] == 0) openEnds++;

            // Thắng tại lưới 5x5 khi có 4 con chặn đầu đuôi cũng được
            if (consecutive >= 3) return 1000000; // Có sẵn 3 + nước chuẩn bị đi (ô mình định thử) là 4 -> THẮNG
            
            if (consecutive > 0)
            {
                // Mảng có 5 phần tử (0 đến 4) nên dùng Math.Min(consecutive+1, 4) để an toàn
                int idx2 = Mathf.Min(consecutive + 1, 4);
                int idx1 = Mathf.Min(consecutive, 4);
                
                if (openEnds == 2)
                {
                    totalScore += (player == 2) ? attackScores[idx2] : defenseScores[idx2];
                }
                else if (openEnds == 1)
                {
                    totalScore += (player == 2) ? attackScores[idx1] : defenseScores[idx1];
                }
            }
        }

        return totalScore;
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

        // Trả lại tương tác cho NPC (Kèm KQ Thắng Thua)
        if (_currentNPC != null)
        {
            bool isPlayerWin = false;
            bool isDraw = false;
            if (_winnerText.Contains("BẠN") || _winnerText.Contains("Player")) isPlayerWin = true;
            if (_winnerText.Contains("BẤT PHÂN") || _winnerText.Contains("HOÀ")) isDraw = true;
            
            _currentNPC.EndMinigame(isPlayerWin, isDraw);

            // Bật màn hình Lật truyện nếu có setup truyện và đã thắng
            if (isPlayerWin && storyImages != null && storyImages.Length > 0 && StoryViewerManager.Instance != null)
            {
                StoryViewerManager.Instance.ShowStory(storyImages);
            }
        }
    }
}
