using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] Animator animator;
    [SerializeField] float moveSpeed;
    [SerializeField] Rigidbody rb;
    Vector3 _moveInput;

    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // New Input System (Project Settings -> Active Input Handling: Input System Package)
        float h = 0f;
        float v = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            h = (kb.dKey.isPressed ? 1f : 0f) + (kb.aKey.isPressed ? -1f : 0f);
            v = (kb.wKey.isPressed ? 1f : 0f) + (kb.sKey.isPressed ? -1f : 0f);
        }
        _moveInput = new Vector3(h, 0f, v).normalized;


        
    }
    private void FixedUpdate()
    {
        Vector3 moveVelocity = _moveInput * moveSpeed;
        Vector3 newPosition = rb.position + moveVelocity * Time.deltaTime;
        rb.MovePosition(newPosition);
        animator.SetFloat("MoveX", _moveInput.x);
        animator.SetFloat("MoveZ", _moveInput.z);
    }
}
