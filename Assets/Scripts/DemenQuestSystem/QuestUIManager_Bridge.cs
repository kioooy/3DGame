using UnityEngine;
using Demen.Quests;

/// <summary>
/// Bridge class to fix compilation errors in legacy scripts.
/// Redirects old Quest system calls to the new DemenQuestSystem.
/// </summary>
public class QuestUIManager : MonoBehaviour
{
    private static QuestUIManager _instance;
    public static QuestUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<QuestUIManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("QuestUIManager_Bridge");
                    _instance = go.AddComponent<QuestUIManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null) _instance = this;
    }

    public void ToggleQuestPanel()
    {
        if (DemenQuestUIManager.Instance != null)
            DemenQuestUIManager.Instance.ToggleQuestPanel();
    }

    public void CompleteQuest(string questId)
    {
        if (DemenQuestManager.Instance != null)
            DemenQuestManager.Instance.CompleteQuest(questId);
    }

    public bool IsQuestCompleted(string questId)
    {
        if (DemenQuestManager.Instance == null) return false;
        var quest = DemenQuestManager.Instance.allQuests.Find(q => q.questId == questId);
        return quest != null && quest.isCompleted;
    }

    public void RefreshQuestUI()
    {
        if (DemenQuestUIManager.Instance != null)
            DemenQuestUIManager.Instance.RefreshQuestUI();
    }

    // Fix for QuestSystemSetupTool.cs
    public void CreateDefaultUI()
    {
        Debug.Log("[QuestUIManager_Bridge] CreateDefaultUI called from legacy setup tool.");
        // We handle UI creation via DemenQuestSetupTool now.
    }
}
