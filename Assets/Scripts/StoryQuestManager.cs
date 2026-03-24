using UnityEngine;
using System;

/// <summary>
/// Singleton quản lý trạng thái cốt truyện chính.
/// Lưu bằng PlayerPrefs để giữ qua các session.
/// 
/// Các cột mốc cốt truyện (theo thứ tự):
///   Phase 0 – Bắt đầu: Dế Mèn tìm Xén Tóc để hỏi vị trí Dế Choắt
///   Phase 1 – Vật tay: Dế Mèn thắng vật tay Xén Tóc, bay vào nhà gặp Kiến
///   Phase 2 – Côn Kiến: Kiến đòi Mật Ong, bắt đi tìm Dế Trũi
///   Phase 3 – Đua xe: Thắng đua xe Dế Trũi -> lấy được Mật Ong
///   Phase 4 – Trả đồ: Đưa Mật Ong cho Kiến -> Được chỉ chỗ Dế Choắt
///   Phase 5 – Kết thúc: Nói chuyện xong với Dế Choắt
/// </summary>
public class StoryQuestManager : MonoBehaviour
{
    public event Action<int> OnPhaseChanged;

    // ── Singleton ──────────────────────────────────────────────────────────
    private static StoryQuestManager _instance;
    private static bool isQuitting = false;

    public static StoryQuestManager Instance
    {
        get
        {
            if (isQuitting) return null;

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<StoryQuestManager>();
                if (_instance == null)
                {
                    var go = new GameObject("StoryQuestManager");
                    _instance = go.AddComponent<StoryQuestManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    void OnApplicationQuit()
    {
        isQuitting = true;
    }

    // ── PlayerPrefs Keys ──────────────────────────────────────────────────
    const string K_PHASE = "story_phase";

    // ── Trạng thái hiện tại ───────────────────────────────────────────────
    [HideInInspector] public int currentPhase = 0;

    // ── Phase constants ───────────────────────────────────────────────────
    public const int PHASE_START            = 0;
    public const int PHASE_BEAT_XENTOC      = 1;
    public const int PHASE_MEET_CONKIEN     = 2;
    public const int PHASE_BEAT_DETRUI      = 3;
    public const int PHASE_GIVE_ITEM        = 4;
    public const int PHASE_ENDING           = 5;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Reset về ban đầu mỗi khi chạy Game mới
        currentPhase = PHASE_START;
    }

    // ── Load / Save ────────────────────────────────────────────────────────
    public void Load()
    {
        // Loại bỏ hàm đọc PlayerPrefs để luôn reset về Phase 0
        NotifyPhaseChanged();
    }

    public void Save()
    {
        // Loại bỏ việc lưu, chỉ báo event
        NotifyPhaseChanged();
    }

    /// <summary>Chuyển sang phase tiếp theo (chỉ tiến về phía trước).</summary>
    public void AdvanceTo(int phase)
    {
        if (phase <= currentPhase) return;
        currentPhase = phase;
        Save();
        Debug.Log($"[Story] ✅ Chuyển sang Phase {phase}");
    }

    /// <summary>Reset cốt truyện về đầu (dùng để debug).</summary>
    public void ResetStory()
    {
        currentPhase = PHASE_START;
        Debug.Log("[Story] 🔄 Đã reset cốt truyện về đầu.");
        NotifyPhaseChanged();
    }

    public void NotifyPhaseChanged()
    {
        OnPhaseChanged?.Invoke(currentPhase);
    }

    // ── Helper shortcuts ───────────────────────────────────────────────────
    public bool HasBeatXenToc    => currentPhase >= PHASE_BEAT_XENTOC;
    public bool HasMetConKien    => currentPhase >= PHASE_MEET_CONKIEN;
    public bool HasBeatDeTrui    => currentPhase >= PHASE_BEAT_DETRUI;
    public bool HasGivenItem     => currentPhase >= PHASE_GIVE_ITEM;
    public bool IsEnding         => currentPhase >= PHASE_ENDING;
}
