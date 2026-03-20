using UnityEngine;
using UnityEngine.InputSystem;

public class ConKienNPC : MonoBehaviour
{
    [Header("Chat Bubble")]
    public ChatBubble chatBubble;
    public GameObject interactionPromptUI; 
    


    [Header("Identidade")]
    public string npcName = "Kiến Chỉ Huy";
    [TextArea(3, 10)]
    public string[] dialogue = new string[] {
        "Hỡi nhà phiêu lưu! Cậu tìm đến Hang Kiến chúng tôi có việc gì?",
        "Xin hãy cẩn thận, dạo này lũ Xén Tóc lộng hành ghê lắm.",
        "Nếu cậu vào hang, hãy theo tôi!" 
    };
    public AudioClip typewriterBeep;

    [Header("Settings Khung Cảnh (Skyrim-like)")]
    public float interactionDistance = 3.0f;
    [Tooltip("Điều chỉnh vị trí camera khi Focus nói chuyện (So với mặt NPC)")]
    public Vector3 cameraFocusOffset = new Vector3(0, 0.4f, 1.2f); 
    public float cameraTransitionSpeed = 5f;

    [Header("Animation")]
    public Animator animator;
    public string talkTrigger = "Talk";
    public string idleTrigger = "Idle";
    public string runBool = "IsRunning";

    [Header("Follow Settings")]
    public float followSpeed = 6f; // Kiến chạy nhanh hơn tẹo
    [Tooltip("Khoảng cách bám theo khi chạy")]
    public float stopDistance = 2.5f;

    // Các biến Logic ẩn danh
    private Transform player;
    private PlayerController playerController;
    private Camera mainCamera;
    
    private bool isPlayerNearby = false;
    private bool isTalking = false;
    private bool isWaitingForChoice = false;
    private bool isFollowing = false;
    
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
            
        // Tìm chữ trong nút bấm UI prompt
        if (interactionPromptUI != null)
        {
            promptTextComp = interactionPromptUI.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (promptTextComp != null) originalPromptText = promptTextComp.text;
        }
    }

    void Update()
    {
        if (player == null) return;
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // --- CÓ LỰA CHỌN (MENU: ĐI CÙNG HAY KHÔNG) ---
        if (isWaitingForChoice)
        {
            HandleCameraFocusSkyrim(); // Vẫn giữ cam khóa chặt mặt
            
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame)
                {
                    isFollowing = true;
                    isWaitingForChoice = false;
                    EndInteraction();
                    chatBubble.Setup("Tiến lên nào!", typewriterBeep);
                    Invoke("HideBubble", 2f);
                }
                else if (kb.digit2Key.wasPressedThisFrame)
                {
                    isFollowing = false;
                    isWaitingForChoice = false;
                    EndInteraction();
                    chatBubble.Setup("Hãy gọi nếu cậu cần bảo vệ!", typewriterBeep);
                    Invoke("HideBubble", 2f);
                }
                // Thoát ngang bằng phím Tab (Như yêu cầu Skyrim)
                else if (kb.tabKey.wasPressedThisFrame)
                {
                    isFollowing = false;
                    isWaitingForChoice = false;
                    EndInteraction();
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

        if (isFollowing)
        {
            if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
            
            float dist = Vector3.Distance(transform.position, player.position);
            bool isMovingNow = false;
            
            if (dist > stopDistance)
            {
                Vector3 targetPos = player.position;
                targetPos.y = transform.position.y;
                Vector3 dir = (targetPos - transform.position).normalized;
                
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, targetPos, followSpeed * Time.deltaTime);
                isMovingNow = true;
            }
            else
            {
                FacePlayerTarget();
                isMovingNow = false;
            }

            if (animator != null) animator.SetBool(runBool, isMovingNow);
            
            if (dist <= interactionDistance && kb != null && kb.fKey.wasPressedThisFrame)
            {
                isFollowing = false;
                if (animator != null) animator.SetBool(runBool, false);
                chatBubble.Setup("Tập hợp hàng ngũ!", typewriterBeep);
                Invoke("HideBubble", 2f);
            }
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool currentlyNearby = distanceToPlayer <= interactionDistance;

        if (currentlyNearby != isPlayerNearby)
        {
            isPlayerNearby = currentlyNearby;
            if (!isTalking && !isWaitingForChoice && interactionPromptUI != null)
            {
                interactionPromptUI.SetActive(isPlayerNearby);
            }
        }

        if (isPlayerNearby && !isTalking && !isWaitingForChoice)
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
        // Xoay Camera ngắm vào khuôn mặt của NPC (Cao hơn thân một tẹo)
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

        // Đã dời xuống EndInteraction để unlock sau khi nói chuyện xong
        
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
        isWaitingForChoice = true;
        if (chatBubble != null) chatBubble.Hide();

        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(true);
            if (promptTextComp != null)
            {
                promptTextComp.text = "[1] Rủ đi cùng\n[2] Bỏ qua";
                promptTextComp.fontSize = 25; 
            }
        }
    }

    public void EndInteraction()
    {
        isTalking = false;
        if (EncyclopediaManager.Instance != null) EncyclopediaManager.Instance.UnlockInsect("ConKien");
        isWaitingForChoice = false;
        
        if (chatBubble != null) chatBubble.Hide();

        // Hoàn thành quest Dế tìm đường vào Hang Kiến
        if (QuestUIManager.Instance != null && !QuestUIManager.Instance.IsQuestCompleted("talk_conkien"))
        {
            QuestUIManager.Instance.CompleteQuest("talk_conkien");
        }

        // Phục hồi lại Chữ & Cỡ Chữ gốc Prompt
        if (promptTextComp != null && !string.IsNullOrEmpty(originalPromptText))
        {
            promptTextComp.text = originalPromptText;
            promptTextComp.fontSize = 50;
        }

        if (isPlayerNearby && !isFollowing)
        {
            if (interactionPromptUI != null) interactionPromptUI.SetActive(true);
        }
        else
        {
            if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
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
}
