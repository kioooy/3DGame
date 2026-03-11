using UnityEngine;

public class RacingCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3f, -5f);
    public float smoothSpeed = 10f;
    public float lookAngle = 20f;

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(lookAngle, 0f, 0f);
        }
    }
}
