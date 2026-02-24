using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Awake()
    {
        mainCameraTransform = Camera.main != null ? Camera.main.transform : null;
    }

    void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // Billboard effect: Always face the camera (Z axis looks away from camera)
            transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                             mainCameraTransform.rotation * Vector3.up);
        }
    }
}
