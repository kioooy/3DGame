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
            List<DemenQuestData> activeQuests = allQuests.Where(q => q.isUnlocked && !q.isCompleted).ToList();

            // Tự động chèn nhiệm vụ Cốt Truyện Chính vào Menu J
            if (StoryQuestManager.Instance != null && StoryQuestManager.Instance.currentPhase < StoryQuestManager.PHASE_ENDING)
            {
                var storyQuest = ScriptableObject.CreateInstance<DemenQuestData>();
                storyQuest.questId = "story_main";
                storyQuest.isUnlocked = true;
                storyQuest.isCompleted = false;

                switch (StoryQuestManager.Instance.currentPhase)
                {
                    case StoryQuestManager.PHASE_START:
                        storyQuest.questName = "Cốt truyện: Tìm kiếm Dế Choắt";
                        storyQuest.description = "Mục tiêu: Gặp Xén Tóc\n\n- Tìm Xén Tóc ở xung quanh vũng nước.\n- Nhấn [F] hoặc Chuột trái để gọi thoại.\n- Chọn phím [1] Nghênh chiến! (Vật Tay) để thắng\n- Thắng vật tay để lấy thông tin về Dế Choắt.";
                        break;
                    case StoryQuestManager.PHASE_BEAT_XENTOC:
                        storyQuest.questName = "Cốt truyện: Lên Bàn Ăn";
                        storyQuest.description = "Mục tiêu: Đạt thỏa thuận\n\n- Bạn đã đánh bại Xén Tóc!\n- Hãy trò chuyện lại và bấm [1] Lên Bàn Ăn để Xén Tóc chở bay lên.";
                        break;
                    case StoryQuestManager.PHASE_MEET_CONKIEN:
                        storyQuest.questName = "Cốt truyện: Chướng ngại trên bàn";
                        storyQuest.description = "Mục tiêu: Thông chốt chặn\n\n- Phát hiện Côn Kiến đang canh giữ trên mặt bàn.\n- Yêu cầu Côn Kiến: Phải tìm Mật Ong từ Dế Trũi ở sân chơi.";
                        break;
                    case StoryQuestManager.PHASE_BEAT_DETRUI:
                        storyQuest.questName = "Cốt truyện: Tranh đoạt Mật Ong";
                        storyQuest.description = "Mục tiêu: Thắng cuộc chạy đua\n\n- Tìm Dế Trũi ở dưới sân.\n- Bấm [1] Chấp nhận chạy đua để giành lấy phần thưởng là hũ mật ong.";
                        break;
                    case StoryQuestManager.PHASE_GIVE_ITEM:
                        storyQuest.questName = "Cốt truyện: Lời xin lỗi muộn màng";
                        storyQuest.description = "Mục tiêu: Tìm gặp Dế Choắt\n\n- Bạn đã đưa Mật Ong cho Côn Kiến, đổi lại thông tin về Dế Choắt.\n- Hãy đến khu vực giường ngủ trong góc kẹt để tìm và hỏi thăm em ấy.";
                        break;
                }
                activeQuests.Insert(0, storyQuest); // Đưa lên đầu danh sách
            }

            return activeQuests;
        }
    }
}
