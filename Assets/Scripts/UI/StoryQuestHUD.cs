using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý UI hiển thị mục tiêu nhiệm vụ (Quest) bên phải màn hình.
/// Tự động sinh giao diện Canvas bằng code.
/// </summary>
public class StoryQuestHUD : MonoBehaviour
{
    private static StoryQuestHUD _instance;
    
    private GameObject questPanel;
    private Text questTitleText;
    private Text questContentText;
    
    private int lastKnownPhase = -1;
    private float markerRefreshTimer = 0f;
    private LineRenderer questLinePointer;
    private Transform currentQuestPillar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoInitialize()
    {
        if (_instance == null)
        {
            var go = new GameObject("StoryQuestHUD");
            _instance = go.AddComponent<StoryQuestHUD>();
            DontDestroyOnLoad(go);
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        SetupUI();
    }

    void Start()
    {
        // Tự động dời Minimap sang góc trên bên phải (The Witcher 3 style)
        FixMinimapLayout();

        // Đăng ký event
        var story = StoryQuestManager.Instance;
        if (story != null)
        {
            story.OnPhaseChanged += UpdateQuestUI;
            // Cập nhật lần đầu
            UpdateQuestUI(story.currentPhase);
        }
    }

    void Update()
    {
        if (StoryQuestManager.Instance == null) return;
        
        int phase = StoryQuestManager.Instance.currentPhase;
        if (phase != lastKnownPhase)
        {
            lastKnownPhase = phase;
            UpdateQuestUI(phase);
        }

        // Đảm bảo Minimap Waypoint liên tục được update đề phòng NPC Spawn trễ hoặc sau UI
        markerRefreshTimer += Time.deltaTime;
        if (markerRefreshTimer >= 2f)
        {
            markerRefreshTimer = 0f;
            UpdateMinimapWaypoint(phase);
        }

        // Tự động ẩn cột sáng chói mắt nếu người chơi đến đủ gần (Dưới 5 mét)
        if (currentQuestPillar != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float dist = Vector3.Distance(player.transform.position, currentQuestPillar.position);
                Renderer r = currentQuestPillar.GetComponent<Renderer>();
                if (r != null) r.enabled = (dist > 5f);
            }
        }
    }

    void OnDestroy()
    {
        if (StoryQuestManager.Instance != null)
        {
            StoryQuestManager.Instance.OnPhaseChanged -= UpdateQuestUI;
        }
    }

    private void FixMinimapLayout()
    {
        GameObject miniUI = GameObject.Find("MinimapUI");
        if (miniUI != null)
        {
            RectTransform miniRT = miniUI.GetComponent<RectTransform>();
            miniRT.anchorMin = new Vector2(0, 1);
            miniRT.anchorMax = new Vector2(0, 1);
            miniRT.pivot = new Vector2(0, 1);
            miniRT.anchoredPosition = new Vector2(20, -20); // Đặt góc trên bên TRÁI giống Witcher 3

            // Cố gắng làm tròn viền ngoài (The Witcher style)
            Transform borderMask = miniUI.transform.Find("BorderMask");
            if (borderMask != null)
            {
                Image bgImg = borderMask.GetComponent<Image>();
                if (bgImg != null)
                {
                    // Đã bỏ tự động tải file Knob vì một số phiên bản Unity/Build không hỗ trợ ngầm Resources.GetBuiltinResource cho PSD.
                    // Bạn có thể tự kéo tay một cái Sprite hình tròn Hình Vành Khuyên hoặc Chấm Tròn vào Mask của MinimapUI/BorderMask trong màn hình nếu muốn.
                }
            }
        }
    }

    private void SetupUI()
    {
        // 1. Tạo Canvas
        GameObject canvasObj = new GameObject("QuestCanvas");
        canvasObj.transform.SetParent(this.transform); // Cho dọn dẹp chung
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // Hiển thị trên các UI khác

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Tạo Panel Nền (Bên dưới Minimap)
        questPanel = new GameObject("QuestPanel");
        questPanel.transform.SetParent(canvasObj.transform, false);
        
        Image panelImg = questPanel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0f); // Tàng hình nền giống The Witcher 3

        RectTransform panelRect = questPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        // Đặt ở vị trí dưới Minimap Góc Trái
        panelRect.anchoredPosition = new Vector2(20, -240); 
        panelRect.sizeDelta = new Vector2(350, 300);

        Font fontToUse = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fontToUse == null) fontToUse = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (fontToUse == null) fontToUse = Font.CreateDynamicFontFromOSFont("Arial", 20);

        // Tạo Text Nội dung tổng (Gộp cả tiêu đề highlight và mô tả)
        GameObject contentObj = new GameObject("QuestContent");
        contentObj.transform.SetParent(questPanel.transform, false);
        
        questContentText = contentObj.AddComponent<Text>();
        questContentText.text = "Đang tải dữ liệu...";
        questContentText.font = fontToUse;
        questContentText.fontSize = 20;
        questContentText.color = Color.white;
        questContentText.alignment = TextAnchor.UpperLeft;
        questContentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        questContentText.verticalOverflow = VerticalWrapMode.Overflow;
        questContentText.supportRichText = true;
        
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.offsetMin = new Vector2(15, 10);    // Lề trái 15, lề dưới 10
        contentRect.offsetMax = new Vector2(-15, -15);  // Lề phải 15, lề trên 15
    }

    private void UpdateQuestUI(int phase)
    {
        if (questContentText == null) return;

        string questString = "";
        string colorHex = "#FFD700"; // Màu vàng Gold The Witcher
        switch(phase)
        {
            case StoryQuestManager.PHASE_START:
                questString = $"<color={colorHex}><b>TÌM KIẾM DẾ CHOẮT</b></color>\n  • Nói chuyện với Xén Tóc ở gần vũng nước\n  • Chiến thắng vật tay để lấy thông tin về Dế Choắt";
                break;
            case StoryQuestManager.PHASE_BEAT_XENTOC:
                questString = $"<color={colorHex}><b>ĐẾN CHỖ CON KIẾN</b></color>\n  • Nhờ Xén Tóc chở bay lên Bàn Ăn";
                break;
            case StoryQuestManager.PHASE_MEET_CONKIEN:
                questString = $"<color={colorHex}><b>LẤY MẬT ONG CỦA DẾ TRŨI</b></color>\n  • Tìm Dế Trũi ở bãi cỏ trong vườn\n  • Thắng cuộc đua để lấy Mật Ong";
                break;
            case StoryQuestManager.PHASE_BEAT_DETRUI:
                questString = $"<color={colorHex}><b>ĐƯA MẬT ONG CHO CON KIẾN</b></color>\n  • Đã có Mật Ong! Hãy đem về giao cho Con Kiến trên bàn ăn";
                break;
            case StoryQuestManager.PHASE_GIVE_ITEM:
                questString = $"<color={colorHex}><b>HỘI NGỘ DẾ CHOẮT</b></color>\n  • Dế Choắt đang nằm trên giường ở góc nhà\n  • Đến nói chuyện với Dế Choắt";
                break;
            case StoryQuestManager.PHASE_ENDING:
                questString = $"<color={colorHex}><b>HOÀN THÀNH CỐT TRUYỆN</b></color>\n  • Tự do khám phá map";
                break;
            default:
                questString = $"<color={colorHex}><b>KHÁM PHÁ THẾ GIỚI</b></color>\n  • Rong chơi";
                break;
        }

        // Định dạng Outline chữ đen để dễ đọc trên mọi nền (Drop Shadow)
        Outline textOutline = questContentText.gameObject.GetComponent<Outline>();
        if (textOutline == null)
        {
            textOutline = questContentText.gameObject.AddComponent<Outline>();
            textOutline.effectColor = new Color(0, 0, 0, 1f);
            textOutline.effectDistance = new Vector2(1, -1);
        }

        questContentText.text = questString;
        
        // Cập nhật vị trí Waypoint trên map
        UpdateMinimapWaypoint(phase);
    }
    
    private void UpdateMinimapWaypoint(int phase)
    {
        bool hasTarget = false;
        // Tìm tất cả DeTruiNPC trong cảnh
        DeTruiNPC[] allNPCs = FindObjectsByType<DeTruiNPC>(FindObjectsSortMode.None);
        foreach (var npc in allNPCs)
        {
            MinimapMarker marker = npc.GetComponent<MinimapMarker>();
            if (marker != null)
            {
                string nameLower = (npc.npcName ?? "").ToLower();
                string objNameLower = npc.gameObject.name.ToLower();
                bool isXenToc = nameLower.Contains("xén") || nameLower.Contains("xen") || objNameLower.Contains("xen");
                bool isConKien = nameLower.Contains("côn") || nameLower.Contains("con") || objNameLower.Contains("con");
                bool isDeTrui = nameLower.Contains("trũi") || nameLower.Contains("trui") || objNameLower.Contains("trui");
                bool isDeChoat = nameLower.Contains("choắt") || nameLower.Contains("choat") || objNameLower.Contains("choat");
                
                bool isTarget = false;
                switch (phase)
                {
                    case StoryQuestManager.PHASE_START:
                        if (isXenToc) isTarget = true; break;
                    case StoryQuestManager.PHASE_BEAT_XENTOC:
                        if (isXenToc || isConKien) isTarget = true; break;
                    case StoryQuestManager.PHASE_MEET_CONKIEN:
                        if (isDeTrui) isTarget = true; break;
                    case StoryQuestManager.PHASE_BEAT_DETRUI:
                        if (isConKien) isTarget = true; break;
                    case StoryQuestManager.PHASE_GIVE_ITEM:
                        if (isDeChoat) isTarget = true; break;
                }

                if (isTarget)
                {
                    hasTarget = true;
                    // Đánh dấu đỏ rực và to lên trên minimap
                    marker.SetColor(Color.red);
                    Transform markerObj = marker.transform.Find("MinimapIcon_" + marker.gameObject.name);
                    if (markerObj != null) markerObj.localScale = new Vector3(8f, 8f, 1f);

                    // 1. Cột sáng trắng trong suốt tại NPC
                    Transform lightPillar = npc.transform.Find("QuestLightPillar");
                    if (lightPillar == null)
                    {
                        GameObject pillarObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        pillarObj.name = "QuestLightPillar";
                        pillarObj.transform.SetParent(npc.transform, false);
                        pillarObj.transform.localPosition = new Vector3(0, 1.5f, 0); // Đặt ở ngang giữa thân NPC
                        pillarObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f); // Kích thước sàn sàn bằng NPC
                        Collider col = pillarObj.GetComponent<Collider>();
                        if (col != null) Destroy(col);
                        Renderer r = pillarObj.GetComponent<Renderer>();
                        if (r != null)
                        {
                            // Sửa lỗi hồng do thiếu Standard shader bằng Shader cơ bản UI/Default hỗ trợ Alpha nhẹ nhàng
                            Material mat = new Material(Shader.Find("UI/Default"));
                            mat.color = new Color(1f, 1f, 1f, 0.15f); // Trắng mờ ảo trong suốt cực thấp
                            r.material = mat;
                        }
                        lightPillar = pillarObj.transform;
                    }
                    lightPillar.gameObject.SetActive(true);
                    currentQuestPillar = lightPillar; // Gán để theo dõi tự động tắt khi đến gần

                    // 2. Mũi tên chỉ đường (chỉ hiện trên minimap layer)
                    UpdatePlayerQuestPointer(npc.transform.position);
                }
                else
                {
                    // Tắt cột sáng nếu không phải mục tiêu Quest
                    Transform lightPillar = npc.transform.Find("QuestLightPillar");
                    if (lightPillar != null) lightPillar.gameObject.SetActive(false);

                    // Trả về vàng hoặc xám mặc định cho NPC ko liên quan
                    marker.SetColor(Color.yellow);
                    Transform markerObj = marker.transform.Find("MinimapIcon_" + marker.gameObject.name);
                    if (markerObj != null) markerObj.localScale = new Vector3(3f, 3f, 1f);
                }
            }
        }

        // Tắt mũi tên và Cột sáng nếu không tìm thấy mục tiêu nào trên Map
        if (!hasTarget)
        {
            if (questLinePointer != null) questLinePointer.enabled = false;
            currentQuestPillar = null;
        }
    }

    private void UpdatePlayerQuestPointer(Vector3 targetPos)
    {
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo == null) return;

        if (questLinePointer == null)
        {
            GameObject pointerObj = new GameObject("QuestLinePointer");
            pointerObj.transform.SetParent(playerGo.transform, false);
            
            // Chỉ hiển thị trong Camera Minimap (Layer 8: MinimapIcon)
            int minimapLayer = LayerMask.NameToLayer("MinimapIcon");
            if (minimapLayer == -1) minimapLayer = 8;
            pointerObj.layer = minimapLayer;

            questLinePointer = pointerObj.AddComponent<LineRenderer>();
            questLinePointer.startWidth = 9.0f; // Bắt đầu dày (Gần player)
            questLinePointer.endWidth = 1.0f;   // Kéo nhọn về hướng NPC (như mũi tên)
            questLinePointer.positionCount = 2;
            questLinePointer.useWorldSpace = true;

            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.white; 
            questLinePointer.material = mat;
        }

        if (questLinePointer != null)
        {
            questLinePointer.enabled = true;
            // Vẽ mũi tên nổi cao trên player (Y=200) để camera minimap thấy rõ 
            Vector3 startPos = playerGo.transform.position;
            startPos.y = 200f; 
            
            Vector3 endPos = targetPos;
            endPos.y = 200f; 

            // Cắt độ dài mũi tên, không nối sát vào NPC
            Vector3 dir = (endPos - startPos).normalized;
            float maxArrowDist = 35f; 
            float currentDist = Vector3.Distance(startPos, endPos);
            float length = Mathf.Clamp(currentDist, 0f, maxArrowDist);

            questLinePointer.SetPosition(0, startPos);
            questLinePointer.SetPosition(1, startPos + dir * length);
        }
    }
}
