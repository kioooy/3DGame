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
        "Muahaha! Ngươi tưởng có thể vượt qua ta sao?",
        "Tên Dế Mèn bé nhỏ kia, đây sẽ là nơi chôn xác ngươi!",
        "Chuẩn bị chịu chết đi!" 
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
    private bool isWaitingForCombat = false; // Thay vì đợi lựa chọn, Boss đợi đánh
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
            if (promptTextComp != null) originalPromptText = promptTextComp.text;
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
                    
                    if (animator != null) animator.SetTrigger(aggroTrigger);
                    chatBubble.Setup("TỚI ĐÂYYY!", typewriterBeep);
                    Invoke("HideBubble", 2f);
                    
                    // TODO: GỌI HÀM BẮT ĐẦU COMBAT Ở ĐÂY
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

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool currentlyNearby = distanceToPlayer <= interactionDistance;

        if (currentlyNearby != isPlayerNearby)
        {
            isPlayerNearby = currentlyNearby;
            if (!isTalking && !isWaitingForCombat && interactionPromptUI != null)
            {
                interactionPromptUI.SetActive(isPlayerNearby);
            }
        }

        if (isPlayerNearby && !isTalking && !isWaitingForCombat)
        {
            if (kb != null && kb.fKey.wasPressedThisFrame)
            {
                StartInteraction();
            }
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

        DisplayCurrentSentence();
    }
    
    void TriggerNextSentence()
    {
        currentDialogueIndex++;
        if (currentDialogueIndex < dialogue.Length)
        {
            DisplayCurrentSentence();
        }
        else
        {
            // Nếu đã từng thắng → hiện lựa chọn cưỡi luôn
            if (_hasWon)
                ShowReturnRideChoice();
            else
                ShowChoice();
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
        
        string resultText = "";
        
        if (isDraw) resultText = "Cứng đầu đấy! Hoà thì hoà, lần sau ta không nhường đâu!";
        else if (isWin)
        {
            resultText = "KHÔNG THỂ NÀO! Sức mạnh của ta bị đánh bại sao?! ...Ngươi... không tệ lắm.";
            _hasWon = true;
            PlayerPrefs.SetInt("XenToc_PlayerWon", 1);
            PlayerPrefs.Save();
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
}
