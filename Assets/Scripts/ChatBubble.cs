using UnityEngine;
using TMPro;

public class ChatBubble : MonoBehaviour
{
    public TextMeshProUGUI textMeshPro;
    public GameObject background;
    
    private Transform mainCameraTransform;

    void Awake()
    {
        mainCameraTransform = Camera.main != null ? Camera.main.transform : null;
        
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

    public void Setup(string text)
    {
        gameObject.SetActive(true);
        textMeshPro.text = text;
        textMeshPro.ForceMeshUpdate();

        // Optional: Adjust background size based on text if needed
        // For now, we assume the background is sliced and automatically handled by layout or manually set fixed size
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
