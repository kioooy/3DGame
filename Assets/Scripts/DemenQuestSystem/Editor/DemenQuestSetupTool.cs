using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Demen.Quests;

namespace Demen.Quests.Editor
{
    public class DemenQuestSetupTool : EditorWindow
    {
        [MenuItem("Quest System/0. Setup NEW Story Quest (Demen)", false, 0)]
        public static void ShowWindow()
        {
            GetWindow<DemenQuestSetupTool>("Quest Setup");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quest System Setup (Demen)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Công cụ này sẽ tự động tạo DemenQuestManager và Quest UI.", MessageType.Info);

            if (GUILayout.Button("Setup / Reset Quest System", GUILayout.Height(40)))
            {
                Setup();
            }
        }

        private static void Setup()
        {
            // 1. Tạo QuestManager
            GameObject managerObj = GameObject.Find("DemenQuestManager");
            if (managerObj == null)
            {
                managerObj = new GameObject("DemenQuestManager");
                managerObj.AddComponent<DemenQuestManager>();
                Undo.RegisterCreatedObjectUndo(managerObj, "Create DemenQuestManager");
            }

            // 2. Tạo Quest UI Canvas
            GameObject canvasObj = GameObject.Find("DemenQuestCanvas");
            if (canvasObj != null) DestroyImmediate(canvasObj);

            canvasObj = new GameObject("DemenQuestCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create DemenQuestCanvas");

            // 3. Tạo Panel
            GameObject panel = new GameObject("QuestPanel");
            panel.transform.SetParent(canvasObj.transform, false);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
            panel.SetActive(false);

            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(1, 1);
            panelRT.anchorMax = new Vector2(1, 1);
            panelRT.pivot = new Vector2(1, 1);
            panelRT.anchoredPosition = new Vector2(-20, -150); // Đưa xuống thấp hơn một chút để tránh vướng map
            panelRT.sizeDelta = new Vector2(320, 380); // Tăng nhẹ kích thước để chứa chữ tốt hơn

            // 4. Tạo Text
            GameObject textObj = new GameObject("QuestText");
            textObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI questText = textObj.AddComponent<TextMeshProUGUI>();
            questText.fontSize = 18;
            questText.alignment = TextAlignmentOptions.TopLeft; // Căn trên bên trái
            questText.margin = new Vector4(25, 25, 25, 25); // Tăng lề để chữ không sát viền
            questText.textWrappingMode = TextWrappingModes.Normal;
            
            RectTransform textRT = questText.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            // 5. Thêm UI Manager
            DemenQuestUIManager uiManager = canvasObj.AddComponent<DemenQuestUIManager>();
            uiManager.questPanel = panel;
            uiManager.questListText = questText;

            // 6. Tạo Sample Quests
            CreateSampleQuests(managerObj.GetComponent<DemenQuestManager>());

            Debug.Log("[DemenQuestSetupTool] Setup thành công!");
            EditorUtility.DisplayDialog("Thành công", "Hệ thống Quest (Demen) đã được thêm.\nNhấn 'Q' để xem.", "OK");
        }

        private static void CreateSampleQuests(DemenQuestManager manager)
        {
            string path = "Assets/Resources/Quests";
            if (!AssetDatabase.IsValidFolder(path))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "Quests");
            }

            manager.allQuests.Clear();

            // Quest 1
            DemenQuestData q1 = CreateQuestAsset(path, "q1_remorse", "Lời xin lỗi chân thành", "Tìm gặp và xin lỗi Dế Choắt về trò đùa tai hại.");
            q1.isUnlocked = true;
            manager.allQuests.Add(q1);

            // Quest 2
            DemenQuestData q2 = CreateQuestAsset(path, "q2_fellowship", "Người bạn đường mới", "Gặp gỡ Dế Trũi và rủ cậu ấy cùng lên đường.");
            manager.allQuests.Add(q2);

            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();
        }

        private static DemenQuestData CreateQuestAsset(string folder, string id, string name, string desc)
        {
            string fullPath = $"{folder}/{id}.asset";
            DemenQuestData quest = AssetDatabase.LoadAssetAtPath<DemenQuestData>(fullPath);
            if (quest == null)
            {
                quest = CreateInstance<DemenQuestData>();
                AssetDatabase.CreateAsset(quest, fullPath);
            }
            quest.questId = id;
            quest.questName = name;
            quest.description = desc;
            quest.ResetQuest();
            return quest;
        }
    }
}
