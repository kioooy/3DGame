using UnityEngine;
using TMPro;
using System.Text;

namespace Demen.Quests
{
    public class DemenQuestUIManager : MonoBehaviour
    {
        public static DemenQuestUIManager Instance { get; private set; }

        [Header("UI Elements")]
        public GameObject questPanel;
        public TextMeshProUGUI questListText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("[DemenQuestUIManager] Singleton initialized.");
            }
            else
            {
                Debug.LogWarning("[DemenQuestUIManager] Multiple instances detected!");
            }
        }

        private void Update()
        {
            // Input detection moved to PlayerController to avoid double-toggling
        }

        public void ToggleQuestPanel()
        {
            if (questPanel != null)
            {
                bool isActive = !questPanel.activeSelf;
                questPanel.SetActive(isActive);
                Debug.Log($"[DemenQuestUIManager] Panel toggled: {isActive}");
                if (isActive) RefreshQuestUI();
            }
            else
            {
                Debug.LogError("[DemenQuestUIManager] questPanel is NULL! Make sure to run the Setup Tool.");
            }
        }

        public void RefreshQuestUI()
        {
            if (DemenQuestManager.Instance == null || questListText == null) return;

            var activeQuests = DemenQuestManager.Instance.GetActiveQuests();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<color=#38BDF8><size=110%><b>NHIỆM VỤ</b></size></color>");
            sb.AppendLine("<size=50%> </size>"); // Giãn dòng nhẹ
            
            if (activeQuests.Count == 0)
            {
                sb.AppendLine("<i><size=85%>Hãy khám phá thế giới!</size></i>");
            }
            else
            {
                foreach (var q in activeQuests)
                {
                    sb.AppendLine($"<size=105%><b>▶ {q.questName}</b></size>");
                    sb.AppendLine($"<size=80%>{q.description}</size>");
                    sb.AppendLine("<size=30%> </size>"); // Khoảng cách giữa các quest
                }
            }

            questListText.text = sb.ToString();
        }
    }
}
