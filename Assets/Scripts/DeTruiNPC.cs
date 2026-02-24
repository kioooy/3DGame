using UnityEngine;

public class DeTruiNPC : MonoBehaviour
{
    [Header("Chat Bubble")]
    public ChatBubble chatBubble;
    public GameObject interactionPromptUI; // New local prompt
    public float timePerSentence = 3f;

    [Header("Identidade")]
    public string npcName = "Dế Trũi";
    [TextArea(3, 10)]
    public string[] dialogue = new string[] {
        "Chào người anh em! Tôi là Dế Trũi đây.",
        "Cuộc đời là những chuyến đi dài, phải không nào?",
        "Nếu cậu cần người đồng hành, tôi luôn sẵn lòng!" 
    };

    [Header("Settings")]
    public float interactionDistance = 3.0f;
    public KeyCode interactKey = KeyCode.F;

    [Header("Animation")]
    public Animator animator;
    public string talkTrigger = "Talk";
    public string idleTrigger = "Idle";

    private Transform player;
    private bool isPlayerNearby = false;
    private bool isTalking = false;

    void Start()
    {
        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) {
                 player = p.transform;
                 Debug.Log("DeTruiNPC: Player found by tag.");
            } else {
                 Debug.LogError("DeTruiNPC: Player NOT found! Make sure Player tag is set.");
            }
        }

        if (animator == null)
            animator = GetComponent<Animator>();
            
        if (chatBubble == null) Debug.LogError("DeTruiNPC: ChatBubble is missing! Run the Setup Tool.");
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        // Debug.Log($"Distance: {distance}"); // Uncomment if needed
        bool currentlyNearby = distance <= interactionDistance;

        if (currentlyNearby != isPlayerNearby)
        {
            isPlayerNearby = currentlyNearby;
            // Only show prompt if NOT talking
            if (!isTalking)
            {
                if (interactionPromptUI != null) 
                    interactionPromptUI.SetActive(isPlayerNearby);
                else if (DialogueManager.Instance != null) // Fallback
                    DialogueManager.Instance.ShowInteractionPrompt(isPlayerNearby);
            }
            
            // If player walks away while talking, end dialogue
            if (!isPlayerNearby && isTalking)
            {
                EndInteraction();
            }
        }

        // Handle Input (New Input System)
        if (isPlayerNearby && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("DeTruiNPC: 'F' pressed. Starting interaction...");
            if (!isTalking)
            {
                StartInteraction();
            }
            else
            {
                // Optional: Advance dialogue if DialogueManager requires it
                // But usually DialogueManager handles the advancement itself
                // We just trigger the initial start here.
            }
        }
    }

    void StartInteraction()
    {
        isTalking = true;
        
        // Hide prompt
        if (interactionPromptUI != null)
            interactionPromptUI.SetActive(false);
        else if (DialogueManager.Instance != null)
            DialogueManager.Instance.ShowInteractionPrompt(false);

        // Face the player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Keep rotation flat
        transform.rotation = Quaternion.LookRotation(direction);

        if (animator != null)
        {
            animator.SetTrigger(talkTrigger);
        }

        // Start Chat Bubble Logic
        StopAllCoroutines();
        StartCoroutine(ShowDialogueRoutine());
    }

    System.Collections.IEnumerator ShowDialogueRoutine()
    {
        if (chatBubble != null)
        {
            foreach (string sentence in dialogue)
            {
                Debug.Log($"DeTruiNPC SAYS: {sentence}");
                chatBubble.Setup(sentence);
                yield return new WaitForSeconds(timePerSentence);
            }
            chatBubble.Hide();
        } else {
            Debug.LogError("DeTruiNPC: ChatBubble is null in Coroutine!");
        }
        EndInteraction();
    }

    public void EndInteraction()
    {
        isTalking = false;
        StopAllCoroutines();
        
        if (chatBubble != null) chatBubble.Hide();

        if (DialogueManager.Instance != null)
        {
             DialogueManager.Instance.ShowInteractionPrompt(false); 
        }
        
        // If still nearby, show prompt again
        if (isPlayerNearby)
        {
            if (interactionPromptUI != null)
                interactionPromptUI.SetActive(true);
            else if (DialogueManager.Instance != null)
                DialogueManager.Instance.ShowInteractionPrompt(true);
        }

        if (animator != null)
        {
            animator.SetTrigger(idleTrigger);
        }
    }
}
