using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Demen.Quests
{
    public class DemenQuestManager : MonoBehaviour
    {
        public static DemenQuestManager Instance { get; private set; }

        [Header("Quests Configuration")]
        public List<DemenQuestData> allQuests = new List<DemenQuestData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void UnlockQuest(string questId)
        {
            var quest = allQuests.FirstOrDefault(q => q.questId == questId);
            if (quest != null)
            {
                quest.isUnlocked = true;
                Debug.Log($"[DemenQuestManager] Unlocked quest: {quest.questName}");
            }
        }

        public void CompleteQuest(string questId)
        {
            var quest = allQuests.FirstOrDefault(q => q.questId == questId);
            if (quest != null && quest.isUnlocked && !quest.isCompleted)
            {
                quest.isCompleted = true;
                Debug.Log($"[DemenQuestManager] Completed quest: {quest.questName}");
            }
        }

        public List<DemenQuestData> GetActiveQuests()
        {
            return allQuests.Where(q => q.isUnlocked && !q.isCompleted).ToList();
        }
    }
}
