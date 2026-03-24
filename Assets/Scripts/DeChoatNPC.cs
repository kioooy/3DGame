using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Script riêng cho NPC Dế Choắt.
/// Chỉ hiện tương tác khi Phase >= GIVE_ITEM.
/// Nói chuyện xong sẽ trigger kết thúc game.
/// </summary>
public class DeChoatNPC : MonoBehaviour
{
    [Header("Chat Bubble")]
    public ChatBubble chatBubble;
    public GameObject interactionPromptUI;

    [Header("Identidade")]
    [SerializeField] private string _npcName = "Dế Choắt";
    public AudioClip typewriterBeep;

    [Header("Dialogue: Chưa đủ điều kiện gặp")]
    [TextArea(3, 10)]
    public string[] dialogueNotReady = new string[]
    {
        "(Dế Choắt): (Trùm chăn rên rỉ) Ai đó? Đừng làm phiền tôi, tôi mệt lắm..."
    };

    [Header("Dialogue: Cảnh kết thúc cảm xúc (Phase >= GIVE_ITEM)")]
    [TextArea(3, 10)]
    public string[] dialogueRescued = new string[]
    {
        "(Dế Choắt): Mày... đến tìm tao thật à, Mèn?",
        "(Dế Mèn): Choắt ơi, tao xin lỗi mày... Tất cả là tại tao ngỗ nghịch...",
        "(Dế Choắt): Tự dưng thấy mày... tao không biết nói gì nữa.",
        "(Dế Mèn): Cảm ơn mọi người, cảm ơn anh Xén Tóc, anh Côn Kiến, Dế Trũi...",
        "(Dế Choắt): ...Nhưng mày mới là người đi đến tận đây vì tao."
    };

    [Header("Settings Khung Cảnh (Skyrim-like)")]
    public float interactionDistance = 6.0f; // Nới rộng khoảng cách do nằm giường
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
                originalPromptText = _npcName;
                promptTextComp.text = originalPromptText;
            }
        }
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
        var kb    = Keyboard.current;
        var mouse = Mouse.current;

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
        bool shouldShowPrompt = isPlayerNearby && isBestCandidate && !isTalking;

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
        if (isTalking)
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

        // Chỉ chơi cảnh kết thúc khi đã đủ điều kiện
        int phase = StoryQuestManager.Instance != null ? StoryQuestManager.Instance.currentPhase : 0;

        if (phase >= StoryQuestManager.PHASE_GIVE_ITEM)
        {
            currentDialogue = dialogueRescued;
            // Advance sang ENDING ngay khi bắt đầu nói chuyện
            StoryQuestManager.Instance?.AdvanceTo(StoryQuestManager.PHASE_ENDING);
        }
        else
        {
            currentDialogue = dialogueNotReady;
        }

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
            isTalking = false;
            EndInteraction();
        }
    }

    void DisplayCurrentSentence()
    {
        if (chatBubble != null && currentDialogue != null && currentDialogueIndex < currentDialogue.Length)
            chatBubble.Setup(currentDialogue[currentDialogueIndex], typewriterBeep);
    }

    public void EndInteraction()
    {
        isTalking = false;

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

        if (EncyclopediaManager.Instance != null) EncyclopediaManager.Instance.UnlockInsect("DeChoat");
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

    public string GetDisplayName() => _npcName;

    void OnGUI()
    {
        bool isBestCandidate = playerController != null && playerController.GetClosestNPC() == gameObject;
        if (isPlayerNearby && isBestCandidate && !isTalking)
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

            string text = "[F] Nói chuyện (Dế Choắt)";
            GUI.Label(new Rect(x + 2, y + 2, w, h), text, shadowStyle);
            GUI.Label(new Rect(x, y, w, h), text, style);
        }
    }
}
