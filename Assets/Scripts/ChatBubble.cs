using UnityEngine;
using TMPro;
using System.Collections;

public class ChatBubble : MonoBehaviour
{
    public TextMeshProUGUI textMeshPro;
    public GameObject background;
    
    [Header("Audio Settings")]
    public AudioClip defaultTypewriterClips;
    [Range(0.1f, 3f)] public float typeWriterPitchMin = 0.9f;
    [Range(0.1f, 3f)] public float typeWriterPitchMax = 1.1f;
    private AudioSource audioSource;
    private AudioClip currentTypewriterClip;

    private Transform mainCameraTransform;
    private Coroutine typingCoroutine;
    
    public bool isTyping { get; private set; }
    private string currentSentence;

    void Awake()
    {
        mainCameraTransform = Camera.main != null ? Camera.main.transform : null;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Load default SFX if null
        if (defaultTypewriterClips == null)
        {
            defaultTypewriterClips = Resources.Load<AudioClip>("sfx_dialogue_tick"); 
#if UNITY_EDITOR
            if (defaultTypewriterClips == null)
            {
                defaultTypewriterClips = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_dialogue_tick.wav");
            }
#endif
        }

        // Hide by default
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // Billboard effect: Always face the camera (Z axis looks away from camera)
            // This ensures the front (XY plane) is visible correctly
            transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                             mainCameraTransform.rotation * Vector3.up);
        }
    }

    public void Setup(string text, AudioClip customBeep = null)
    {
        gameObject.SetActive(true);
        currentTypewriterClip = customBeep != null ? customBeep : defaultTypewriterClips;
        currentSentence = text;

        // Duck BGM
        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.DuckAudio(0.05f, 0.5f);
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(text));
    }

    public void FastForward()
    {
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            textMeshPro.text = currentSentence;
            textMeshPro.ForceMeshUpdate();
            isTyping = false;
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        textMeshPro.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            textMeshPro.text += letter;
            
            // Play typing sound, skip spaces
            if (letter != ' ' && audioSource != null && currentTypewriterClip != null)
            {
                audioSource.pitch = Random.Range(typeWriterPitchMin, typeWriterPitchMax);
                audioSource.PlayOneShot(currentTypewriterClip, 0.4f);
            }
            
            yield return new WaitForSeconds(0.03f);
        }
        textMeshPro.ForceMeshUpdate();
        isTyping = false;
    }

    public void Hide()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        gameObject.SetActive(false);
        
        // Restore BGM
        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.RestoreAudio(1.0f);
        }
    }
}
