using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public GameObject interactionPrompt;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI nameText;
    [Header("Audio Settings")]
    public AudioClip defaultTypewriterClips;
    [Range(0.1f, 3f)] public float typeWriterPitchMin = 0.9f;
    [Range(0.1f, 3f)] public float typeWriterPitchMax = 1.1f;
    private AudioSource audioSource;
    private AudioClip currentTypewriterClip;

    private Queue<string> sentences;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        sentences = new Queue<string>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Load default SFX if null
        if (defaultTypewriterClips == null)
        {
            defaultTypewriterClips = Resources.Load<AudioClip>("sfx_dialogue_tick"); 
            // Also check direct path for the python generated one if Resources fails later:
#if UNITY_EDITOR
            if (defaultTypewriterClips == null)
            {
                defaultTypewriterClips = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_dialogue_tick.wav");
            }
#endif
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    [ContextMenu("Generate UI")]
    public void CreateDefaultUI()
    {
        // 1. Ensure EventSystem exists
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        // 2. Find or Create Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DialogueCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 3. Create Interaction Prompt
        if (interactionPrompt == null)
        {
            interactionPrompt = new GameObject("InteractionPrompt");
            interactionPrompt.transform.SetParent(canvas.transform, false);
            TextMeshProUGUI promptText = interactionPrompt.AddComponent<TextMeshProUGUI>();
            promptText.text = "[F] Nói chuyện";
            promptText.fontSize = 30;
            promptText.color = Color.white;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.fontStyle = FontStyles.Bold;
            promptText.outlineWidth = 0.2f;
            promptText.outlineColor = Color.black;

            RectTransform promptRect = interactionPrompt.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0.5f);
            promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.anchoredPosition = new Vector2(0, -100);
            promptRect.sizeDelta = new Vector2(400, 60);
        }

        // 4. Create Dialogue Panel
        if (dialoguePanel == null)
        {
            dialoguePanel = new GameObject("DialoguePanel");
            dialoguePanel.transform.SetParent(canvas.transform, false);
            Image panelImage = dialoguePanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.85f);
            
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0);
            panelRect.anchorMax = new Vector2(0.5f, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.anchoredPosition = new Vector2(0, 50);
            panelRect.sizeDelta = new Vector2(800, 180);

            // Name Text
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(dialoguePanel.transform, false);
            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.fontSize = 28;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Color.yellow;
            nameText.text = "Name";
            
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(0, 1);
            nameRect.pivot = new Vector2(0, 1);
            nameRect.anchoredPosition = new Vector2(30, -15);
            nameRect.sizeDelta = new Vector2(300, 45);

            // Dialogue Text
            GameObject dialogueObj = new GameObject("DialogueText");
            dialogueObj.transform.SetParent(dialoguePanel.transform, false);
            dialogueText = dialogueObj.AddComponent<TextMeshProUGUI>();
            dialogueText.fontSize = 24;
            dialogueText.color = Color.white;
            dialogueText.text = "Dialogue goes here...";
            
            RectTransform dialogueRect = dialogueObj.GetComponent<RectTransform>();
            dialogueRect.anchorMin = new Vector2(0, 0);
            dialogueRect.anchorMax = new Vector2(1, 1);
            dialogueRect.offsetMin = new Vector2(30, 30);
            dialogueRect.offsetMax = new Vector2(-30, -60);
        }
    }

    public void ShowInteractionPrompt(bool show)
    {
        if (interactionPrompt != null && (dialoguePanel == null || !dialoguePanel.activeSelf))
            interactionPrompt.SetActive(show);
    }

    public void StartDialogue(string npcName, string[] dialogue, AudioClip customTypewriterClip = null)
    {
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (nameText != null) nameText.text = npcName;
        
        currentTypewriterClip = customTypewriterClip != null ? customTypewriterClip : defaultTypewriterClips;

        // Duck background music
        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.DuckAudio(0.05f, 0.5f);
        }

        sentences.Clear();
        foreach (string sentence in dialogue)
            sentences.Enqueue(sentence);

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        string sentence = sentences.Dequeue();
        StopAllCoroutines();
        StartCoroutine(TypeSentence(sentence));
    }

    IEnumerator TypeSentence(string sentence)
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
            foreach (char letter in sentence.ToCharArray())
            {
                dialogueText.text += letter;
                
                // Play typing sound, skip spaces for a more dynamic feel
                if (letter != ' ' && audioSource != null && currentTypewriterClip != null)
                {
                    audioSource.pitch = Random.Range(typeWriterPitchMin, typeWriterPitchMax);
                    audioSource.PlayOneShot(currentTypewriterClip, 0.4f);
                }
                
                yield return new WaitForSeconds(0.03f);
            }
        }
    }

    public void EndDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        
        // Restore background music
        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.RestoreAudio(1.0f);
        }
    }

    void Update() {
        var kb = Keyboard.current;
        if (kb != null && dialoguePanel != null && dialoguePanel.activeSelf && (kb.spaceKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)) {
            DisplayNextSentence();
        }
    }
}
