using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OngNPC : MonoBehaviour
{
    [Header("NPC Discovery")]
    public string npcName = "Chị Ong";
    public GameObject interactionPromptUI;
    public float interactionDistance = 3f;

    [Header("Dialogue Content")]
    [TextArea(3, 5)]
    public List<string> dialogueLines = new List<string>() {
        "Chào em, chị đang bận đi kiếm mật. Em có thấy bông hoa nào đẹp không?",
        "Chăm chỉ là chìa khóa của thành công đấy em ạ.",
        "Hãy luôn giúp đỡ mọi người như cách chị thụ phấn cho hoa nhé!"
    };

    [Header("References")]
    public Animator animator;
    public string talkTrigger = "Talk";
    public string idleTrigger = "Idle";
    public Transform headBone;

    private PlayerController playerController;
    private bool isPlayerNearby = false;
    private bool isTalking = false;
    private ChatBubble chatBubble;

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        chatBubble = GetComponentInChildren<ChatBubble>();
        if (animator == null) animator = GetComponent<Animator>();
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
    }

    void Update()
    {
        if (isTalking) return;

        float distance = Vector3.Distance(transform.position, playerController.transform.position);
        isPlayerNearby = (distance <= interactionDistance);

        if (interactionPromptUI != null)
            interactionPromptUI.SetActive(isPlayerNearby);

        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(StartInteraction());
        }
    }

    IEnumerator StartInteraction()
    {
        isTalking = true;
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);

        FacePlayerTarget();
        
        if (animator != null)
        {
            animator.applyRootMotion = false;
            if (HasParameter(talkTrigger)) animator.SetTrigger(talkTrigger);
        }

        if (playerController != null)
        {
            playerController.isDialoguing = true;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(npcName, dialogueLines.ToArray());
            while (DialogueManager.Instance.dialoguePanel != null && DialogueManager.Instance.dialoguePanel.activeSelf)
            {
                yield return null;
            }
        }
        else
        {
            if (chatBubble != null) chatBubble.Setup(dialogueLines[0]);
            yield return new WaitForSeconds(3f);
        }

        EndInteraction();
    }

    void EndInteraction()
    {
        isTalking = false;
        if (animator != null)
        {
            animator.applyRootMotion = true;
            if (HasParameter(idleTrigger)) animator.SetTrigger(idleTrigger);
        }

        if (playerController != null)
        {
            playerController.isDialoguing = false;
        }
    }

    private void FacePlayerTarget()
    {
        Vector3 direction = playerController.transform.position - transform.position;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private bool HasParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    void OnGUI()
    {
        if (isPlayerNearby && !isTalking)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 100, 200, 50), "Nhấn [F] để trò chuyện", style);
        }
    }
}
