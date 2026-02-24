using UnityEngine;
using UnityEditor;

public class DialogueSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup Dialogue UI")]
    public static void SetupUI()
    {
        // 1. Find or Create DialogueManager
        DialogueManager manager = Object.FindFirstObjectByType<DialogueManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("DialogueManager");
            manager = go.AddComponent<DialogueManager>();
            Debug.Log("Created DialogueManager GameObject.");
        }

        // 2. Trigger UI Generation
        manager.CreateDefaultUI();
        
        // 3. Ensure Singleton is set if playing (not really needed in Editor, but good for reference)
        DialogueManager.Instance = manager;

        Debug.Log("Dialogue UI has been generated/verified. Check the Canvas in your scene.");
        
        // Select it for the user
        Selection.activeGameObject = manager.gameObject;
    }
}
