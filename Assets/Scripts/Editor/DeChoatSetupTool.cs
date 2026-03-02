using UnityEngine;
using UnityEditor;

public class DeChoatSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup De Choat NPC")]
    public static void SetupDeChoatNPC()
    {
        // 1. Tìm object Dế Choắt trong scene (nếu có)
        GameObject deChoatObj = GameObject.Find("DeChoat");
        if (deChoatObj == null)
        {
            deChoatObj = new GameObject("DeChoat");
            deChoatObj.transform.position = new Vector3(2, 0, 2); // Vị trí ví dụ
            Debug.Log("DeChoatSetupTool: Đã tạo object DeChoat.");
        }

        // 2. Thêm component DeChoatNPC nếu chưa có
        DeChoatNPC npcScript = deChoatObj.GetComponent<DeChoatNPC>();
        if (npcScript == null)
        {
            npcScript = deChoatObj.AddComponent<DeChoatNPC>();
            Debug.Log("DeChoatSetupTool: Đã thêm script DeChoatNPC.");
        }

        // 3. Tìm hoặc tạo ChatBubble
        ChatBubble chatBubble = deChoatObj.GetComponentInChildren<ChatBubble>();
        if (chatBubble == null)
        {
            GameObject bubbleObj = new GameObject("ChatBubble");
            bubbleObj.transform.SetParent(deChoatObj.transform);
            bubbleObj.transform.localPosition = new Vector3(0, 2f, 0); // Đặt trên đầu NPC
            chatBubble = bubbleObj.AddComponent<ChatBubble>();
            
            // Note: Cần setup UI Text cho ChatBubble nếu chưa có, 
            // phụ thuộc vào cách ChatBubble được viết.
            Debug.Log("DeChoatSetupTool: Đã tạo ChatBubble placeholder.");
        }
        npcScript.chatBubble = chatBubble;

        // 4. Tìm hoặc tạo Interaction Prompt UI
        if (npcScript.interactionPromptUI == null)
        {
            Transform promptTransform = deChoatObj.transform.Find("InteractionPrompt");
            GameObject promptObj;
            if (promptTransform != null)
            {
                promptObj = promptTransform.gameObject;
            }
            else
            {
                promptObj = new GameObject("InteractionPrompt");
                promptObj.transform.SetParent(deChoatObj.transform);
                promptObj.transform.localPosition = new Vector3(0, 1.5f, 0);
                
                // Add a simple 3D text or Sprite Renderer as a prompt (e.g., "[F] Talk")
                // Here we just create an empty object to hold it.
                Debug.Log("DeChoatSetupTool: Đã tạo InteractionPrompt placeholder.");
            }
            npcScript.interactionPromptUI = promptObj;
            promptObj.SetActive(false); // Ẩn mặc định
        }

        // 5. Thêm Animator (nếu chưa có, nhưng thường model sẽ có sẵn)
        Animator animator = deChoatObj.GetComponent<Animator>();
        if (animator == null)
        {
             animator = deChoatObj.AddComponent<Animator>();
             Debug.Log("DeChoatSetupTool: Đã thêm Animator.");
        }
        npcScript.animator = animator;

        // 6. Thêm CapsuleCollider hoặc SphereCollider làm trigger tương tác
        SphereCollider trigger = deChoatObj.GetComponent<SphereCollider>();
        if (trigger == null)
        {
            trigger = deChoatObj.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = npcScript.interactionDistance;
            Debug.Log("DeChoatSetupTool: Đã thêm SphereCollider (Trigger).");
        }

        // Đánh dấu dirty để Unity lưu
        EditorUtility.SetDirty(deChoatObj);
        if (npcScript != null) EditorUtility.SetDirty(npcScript);

        Debug.Log("DeChoatSetupTool: Hoàn tất setup Dế Choắt! Hãy kiểm tra lại các reference.");

        // Chọn object để user dễ thấy
        Selection.activeGameObject = deChoatObj;
    }
}
