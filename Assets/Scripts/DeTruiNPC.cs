using UnityEngine;
using UnityEngine.InputSystem;

public class DeTruiNPC : MonoBehaviour
{
    [Header("Chat Bubble")]
    public ChatBubble chatBubble;
    public GameObject interactionPromptUI; 
    
    // Tốc độ bình thường mỗi câu chữ
    public float timePerSentence = 2.5f;

    [Header("Identidade")]
    public string npcName = "Dế Trũi";
    [TextArea(3, 10)]
    public string[] dialogue = new string[] {
        "Chào người anh em! Tôi là Dế Trũi đây.",
        "Cuộc đời là những chuyến đi dài, phải không nào?",
        "Nếu cậu cần người đồng hành, tôi luôn sẵn lòng!" 
    };

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
    public float followSpeed = 4f;
    [Tooltip("Khoảng cách bám theo khi chạy")]
    public float stopDistance = 2.5f;

    [Header("Jump / Physics Settings")]
    public float jumpForce = 8f;
    public float gravity = -20f;
    public float jumpObstacleCheckDist = 0.8f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    // Các biến Logic ẩn danh
    private float verticalVelocity = 0f;
    private bool isJumping = false;

    private Transform player;
    private PlayerController playerController;
    private Camera mainCamera;
    
    private bool isPlayerNearby = false;
    private bool isTalking = false;
    private bool isWaitingForChoice = false;
    private bool isFollowing = false;
    
    // Quản lý đoạn hội thoại Skyrim
    private int currentDialogueIndex = 0;
    private float dialogueTimerLengthThreshold = 0f;
    
    // Lưu tạm thời vị trí Camera (Trở về ban đầu kết thúc Dialog)
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private Transform originalCameraParent;

    private TMPro.TextMeshProUGUI promptTextComp;
    private string originalPromptText;


    private Rigidbody rb;
    private Vector3 currentVelocity;

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
            
        if (interactionPromptUI != null)
        {
            promptTextComp = interactionPromptUI.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (promptTextComp != null) originalPromptText = promptTextComp.text;
        }

        rb = GetComponent<Rigidbody>();

        // --- Minimap Marker ---
        MinimapMarker marker = gameObject.AddComponent<MinimapMarker>();
        marker.markerColor = Color.yellow; // Friendly NPC / Follower
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
                    chatBubble.Setup("Được thôi, tôi sẽ đi theo cậu!");
                    Invoke("HideBubble", 2f);
                }
                else if (kb.digit2Key.wasPressedThisFrame)
                {
                    isFollowing = false;
                    isWaitingForChoice = false;
                    EndInteraction();
                    chatBubble.Setup("Không sao, hẹn gặp lại nhé!");
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

            // Đếm thời gian tự động đổi dòng
            dialogueTimerLengthThreshold += Time.deltaTime;

            if ((mouse != null && mouse.leftButton.wasPressedThisFrame) || dialogueTimerLengthThreshold >= timePerSentence)
            {
                // Qua câu tiếp theo
                TriggerNextSentence();
            }
            return;
        }

        // --- LOGIC Y HỆT NHƯ CŨ + THÊM GRAVITY & JUMP BẰNG RIGIDBODY ---
        if (isFollowing)
        {
            if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
            
            float dist = Vector3.Distance(transform.position, player.position);
            bool isMovingNow = false;

            // --- JUMP & GRAVITY LOGIC ---
            // Có collider nên bắn tia ray cao hơn xí để tránh dính đít collider (0.2f)
            bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.5f, groundLayer);
            
            if (isGrounded && verticalVelocity <= 0)
            {
                verticalVelocity = -1f; // Stick to ground
                if (isJumping)
                {
                    isJumping = false;
                    // Bỏ qua animator SetBool vì Animator của Dế Trũi hiện không có Parameter Jump
                }
            } 
            else 
            {
                verticalVelocity += gravity * Time.deltaTime; // Apply gravity
            }

            Vector3 moveDir = Vector3.zero;

            if (dist > stopDistance)
            {
                Vector3 targetPos = player.position;
                targetPos.y = transform.position.y;
                Vector3 dir = (targetPos - transform.position).normalized;
                
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

                float currentSpeed = followSpeed;
                if (dist > stopDistance + 2f)
                {
                    currentSpeed = followSpeed * 1.8f;
                }

                moveDir = dir * currentSpeed;
                isMovingNow = true;

                // --- OBSTACLE DETECTION FOR JUMP ---
                if (isGrounded && !isJumping)
                {
                    // Bắn tia ray ngang gối/đùi (cách đáy 0.3f)
                    Vector3 rayStart = transform.position + Vector3.up * 0.3f;
                    bool hitWall = Physics.Raycast(rayStart, dir, jumpObstacleCheckDist, obstacleLayer);
                    if (hitWall)
                    {
                        // Thấy tường gần -> Nhảy!
                        verticalVelocity = jumpForce;
                        isJumping = true;
                        // Bỏ qua animator SetBool vì Animator của Dế Trũi hiện không có Parameter Jump
                    }
                }
            }
            else
            {
                FacePlayerTarget();
                isMovingNow = false;
            }

            // Lưu vận tốc vào currentVelocity để dùng trong FixedUpdate
            currentVelocity = moveDir;

            if (animator != null) animator.SetBool(runBool, isMovingNow);
            
            if (dist <= interactionDistance && kb != null && kb.fKey.wasPressedThisFrame)
            {
                isFollowing = false;
                if (animator != null) animator.SetBool(runBool, false);
                chatBubble.Setup("Tôi sẽ đứng chờ ở đây!");
                Invoke("HideBubble", 2f);
            }
            return;
        }

        // Logic khi ĐỨNG YÊN (Không follow)
        // Vẫn phải check chạm đất nếu bị rớt
        bool groundCheckIdle = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.5f, groundLayer);
        if (groundCheckIdle && verticalVelocity <= 0) {
            verticalVelocity = -1f;
        } else {
            verticalVelocity += gravity * Time.deltaTime;
        }
        currentVelocity = Vector3.zero;


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

    void FixedUpdate()
    {
        if (rb != null)
        {
            // Di chuyển bằng Rigidbody thay vì Transform
            Vector3 finalMove = currentVelocity * Time.fixedDeltaTime;
            finalMove.y = verticalVelocity * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + finalMove);
        }
    }
    
    private void HandleCameraFocusSkyrim()
    {
        if (mainCamera == null) return;
        
        FacePlayerTarget();

        // Target Cận mặt nhân vật NPC một chút
        Vector3 targetPos = transform.position + transform.rotation * cameraFocusOffset;
        // Xoay Camera ngắm vào khuôn mặt của NPC DeTrui (Cao hơn thân một tẹo)
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

    void StartInteraction()
    {
        isTalking = true;
        
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);

        FacePlayerTarget();
        if (animator != null) animator.SetTrigger(talkTrigger);

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
        dialogueTimerLengthThreshold = 0f;
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
        dialogueTimerLengthThreshold = 0f; // Reset khung giờ chờ
        if (chatBubble != null)
        {
            chatBubble.Setup(dialogue[currentDialogueIndex]);
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
        isWaitingForChoice = false;
        
        if (chatBubble != null) chatBubble.Hide();

        if (QuestUIManager.Instance != null && !QuestUIManager.Instance.IsQuestCompleted("talk_detrui"))
        {
            QuestUIManager.Instance.CompleteQuest("talk_detrui");
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

        if (animator != null) animator.SetTrigger(idleTrigger);

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
