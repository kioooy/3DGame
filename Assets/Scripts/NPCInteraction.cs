using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    public string npcName = "Dế Trũi";
    [TextArea(3, 10)]
    public string[] dialogue = new string[] { "Chào bạn! Tôi là Dế Trũi đây.", "Rất vui được gặp bạn trong khu vườn này.", "Bạn cần tôi giúp gì không?" };
    
    public float interactionDistance = 4f;
    private Transform player;
    private bool isPlayerNearby = false;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) {
             GameObject p = GameObject.FindGameObjectWithTag("Player");
             if (p != null) player = p.transform;
             return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool currentlyNearby = distance <= interactionDistance;

        if (currentlyNearby != isPlayerNearby)
        {
            isPlayerNearby = currentlyNearby;
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.ShowInteractionPrompt(isPlayerNearby);
        }

        if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
        {
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue(npcName, dialogue);
        }
    }
}
