using UnityEngine;
using UnityEngine.InputSystem;

public class DeTruiNPC : MonoBehaviour, INPCMinigame
{
    [Header("Chat Bubble")]
    public ChatBubble chatBubble;
    public GameObject interactionPromptUI; 
    
    // Tốc độ bình thường mỗi câu chữ
    public float timePerSentence = 2.5f;

    [Header("Identidade")]
    [SerializeField] private string _npcName = "Dế Trũi";
    public string npcName { get => _npcName; set => _npcName = value; }
    [TextArea(3, 10)]
    public string[] dialogue = new string[] {
        "Chào người anh em! Tôi là Dế Trũi đây.",
        "Cuộc đời là những chuyến đi dài, phải không nào?",
        "Nếu cậu cần người đồng hành, tôi luôn sẵn lòng!" 
    };

    [Header("Minigames Options")]
    public bool enableCaro = true;
    public bool enableArmWrestling = true;
    public bool enableRacing = true;

    [Header("Wandering Settings")]
    public bool enableWandering = true;
    public float wanderRadius = 10f;
    public float wanderWaitTime = 3f;
    public float wanderSpeed = 2f;
    private Vector3 homePosition;
    private Vector3 wanderTarget;
    private float wanderTimer = 0f;
    private bool isWanderingToTarget = false;

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

    [Header("Name Tag")]
    public Transform nameTagTransform;

    private Transform player;
    private PlayerController playerController;
    private Camera mainCamera;
    
    private bool isPlayerNearby = false;
    private bool isTalking = false;
    private bool isWaitingForChoice = false;
    private bool isFollowing = false;
    public bool isMinigameActive { get; set; }
    
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
        homePosition = transform.position;
        wanderTimer = Random.Range(0f, wanderWaitTime);

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
        if (player == null || isMinigameActive) return;
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        if (nameTagTransform != null && mainCamera != null)
        {
            nameTagTransform.rotation = mainCamera.transform.rotation;
        }

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
                // Option 3: SOLO CARO
                else if (enableCaro && kb.digit3Key.wasPressedThisFrame)
                {
                    isWaitingForChoice = false;
                    isMinigameActive = true;
                    if (chatBubble != null) chatBubble.Hide();
                    if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
                    
                    // Attach Manager if Missing (Lazy load)
                    CaroGameManager caro = FindFirstObjectByType<CaroGameManager>();
                    if (caro == null)
                    {
                        GameObject gmObj = new GameObject("CaroGameManager");
                        caro = gmObj.AddComponent<CaroGameManager>();
                    }
                    caro.StartGame(this);
                }
                // Option 4: VẬT TAY (AUDITION STYLE)
                else if (enableArmWrestling && kb.digit4Key.wasPressedThisFrame)
                {
                    isWaitingForChoice = false;
                    isMinigameActive = true;
                    if (chatBubble != null) chatBubble.Hide();
                    if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
                    
                    // Lấy hoặc tự tạo Manager lúc Runtime (tránh việc báo lỗi vì user quên set vào scene)
                    ArmWrestlingManager armWrestle = FindFirstObjectByType<ArmWrestlingManager>();
                    if (armWrestle == null)
                    {
                        GameObject awObj = new GameObject("ArmWrestlingManager");
                        armWrestle = awObj.AddComponent<ArmWrestlingManager>();
                    }
                    armWrestle.StartGame(this);
                }
                // Option 5: CHẠY ĐUA
                else if (enableRacing && kb.digit5Key.wasPressedThisFrame)
                {
                    isWaitingForChoice = false;
                    EndInteraction();
                    
                    // Lưu lại vị trí để khi kết thúc Race quay lại đúng chỗ này
                    PlayerPrefs.SetString("PreviousScene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                    PlayerPrefs.SetFloat("PlayerRawPosX", player.position.x);
                    PlayerPrefs.SetFloat("PlayerRawPosY", player.position.y);
                    PlayerPrefs.SetFloat("PlayerRawPosZ", player.position.z);
                    PlayerPrefs.SetInt("HasSavedPostRacePosition", 1);
                    
                    // Load Scene RacingMinigame (người dùng phải add vào Build Settings)
                    UnityEngine.SceneManagement.SceneManager.LoadScene("RacingMinigame");
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
            HandleCameraFocusSkyrim(); // Hàm này bản chất đã liên tục gọi FacePlayerTarget() cho NPC nhìn player
            
            // Ép Player cũng phải quay mặt chăm chú nhìn lại NPC
            if (player != null)
            {
                Vector3 playerToNpc = (transform.position - player.position).normalized;
                playerToNpc.y = 0;
                if (playerToNpc != Vector3.zero)
                {
                    player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(playerToNpc), 10f * Time.deltaTime);
                }
            }

            // Chắc chắn NPC đang tắt animation đi dạo (T-Pose) và rơi vào trạng thái Idle
            if (animator != null) animator.SetBool(runBool, false);

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

        // Logic khi ĐỨNG YÊN HOẶC ĐI DẠO (Không follow)
        // Vẫn phải check chạm đất nếu bị rớt
        bool groundCheckIdle = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.5f, groundLayer);
        if (groundCheckIdle && verticalVelocity <= 0) {
            verticalVelocity = -1f;
            isJumping = false;
        } else {
            verticalVelocity += gravity * Time.deltaTime;
        }
        
        bool isWanderMoving = false;
        currentVelocity = Vector3.zero;

        if (enableWandering)
        {
            if (isWanderingToTarget)
            {
                Vector3 targetPos = wanderTarget;
                targetPos.y = transform.position.y;
                float distToTarget = Vector3.Distance(transform.position, targetPos);
                
                if (distToTarget > 0.5f)
                {
                    Vector3 dir = (targetPos - transform.position).normalized;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
                    currentVelocity = dir * wanderSpeed;
                    isWanderMoving = true;

                    // Obstacle jump
                    if (groundCheckIdle && !isJumping)
                    {
                        Vector3 rayStart = transform.position + Vector3.up * 0.3f;
                        if (Physics.Raycast(rayStart, dir, jumpObstacleCheckDist, obstacleLayer))
                        {
                            verticalVelocity = jumpForce;
                            isJumping = true;
                        }
                    }
                }
                else
                {
                    isWanderingToTarget = false;
                    wanderTimer = wanderWaitTime;
                }
            }
            else
            {
                wanderTimer -= Time.deltaTime;
                if (wanderTimer <= 0f)
                {
                    // Chọn một điểm ngẫu nhiên xung quanh khu vực nhà (home)
                    Vector2 randCircle = Random.insideUnitCircle * wanderRadius;
                    wanderTarget = homePosition + new Vector3(randCircle.x, 0, randCircle.y);
                    isWanderingToTarget = true;
                }
            }
        }
        
        if (animator != null) animator.SetBool(runBool, isWanderMoving);

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
        if (isMinigameActive) return;
        
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
        currentVelocity = Vector3.zero; // TRIỆT TIÊU LỰC CHẠY
        isWanderingToTarget = false; // HỦY BỎ ĐIỂM ĐẾN PHÍA TRƯỚC
        
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);

        // SNAP Xoay mặt thẳng vào mặt sếp ngay lập tức không chần chừ (Bỏ slerp lúc FaceTarget mồi)
        if (player != null) {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);
        }

        if (animator != null) 
        {
            animator.SetBool(runBool, false); // TẮT T-POSE / RUN ANIM
            animator.SetTrigger(talkTrigger); // KÍCH HOẠT NÓI CHUYỆN
        }

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
                string choices = "[1] Rủ đi cùng\n[2] Bỏ qua";
                string nameLower = _npcName.ToLower();
                
                if (enableCaro) 
                {
                    if (nameLower.Contains("dechoat")) choices += "\n[3] Giao lưu Cờ Caro (Chữa Bệnh)";
                    else choices += "\n[3] Giao lưu Cờ Caro (3x3)";
                }
                
                if (enableArmWrestling) 
                {
                    if (nameLower.Contains("xentoc")) choices += "\n[4] Tỷ thí Đọ Ngàm (Vật Tay)";
                    else choices += "\n[4] Vật Tay Sinh Tử";
                }
                
                if (enableRacing && nameLower.Contains("detrui"))
                {
                    choices += "\n[5] Đua Xe Bọ (Chạy đua)";
                }
                
                promptTextComp.text = choices;
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

    public void EndMinigame(bool isWin, bool isDraw = false)
    {
        isMinigameActive = false;
        isTalking = true;
        isWaitingForChoice = false;
        
        string resultText = "";
        
        string nameLower = _npcName.ToLower();
        if (nameLower.Contains("detrui"))
        {
            if (isDraw) resultText = "Chà, không ngờ cậu cầm hòa được tôi cơ đấy!";
            else if (isWin) resultText = "Quá xuất sắc! Cậu lại thắng tôi rồi, bái phục bái phục!";
            else resultText = "Hahaha! Lần sau cố gắng hơn nhé, tôi thắng rồi!";
        }
        else if (nameLower.Contains("dechoat"))
        {
            if (isDraw) resultText = "Hức... một ván hòa... coi như cậu nể mặt kẻ ốm yếu này...";
            else if (isWin) resultText = "Khụ khụ... tuổi trẻ tài cao... cậu thắng rồi...";
            else resultText = "Khà khà... Gừng càng già càng cay nhé chàng trai!";
        }
        else if (nameLower.Contains("xentoc"))
        {
            if (isDraw) resultText = "Cứng đầu đấy! Hoà thì hoà, lần sau ta không nhường đâu!";
            else if (isWin) resultText = "KHÔNG THỂ NÀO! Sức mạnh của ta bị đánh bại sao?!";
            else resultText = "Há há há! Dăm ba cái đồ tôm tép, ngoan ngoãn chắp tay gọi ta bằng ngài đi!";
        }
        else 
        {
            if (isDraw) resultText = "Một kết quả Hòa đầy kịch tính!";
            else if (isWin) resultText = "Xin chúc mừng vị anh hùng chiến thắng!";
            else resultText = "Rất tiếc, may mắn chưa mỉm cười với bạn.";
        }
        
        // Cập nhật lại khung chat
        if (chatBubble != null) 
        {
            chatBubble.Setup(resultText);
        }
        
        // Tắt sau 3 giây
        Invoke(nameof(EndInteraction), 3f);
    }
}
