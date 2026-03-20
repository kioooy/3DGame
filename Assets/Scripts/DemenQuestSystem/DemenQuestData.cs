using UnityEngine;

namespace Demen.Quests
{
    [CreateAssetMenu(fileName = "New Quest", menuName = "Demen Game/Quest")]
    public class DemenQuestData : ScriptableObject
    {
        [Header("Quest Info")]
        public string questId;
        public string questName;
        [TextArea(3, 10)]
        public string description;

        [Header("State")]
        public bool isCompleted = false;
        public bool isUnlocked = false;

        [Header("Target")]
        [Tooltip("The NPC or location this quest points to. Assign in Inspector.")]
        public Transform targetLocation;

        [Header("Reward")]
        public string rewardDescription;

        public void ResetQuest()
        {
            isCompleted = false;
            isUnlocked = false;
        }
    }
}
