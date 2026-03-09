using UnityEngine;
using System.Collections;

public class CaroGameManager : MonoBehaviour
{
    public static CaroGameManager Instance { get; private set; }

    [Header("UI Settings")]
    public int cellSize = 100;

    private bool _isGameActive = false;
    public bool IsGameActive => _isGameActive;

    private int[,] _board = new int[3, 3]; // 0: Trống, 1: Player (X), 2: Dế Trũi (O)
    private bool _isPlayerTurn = true;
    private bool _isGameOver = false;
    private string _winnerText = "";
    
    // Lưu tham chiếu npc để trả lại control khi kết thúc
    private DeTruiNPC _currentNPC;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Bắt đầu một ván cờ Caro 3x3 với Dế Trũi.
    /// </summary>
    public void StartGame(DeTruiNPC npc)
    {
        _currentNPC = npc;
        
        // Reset Bàn cờ
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                _board[i, j] = 0;
            }
        }
        
        _isPlayerTurn = true;
        _isGameOver = false;
        _winnerText = "";
        _isGameActive = true;
        
        // Mở khoá con trỏ chuột để bấm cờ
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnGUI()
    {
        if (!_isGameActive) return;

        // Vẽ màn chắn nền đen mờ
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Căn giữa bàn cờ
        float startX = (Screen.width - (cellSize * 3)) / 2f;
        float startY = (Screen.height - (cellSize * 3)) / 2f;

        // Tiêu đề
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 40,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(0, startY - 80, Screen.width, 60), "Cờ Caro Sinh Tử (3x3)", titleStyle);
        
        // Lượt đi
        GUIStyle turnStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24
        };
        string turnText = _isGameOver ? _winnerText : (_isPlayerTurn ? "Lượt của BẠN (X)" : "Dế Trũi đang suy nghĩ (O)...");
        turnStyle.normal.textColor = _isGameOver ? Color.yellow : (_isPlayerTurn ? Color.green : Color.red);
        GUI.Label(new Rect(0, startY - 30, Screen.width, 30), turnText, turnStyle);

        // Vẽ Khung Lưới Bàn Cờ
        GUIStyle cellStyle = new GUIStyle(GUI.skin.button) { fontSize = 36, fontStyle = FontStyle.Bold };

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                Rect cellRect = new Rect(startX + x * cellSize, startY + y * cellSize, cellSize, cellSize);
                string cellText = "";
                if (_board[x, y] == 1) cellText = "X";
                else if (_board[x, y] == 2) cellText = "O";

                // Nút được bấm
                if (GUI.Button(cellRect, cellText, cellStyle))
                {
                    // Lượt của Player và ô chưa đánh
                    if (_isPlayerTurn && !_isGameOver && _board[x, y] == 0)
                    {
                        PlayerMove(x, y);
                    }
                } // End if button click
            }
        } // End for

        // Vẽ Nút Thoát / Chơi lại khi Game kết thúc
        if (_isGameOver)
        {
            if (GUI.Button(new Rect(Screen.width / 2f - 110, startY + cellSize * 3 + 40, 100, 40), "Chơi Lại"))
            {
                StartGame(_currentNPC);
            }
            if (GUI.Button(new Rect(Screen.width / 2f + 10, startY + cellSize * 3 + 40, 100, 40), "Nghỉ Tay"))
            {
                QuitGame();
            }
        }
    }

    private void PlayerMove(int x, int y)
    {
        _board[x, y] = 1; // 1 = X
        CheckWinCondition();

        if (!_isGameOver)
        {
            _isPlayerTurn = false;
            // Cho Bot AI nhường 1 xíu delay nhìn có vẻ như đang nghĩ
            StartCoroutine(NpcMoveCoroutine());
        }
    }

    private IEnumerator NpcMoveCoroutine()
    {
        yield return new WaitForSeconds(0.6f); // Dế Trũi suy nghĩ...
        
        if (!_isGameOver)
        {
            int[] bestMove = FindBestMove();
            if (bestMove[0] != -1)
            {
                _board[bestMove[0], bestMove[1]] = 2; // 2 = O
            }
            
            CheckWinCondition();
            if (!_isGameOver)
            {
                _isPlayerTurn = true;
            }
        }
    }

    private void CheckWinCondition()
    {
        int winStatus = EvaluateBoard();
        if (winStatus == 10)
        {
            _isGameOver = true;
            _winnerText = "XUẤT SẮC! BẠN ĐÃ CHIẾN THẮNG DẾ TRŨI!";
        }
        else if (winStatus == -10)
        {
            _isGameOver = true;
            _winnerText = "GÀ! DẾ TRŨI ĐÃ CHIẾN THẮNG!";
        }
        else if (!IsMovesLeft())
        {
            _isGameOver = true;
            _winnerText = "BẤT PHÂN THẮNG BẠI! HOÀ RỒI!";
        }
    }

    /// <summary>
    /// Thuật toán kinh điển đánh Tic-Tac-Toe cực gắt của AI (Minimax).
    /// </summary>
    private int[] FindBestMove()
    {
        int bestVal = 1000;
        int[] bestMove = new int[] { -1, -1 };

        // Quét tất cả các ô trên bàn cờ
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                // Nếu ô trống
                if (_board[i, j] == 0)
                {
                    // Lướt nước đi
                    _board[i, j] = 2; // AI thử đánh ván này

                    // Tính điểm nước đi bằng hàm đệ quy Minimax (AI là phe điểm thấp -10)
                    int moveVal = Minimax(0, true);

                    // Khôi phục ô trống để thử đường khác
                    _board[i, j] = 0;

                    // AI O luôn cố gắng làm cho điểm càng thấp (về -10) càng tốt
                    if (moveVal < bestVal)
                    {
                        bestMove[0] = i;
                        bestMove[1] = j;
                        bestVal = moveVal;
                    }
                }
            }
        }
        return bestMove;
    }

    private int Minimax(int depth, bool isMax)
    {
        int score = EvaluateBoard();

        // Nếu Người (X) thắng
        if (score == 10) return score;
        // Nếu AI (O) thắng
        if (score == -10) return score;
        // Nếu kẹt bàn
        if (!IsMovesLeft()) return 0;

        if (isMax)
        {
            // Lượt của Player (X - Phe tìm điểm cao 10)
            int best = -1000;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (_board[i, j] == 0)
                    {
                        _board[i, j] = 1;
                        best = Mathf.Max(best, Minimax(depth + 1, !isMax));
                        _board[i, j] = 0;
                    }
                }
            }
            return best;
        }
        else
        {
            // Lượt của AI (O - Phe tìm điểm thấp -10)
            int best = 1000;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (_board[i, j] == 0)
                    {
                        _board[i, j] = 2;
                        best = Mathf.Min(best, Minimax(depth + 1, !isMax));
                        _board[i, j] = 0;
                    }
                }
            }
            return best;
        }
    }

    private int EvaluateBoard()
    {
        // Check dọc
        for (int col = 0; col < 3; col++)
        {
            if (_board[0, col] == _board[1, col] && _board[1, col] == _board[2, col])
            {
                if (_board[0, col] == 1) return 10;
                else if (_board[0, col] == 2) return -10;
            }
        }
        
        // Check ngang
        for (int row = 0; row < 3; row++)
        {
            if (_board[row, 0] == _board[row, 1] && _board[row, 1] == _board[row, 2])
            {
                if (_board[row, 0] == 1) return 10;
                else if (_board[row, 0] == 2) return -10;
            }
        }

        // Check chéo chính
        if (_board[0, 0] == _board[1, 1] && _board[1, 1] == _board[2, 2])
        {
            if (_board[0, 0] == 1) return 10;
            else if (_board[0, 0] == 2) return -10;
        }

        // Check chéo phụ
        if (_board[0, 2] == _board[1, 1] && _board[1, 1] == _board[2, 0])
        {
            if (_board[0, 2] == 1) return 10;
            else if (_board[0, 2] == 2) return -10;
        }

        return 0; // Chưa ai thắng
    }

    private bool IsMovesLeft()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (_board[i, j] == 0) return true;
            }
        }
        return false;
    }

    private void QuitGame()
    {
        _isGameActive = false;
        
        // Khóa lại chuột nếu cần
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Trả lại tương tác cho NPC (Kèm KQ Thắng Thua)
        if (_currentNPC != null)
        {
            bool isPlayerWin = false;
            bool isDraw = false;
            if (_winnerText.Contains("BẠN") || _winnerText.Contains("Player")) isPlayerWin = true;
            if (_winnerText.Contains("BẤT PHÂN") || _winnerText.Contains("HOÀ")) isDraw = true;
            
            _currentNPC.EndMinigame(isPlayerWin, isDraw);
        }
    }
}
