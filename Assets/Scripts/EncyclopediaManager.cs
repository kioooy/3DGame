using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý dữ liệu hệ thống Bách Khoa Toàn Thư (Bestiary).
/// Sử dụng PlayerPrefs để lưu các ID côn trùng đã thu thập được.
/// </summary>
public class EncyclopediaManager : MonoBehaviour
{
    public static EncyclopediaManager Instance { get; private set; }

    [Header("Data")]
    // Danh sách data tự động lấy từ thư mục Resources/Encyclopedia
    public List<InsectData> allInsects = new List<InsectData>();

    // Danh sách ID các côn trùng đã mở khóa
    private HashSet<string> unlockedInsectIDs = new HashSet<string>();

    public delegate void InsectUnlockedHandler(InsectData data);
    public event InsectUnlockedHandler OnInsectUnlocked;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllInsectData();
        
        // Load save cũ (nếu có) khi mới bật game
        LoadData();
    }

    private void LoadAllInsectData()
    {
        InsectData[] loaded = Resources.LoadAll<InsectData>("Encyclopedia");
        allInsects.AddRange(loaded);
        Debug.Log($"[Encyclopedia Manager] Đã load {allInsects.Count} Insect Data từ Resources/Encyclopedia.");
    }

    public void UnlockInsect(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (!unlockedInsectIDs.Contains(id))
        {
            unlockedInsectIDs.Add(id);
            InsectData data = GetInsectData(id);
            if (data != null)
            {
                Debug.Log($"[Encyclopedia Manager] Vừa mở khóa sinh vật mới: {data.insectName}!");
                OnInsectUnlocked?.Invoke(data);
                
                // Tự động Save luôn khi có đồ mới để phòng crash mất dữ liệu
                SaveData();
            }
        }
    }

    public bool IsUnlocked(string id)
    {
        return unlockedInsectIDs.Contains(id);
    }

    public InsectData GetInsectData(string id)
    {
        return allInsects.Find(x => x.insectID == id);
    }

    public List<InsectData> GetAllInsects()
    {
        return allInsects;
    }

    // ─── LƯU & TẢI DỮ LIỆU SỔ TAY ──────────────────────────────────────────────
    
    public void SaveData()
    {
        string joined = string.Join(",", unlockedInsectIDs);
        PlayerPrefs.SetString("UnlockedInsects", joined);
        PlayerPrefs.Save();
        Debug.Log("[Encyclopedia Manager] Saved Unlocked Insects: " + joined);
    }

    public void LoadData()
    {
        unlockedInsectIDs.Clear();
        string joined = PlayerPrefs.GetString("UnlockedInsects", "");
        if (!string.IsNullOrEmpty(joined))
        {
            string[] ids = joined.Split(',');
            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    unlockedInsectIDs.Add(id);
                }
            }
        }
    }
}
