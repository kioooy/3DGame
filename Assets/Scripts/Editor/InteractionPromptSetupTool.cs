using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class InteractionPromptSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup Interaction Prompt for DeTrui")]
    public static void SetupPrompt()
    {
        // 1. Find DeTrui
        DeTruiNPC npc = Object.FindFirstObjectByType<DeTruiNPC>();
        if (npc == null)
        {
            Debug.LogError("Could not find DeTruiNPC in the scene.");
            return;
        }

        // 2. Remove existing prompt if any
        Transform existingPrompt = npc.transform.Find("InteractionPrompt");
        if (existingPrompt != null)
        {
            Object.DestroyImmediate(existingPrompt.gameObject);
        }

        // 3. Create Canvas (World Space)
        GameObject promptObj = new GameObject("InteractionPrompt");
        promptObj.transform.SetParent(npc.transform, false);
        promptObj.transform.localPosition = new Vector3(0, 2.0f, 0); // Above head, but below chat bubble

        Canvas canvas = promptObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        RectTransform rect = promptObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 100); 
        rect.localScale = Vector3.one * 0.005f; 

        // 4. Create Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(promptObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // 5. Create Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(promptObj.transform, false);
        TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
        textComp.text = "[F] Nói chuyện";
        textComp.alignment = TextAlignmentOptions.Center;
        textComp.fontSize = 50; 
        textComp.color = Color.yellow;
        textComp.fontStyle = FontStyles.Bold;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // 6. Add Billboard
        promptObj.AddComponent<Billboard>();

        // 7. Link to NPC
        npc.interactionPromptUI = promptObj;
        promptObj.SetActive(false); // Hide by default
        
        EditorUtility.SetDirty(npc);

        Debug.Log("World Space Interaction Prompt created and linked to DeTrui!");
        Selection.activeGameObject = promptObj;
    }
}
