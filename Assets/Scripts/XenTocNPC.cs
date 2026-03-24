using UnityEngine;
using UnityEngine.InputSystem;

public class XenTocNPC : MonoBehaviour, INPCMinigame
{
    [Header("Chat Bubble / Boss UI")]
    public ChatBubble chatBubble;
    public GameObject interactionPromptUI; 
    


    [Header("Identidade")]
    [SerializeField] private string _npcName = "Xén Tóc (Boss)";
    public string npcName { get => _npcName; set => _npcName = value; }
    [TextArea(3, 10)]
    public string[] dialogue = new string[] {
        "(Xén Tóc): Ồ! Dế Mèn hả? Lâu rồi không gặp. Mày tìm ta vì chuyện gì?",
        "(Dế Mèn): Chào anh, tôi đang đi tìm Dế Choắt, anh có biết hắn ở đâu không?",
        "(Xén Tóc): Dế Choắt á? Heh... ta biết hắn ở đâu đó.",
        "(Xén Tóc): Nhưng ta không chỉ đường miễn phí đâu nhé. Muốn biết, hãy thắng ta trong một cú vật tay cái đã!"
    };

    [Header("Story: Dialogue sau khi đã thắng")]
    [TextArea(3, 10)]
    public string[] dialogueAfterWin = new string[] {
        "(Xén Tóc): Ừ, ngươi thắng rồi đó Mèn... Ta giữ lời.",
        "(Xén Tóc): Dế Choắt đang ở trong trần nhà kia. Nhưng đường đó Côn Kiến đang canh giữ bấy lâu.",
        "(Xén Tóc): Leo lên lưng ta đi, ta cõng ngươi bay thẳng vào trong nhà!"
    };
    [TextArea(3, 10)]
    public string[] dialoguePhase2 = new string[] {
        "(Xén Tóc): Sao cơ? Lão Côn Kiến bắt mày đi tìm mật ong của Dế Trũi à?",
        "(Xén Tóc): Lão khập khiễng đó đúng là khó tính... Cơ mà ta biết hang Dế Trũi ở đâu đấy!",
        "(Xén Tóc): Đường xa lắm, lên lưng tao đi, tao chở mày bay qua đó!"
    };

    [Header("Story: Dialogue khi đã có Mật Ong (Chưa qua lính gác)")]
    [TextArea(3, 10)]
    public string[] dialoguePhase3 = new string[] {
        "(Xén Tóc): Giỏi lắm! Lấy được mật ong rồi đúng không? Mùi thơm bay tận ra đây.",
        "(Xén Tóc): Nhanh leo lên lưng đi, tao sẽ chở mày trở lại chỗ Côn Kiến trên mái nhà!"
    };

    [Header("Story: Dialogue khi đã Vượt Lính Gác Côn Kiến")]
    [TextArea(3, 10)]
    public string[] dialoguePhase4 = new string[] {
        "(Xén Tóc): Lão Côn Kiến chịu nhường đường rồi à? Khá lắm!",
        "(Xén Tóc): Dế Choắt đang ở góc đó kìa. Cứ đi bộ vào là thấy.",
        "(Xén Tóc): Nếu thích dạo chơi thêm thì cứ nhảy lên lưng tao nhé!"
    };
    public AudioClip typewriterBeep;

    [Header("Settings Khung Cảnh (Skyrim-like)")]
    public float interactionDistance = 4.0f; // Khoảng cách nói chuyện xa hơn vì là Boss
    [Tooltip("Điều chỉnh vị trí camera khi Focus nói chuyện (So với mặt NPC)")]
    public Vector3 cameraFocusOffset = new Vector3(0, 1.0f, 2.0f); // Boss bự nên góc rộng hơn
    public float cameraTransitionSpeed = 5f;

    [Header("Animation")]
    public Animator animator;
    public string talkTrigger = "Talk";
    public string idleTrigger = "Idle";
    public string aggroTrigger = "Aggro";

    // Các biến Logic ẩn danh
    private Transform player;
    private PlayerController playerController;
    private Camera mainCamera;
    
    private bool isPlayerNearby = false;
    private bool isTalking = false;
    private bool isWaitingForCombat = false;
    private bool _isWaitingMinigameResultDelay = false;
    public bool isMinigameActive { get; set; }

    // --- CHẾ ĐỘ CƯỠI (Hidden Interaction) ---
    private bool _hasWon = false;          // Đã từng thắng Xén Tóc chưa
    private bool _isWaitingRideChoice = false; // Đang chờ chọn Cưỡi hay không
    private MountXenTocController _mounter; // Component điều khiển cưỡi
    
    // Quản lý đoạn hội thoại Skyrim
    private int currentDialogueIndex = 0;

    
    // Lưu tạm thời vị trí Camera (Trở về ban đầu kết thúc Dialog)
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private Transform originalCameraParent;

    private TMPro.TextMeshProUGUI promptTextComp;
    private string originalPromptText;


    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) {
                 player = p.transform;
                 playerController = p.GetComponent<PlayerController>();
            }
        }
        
        int phase = StoryQuestManager.Instance != null ? StoryQuestManager.Instance.currentPhase : 0;
        if (phase >= StoryQuestManager.PHASE_BEAT_XENTOC && PlayerPrefs.GetInt("HasSavedXenTocPos", 0) == 1)
        {
            float px = PlayerPrefs.GetFloat("XenTocPosX", transform.position.x);
            float py = PlayerPrefs.GetFloat("XenTocPosY", transform.position.y);
            float pz = PlayerPrefs.GetFloat("XenTocPosZ", transform.position.z);
            Vector3 savedPos = new Vector3(px, py, pz);

            if (Vector3.Distance(transform.position, savedPos) > 1f)
            {
                transform.position = savedPos;
            }
        }

        mainCamera = Camera.main;

        if (animator == null) animator = GetComponent<Animator>();

        // Thêm component MountController nếu chưa có
        _mounter = GetComponent<MountXenTocController>();
        if (_mounter == null)
            _mounter = gameObject.AddComponent<MountXenTocController>();
            
        // Tìm chữ trong nút bấm UI prompt
        if (interactionPromptUI != null)
        {
            promptTextComp = interactionPromptUI.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (promptTextComp != null)
            {
                originalPromptText = _npcName;
                promptTextComp.text = originalPromptText;
            }
        }

        // Khôi phục trạng thái thắng từ lần chơi trước
        _hasWon = PlayerPrefs.GetInt("XenToc_PlayerWon", 0) == 1;
    }

    void Update()
    {
        bool isAnyMinigameActiveGlobally = isMinigameActive || 
            (CaroGameManager.Instance != null && CaroGameManager.Instance.IsGameActive) ||
            (ArmWrestlingManager.Instance != null && ArmWrestlingManager.Instance.IsGameActive);

        if (player == null || isAnyMinigameActiveGlobally) 
        {
            if (isAnyMinigameActiveGlobally)
            {
                if (animator != null && HasParameter(idleTrigger)) animator.SetTrigger(idleTrigger);
            }
            return;
        }
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // --- LỰA CHỌN CƯỠI (sau khi thắng vật tay) ---
        if (_isWaitingRideChoice)
        {
            HandleCameraFocusSkyrim();
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame)
                {
                    _isWaitingRideChoice = false;
                    HidePrompt();
                    EndInteraction();
                    if (_mounter != null && player != null)
                        _mounter.Mount(player);
                }
                else if (kb.digit2Key.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
                {
                    _isWaitingRideChoice = false;
                    EndInteraction();
                }
            }
            return;
        }

        // --- CÓ LỰA CHỌN (BẮT ĐẦU ĐÁNH NHAU) ---
        if (isWaitingForCombat)
        {
            HandleCameraFocusSkyrim(); // Vẫn giữ cam khóa chặt mặt
            
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame)
                {
                    isWaitingForCombat = false;
                    EndInteractionForCombat();
                    
                    if (animator != null && HasParameter(aggroTrigger)) animator.SetTrigger(aggroTrigger);
                    chatBubble.Setup("TỚI ĐÂYYY!", typewriterBeep);
                    Invoke("HideBubble", 2f);
                    
                    if (ArmWrestlingManager.Instance != null)
                    {
                        ArmWrestlingManager.Instance.StartGame(this);
                    }
                    else
                    {
                        Debug.LogError("ArmWrestlingManager.Instance is NULL!");
                    }
                }
                else if (kb.digit2Key.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
                {
                    isWaitingForCombat = false;
                    EndInteraction(); // Cho chạy trốn
                    chatBubble.Setup("Hahaha! Kẻ hèn nhát!", typewriterBeep);
                    Invoke("HideBubble", 2f);
                }
            }
            return;
        }

        // --- TRONG QUÁ TRÌNH HỘI THOẠI (NEXT BẰNG CHUỘT / THOÁT BẰNG TAB) ---
        if (isTalking)
        {
            HandleCameraFocusSkyrim();
            
            if (kb != null && kb.tabKey.wasPressedThisFrame)
            {
                // Bấm TAB thoát ngay lập tức
                EndInteraction();
                return;
            }

            bool nextPressed = (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                               (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame));

            if (nextPressed)
            {
                if (chatBubble != null && chatBubble.isTyping)
                {
                    chatBubble.FastForward();
                }
                else
                {
                    TriggerNextSentence();
                }
            }
            return;
        }

        // --- LOGIC PHÍA DƯỚI LÀ DEFAULT KHI KHÔNG NÓI CHUYỆN ---

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        bool nowNearby = distToPlayer <= interactionDistance && !MountXenTocController.IsRiding;
        isPlayerNearby = nowNearby;

        // KIỂM TRA ĐỘ ƯU TIÊN: Chỉ hiện nút F nếu là NPC được Dế Mèn nhìn vào rõ nhất
        bool isBestCandidate = playerController != null && playerController.GetClosestNPC() == gameObject;
        bool shouldShowPrompt = isPlayerNearby && isBestCandidate && !isTalking && !isWaitingForCombat && !isMinigameActive;

        if (interactionPromptUI != null && interactionPromptUI.activeSelf != shouldShowPrompt)
        {
            interactionPromptUI.SetActive(shouldShowPrompt);
        }

        if (shouldShowPrompt)
        {
            if (kb != null && kb.fKey.wasPressedThisFrame)
            {
                StartInteraction();
            }
        }

        // Xoay mặt mượt mà khi đang nói chuyện
        if (isTalking || isWaitingForCombat || _isWaitingRideChoice)
        {
            FacePlayerTarget();
        }
    }
    
    private void HandleCameraFocusSkyrim()
    {
        if (mainCamera == null) return;
        
        FacePlayerTarget();

        // Target Cận mặt nhân vật NPC một chút
        Vector3 targetPos = transform.position + transform.rotation * cameraFocusOffset;
        // Xoay Camera ngắm vào khuôn mặt
        Quaternion targetRot = Quaternion.LookRotation((transform.position + Vector3.up * 0.5f) - targetPos);

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, Time.deltaTime * cameraTransitionSpeed);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRot, Time.deltaTime * cameraTransitionSpeed);
    }
    
    void FacePlayerTarget()
    {
        if (player == null) return;
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; 
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
    }

    void HideBubble() { if (chatBubble != null) chatBubble.Hide(); }

    private bool HasParameter(string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    void StartInteraction()
    {
        isTalking = true;

        // Đã dời xuống EndMinigame để unlock sau khi đấu xong
        
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);

        FacePlayerTarget();
        if (animator != null && HasParameter(talkTrigger)) animator.SetTrigger(talkTrigger);

        if (playerController != null)
        {
            // Skyrim action: FREEZE PLAYER
            playerController.isDialoguing = true;
        }

        if (mainCamera != null)
        {
            originalCameraParent = mainCamera.transform.parent;
            originalCameraPos = mainCamera.transform.localPosition;
            originalCameraRot = mainCamera.transform.localRotation;
        }

        currentDialogueIndex = 0;
        var story = StoryQuestManager.Instance;

        int phase = story.currentPhase;
        if (phase == StoryQuestManager.PHASE_START)
        {
            // Vẫn dùng dialogue mặc định cho Phase 0
        }
        else if (phase == StoryQuestManager.PHASE_BEAT_XENTOC)
        {
            dialogue = dialogueAfterWin;
        }
        else if (phase == StoryQuestManager.PHASE_MEET_CONKIEN)
        {
            dialogue = dialoguePhase2;
        }
        else if (phase == StoryQuestManager.PHASE_BEAT_DETRUI)
        {
            dialogue = dialoguePhase3;
        }
        else // Phase >= PHASE_GIVE_ITEM (Đã vượt lính gác)
        {
            dialogue = dialoguePhase4;
        }

        DisplayCurrentSentence();
    }
    
    void TriggerNextSentence()
    {
        if (_isWaitingMinigameResultDelay) return;

        currentDialogueIndex++;
        if (currentDialogueIndex < dialogue.Length)
        {
            DisplayCurrentSentence();
        }
        else
        {
            int phase = StoryQuestManager.Instance.currentPhase;
            if (phase == StoryQuestManager.PHASE_START)
                ShowChoice();
            else
                ShowReturnRideChoice();
        }
    }
    
    void DisplayCurrentSentence()
    {

        if (chatBubble != null)
        {
            chatBubble.Setup(dialogue[currentDialogueIndex], typewriterBeep);
        }
    }

    void ShowChoice()
    {
        isTalking = false;
        isWaitingForCombat = true;
        if (chatBubble != null) chatBubble.Hide();

        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(true);
            if (promptTextComp != null)
            {
                promptTextComp.text = "[1] Nghênh Chiến!\n[2] Bỏ Trốn";
                promptTextComp.fontSize = 25; 
            }
        }
    }

    // Kết thúc nói chuyện bình thường (Chưa đánh bại)
    public void EndInteraction()
    {
        isTalking = false;
        isWaitingForCombat = false;
        _isWaitingRideChoice = false;
        _isWaitingMinigameResultDelay = false;
        
        if (chatBubble != null) chatBubble.Hide();

        if (QuestUIManager.Instance != null && !QuestUIManager.Instance.IsQuestCompleted("talk_xentoc"))
        {
            QuestUIManager.Instance.CompleteQuest("talk_xentoc");
        }

        // Phục hồi lại Chữ & Cỡ Chữ gốc Prompt
        if (promptTextComp != null && !string.IsNullOrEmpty(originalPromptText))
        {
            promptTextComp.text = originalPromptText;
            promptTextComp.fontSize = 50;
        }

        if (isPlayerNearby)
        {
            if (interactionPromptUI != null) interactionPromptUI.SetActive(true);
        }

        if (animator != null && HasParameter(idleTrigger)) animator.SetTrigger(idleTrigger);

        // -- Skyrim Action RECOVER --
        if (playerController != null)
        {
            playerController.isDialoguing = false; // UNFREEZE
        }
        if (mainCamera != null)
        {
            // Trả Camera lại điểm gốc
            mainCamera.transform.localPosition = originalCameraPos;
            mainCamera.transform.localRotation = originalCameraRot;
        }
    }
    
    // Kết thúc nói chuyện và chuyển hẳn sang chiến đấu
    public void EndInteractionForCombat()
    {
        EndInteraction();
        
        // Khi người chơi thắng trận thì mới gọi hàm này ở nơi khác (ví dụ Health == 0):
        }

    public void EndMinigame(bool isWin, bool isDraw = false)
    {
        isMinigameActive = false;
        if (EncyclopediaManager.Instance != null) EncyclopediaManager.Instance.UnlockInsect("XenToc");
        isTalking = true;
        isWaitingForCombat = false;
        _isWaitingMinigameResultDelay = true;
        
        string resultText = "";
        
        if (isDraw) resultText = "Cứng đầu đấy! Hoà thì hoà, lần sau ta không nhường đâu!";
        else if (isWin)
        {
            resultText = "KHÔNG THỂ NÀO! Sức mạnh của ta bị đánh bại sao?! ...Ngươi... không tệ lắm.";
            _hasWon = true;
            PlayerPrefs.SetInt("XenToc_PlayerWon", 1);
            PlayerPrefs.Save();
            // ── Tiến cốt truyện ──────────────────────────────────────────
            StoryQuestManager.Instance.AdvanceTo(StoryQuestManager.PHASE_BEAT_XENTOC);
            // Đổi sang dialogue cốt truyện cho lần cưỡi
            if (dialogueAfterWin != null && dialogueAfterWin.Length > 0)
                dialogue = dialogueAfterWin;
        }
        else resultText = "Há há há! Dăm ba cái đồ tôm tép, ngoan ngoãn chắp tay gọi ta bằng ngài đi!";
        
        if (QuestUIManager.Instance != null && !QuestUIManager.Instance.IsQuestCompleted("minigame_xentoc"))
        {
            QuestUIManager.Instance.CompleteQuest("minigame_xentoc");
        }
        
        if (chatBubble != null) 
        {
            chatBubble.Setup(resultText, typewriterBeep);
        }
        
        if (isWin)
        {
            // Sau 3 giây hiện lựa chọn Cưỡi
            Invoke(nameof(ShowRideChoice), 3f);
        }
        else
        {
            Invoke(nameof(EndInteraction), 3f);
        }
    }

    // Hiện lựa chọn bí mật: Cưỡi Xén Tóc
    void ShowRideChoice()
    {
        isTalking = false;
        _isWaitingRideChoice = true;
        _isWaitingMinigameResultDelay = false;
        if (chatBubble != null) chatBubble.Hide();

        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(true);
            if (promptTextComp != null)
            {
                promptTextComp.text = "[1] Cưỡi Xén Tóc!\n[2] Bỏ qua";
                promptTextComp.fontSize = 22;
            }
        }
    }

    // Hiện lựa chọn khi player quay lại nói chuyện sau khi đã thắng
    void ShowReturnRideChoice()
    {
        isTalking = false;
        _isWaitingRideChoice = true;
        if (chatBubble != null) chatBubble.Hide();

        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(true);
            if (promptTextComp != null)
            {
                promptTextComp.text = "Muốn cưỡi ta không?\n[1] Cưỡi tiếp!\n[2] Thôi";
                promptTextComp.fontSize = 22;
            }
        }
    }

    void HidePrompt()
    {
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
    }

    // Đã gỡ bỏ IsClosestNPC

    void OnGUI()
    {
        if (isPlayerNearby && !isTalking && !isWaitingForCombat && !_isWaitingRideChoice && !isMinigameActive)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            // Draw shadow
            GUIStyle shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = Color.black;

            float w = 200;
            float h = 50;
            float x = (Screen.width - w) / 2;
            float y = (Screen.height - h) / 2;

            string text = "[F] Nói chuyện";
            GUI.Label(new Rect(x + 2, y + 2, w, h), text, shadowStyle);
            GUI.Label(new Rect(x, y, w, h), text, style);
        }
    }
}
