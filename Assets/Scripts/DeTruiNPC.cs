using UnityEngine;
using UnityEngine.InputSystem;

public class DeTruiNPC : MonoBehaviour, INPCMinigame
{
    [Header("Chat Bubble")]
    public ChatBubble chatBubble;
    public GameObject interactionPromptUI; 
    


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
            if (promptTextComp != null) 
            {
                originalPromptText = GetDisplayName(); 
                promptTextComp.text = originalPromptText;
            }
        }

        rb = GetComponent<Rigidbody>();

        // --- Minimap Marker ---
        MinimapMarker marker = gameObject.AddComponent<MinimapMarker>();
        marker.markerColor = Color.yellow; // Friendly NPC / Follower
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
                currentVelocity = Vector3.zero;
                isFollowing = false;
                isWanderingToTarget = false;
                if (animator != null && HasParameter(runBool)) animator.SetBool(runBool, false);
                if (rb != null && !rb.isKinematic)
                {
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                    rb.angularVelocity = Vector3.zero;
                }
            }
            return;
        }
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
                // Option 3: SOLO MINIGAME THEO ĐẶC ĐIỂM NPC
                else if (kb.digit3Key.wasPressedThisFrame)
                {
                    string nameLower = _npcName.ToLower();
                    if (enableRacing && (nameLower.Contains("dế trũi") || nameLower.Contains("detrui")))
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
                    else if (enableCaro)
                    {
                        isWaitingForChoice = false;
                        isMinigameActive = true;
                        if (chatBubble != null) chatBubble.Hide();
                        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
                        
                        CaroGameManager caro = FindFirstObjectByType<CaroGameManager>();
                        if (caro == null)
                        {
                            GameObject gmObj = new GameObject("CaroGameManager");
                            caro = gmObj.AddComponent<CaroGameManager>();
                        }
                        caro.StartGame(this);
                    }
                    else if (enableArmWrestling)
                    {
                        isWaitingForChoice = false;
                        isMinigameActive = true;
                        if (chatBubble != null) chatBubble.Hide();
                        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
                        
                        // Lấy hoặc tự tạo Manager lúc Runtime
                        ArmWrestlingManager armWrestle = FindFirstObjectByType<ArmWrestlingManager>();
                        if (armWrestle == null)
                        {
                            GameObject awObj = new GameObject("ArmWrestlingManager");
                            armWrestle = awObj.AddComponent<ArmWrestlingManager>();
                        }
                        armWrestle.StartGame(this);
                    }
                }
                // Option 4: Cưỡi Xén Tóc (Hiện luôn nhưng khóa nếu chưa thắng)
                else if (kb.digit4Key.wasPressedThisFrame)
                {
                    string nameLower = _npcName.ToLower();
                    string objNameLower = gameObject.name.ToLower();
                    bool isXenToc = nameLower.Contains("xén") || nameLower.Contains("xen") || objNameLower.Contains("xen");
                    
                    if (enableArmWrestling && isXenToc)
                    {
                        if (PlayerPrefs.GetInt("XenToc_PlayerWon", 0) == 1)
                        {
                            isWaitingForChoice = false;
                            EndInteraction();
                            MountXenTocController mounter = GetComponent<MountXenTocController>();
                            if (mounter == null) mounter = gameObject.AddComponent<MountXenTocController>();
                            if (mounter != null) mounter.Mount(player);
                        }
                        else
                        {
                            if (chatBubble != null) 
                            {
                                chatBubble.Setup("Ngươi chê sống lâu quá à? Thắng ta trước rồi tính tiếp!");
                                Invoke("HideBubble", 2.5f);
                            }
                        }
                    }
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
        bool currentlyNearby = distanceToPlayer <= interactionDistance && IsClosestNPC();

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
        bool isAnyMinigameActiveGlobally = isMinigameActive || 
            (CaroGameManager.Instance != null && CaroGameManager.Instance.IsGameActive) ||
            (ArmWrestlingManager.Instance != null && ArmWrestlingManager.Instance.IsGameActive);

        if (isAnyMinigameActiveGlobally) return;
        
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
            if (HasParameter(talkTrigger)) animator.SetTrigger(talkTrigger); // KÍCH HOẠT NÓI CHUYỆN
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
                
                if (enableRacing) 
                {
                    choices += "\n[3] Chạy đua";
                }
                else if (enableCaro) 
                {
                    choices += "\n[3] Giao lưu cờ caro";
                }
                else if (enableArmWrestling) 
                {
                    string objNameLower = gameObject.name.ToLower();
                    bool isXenToc = nameLower.Contains("xén") || nameLower.Contains("xen") || objNameLower.Contains("xen");
                    
                    if (isXenToc) 
                    {
                        choices += "\n[3] Tỷ thí Đọ Ngàm (Vật Tay)";
                        if (PlayerPrefs.GetInt("XenToc_PlayerWon", 0) == 1)
                        {
                            choices += "\n[4] Cưỡi Xén Tóc";
                        }
                        else
                        {
                            choices += "\n[4] <color=gray>Cưỡi Xén Tóc (Khóa - Hãy Đánh Bại Xén Tóc)</color>";
                        }
                    }
                    else choices += "\n[3] Vật Tay";
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
        if (EncyclopediaManager.Instance != null) EncyclopediaManager.Instance.UnlockInsect("DeTrui");
        
        if (chatBubble != null) chatBubble.Hide();

        if (QuestUIManager.Instance != null)
        {
            string nameLower = _npcName.ToLower();
            string talkQuest = "talk_detrui";
            if (nameLower.Contains("dechoat") || nameLower.Contains("dế choắt")) talkQuest = "talk_dechoat";
            else if (nameLower.Contains("xentoc") || nameLower.Contains("xén tóc")) talkQuest = "talk_xentoc";
            else if (nameLower.Contains("kien") || nameLower.Contains("kiến")) talkQuest = "talk_conkien";

            if (!QuestUIManager.Instance.IsQuestCompleted(talkQuest))
            {
                QuestUIManager.Instance.CompleteQuest(talkQuest);
            }
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

    public void EndMinigame(bool isWin, bool isDraw = false)
    {
        isMinigameActive = false;
        if (EncyclopediaManager.Instance != null) EncyclopediaManager.Instance.UnlockInsect("DeTrui");
        isTalking = true;
        isWaitingForChoice = false;
        
        string resultText = "";
        
        string nameLower = _npcName.ToLower();
        string objNameLower = gameObject.name.ToLower();
        
        bool isXenToc = nameLower.Contains("xén") || nameLower.Contains("xen") || objNameLower.Contains("xen");
        bool isDeChoat = nameLower.Contains("choắt") || nameLower.Contains("choat") || objNameLower.Contains("choat");
        bool isDeTrui = nameLower.Contains("trũi") || nameLower.Contains("trui") || objNameLower.Contains("trui");

        if (isXenToc)
        {
            if (isDraw) resultText = "Cứng đầu đấy! Hoà thì hoà, lần sau ta không nhường đâu!";
            else if (isWin) 
            {
                resultText = "Sức mạnh của ta... bị đánh bại sao?! Ngươi làm ta bất ngờ đấy.";
                PlayerPrefs.SetInt("XenToc_PlayerWon", 1);
                PlayerPrefs.Save();
            }
            else resultText = "Há há há! Dăm ba cái đồ tôm tép, ngoan ngoãn chắp tay gọi ta bằng ngài đi!";
            
            if (QuestUIManager.Instance != null && !QuestUIManager.Instance.IsQuestCompleted("minigame_xentoc"))
                QuestUIManager.Instance.CompleteQuest("minigame_xentoc");
        }
        else if (isDeChoat)
        {
            if (isDraw) resultText = "Hức... một ván hòa... coi như cậu nể mặt kẻ ốm yếu này...";
            else if (isWin) resultText = "Khụ khụ... tuổi trẻ tài cao... cậu thắng rồi...";
            else resultText = "Khà khà... Gừng càng già càng cay nhé chàng trai!";
            
            if (QuestUIManager.Instance != null && !QuestUIManager.Instance.IsQuestCompleted("minigame_dechoat"))
                QuestUIManager.Instance.CompleteQuest("minigame_dechoat");
        }
        else if (isDeTrui || true) // Mặc định là Dế Trũi nếu không nhận diện được
        {
            if (isDraw) resultText = "Chà, không ngờ cậu cầm hòa được tôi cơ đấy!";
            else if (isWin) 
            {
                // Câu thoại tự nhiên dành cho Dế Trũi khi thắng
                resultText = "Cậu rành môn này quá! Tôi thua tâm phục khẩu phục!";
            }
            else resultText = "Hahaha! Lần sau cố gắng hơn nhé, tôi thắng rồi!";
            
            if (QuestUIManager.Instance != null && !QuestUIManager.Instance.IsQuestCompleted("minigame_detrui"))
                QuestUIManager.Instance.CompleteQuest("minigame_detrui");
        }
        
        // Cập nhật lại khung chat
        if (chatBubble != null) 
        {
            chatBubble.Setup(resultText);
        }
        
        // Tắt sau 3 giây
        Invoke(nameof(EndInteraction), 3f);
    }

    private bool IsClosestNPC()
    {
        if (player == null) return false;
        float myDist = Vector3.Distance(transform.position, player.position);
        var allNPCs = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var npc in allNPCs)
        {
            if (npc != this && npc is INPCMinigame)
            {
                float otherDist = Vector3.Distance(npc.transform.position, player.position);
                if (otherDist < myDist) return false;
                if (Mathf.Abs(otherDist - myDist) < 0.01f && npc.gameObject.GetInstanceID() < gameObject.GetInstanceID()) return false;
            }
        }
        return true;
    }

    private void OnGUI()
    {
        // Kiểm tra xem có đang bị Minigame nào chiếm dụng toàn cục không
        bool isAnyMinigameActiveGlobally = isMinigameActive || 
            (CaroGameManager.Instance != null && CaroGameManager.Instance.IsGameActive) ||
            (ArmWrestlingManager.Instance != null && ArmWrestlingManager.Instance.IsGameActive);
        
        if (isAnyMinigameActiveGlobally) return;

        // Vẽ chữ báo hiệu "Bấm F..." ở cạnh dưới giữa màn hình
        if (isPlayerNearby && !isTalking && !isWaitingForChoice && !isFollowing)
        {
            DrawBottomPrompt("Ấn [F] để nói chuyện với " + GetDisplayName());
        }
        else if (isFollowing && player != null && Vector3.Distance(transform.position, player.position) <= interactionDistance)
        {
            DrawBottomPrompt("Ấn [F] để bảo " + GetDisplayName() + " đứng lại");
        }
    }

    public string GetDisplayName()
    {
        string lower = gameObject.name.ToLower();
        if (lower.Contains("xen") || lower.Contains("xén")) return "Xén Tóc";
        if (lower.Contains("choat") || lower.Contains("choắt")) return "Dế Choắt";
        if (lower.Contains("kien") || lower.Contains("kiến")) return "Côn Kiến";
        if (lower.Contains("trui") || lower.Contains("trũi")) return "Dế Trũi";
        
        return !string.IsNullOrEmpty(_npcName) ? _npcName : gameObject.name;
    }

    private void DrawBottomPrompt(string msg)
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 35; // Cỡ chữ bự để dễ đọc
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;

        // Đổ bóng (Viền viền đèn)
        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;

        float w = 600f;
        float h = 60f;
        float x = (Screen.width - w) / 2f; // Căn giữa màn hình ngang
        float y = Screen.height - 150f;    // Nằm ở phần dưới màn hình dọc

        // Vẽ Bóng đen xê dịch đi 2 pixel
        GUI.Label(new Rect(x + 2, y + 2, w, h), msg, shadowStyle);
        // Vẽ Chữ trắng đè lên
        GUI.Label(new Rect(x, y, w, h), msg, style);
    }
}
