using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Giao diện danh sách NPC hiện ra khi mở Minimap (Fullscreen).
/// Tích vào ô sẽ sinh ra chấm than 3D (!) trên đầu NPC để dễ tìm đường bay đến.
/// </summary>
public class MinimapNPCListUI : MonoBehaviour
{
    [Header("=== CÀI ĐẶT MÀU SẮC CỘT SÁNG ===")]
    // Sếp đổi màu ở dòng này nhé (Ví dụ: Color.red, Color.yellow, Color.cyan, Color.green...)
    public Color pillarBaseColor = new Color(1f, 1f, 1f, 0.15f); // Trắng cực kỳ trong suốt (15% đục)
    public Color pillarGlowColor = Color.white * 1.5f;           // Màu phát sáng dạ quang

    private MinimapCamera _minimapCamera;
    private DeTruiNPC[] _allNPCs;
    
    private Dictionary<int, bool> _npcToggleState = new Dictionary<int, bool>();
    private Dictionary<int, GameObject> _npcMarkers = new Dictionary<int, GameObject>();

    private GUIStyle _windowStyle;
    private GUIStyle _toggleStyle;
    private Rect _windowRect;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSetup()
    {
        // Tự động được thêm vào khi load Game mà không cần kéo thả tay
        GameObject go = new GameObject("MinimapNPCListUI_System");
        go.AddComponent<MinimapNPCListUI>();
        DontDestroyOnLoad(go);
    }

    void Start()
    {
        _minimapCamera = FindFirstObjectByType<MinimapCamera>();
        _allNPCs = FindObjectsByType<DeTruiNPC>(FindObjectsSortMode.None);

        foreach (var npc in _allNPCs)
        {
            _npcToggleState[npc.GetInstanceID()] = false;
        }

        _windowRect = new Rect(Screen.width - 270, 100, 250, 300);
    }

    void OnGUI()
    {
        // Chỉ hiện khi Minimap Fullscreen được bật
        if (_minimapCamera != null && _minimapCamera.IsFullscreen())
        {
            InitializeStyles();
            _windowRect.x = Screen.width - _windowRect.width - 20; // Bám biên phải
            
            // Fix kích thước Height theo số lượng NPC
            _windowRect.height = 50 + (_allNPCs.Length * 40);

            _windowRect = GUILayout.Window(888, _windowRect, DrawWindow, "DANH SÁCH NPC", _windowStyle);
        }
    }

    private void InitializeStyles()
    {
        if (_windowStyle == null)
        {
            _windowStyle = new GUIStyle(GUI.skin.window);
            _windowStyle.fontSize = 20;
            _windowStyle.fontStyle = FontStyle.Bold;
        }
        if (_toggleStyle == null)
        {
            _toggleStyle = new GUIStyle(GUI.skin.toggle);
            _toggleStyle.fontSize = 18;
            _toggleStyle.margin = new RectOffset(10, 10, 10, 10);
        }
    }

    private void DrawWindow(int windowID)
    {
        GUILayout.Space(10);
        
        foreach (var npc in _allNPCs)
        {
            if (npc == null) continue;

            int id = npc.GetInstanceID();
            bool isChecked = _npcToggleState[id];

            // Tên ưu tiên lấy Tên Game Object (XenToc, ConKien) thay vì npcName (bị copy trùng "Dế Trũi")
            string displayName = npc.gameObject.name;
            
            bool newChecked = GUILayout.Toggle(isChecked, " " + displayName, _toggleStyle);
            
            if (newChecked != isChecked)
            {
                _npcToggleState[id] = newChecked;
                ToggleMarker(id, npc, newChecked);
            }
        }
    }

    private void ToggleMarker(int id, DeTruiNPC npc, bool show)
    {
        if (show)
        {
            if (!_npcMarkers.ContainsKey(id) || _npcMarkers[id] == null)
            {
                _npcMarkers[id] = CreateLightPillarMarker(npc.transform);
            }
            _npcMarkers[id].SetActive(true);
        }
        else
        {
            if (_npcMarkers.ContainsKey(id) && _npcMarkers[id] != null)
            {
                _npcMarkers[id].SetActive(false);
            }
        }
    }

    private GameObject CreateLightPillarMarker(Transform parentTarget)
    {
        // 1. Dùng khối trụ để làm đường dẫn sáng
        GameObject markerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        markerObj.name = "WaypointMarker_LightPillar_" + parentTarget.name;
        
        // 2. Xoá mọi va chạm để xe tăng hay Xén Tóc đi ngang qua không bị chặn
        Destroy(markerObj.GetComponent<Collider>());
        
        // 3. Giải phóng khỏi NPC để không dính tỉ lệ Scale ảo ma
        markerObj.transform.SetParent(null);
        
        // 4. Tính toán độ cao để cắm chân cột sáng vừa đúng đầu NPC
        float yExtents = 0f; 
        Collider col = parentTarget.GetComponent<Collider>();
        if (col != null) yExtents = col.bounds.extents.y;
        
        float pillarHeight = 350f; // Siêu cao
        float pillarRadius = 4f;   // Siêu bự
        markerObj.transform.localScale = new Vector3(pillarRadius, pillarHeight / 2f, pillarRadius);
        
        // Cắm trụ xuống đầu 
        var follower = markerObj.AddComponent<MarkerFollowTarget>();
        follower.target = parentTarget;
        follower.heightOffset = yExtents + (pillarHeight / 2f);

        // 5. Cấu hình màu cho Cột sáng (Trắng mờ trong suốt, phát sáng)
        Renderer ren = markerObj.GetComponent<Renderer>();
        // Sử dụng Shader 'Sprites/Default' của Unity vì nó Hỗ Trợ Mọi Bản Đồ Render (Built-in, URP, HDRP) 
        // Vừa trong suốt tự nhiên, vừa sáng đục kiểu bóng ma mà không bao giờ bị lỗi màu hồng (Magenta)
        Material mat = new Material(Shader.Find("Sprites/Default")); 
        
        // Gắn màu vào
        mat.color = pillarBaseColor; 
        
        ren.material = mat;

        return markerObj;
    }
}

public class MarkerFollowTarget : MonoBehaviour
{
    public Transform target;
    public float heightOffset;

    void LateUpdate()
    {
        if (target != null)
        {
            // Bám theo toạ độ tuyệt đối trên không trung
            transform.position = target.position + Vector3.up * heightOffset;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

public class FaceCameraAlways : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // Hướng bảng luôn đập thẳng vào mắt camera
            transform.forward = Camera.main.transform.forward;
        }
    }
}
