using UnityEngine;
using System.Collections;

public class CaroGameManager : MonoBehaviour
{
    public static CaroGameManager Instance { get; private set; }

    // ── Board Config ─────────────────────────────────────────────────
    private const int BOARD_SIZE    = 20;   // 20x20
    private const int WIN_COUNT     = 5;    // 5 in a row to win

    [Header("UI Settings")]
    public int cellSize = 36;               // smaller cells to fit 20x20

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

    // ── State ─────────────────────────────────────────────────────────
    private int[,] _board = new int[BOARD_SIZE, BOARD_SIZE]; // 0=empty, 1=player(X), 2=AI(O)
    private bool   _isPlayerTurn = true;
    private bool   _isGameOver   = false;
    private string _winnerText   = "";

    // Camera/scroll for large board
    private Vector2 _scroll = Vector2.zero;

    private INPCMinigame _currentNPC;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake  = false;
        _audioSource.spatialBlend = 0f;

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake  = false;
        _bgmSource.spatialBlend = 0f;
        _bgmSource.loop         = true;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
            _audioSource.PlayOneShot(clip);
    }

    private string GetNpcDisplayName()
    {
        if (_currentNPC == null) return "Doi thu";
        string n = _currentNPC.npcName.ToLower();
        if (n.Contains("detrui"))  return "De Trui";
        if (n.Contains("dechoat")) return "De Choat";
        if (n.Contains("xentoc"))  return "Xen Toc";
        return _currentNPC.npcName;
    }

    // ── Public API ────────────────────────────────────────────────────
    public void StartGame(INPCMinigame npc)
    {
        _currentNPC = npc;

        // Reset board
        for (int i = 0; i < BOARD_SIZE; i++)
            for (int j = 0; j < BOARD_SIZE; j++)
                _board[i, j] = 0;

        _isPlayerTurn = true;
        _isGameOver   = false;
        _winnerText   = "";
        _isGameActive = true;
        if (_currentNPC != null) _currentNPC.isMinigameActive = true;

        // Center scroll so board is visible
        float boardPx = cellSize * BOARD_SIZE;
        _scroll = new Vector2((boardPx - Screen.width) / 2f, (boardPx - Screen.height) / 2f);
        _scroll.x = Mathf.Max(0, _scroll.x);
        _scroll.y = Mathf.Max(0, _scroll.y);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (BackgroundMusicManager.Instance != null) BackgroundMusicManager.Instance.PauseMusic();
        if (_bgmSource != null && bgmClip != null)
        {
            _bgmSource.clip   = bgmClip;
            _bgmSource.volume = 0.5f;
            _bgmSource.Play();
        }
    }

    // ── OnGUI ─────────────────────────────────────────────────────────
    private void OnGUI()
    {
        if (!_isGameActive) return;

        // Dark overlay
        GUI.color = new Color(0, 0, 0, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float boardPx = cellSize * BOARD_SIZE;

        // ── Header bar ───────────────────────────────────────────────
        float headerH = 56f;
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 28,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(0, 4, Screen.width, 36), "Co Caro Sinh Tu (20x20 - 5 lien tiep)", titleStyle);

        string npcName  = GetNpcDisplayName();
        string turnText = _isGameOver
            ? _winnerText
            : (_isPlayerTurn ? "Luot cua BAN (X)" : $"{npcName} dang suy nghi (O)...");

        GUIStyle turnStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 20
        };
        turnStyle.normal.textColor = _isGameOver ? Color.yellow
                                   : (_isPlayerTurn ? Color.green : Color.red);
        GUI.Label(new Rect(0, 36, Screen.width, 24), turnText, turnStyle);

        // ── Scrollable board ─────────────────────────────────────────
        float viewW  = Screen.width;
        float viewH  = Screen.height - headerH - 52f; // leave room for button bar
        Rect viewRect = new Rect(0, headerH, viewW, viewH);
        Rect contentRect = new Rect(0, 0, boardPx, boardPx);

        _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);

        GUIStyle cellStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = cellSize > 40 ? 20 : 14,
            fontStyle = FontStyle.Bold
        };

        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                Rect cellRect = new Rect(x * cellSize, y * cellSize, cellSize, cellSize);
                string cellText = "";

                GUI.backgroundColor = Color.white;
                if (_board[x, y] == 1)
                {
                    cellText = "X";
                    GUI.backgroundColor = new Color(0.35f, 0.9f, 0.35f);
                }
                else if (_board[x, y] == 2)
                {
                    cellText = "O";
                    GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
                }
                else if (!_isGameOver && _isPlayerTurn && cellRect.Contains(Event.current.mousePosition))
                {
                    GUI.backgroundColor = new Color(0.75f, 0.75f, 0.75f);
                }

                if (GUI.Button(cellRect, cellText, cellStyle))
                {
                    if (_isPlayerTurn && !_isGameOver && _board[x, y] == 0)
                    {
                        PlaySFX(clickSFX);
                        PlayerMove(x, y);
                    }
                }
                GUI.backgroundColor = Color.white;
            }
        }

        GUI.EndScrollView();

        // ── Bottom button bar ────────────────────────────────────────
        float btnY = Screen.height - 48f;
        if (_isGameOver)
        {
            if (GUI.Button(new Rect(Screen.width / 2f - 115, btnY, 110, 38), "Choi Lai"))
                StartGame(_currentNPC);

            if (GUI.Button(new Rect(Screen.width / 2f + 5, btnY, 110, 38), "Nghi Tay"))
                QuitGame();
        }
        else
        {
            if (GUI.Button(new Rect(Screen.width - 120, btnY, 110, 38), "Nghi Tay"))
                QuitGame();
        }
    }

    // ── Player / AI move ──────────────────────────────────────────────
    private void PlayerMove(int x, int y)
    {
        _board[x, y] = 1;
        if (CheckWin(1))
        {
            EndGame(true, false);
            return;
        }
        if (!IsMovesLeft())
        {
            EndGame(false, true);
            return;
        }
        _isPlayerTurn = false;
        StartCoroutine(NpcMoveCoroutine());
    }

    private IEnumerator NpcMoveCoroutine()
    {
        yield return new WaitForSeconds(0.45f);

        if (_isGameOver) yield break;

        int[] move = FindBestMoveAI();
        if (move[0] != -1)
        {
            PlaySFX(clickSFX);
            _board[move[0], move[1]] = 2;
            if (CheckWin(2))
            {
                EndGame(false, false);
                yield break;
            }
            if (!IsMovesLeft())
            {
                EndGame(false, true);
                yield break;
            }
        }
        _isPlayerTurn = true;
    }

    private void EndGame(bool playerWins, bool isDraw)
    {
        _isGameOver = true;
        string npcUpper = GetNpcDisplayName().ToUpper();
        if (playerWins)
        {
            _winnerText = $"XUAT SAC! BAN DA CHIEN THANG {npcUpper}!";
            PlaySFX(winSFX);
        }
        else if (isDraw)
        {
            _winnerText = "BAT PHAN THANG BAI! HOA ROI!";
            PlaySFX(loseSFX);
        }
        else
        {
            _winnerText = $"GA! {npcUpper} DA CHIEN THANG!";
            PlaySFX(loseSFX);
        }
    }

    // ── Win check ─────────────────────────────────────────────────────
    /// <summary>Returns true if 'player' has WIN_COUNT in a row anywhere on board.</summary>
    private bool CheckWin(int player)
    {
        int[] dx = { 1, 0, 1,  1 };
        int[] dy = { 0, 1, 1, -1 };

        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                if (_board[x, y] != player) continue;
                for (int d = 0; d < 4; d++)
                {
                    int count = 1;
                    int nx = x + dx[d];
                    int ny = y + dy[d];
                    while (InBounds(nx, ny) && _board[nx, ny] == player)
                    {
                        count++;
                        nx += dx[d];
                        ny += dy[d];
                    }
                    if (count >= WIN_COUNT) return true;
                }
            }
        }
        return false;
    }

    private bool InBounds(int x, int y) =>
        x >= 0 && x < BOARD_SIZE && y >= 0 && y < BOARD_SIZE;

    private bool IsMovesLeft()
    {
        for (int i = 0; i < BOARD_SIZE; i++)
            for (int j = 0; j < BOARD_SIZE; j++)
                if (_board[i, j] == 0) return true;
        return false;
    }

    // ── AI: Heuristic scoring (Minimax quá chậm cho 20x20) ───────────
    /// <summary>
    /// Chấm điểm từng ô trống, ưu tiên:
    ///   1. Nước thắng ngay (AI)
    ///   2. Chặn nước thắng ngay của Player
    ///   3. Tạo 4-liên tiếp của AI
    ///   4. Chặn 4-liên tiếp của Player
    ///   ... v.v.
    /// </summary>
    private int[] FindBestMoveAI()
    {
        int bestScore = int.MinValue;
        int[] bestMove = { -1, -1 };

        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                if (_board[x, y] != 0) continue;

                // Only consider cells near existing stones (optimization)
                if (!HasNeighbour(x, y, 2)) continue;

                int score = ScoreMove(x, y);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove[0] = x;
                    bestMove[1] = y;
                }
            }
        }

        // Fallback: if board nearly empty pick center
        if (bestMove[0] == -1)
        {
            bestMove[0] = BOARD_SIZE / 2;
            bestMove[1] = BOARD_SIZE / 2;
        }

        return bestMove;
    }

    /// <summary>Returns true if any cell within 'radius' squares is non-empty.</summary>
    private bool HasNeighbour(int x, int y, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (InBounds(nx, ny) && _board[nx, ny] != 0) return true;
            }
        return false;
    }

    /// <summary>Heuristic score for placing AI(2) at (x,y).</summary>
    private int ScoreMove(int x, int y)
    {
        int score = 0;

        // --- AI attack score ---
        _board[x, y] = 2;
        score += LineScore(x, y, 2) * 10; // aggressive
        _board[x, y] = 0;

        // --- Block player score ---
        _board[x, y] = 1;
        score += LineScore(x, y, 1) * 9;  // slightly less priority than winning
        _board[x, y] = 0;

        // Center bonus (slight preference)
        int cx = BOARD_SIZE / 2, cy = BOARD_SIZE / 2;
        score -= (Mathf.Abs(x - cx) + Mathf.Abs(y - cy));

        return score;
    }

    private static readonly int[] DX4 = { 1, 0, 1,  1 };
    private static readonly int[] DY4 = { 0, 1, 1, -1 };

    /// <summary>
    /// Score based on consecutive run length for 'player' at (x,y).
    /// Weights: 5-in-row=100000, 4=10000, 3=1000, 2=100, 1=10
    /// </summary>
    private int LineScore(int x, int y, int player)
    {
        int total = 0;
        for (int d = 0; d < 4; d++)
        {
            int count = ConsecutiveCount(x, y, DX4[d], DY4[d], player)
                      + ConsecutiveCount(x, y, -DX4[d], -DY4[d], player)
                      - 1; // (x,y) counted twice

            if      (count >= 5) total += 100000;
            else if (count == 4) total += 10000;
            else if (count == 3) total += 1000;
            else if (count == 2) total += 100;
            else if (count == 1) total += 10;
        }
        return total;
    }

    private int ConsecutiveCount(int x, int y, int dx, int dy, int player)
    {
        int count = 0;
        int nx = x, ny = y;
        while (InBounds(nx, ny) && _board[nx, ny] == player)
        {
            count++;
            nx += dx;
            ny += dy;
        }
        return count;
    }

    // ── Quit ──────────────────────────────────────────────────────────
    private void QuitGame()
    {
        _isGameActive = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (_bgmSource != null) _bgmSource.Stop();
        if (BackgroundMusicManager.Instance != null) BackgroundMusicManager.Instance.ResumeMusic();

        if (_currentNPC != null)
        {
            bool playerWin = _winnerText.Contains("BAN") || _winnerText.Contains("Player");
            bool isDraw    = _winnerText.Contains("HOA") || _winnerText.Contains("BAT PHAN");
            _currentNPC.EndMinigame(playerWin, isDraw);

            if (playerWin && storyImages != null && storyImages.Length > 0 && StoryViewerManager.Instance != null)
                StoryViewerManager.Instance.ShowStory(storyImages);
        }
    }
}
