using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Script riêng cho NPC Côn Kiến.
/// Quản lý hội thoại và logic quest theo Phase câu chuyện.
/// </summary>
public class ConKienNPC : MonoBehaviour
{
    [Header("Chat Bubble")]
    public ChatBubble chatBubble;
    public GameObject interactionPromptUI;

    [Header("Identidade")]
    public string npcName = "Côn Kiến";
    public AudioClip typewriterBeep;

    [Header("Dialogue: Phase 0 - Chưa gặp Xén Tóc")]
    [TextArea(3, 10)]
    public string[] dialogueDefault = new string[]
    {
        "(Côn Kiến): Hỡi nhà phiêu lưu! Cậu tìm đến đây có việc gì?",
        "(Dế Mèn): Tôi đang đi tìm tung tích của người anh em...",
        "(Côn Kiến): Dạo này lũ Xén Tóc lộng hành lắm, cẩn thận đấy nhé."
    };

    [Header("Dialogue: Phase 1 - Chặn đường đòi Mật Ong")]
    [TextArea(3, 10)]
    public string[] dialogueBlockPath = new string[]
    {
        "(Côn Kiến): Dừng lại! Khu vực này do ta quản. Chú em lên đây làm gì?",
        "(Dế Mèn): Tôi đang tìm tung tích Dế Choắt, nghe nói anh biết?",
        "(Côn Kiến): Ta biết nó ở đâu. Nhưng muốn ta chỉ đường...",
        "(Côn Kiến): ...thì hãy xuống sân tìm Dế Trũi và đem về cho ta một hũ Mật Ong!"
    };

    [Header("Dialogue: Phase 2 - Đang chờ Mật Ong")]
    [TextArea(3, 10)]
    public string[] dialogueWaiting = new string[]
    {
        "(Côn Kiến): Vẫn còn đây hả? Mau xuống tìm kiếm Dế Trũi đi!",
        "(Côn Kiến): Hũ Mật Ong đó ta cần gấp lắm!"
    };

    [Header("Dialogue: Phase 3 - Nhận Mật Ong từ Dế Trũi")]
    [TextArea(3, 10)]
    public string[] dialogueReceiveHoney = new string[]
    {
        "(Côn Kiến): Ngươi mang Mật Ong về rồi chứ? Chà chà...",
        "(Dế Mèn): Đây, phần của anh đây.",
        "(Côn Kiến): Lẹ tay đưa đây cho ta!"
    };

    [Header("Dialogue: Phase 4+ - Đã nhận xong, chỉ đường Dế Choắt")]
    [TextArea(3, 10)]
    public string[] dialogueAfterRace = new string[]
    {
        "(Côn Kiến): (Soạt... chép chép) Ngon lắm! Mật ong của thằng Trũi lúc nào cũng hảo hạng.",
        "(Côn Kiến): Ta giữ lời. Thằng Choắt bạn ngươi đang nằm nghỉ trên chiếc giường lớn ở góc nhà kìa.",
        "(Dế Mèn): Cảm ơn anh. Tôi đi ngay đây!",
        "(Côn Kiến): Qua đó mà xem, đường xá cẩn thận!"
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

    // Private state
    private Transform player;
    private PlayerController playerController;
    private Camera mainCamera;

    private bool isPlayerNearby = false;
    private bool isTalking = false;
    private bool isWaitingForChoice = false;

    private int currentDialogueIndex = 0;
    private string[] currentDialogue;

    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private Transform originalCameraParent;

    private TMPro.TextMeshProUGUI promptTextComp;
    private string originalPromptText;

    // ─────────────────────────────────────────────────────────────
    void Start()
    {
        if (chatBubble == null) chatBubble = GetComponentInChildren<ChatBubble>(true);
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerController = p.GetComponent<PlayerController>();
        }

        mainCamera = Camera.main;

        if (animator == null) animator = GetComponent<Animator>();

        if (interactionPromptUI != null)
        {
            promptTextComp = interactionPromptUI.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (promptTextComp != null)
            {
                originalPromptText = npcName;
                promptTextComp.text = originalPromptText;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    void Update()
    {
        if (player == null) return;
        var kb    = Keyboard.current;
        var mouse = Mouse.current;

        // === Đang chờ lựa chọn ===
        if (isWaitingForChoice)
        {
            HandleCameraFocusSkyrim();
            if (kb == null) return;

            int phase = StoryQuestManager.Instance != null ? StoryQuestManager.Instance.currentPhase : 0;

            if (kb.digit1Key.wasPressedThisFrame)
            {
                isWaitingForChoice = false;
                EndInteraction();

                if (phase == StoryQuestManager.PHASE_BEAT_XENTOC)
                {
                    // Đồng ý đổi Mật Ong lấy thông tin
                    StoryQuestManager.Instance.AdvanceTo(StoryQuestManager.PHASE_MEET_CONKIEN);
                    if (chatBubble != null) chatBubble.Setup("Tốt lắm! Xuống sân tìm Dế Trũi đi!", typewriterBeep);
                    Invoke(nameof(HideBubble), 3f);
                }
                else if (phase == StoryQuestManager.PHASE_BEAT_DETRUI)
                {
                    // Giao Mật Ong - đổi lấy thông tin Dế Choắt
                    if (InventoryManager.Instance != null && InventoryManager.Instance.HasItemType(ItemType.QuestItem_MatOng, 1))
                        InventoryManager.Instance.RemoveItemType(ItemType.QuestItem_MatOng, 1);

                    StoryQuestManager.Instance.AdvanceTo(StoryQuestManager.PHASE_GIVE_ITEM);
                    if (chatBubble != null) chatBubble.Setup("Tuyệt hảo! Dế Choắt đang nằm trên giường ở góc nhà!", typewriterBeep);
                    Invoke(nameof(HideBubble), 4f);
                }
            }
            else if (kb.digit2Key.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
            {
                // Rời đi / thoát nhanh
                isWaitingForChoice = false;
                EndInteraction();
                if (chatBubble != null) chatBubble.Setup("Vậy à, đi thong thả nhé!", typewriterBeep);
                Invoke(nameof(HideBubble), 2f);
            }
            return;
        }

        // === Trong hội thoại ===
        if (isTalking)
        {
            HandleCameraFocusSkyrim();

            if (kb != null && kb.tabKey.wasPressedThisFrame)
            {
                EndInteraction();
                return;
            }

            bool nextPressed = (mouse != null && mouse.leftButton.wasPressedThisFrame)
                            || (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame));

            if (nextPressed)
            {
                if (chatBubble != null && chatBubble.isTyping) chatBubble.FastForward();
                else TriggerNextSentence();
            }
            return;
        }

        // === Idle – phát hiện khoảng cách ===
        float dist = Vector3.Distance(transform.position, player.position);
        bool nearby = dist <= interactionDistance;
        isPlayerNearby = nearby;

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
    void StartInteraction()
    {
        isTalking = true;
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);

        FacePlayerTarget();
        if (animator != null && HasParameter(talkTrigger)) animator.SetTrigger(talkTrigger);
        if (playerController != null) playerController.isDialoguing = true;

        if (mainCamera != null)
        {
            originalCameraParent = mainCamera.transform.parent;
            originalCameraPos    = mainCamera.transform.localPosition;
            originalCameraRot    = mainCamera.transform.localRotation;
        }

        currentDialogueIndex = 0;

        // Chọn bộ dialogue phù hợp Phase
        int phase = StoryQuestManager.Instance != null ? StoryQuestManager.Instance.currentPhase : 0;

        if      (phase >= StoryQuestManager.PHASE_GIVE_ITEM)   currentDialogue = dialogueAfterRace;
        else if (phase == StoryQuestManager.PHASE_BEAT_DETRUI)  currentDialogue = dialogueReceiveHoney;
        else if (phase == StoryQuestManager.PHASE_MEET_CONKIEN) currentDialogue = dialogueWaiting;
        else if (phase == StoryQuestManager.PHASE_BEAT_XENTOC)  currentDialogue = dialogueBlockPath;
        else                                                     currentDialogue = dialogueDefault;

        DisplayCurrentSentence();
    }

    void TriggerNextSentence()
    {
        currentDialogueIndex++;
        if (currentDialogueIndex < currentDialogue.Length)
        {
            DisplayCurrentSentence();
        }
        else
        {
            int phase = StoryQuestManager.Instance != null ? StoryQuestManager.Instance.currentPhase : 0;

            // Chỉ hiện lựa chọn khi đang chặn đường hoặc chờ nhận đồ
            bool needChoice = phase == StoryQuestManager.PHASE_BEAT_XENTOC
                           || phase == StoryQuestManager.PHASE_BEAT_DETRUI;

            if (needChoice) ShowChoice();
            else
            {
                isTalking = false;
                EndInteraction();
            }
        }
    }

    void DisplayCurrentSentence()
    {
        if (chatBubble != null && currentDialogue != null && currentDialogueIndex < currentDialogue.Length)
            chatBubble.Setup(currentDialogue[currentDialogueIndex], typewriterBeep);
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
                int phase = StoryQuestManager.Instance != null ? StoryQuestManager.Instance.currentPhase : 0;

                if (phase == StoryQuestManager.PHASE_BEAT_XENTOC)
                    promptTextComp.text = "[1] \"Đồng ý, tôi sẽ xuống tìm Dế Trũi!\"\n[2] \"Không, tôi đi đây.\"";
                else if (phase == StoryQuestManager.PHASE_BEAT_DETRUI)
                    promptTextComp.text = "[1] \"Nhận lấy Mật Ong đây!\"\n[2] \"Chưa, hẹn tí.\"";
                else
                    promptTextComp.text = "[2] Rời đi";

                promptTextComp.fontSize = 25;
            }
        }
    }

    public void EndInteraction()
    {
        isTalking = false;
        isWaitingForChoice = false;

        if (chatBubble != null) chatBubble.Hide();

        if (promptTextComp != null && !string.IsNullOrEmpty(originalPromptText))
        {
            promptTextComp.text     = originalPromptText;
            promptTextComp.fontSize = 50;
        }

        if (interactionPromptUI != null)
            interactionPromptUI.SetActive(isPlayerNearby);

        if (animator != null && HasParameter(idleTrigger)) animator.SetTrigger(idleTrigger);

        if (playerController != null) playerController.isDialoguing = false;

        if (mainCamera != null)
        {
            mainCamera.transform.localPosition = originalCameraPos;
            mainCamera.transform.localRotation = originalCameraRot;
        }

        if (EncyclopediaManager.Instance != null) EncyclopediaManager.Instance.UnlockInsect("ConKien");
    }

    // ─────────────────────────────────────────────────────────────
    private void HandleCameraFocusSkyrim()
    {
        if (mainCamera == null) return;
        FacePlayerTarget();

        Vector3    targetPos = transform.position + transform.rotation * cameraFocusOffset;
        Quaternion targetRot = Quaternion.LookRotation((transform.position + Vector3.up * 0.5f) - targetPos);

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, Time.deltaTime * cameraTransitionSpeed);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRot, Time.deltaTime * cameraTransitionSpeed);
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

    private bool HasParameter(string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == paramName) return true;
        return false;
    }

    void OnGUI()
    {
        if (isPlayerNearby && !isTalking && !isWaitingForChoice)
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
