using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Script riêng cho NPC Dế Trũi.
/// Xử lý: hội thoại, racing minigame, AI đi lang thang.
/// </summary>
public class DeTruiNPC : MonoBehaviour, INPCMinigame
{
    [Header("Chat Bubble")]
    public ChatBubble chatBubble;
    public GameObject interactionPromptUI;

    [Header("Identidade")]
    [SerializeField] private string _npcName = "Dế Trũi";
    public string npcName { get => _npcName; set => _npcName = value; }
    public AudioClip typewriterBeep;

    [Header("Dialogue: Giới thiệu - Mời chạy đua (Phase MEET_CONKIEN)")]
    [TextArea(3, 10)]
    public string[] deTruiIntro = new string[]
    {
        "(Dế Trũi): Chào Mèn! Nghe nói mày đang cần Mật Ong à?",
        "(Dế Mèn): Đúng vậy, lão Kiến bắt tao phải có Mật Ong mới cho qua.",
        "(Dế Trũi): Tao có đây, nhưng dạo này tao cuồng tốc độ lắm.",
        "(Dế Trũi): Chạy đua vòng quanh sân với tao đi! Thắng thì tao cho!"
    };

    [Header("Dialogue: Sau khi thắng cuộc đua")]
    [TextArea(3, 10)]
    public string[] deTruiWon = new string[]
    {
        "(Dế Trũi): Haha! Mày ch\u1ea1y c\u1eebu l\u1eafm Mèn ạ!",
        "(Dế Mèn): Cảm ơn mày, giờ đưa Mật Ong cho tao được chưa?",
        "(Dế Trũi): Đúng là anh em ruột của tao. Nhận lấy hũ Mật Ong này đi!"
    };

    [Header("Dialogue: Sau khi thua cuộc đua")]
    [TextArea(3, 10)]
    public string[] deTruiLost = new string[]
    {
        "(Dế Trũi): Tiếc quá Mèn ơi, hôm nay mày chậm thế?",
        "(Dế Mèn): Tao sơ suất tí thôi, chạy đua lại không?",
        "(Dế Trũi): Chắc chắn rồi, muốn lấy Mật Ong thì phải thắng tao!"
    };

    [Header("Dialogue: Sau khi đã nhận Mật Ong (Phase >= BEAT_DETRUI)")]
    [TextArea(3, 10)]
    public string[] deTruiDone = new string[]
    {
        "(Dế Trũi): Chà, mày đưa Mật Ong cho lão Kiến rồi chứ?",
        "(Dế Mèn): Đưa rồi, gã tham lam lắm.",
        "(Dế Trũi): Tốt lắm, mong là lão không quấy rầy mày nữa!"
    };

    [Header("Dialogue: Skyrim Side Answers")]
    [TextArea(3, 10)] public string[] answer1 = new string[] { "Mật ong này trân quý lắm, ngọt lịm!", "Côn Kiến chết mê chết mệt món này đấy." };

    [Header("Minigames Options")]
    public bool enableCaro = true;
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
    public float interactionDistance = 5.5f;
    [Tooltip("Điều chỉnh vị trí camera khi Focus nói chuyện (So với mặt NPC)")]
    public Vector3 cameraFocusOffset = new Vector3(0, 0.4f, 1.2f);
    public float cameraTransitionSpeed = 5f;

    [Header("Animation")]
    public Animator animator;
    public string talkTrigger = "Talk";
    public string idleTrigger = "Idle";
    public string runBool     = "IsRunning";

    [Header("Follow Settings")]
    public float followSpeed = 4f;
    public float stopDistance = 2.5f;

    [Header("Jump / Physics Settings")]
    public float jumpForce = 8f;
    public float gravity   = -20f;
    public float jumpObstacleCheckDist = 0.8f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    [Header("Name Tag")]
    public Transform nameTagTransform;

    // Private state
    private Transform player;
    private PlayerController playerController;
    private Camera mainCamera;

    private bool isPlayerNearby = false;
    private bool isTalking       = false;
    private bool isWaitingForChoice = false;
    private bool isFollowing     = false;
    public  bool isMinigameActive { get; set; }

    private bool isSideTalking = false;

    private int      currentDialogueIndex = 0;
    private string[] dialogue;

    private Vector3    originalCameraPos;
    private Quaternion originalCameraRot;
    private Transform  originalCameraParent;

    private TMPro.TextMeshProUGUI promptTextComp;
    private string originalPromptText;

    private float verticalVelocity = 0f;
    private bool  isJumping = false;

    private Rigidbody rb;
    private Vector3 currentVelocity;

    // ─────────────────────────────────────────────────────────────
    void Start()
    {
        if (chatBubble == null) chatBubble = GetComponentInChildren<ChatBubble>(true);
        // Bypass Inspector Serialization Cache cho hội thoại
        if (deTruiIntro == null || deTruiIntro.Length <= 1)
        {
            deTruiIntro = new string[] {
                "(Dế Trũi): Chào Mèn! Nghe nói mày đang cần Mật Ong à?",
                "(Dế Mèn): Đúng vậy, lão Kiến bắt tao phải có Mật Ong mới cho qua.",
                "(Dế Trũi): Tao có đây, nhưng dạo này tao cuồng tốc độ lắm.",
                "(Dế Trũi): Chạy đua vòng quanh sân với tao đi! Thắng thì tao cho!"
            };
        }
        if (deTruiWon == null || deTruiWon.Length <= 1)
        {
            deTruiWon = new string[] {
                "(Dế Trũi): Haha! Mày ch\u1ea1y c\u1eebu l\u1eafm Mèn ạ!",
                "(Dế Mèn): Cảm ơn mày, giờ đưa Mật Ong cho tao được chưa?",
                "(Dế Trũi): Đúng là anh em ruột của tao. Nhận lấy hũ Mật Ong này đi!"
            };
        }
        if (deTruiLost == null || deTruiLost.Length <= 1)
        {
            deTruiLost = new string[] {
                "(Dế Trũi): Tiếc quá Mèn ơi, hôm nay mày chậm thế?",
                "(Dế Mèn): Tao sơ suất tí thôi, chạy đua lại không?",
                "(Dế Trũi): Chắc chắn rồi, muốn lấy Mật Ong thì phải thắng tao!"
            };
        }

        homePosition = transform.position;
        wanderTimer  = Random.Range(0f, wanderWaitTime);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<PlayerController>();
        }

        mainCamera = Camera.main;
        if (animator == null) animator = GetComponent<Animator>();

        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.useGravity = false; // Ngăn Rigidbody tự kéo xuống nếu mất Collider

        if (interactionPromptUI != null)
        {
            promptTextComp = interactionPromptUI.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (promptTextComp != null)
            {
                originalPromptText = _npcName;
                promptTextComp.text = originalPromptText;
            }
        }

        // Minimap marker - gán qua Inspector hoặc NPCSetupTool nếu cần
    }

    // ─────────────────────────────────────────────────────────────
    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) {
                 player = p.transform;
                 playerController = p.GetComponent<PlayerController>();
            }
            if (player == null) return;
        }

        bool isAnyMinigameActive = isMinigameActive
            || (CaroGameManager.Instance   != null && CaroGameManager.Instance.IsGameActive)
            || (ArmWrestlingManager.Instance != null && ArmWrestlingManager.Instance.IsGameActive);

        if (player == null || isAnyMinigameActive)
        {
            if (isAnyMinigameActive)
            {
                currentVelocity      = Vector3.zero;
                isFollowing          = false;
                isWanderingToTarget  = false;
                if (animator != null && HasParameter(runBool)) animator.SetBool(runBool, false);
                if (rb != null)
                {
                    rb.isKinematic      = false;  // Tạm bật để có thể set velocity
                    rb.linearVelocity   = Vector3.zero;
                    rb.angularVelocity  = Vector3.zero;
                    rb.isKinematic      = true;   // Khoá lại
                }
            }
            return;
        }

        if (rb != null) rb.isKinematic = false;

        var kb    = Keyboard.current;
        var mouse = Mouse.current;

        // Tag name bảng hiệu
        if (nameTagTransform != null && mainCamera != null)
            nameTagTransform.rotation = mainCamera.transform.rotation;

        // === Đang chờ lựa chọn ===
        if (isWaitingForChoice)
        {
            HandleCameraFocusSkyrim();
            if (player != null)
            {
                Vector3 dir = (transform.position - player.position).normalized;
                dir.y = 0;
                if (dir != Vector3.zero)
                    player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
            }
            if (animator != null) animator.SetBool(runBool, false);

            if (kb == null) return;

            int phase = StoryQuestManager.Instance != null ? StoryQuestManager.Instance.currentPhase : 0;

            if (kb.digit1Key.wasPressedThisFrame)
            {
                // [1] Chấp nhận chạy đua
                if (phase == StoryQuestManager.PHASE_MEET_CONKIEN && PlayerPrefs.GetInt("ReturnedFromRace", 0) == 0)
                {
                    isWaitingForChoice = false;
                    EndInteraction();
                    PlayerPrefs.SetString("PreviousScene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                    PlayerPrefs.SetFloat("PlayerRawPosX", player.position.x);
                    PlayerPrefs.SetFloat("PlayerRawPosY", player.position.y);
                    PlayerPrefs.SetFloat("PlayerRawPosZ", player.position.z);
                    PlayerPrefs.SetInt("HasSavedPostRacePosition", 1);
                    UnityEngine.SceneManagement.SceneManager.LoadScene("RacingMinigame");
                }
                else
                {
                    isFollowing = true;
                    isWaitingForChoice = false;
                    EndInteraction();
                    if (chatBubble != null) chatBubble.Setup("Được thôi, tôi đi theo cậu!");
                    Invoke(nameof(HideBubble), 2f);
                }
            }
            else if (kb.digit2Key.wasPressedThisFrame)
            {
                if (phase == StoryQuestManager.PHASE_MEET_CONKIEN && PlayerPrefs.GetInt("ReturnedFromRace", 0) == 0)
                {
                    // Hỏi về Mật Ong (side dialogue)
                    TriggerSideDialogue(answer1);
                }
                else
                {
                    isFollowing = false;
                    isWaitingForChoice = false;
                    EndInteraction();
                    if (chatBubble != null) chatBubble.Setup("Chào người anh em, đi thong thả nhé!");
                    Invoke(nameof(HideBubble), 2f);
                }
            }
            else if (kb.digit4Key.wasPressedThisFrame && enableCaro)
            {
                isWaitingForChoice = false;
                isMinigameActive   = true;
                if (chatBubble != null) chatBubble.Hide();
                if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
                CaroGameManager caro = FindFirstObjectByType<CaroGameManager>();
                if (caro == null) caro = new GameObject("CaroGameManager").AddComponent<CaroGameManager>();
                caro.StartGame(this);
            }
            else if (kb.tabKey.wasPressedThisFrame)
            {
                isFollowing = false;
                isWaitingForChoice = false;
                EndInteraction();
            }
            return;
        }

        // === Trong hội thoại ===
        if (isTalking)
        {
            HandleCameraFocusSkyrim();

            // NPC quay nhìn về phía Player
            if (player != null)
            {
                Vector3 npcDir = (player.position - transform.position).normalized;
                npcDir.y = 0;
                if (npcDir != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(npcDir), 8f * Time.deltaTime);
            }

            // Player quay nhìn về phía NPC
            if (player != null)
            {
                Vector3 dir = (transform.position - player.position).normalized;
                dir.y = 0;
                if (dir != Vector3.zero)
                    player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
            }
            if (animator != null) animator.SetBool(runBool, false);

            if (kb != null && kb.tabKey.wasPressedThisFrame) { EndInteraction(); return; }

            bool next = (mouse != null && mouse.leftButton.wasPressedThisFrame)
                     || (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame));

            if (next)
            {
                if (chatBubble != null && chatBubble.isTyping) chatBubble.FastForward();
                else TriggerNextSentence();
            }
            return;
        }

        // === Follow ===
        if (isFollowing)
        {
            if (interactionPromptUI != null) interactionPromptUI.SetActive(false);

            float dist = Vector3.Distance(transform.position, player.position);
            bool moving = false;

            bool grounded = (groundLayer.value == 0) ? true : Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.5f, groundLayer);
            if (grounded && verticalVelocity <= 0) { verticalVelocity = -1f; isJumping = false; }
            else if (!grounded) verticalVelocity += gravity * Time.deltaTime;

            Vector3 moveDir = Vector3.zero;

            if (dist > stopDistance)
            {
                Vector3 target = new Vector3(player.position.x, transform.position.y, player.position.z);
                Vector3 dir    = (target - transform.position).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

                float spd = dist > stopDistance + 2f ? followSpeed * 1.8f : followSpeed;
                moveDir = dir * spd;
                moving  = true;

                bool wallHit = Physics.Raycast(transform.position + Vector3.up * 0.3f, dir, jumpObstacleCheckDist, obstacleLayer);
                if (grounded && !isJumping && wallHit) { verticalVelocity = jumpForce; isJumping = true; }
            }
            else
            {
                FacePlayerTarget();
            }

            currentVelocity = moveDir;
            if (animator != null) animator.SetBool(runBool, moving);

            if (dist <= interactionDistance && kb != null && kb.fKey.wasPressedThisFrame)
            {
                isFollowing = false;
                if (animator != null) animator.SetBool(runBool, false);
                if (chatBubble != null) chatBubble.Setup("Tôi sẽ đứng chờ ở đây!");
                Invoke(nameof(HideBubble), 2f);
            }
            return;
        }

        // === Idle / Wander ===
        bool groundIdle = (groundLayer.value == 0) ? true : Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.5f, groundLayer);
        if (groundIdle && verticalVelocity <= 0) { verticalVelocity = -1f; isJumping = false; }
        else if (!groundIdle) verticalVelocity += gravity * Time.deltaTime;

        bool wanderMoving = false;
        currentVelocity = Vector3.zero;

        if (enableWandering)
        {
            if (isWanderingToTarget)
            {
                Vector3 target = new Vector3(wanderTarget.x, transform.position.y, wanderTarget.z);
                float   d      = Vector3.Distance(transform.position, target);
                if (d > 0.5f)
                {
                    Vector3 dir = (target - transform.position).normalized;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
                    currentVelocity    = dir * wanderSpeed;
                    wanderMoving       = true;

                    bool wallHit = Physics.Raycast(transform.position + Vector3.up * 0.3f, dir, jumpObstacleCheckDist, obstacleLayer);
                    if (groundIdle && !isJumping && wallHit) { verticalVelocity = jumpForce; isJumping = true; }
                }
                else { isWanderingToTarget = false; wanderTimer = wanderWaitTime; }
            }
            else
            {
                wanderTimer -= Time.deltaTime;
                if (wanderTimer <= 0f)
                {
                    Vector2 r  = Random.insideUnitCircle * wanderRadius;
                    wanderTarget       = homePosition + new Vector3(r.x, 0, r.y);
                    isWanderingToTarget = true;
                }
            }
        }
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetBool(runBool, wanderMoving);

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        bool nowNearby    = distToPlayer <= interactionDistance;
        isPlayerNearby = nowNearby;

        // KIỂM TRA ĐỘ ƯU TIÊN: Chỉ hiện nút F nếu là NPC được Dế Mèn nhìn vào rõ nhất
        bool isBestCandidate = playerController != null && playerController.GetClosestNPC() == gameObject;
        bool shouldShowPrompt = isPlayerNearby && isBestCandidate && !isTalking && !isWaitingForChoice;

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
        if (isTalking || isWaitingForChoice)
        {
            FacePlayerTarget();
        }
    }

    // ─────────────────────────────────────────────────────────────
    void FixedUpdate()
    {
        bool isAnyMinigame = isMinigameActive
            || (CaroGameManager.Instance   != null && CaroGameManager.Instance.IsGameActive)
            || (ArmWrestlingManager.Instance != null && ArmWrestlingManager.Instance.IsGameActive);
        if (isAnyMinigame || rb == null) return;

        Vector3 move = currentVelocity * Time.fixedDeltaTime;
        move.y = verticalVelocity * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }

    // ─────────────────────────────────────────────────────────────
    void StartInteraction()
    {
        isTalking            = true;
        currentVelocity      = Vector3.zero;
        isWanderingToTarget  = false;

        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);

        // Snap nhìn thẳng vào player
        if (player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        }

        if (animator != null)
        {
            animator.SetBool(runBool, false);
            if (HasParameter(talkTrigger)) animator.SetTrigger(talkTrigger);
        }

        if (playerController != null) playerController.isDialoguing = true;

        if (mainCamera != null)
        {
            originalCameraParent = mainCamera.transform.parent;
            originalCameraPos    = mainCamera.transform.localPosition;
            originalCameraRot    = mainCamera.transform.localRotation;
        }

        currentDialogueIndex = 0;

        int phase = StoryQuestManager.Instance != null ? StoryQuestManager.Instance.currentPhase : 0;

        // Xử lý kết quả đua xe sau khi từ RacingMinigame về
        if (PlayerPrefs.GetInt("ReturnedFromRace", 0) == 1 && phase == StoryQuestManager.PHASE_MEET_CONKIEN)
        {
            bool wonRace = PlayerPrefs.GetInt("WonRace", 0) == 1;
            dialogue = wonRace ? deTruiWon : deTruiLost;

            if (wonRace)
            {
                // Trao Mật Ong ngay lập tức
                var story = StoryQuestManager.Instance;
                if (story != null) story.AdvanceTo(StoryQuestManager.PHASE_BEAT_DETRUI);

                var matOng = ScriptableObject.CreateInstance<ItemData>();
                matOng.itemName  = "Mật Ong";
                matOng.itemType  = ItemType.QuestItem_MatOng;
                matOng.description = "Bình mật ong thơm lừng lấy từ Dế Trũi.";
                
                // Tải icon từ Resources vừa được AI tạo
                Sprite loadedIcon = Resources.Load<Sprite>("Items/HoneyIcon");
                if (loadedIcon != null)
                {
                    matOng.itemIcon = loadedIcon;
                }
                
                InventoryManager.Instance?.AddItem(matOng, 1);
            }

            PlayerPrefs.SetInt("ReturnedFromRace", 0);
            PlayerPrefs.Save();
        }
        else if (phase >= StoryQuestManager.PHASE_BEAT_DETRUI)
        {
            dialogue = deTruiDone;
        }
        else
        {
            dialogue = deTruiIntro;
        }

        DisplayCurrentSentence();
    }

    void TriggerNextSentence()
    {
        currentDialogueIndex++;
        if (currentDialogueIndex < dialogue.Length)
        {
            DisplayCurrentSentence();
            return;
        }

        // Hết dialogue
        int phase = StoryQuestManager.Instance != null ? StoryQuestManager.Instance.currentPhase : 0;

        if (isSideTalking)
        {
            isSideTalking = false;
            dialogue = deTruiIntro;
            ShowChoice();
            return;
        }

        if (phase == StoryQuestManager.PHASE_MEET_CONKIEN && PlayerPrefs.GetInt("ReturnedFromRace", 0) == 0)
            ShowChoice();
        else
        {
            isTalking = false;
            EndInteraction();
        }
    }

    void DisplayCurrentSentence()
    {
        if (chatBubble != null && dialogue != null && currentDialogueIndex < dialogue.Length)
            chatBubble.Setup(dialogue[currentDialogueIndex]);
    }

    void ShowChoice()
    {
        isTalking          = false;
        isWaitingForChoice = true;
        if (chatBubble != null) chatBubble.Hide();

        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(true);
            if (promptTextComp != null)
            {
                int phase = StoryQuestManager.Instance != null ? StoryQuestManager.Instance.currentPhase : 0;

                if (phase == StoryQuestManager.PHASE_MEET_CONKIEN && PlayerPrefs.GetInt("ReturnedFromRace", 0) == 0)
                    promptTextComp.text = "[1] \"Chấp nhận chạy đua!\"\n[2] \"Kể về Mật Ong đi.\"\n[TAB] Rời đi";
                else
                    promptTextComp.text = "[1] \"Trũi, đi cùng tôi không?\"\n[2] \"Thôi, hẹn lần khác.\"\n[4] \"Làm ván Cờ Caro nào!\"";

                promptTextComp.fontSize = 25;
            }
        }
    }

    void TriggerSideDialogue(string[] sideDialogue)
    {
        isWaitingForChoice = false;
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        isTalking      = true;
        isSideTalking  = true;
        currentDialogueIndex = 0;
        dialogue       = sideDialogue;
        DisplayCurrentSentence();
    }

    public void EndInteraction()
    {
        isTalking          = false;
        isWaitingForChoice = false;

        if (chatBubble != null) chatBubble.Hide();

        if (promptTextComp != null && !string.IsNullOrEmpty(originalPromptText))
        {
            promptTextComp.text     = originalPromptText;
            promptTextComp.fontSize = 50;
        }

        if (interactionPromptUI != null)
            interactionPromptUI.SetActive(isPlayerNearby && !isFollowing);

        if (animator != null && HasParameter(idleTrigger)) animator.SetTrigger(idleTrigger);

        if (playerController != null) playerController.isDialoguing = false;

        if (mainCamera != null)
        {
            mainCamera.transform.localPosition = originalCameraPos;
            mainCamera.transform.localRotation = originalCameraRot;
        }

        if (EncyclopediaManager.Instance != null) EncyclopediaManager.Instance.UnlockInsect("DeTrui");
        if (QuestUIManager.Instance != null && !QuestUIManager.Instance.IsQuestCompleted("talk_detrui"))
            QuestUIManager.Instance.CompleteQuest("talk_detrui");
    }

    public void EndMinigame(bool isWin, bool isDraw = false)
    {
        isMinigameActive   = false;
        isTalking          = true;
        isWaitingForChoice = false;

        string txt;
        if (isDraw)    txt = "Chà, không ngờ cậu cầm hòa được tôi cơ đấy!";
        else if (isWin) txt = "Cậu rành môn này quá! Tôi thua tâm phục khẩu phục!";
        else           txt = "Hahaha! Lần sau cố gắng hơn nhé, tôi thắng rồi!";

        if (chatBubble != null) chatBubble.Setup(txt);
        if (EncyclopediaManager.Instance != null) EncyclopediaManager.Instance.UnlockInsect("DeTrui");
        if (QuestUIManager.Instance != null && !QuestUIManager.Instance.IsQuestCompleted("minigame_detrui"))
            QuestUIManager.Instance.CompleteQuest("minigame_detrui");

        Invoke(nameof(EndInteraction), 3f);
    }

    // ─────────────────────────────────────────────────────────────
    private void HandleCameraFocusSkyrim()
    {
        if (mainCamera == null) return;
        FacePlayerTarget();

        Vector3    pos = transform.position + transform.rotation * cameraFocusOffset;
        Quaternion rot = Quaternion.LookRotation((transform.position + Vector3.up * 0.5f) - pos);

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, pos, Time.deltaTime * cameraTransitionSpeed);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, rot, Time.deltaTime * cameraTransitionSpeed);
    }

    void FacePlayerTarget()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    void HideBubble() { if (chatBubble != null) chatBubble.Hide(); }

    private bool HasParameter(string name)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(name)) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == name) return true;
        return false;
    }

    // Đã gỡ bỏ IsClosestNPC

    public string GetDisplayName() => _npcName;

    void OnGUI()
    {
        bool anyMinigame = isMinigameActive
            || (CaroGameManager.Instance != null && CaroGameManager.Instance.IsGameActive)
            || (ArmWrestlingManager.Instance != null && ArmWrestlingManager.Instance.IsGameActive);
            
        if (anyMinigame) return;

        if (isPlayerNearby && !isTalking && !isWaitingForChoice && !isFollowing)
        {
            DrawCenterPrompt("[F] Nói chuyện");
        }
        else if (isFollowing && player != null && Vector3.Distance(transform.position, player.position) <= interactionDistance)
        {
            DrawCenterPrompt("[F] Ra lệnh đứng lại");
        }
    }

    private void DrawCenterPrompt(string text)
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

        GUI.Label(new Rect(x + 2, y + 2, w, h), text, shadowStyle);
        GUI.Label(new Rect(x, y, w, h), text, style);
    }
}
