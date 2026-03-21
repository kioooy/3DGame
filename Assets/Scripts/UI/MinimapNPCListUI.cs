using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Giao diện danh sách NPC hiện ra khi mở Minimap (Fullscreen).
/// Tích vào ô sẽ sinh ra chấm than 3D (!) trên đầu NPC và tạo Mũi Tên GPS trên người Player để chỉ hướng.
/// </summary>
public class MinimapNPCListUI : MonoBehaviour
{
    [Header("=== CÀI ĐẶT HIỂN THỊ ===")]
    public Color pillarBaseColor = new Color(1f, 1f, 1f, 0.15f); // Trắng trong suốt
    public Color arrowColor = new Color(1f, 0.2f, 0.2f, 1f);     // Đỏ chỉ hướng

    private MinimapCamera _minimapCamera;
    
    // Lưu mọi NPC thuộc nhiều Script khác nhau
    private List<MonoBehaviour> _allNPCs = new List<MonoBehaviour>();
    
    private Dictionary<int, bool> _npcToggleState = new Dictionary<int, bool>();
    private Dictionary<int, GameObject> _npcMarkers = new Dictionary<int, GameObject>();

    private GUIStyle _windowStyle;
    private GUIStyle _toggleStyle;
    private Rect _windowRect;
    
    private Transform playerTransform;
    
    // Con trỏ hiển thị hướng nhìn của Player trên Minimap
    private GameObject _playerFacingIndicator;

    private static MinimapNPCListUI _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSetup()
    {
        if (_instance != null) return;

        // Tự động Add Component vào Game
        GameObject go = new GameObject("MinimapNPCListUI_System");
        _instance = go.AddComponent<MinimapNPCListUI>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset và tìm lại mọi thứ khi đổi Scene
        InitializeInScene();
    }

    void Start()
    {
        InitializeInScene();
    }

    private void InitializeInScene()
    {
        // 0. Dọn dẹp cũ
        if (_playerFacingIndicator != null) Destroy(_playerFacingIndicator);
        foreach (var marker in _npcMarkers.Values) if (marker != null) Destroy(marker);
        _npcMarkers.Clear();
        _allNPCs.Clear();
        _npcToggleState.Clear();

        // 1. Tìm Camera Minimap
        _minimapCamera = FindFirstObjectByType<MinimapCamera>();
        
        // 2. Càn quét toàn bộ NPC 
        var deTruis = FindObjectsByType<DeTruiNPC>(FindObjectsSortMode.None);
        var deChoats = FindObjectsByType<DeChoatNPC>(FindObjectsSortMode.None);
        
        _allNPCs.AddRange(deTruis);
        _allNPCs.AddRange(deChoats);

        foreach (var npc in _allNPCs)
        {
            _npcToggleState[npc.GetInstanceID()] = false;
        }

        _windowRect = new Rect(Screen.width - 270, 100, 250, 300);
        
        // 3. Tìm Player
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) playerTransform = p.transform;
        
        // 4. Tạo lại con trỏ hướng nhìn
        if (playerTransform != null)
        {
            CreatePlayerFacingIndicator();
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            // Thử tìm lại Player nếu bị lạc (do Load scene chậm hoặc spawn muộn)
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) 
            {
                playerTransform = p.transform;
                if (_playerFacingIndicator == null) CreatePlayerFacingIndicator();
            }
            return;
        }

        // Tự động tìm lại camera nếu chưa thấy
        if (_minimapCamera == null) _minimapCamera = FindFirstObjectByType<MinimapCamera>();

        // 1. Tự động tắt đánh dấu Minimap khi lại gần NPC đích
        List<int> keys = new List<int>(_npcToggleState.Keys);
        foreach (var id in keys)
        {
            if (_npcToggleState[id])
            {
                var npc = _allNPCs.Find(n => n != null && n.GetInstanceID() == id);
                if (npc != null)
                {
                    float dist = Vector3.Distance(playerTransform.position, npc.transform.position);
                    if (dist <= 7f)
                    {
                        // Đã tới nơi -> Bỏ Tick và Tắt Marker
                        _npcToggleState[id] = false;
                        ToggleMarker(id, npc, false);
                    }
                }
            }
        }
        
        // 2. Cập nhật vị trí và góc quay cho Con trỏ hướng nhìn Player trên Minimap
        if (_playerFacingIndicator != null)
        {
            // Trôi nổi tít trên cao để không bao giờ bị lá cây tre
            _playerFacingIndicator.transform.position = playerTransform.position + Vector3.up * 65f;
            // Xoay chuẩn theo Mắt người chơi (Chỉ xét trục Y)
            _playerFacingIndicator.transform.rotation = Quaternion.Euler(0, playerTransform.eulerAngles.y, 0);
        }
    }

    private void CreatePlayerFacingIndicator()
    {
        _playerFacingIndicator = new GameObject("PlayerFacing_MinimapArrow");
        
        // Khởi tạo thuật toán vẽ mặt lưới Mesh Hình Tam Giác chuẩn
        Mesh arrowMesh = CreateTriangleMesh();

        // LỚP 1: VIỀN ĐEN BÊN DƯỚI (Outline)
        GameObject outline = new GameObject("Arrow_Outline");
        outline.transform.SetParent(_playerFacingIndicator.transform);
        outline.transform.localPosition = new Vector3(0, -0.5f, 0); // Chìm xuống dưới để lót
        outline.transform.localScale = new Vector3(1.4f, 1f, 1.4f); // Phóng to 1.4 lần tạo viền dày
        
        MeshFilter mfOut = outline.AddComponent<MeshFilter>();
        mfOut.mesh = arrowMesh;
        MeshRenderer mrOut = outline.AddComponent<MeshRenderer>();
        Material matOut = new Material(Shader.Find("Sprites/Default"));
        matOut.color = Color.black;
        mrOut.material = matOut;

        // LỚP 2: LÕI MÀU VÀNG Ở TRÊN 
        GameObject core = new GameObject("Arrow_Core");
        core.transform.SetParent(_playerFacingIndicator.transform);
        core.transform.localPosition = Vector3.zero;
        core.transform.localScale = Vector3.one;
        
        MeshFilter mfCore = core.AddComponent<MeshFilter>();
        mfCore.mesh = arrowMesh;
        MeshRenderer mrCore = core.AddComponent<MeshRenderer>();
        Material matCore = new Material(Shader.Find("Sprites/Default"));
        // Màu vàng cam cực chuẩn giống bảng chỉ đường Minimap
        matCore.color = new Color(1f, 0.77f, 0.15f); 
        mrCore.material = matCore;
    }

    // Thủ thuật dựng Hình Tam Giác ngay trong mã C# mà không cần load tệp tin 3D
    private Mesh CreateTriangleMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[3] {
            new Vector3(0, 0, 5f),     // Mũi nhọn hướng vút lên trên (+Z)
            new Vector3(-3.5f, 0, -3f),  // Góc vuốt trái 
            new Vector3(3.5f, 0, -3f)    // Góc vuốt phải 
        };
        // Vẽ tam giác 2 mặt để dù nhìn từ chiều nào cũng thấy
        int[] triangles = new int[6] { 0, 1, 2, 0, 2, 1 }; 
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    void OnGUI()
    {
        // Chỉ xuất hiện Bảng chọn khi phóng to bản đồ
        if (_minimapCamera != null && _minimapCamera.IsFullscreen())
        {
            InitializeStyles();
            _windowRect.x = Screen.width - _windowRect.width - 20; 
            _windowRect.height = 50 + (_allNPCs.Count * 40);
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

    private string GetNPCName(MonoBehaviour npc)
    {
        if (npc is DeTruiNPC trui) return trui.GetDisplayName();
        if (npc is DeChoatNPC choat) return choat.GetDisplayName();
        return npc.gameObject.name;
    }

    private void DrawWindow(int windowID)
    {
        GUILayout.Space(10);
        
        foreach (var npc in _allNPCs)
        {
            if (npc == null) continue;

            int id = npc.GetInstanceID();
            bool isChecked = _npcToggleState[id];

            string displayName = GetNPCName(npc);
            bool newChecked = GUILayout.Toggle(isChecked, " " + displayName, _toggleStyle);
            
            if (newChecked != isChecked)
            {
                // Logic: Nếu chọn 1 NPC mới, hãy Bỏ Chọn mọi NPC cũ để Mũi Tên không chỉ loạn xạ
                if (newChecked)
                {
                    var keys = new List<int>(_npcToggleState.Keys);
                    foreach(var k in keys) 
                    {
                        if (k != id && _npcToggleState[k]) 
                        {
                            _npcToggleState[k] = false;
                            ToggleMarker(k, null, false);
                        }
                    }
                }

                _npcToggleState[id] = newChecked;
                ToggleMarker(id, npc, newChecked);
            }
        }
    }

    private void ToggleMarker(int id, MonoBehaviour npc, bool show)
    {
        if (show && npc != null)
        {
            if (!_npcMarkers.ContainsKey(id) || _npcMarkers[id] == null)
            {
                _npcMarkers[id] = CreateMarkerWithIconAndArrow(npc.transform);
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

    private GameObject CreateMarkerWithIconAndArrow(Transform targetNPC)
    {
        // Thùng chứa (Nhóm lại dễ quản lý lệnh huỷ/bật tắt)
        GameObject markerContainer = new GameObject("MarkerGroup_" + targetNPC.name);
        
        // ==========================================
        // 1. CỘT SÁNG (Light Pillar) cắm trên đầu NPC
        // ==========================================
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(pillar.GetComponent<Collider>()); // Bỏ cản đường
        pillar.transform.SetParent(markerContainer.transform);
        pillar.transform.localPosition = Vector3.zero;
        
        float pillarHeight = 250f;
        float pillarRadius = 3f;
        pillar.transform.localScale = new Vector3(pillarRadius, pillarHeight / 2f, pillarRadius);
        
        Material matPillar = new Material(Shader.Find("Sprites/Default")); 
        matPillar.color = pillarBaseColor; 
        pillar.GetComponent<Renderer>().material = matPillar;

        // ==========================================
        // 2. ICON (Chấm than) to nổi bật bên trên cột sáng
        // ==========================================
        GameObject iconObj = new GameObject("Icon_Text");
        iconObj.transform.SetParent(markerContainer.transform);
        iconObj.transform.localPosition = new Vector3(0, (pillarHeight / 2f) + 15f, 0); 
        
        var textMesh = iconObj.AddComponent<TextMesh>();
        textMesh.text = "!";
        textMesh.characterSize = 5f;
        textMesh.fontSize = 200;
        textMesh.color = Color.yellow;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        // Đập vô góc nhìn Camera
        iconObj.AddComponent<FaceCameraAlways>();

        // ==========================================
        // 3. MŨI TÊN CHỈ HƯỚNG BÁM THEO PLAYER
        // ==========================================
        GameObject arrowBase = new GameObject("PlayerNavArrow");
        arrowBase.transform.SetParent(markerContainer.transform); 
        
        // Thân mũi tên
        GameObject arrowBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(arrowBody.GetComponent<Collider>());
        arrowBody.transform.SetParent(arrowBase.transform);
        arrowBody.transform.localPosition = new Vector3(0, 0, 3f);
        // Kích thước nhỏ lại theo yêu cầu
        arrowBody.transform.localScale = new Vector3(1.5f, 0.2f, 6f);
        
        // Đầu mũi tên (Hình kim cương xoay 45 độ)
        GameObject arrowHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(arrowHead.GetComponent<Collider>());
        arrowHead.transform.SetParent(arrowBase.transform);
        arrowHead.transform.localPosition = new Vector3(0, 0, 6f);
        arrowHead.transform.localScale = new Vector3(4f, 0.2f, 4f);
        arrowHead.transform.localRotation = Quaternion.Euler(0, 45, 0); 
        
        Material matArrow = new Material(Shader.Find("Sprites/Default"));
        matArrow.color = arrowColor; 
        arrowBody.GetComponent<Renderer>().material = matArrow;
        arrowHead.GetComponent<Renderer>().material = matArrow;

        // ==========================================
        // 4. KÍCH HOẠT KỊCH BẢN BÁM THEO (GPS)
        // ==========================================
        var follower = markerContainer.AddComponent<AdvancedMarkerFollower>();
        follower.npcTarget = targetNPC;
        
        // Cập nhật linh hoạt target Player (đề phòng Player thay đổi/Load scene)
        if (playerTransform == null && GameObject.FindGameObjectWithTag("Player") != null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }
        
        follower.playerTarget = playerTransform;
        follower.navArrow = arrowBase.transform;
        
        Collider col = targetNPC.GetComponent<Collider>();
        float yExtents = col != null ? col.bounds.extents.y : 2f;
        follower.pillarHeightOffset = yExtents + (pillarHeight / 2f);

        return markerContainer;
    }
}

// ==========================================
// CÁC LỚP HỖ TRỢ VỊ TRÍ (HELPERS)
// ==========================================
public class AdvancedMarkerFollower : MonoBehaviour
{
    public Transform npcTarget;
    public Transform playerTarget;
    
    public Transform navArrow;
    public float pillarHeightOffset;

    void LateUpdate()
    {
        if (npcTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        // 1. Nhóm Icon + Pillar di chuyển trút lên đầu NPC
        transform.position = npcTarget.position + Vector3.up * pillarHeightOffset;

        // 2. Tách đối tượng Mũi tên G.P.S (navArrow) để chạy quanh Player
        if (navArrow != null && playerTarget != null)
        {
            navArrow.gameObject.SetActive(true);
            
            // Lơ lửng trên không trung ở mức an toàn
            navArrow.position = playerTarget.position + Vector3.up * 70f; 
            
            // Xoay hướng thẳng về NPC
            Vector3 dir = npcTarget.position - playerTarget.position;
            dir.y = 0; // Khoá trục Y 
            if (dir.sqrMagnitude > 0.1f)
            {
                navArrow.rotation = Quaternion.Lerp(navArrow.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
            }
        }
        else if (navArrow != null)
        {
            navArrow.gameObject.SetActive(false);
        }
    }
}

public class FaceCameraAlways : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // Trượt mặt phẳng thẳng vào màn hình người chơi
            transform.forward = Camera.main.transform.forward;
        }
    }
}
