using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class ChatBubbleSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup Chat Bubble for DeTrui")]
    public static void SetupChatBubble()
    {
        // 1. Find DeTrui
        DeTruiNPC npc = Object.FindFirstObjectByType<DeTruiNPC>();
        if (npc == null)
        {
            Debug.LogError("Could not find DeTruiNPC in the scene.");
            return;
        }

        // 2. check if Bubble already exists
        Transform existingBubble = npc.transform.Find("ChatBubble");
        if (existingBubble != null)
        {
            Object.DestroyImmediate(existingBubble.gameObject);
        }

        // 3. Create Canvas (World Space)
        GameObject bubbleObj = new GameObject("ChatBubble");
        bubbleObj.transform.SetParent(npc.transform, false);
        bubbleObj.transform.localPosition = new Vector3(0, 2.5f, 0); // Position slightly higher

        Canvas canvas = bubbleObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        RectTransform rect = bubbleObj.GetComponent<RectTransform>();
        // Fix: Use high resolution for clean text, scale down for world size
        rect.sizeDelta = new Vector2(800, 300); 
        rect.localScale = Vector3.one * 0.005f; // Scale down significantly

        // 4. Create Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(bubbleObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // 5. Create Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(bubbleObj.transform, false);
        TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
        textComp.text = "...";
        textComp.alignment = TextAlignmentOptions.Center;
        textComp.fontSize = 42; 
        textComp.color = Color.white;
        textComp.textWrappingMode = TextWrappingModes.Normal;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.1f);
        textRect.anchorMax = new Vector2(0.95f, 0.9f);
        textRect.sizeDelta = Vector2.zero;

        // 6. Setup ChatBubble Component
        ChatBubble bubbleScript = bubbleObj.AddComponent<ChatBubble>();
        bubbleScript.textMeshPro = textComp;
        bubbleScript.background = bgObj;

        // 7. Link to NPC
        npc.chatBubble = bubbleScript;
        EditorUtility.SetDirty(npc);
        
        // Hide it by default so it doesn't obstruct view, relying on script to show it
        bubbleObj.SetActive(false);

        Debug.Log("Chat Bubble created and linked to DeTrui!");
        Selection.activeGameObject = bubbleObj;
    }
}
