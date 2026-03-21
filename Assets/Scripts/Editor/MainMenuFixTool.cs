using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class MainMenuFixTool : EditorWindow
{
    [MenuItem("Window/Game Setup/Fix Main Menu UI")]
    public static void FixMainMenu()
    {
        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.name != "MainMenu" && currentScene.name != "MainMenuScene")
        {
            if (!EditorUtility.DisplayDialog("Kiểm tra Scene", "Hệ thống phát hiện sếp không ở Scene MainMenu. Sếp có chắc muốn sửa UI ở Scene này không?", "Cứ sửa", "Thôi bỏ"))
            {
                return;
            }
        }

        bool madeChanges = false;

        // 1. Kiểm tra và thêm EventSystem nếu thiếu (Sửa lỗi InvalidOperationException: Input System)
        UnityEngine.EventSystems.EventSystem[] allES = Resources.FindObjectsOfTypeAll<UnityEngine.EventSystems.EventSystem>();
        if (allES.Length == 0)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            madeChanges = true;
            Debug.Log("[MainMenuFix] Đã tạo mới EventSystem với InputSystemUIInputModule!");
        }
        else
        {
            foreach (var es in allES)
            {
                if (!es.gameObject.scene.IsValid()) continue;
                UnityEngine.EventSystems.BaseInputModule[] allModules = es.GetComponents<UnityEngine.EventSystems.BaseInputModule>();
                bool hasNewModule = false;
                foreach (var mod in allModules)
                {
                    if (mod.GetType().Name.Contains("InputSystemUIInputModule")) hasNewModule = true;
                    else if (mod.GetType().Name.Contains("StandaloneInputModule"))
                    {
                        DestroyImmediate(mod);
                        madeChanges = true;
                    }
                }
                if (!hasNewModule)
                {
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    madeChanges = true;
                }
            }
        }

        // 2. Chỉnh sửa chữ DẾ MÈN PHIÊU LƯU KÝ (Quét cả Text và TMP)
        var allTitleObjs = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject g in allTitleObjs)
        {
            if (!g.scene.IsValid()) continue;
            string txtValue = "";
            var txtLegacy = g.GetComponent<Text>();
            var txtTMP = g.GetComponent<TMPro.TMP_Text>();
            
            if (txtLegacy != null) txtValue = txtLegacy.text;
            else if (txtTMP != null) txtValue = txtTMP.text;

            if (txtValue.Contains("DẾ MÈN PHIÊU LƯU KÝ"))
            {
                if (txtLegacy != null) txtLegacy.raycastTarget = false;
                if (txtTMP != null) txtTMP.raycastTarget = false;

                RectTransform rt = g.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0, -100); 
                
                if (txtLegacy != null) txtLegacy.alignment = TextAnchor.UpperCenter;
                if (txtTMP != null) txtTMP.alignment = TMPro.TextAlignmentOptions.Top;

                madeChanges = true;
                Debug.Log("[MainMenuFix] Đã dời Tiêu Đề lên giữa!");
            }
        }

        // 3. Tìm nút Play và nút Thoát
        Button playBtn = null;
        Button exitBtn = null;
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();

        foreach (Button b in allButtons)
        {
            if (!b.gameObject.scene.IsValid()) continue;
            string bTxt = "";
            var tL = b.GetComponentInChildren<Text>();
            var tT = b.GetComponentInChildren<TMPro.TMP_Text>();
            if (tL != null) bTxt = tL.text;
            else if (tT != null) bTxt = tT.text;

            bTxt = bTxt.Trim().ToUpper();
            if (bTxt == "PLAY" || bTxt == "CHƠI") playBtn = b;
            if (bTxt == "THOÁT" || bTxt == "THOÁT GAME" || bTxt == "EXIT") exitBtn = b;
        }

        // Nếu sếp chưa có nút Thoát, lấy mẫu từ nút Play hoặc Chơi
        if (exitBtn == null && playBtn != null)
        {
            GameObject exitObj = (GameObject)PrefabUtility.InstantiatePrefab(PrefabUtility.GetCorrespondingObjectFromSource(playBtn.gameObject) ?? playBtn.gameObject, playBtn.transform.parent);
            if (exitObj == null) exitObj = Instantiate(playBtn.gameObject, playBtn.transform.parent);

            exitObj.name = "ExitButton";
            RectTransform exitRt = exitObj.GetComponent<RectTransform>();
            RectTransform playRt = playBtn.GetComponent<RectTransform>();
            
            exitRt.anchoredPosition = new Vector2(playRt.anchoredPosition.x, playRt.anchoredPosition.y - 120);
            
            var tL = exitObj.GetComponentInChildren<Text>();
            var tT = exitObj.GetComponentInChildren<TMPro.TMP_Text>();
            if (tL != null) tL.text = "THOÁT GAME";
            if (tT != null) tT.text = "THOÁT GAME";
            
            exitBtn = exitObj.GetComponent<Button>();
            madeChanges = true;
            Debug.Log("[MainMenuFix] Đã tạo thành công nút THOÁT GAME!");
        }

        // 5. Găm code vào Manager
        MainMenuManager manager = GameObject.FindFirstObjectByType<MainMenuManager>();
        if (manager != null)
        {
            if (playBtn != null) 
            {
                manager.playButton = playBtn;
                var tL = playBtn.GetComponentInChildren<Text>();
                var tT = playBtn.GetComponentInChildren<TMPro.TMP_Text>();
                if (tL != null) tL.text = "CHƠI";
                if (tT != null) tT.text = "CHƠI";
            }
            if (exitBtn != null) manager.exitButton = exitBtn;
            EditorUtility.SetDirty(manager);
            madeChanges = true;
        }

        if (madeChanges)
        {
            EditorSceneManager.MarkSceneDirty(currentScene);
            Debug.Log("[MainMenuFix] Đã hoàn thiện xong giao diện Main Menu! Sếp hãy Lưu Scene (Ctrl + S) và test ngay.");
        }
        else
        {
            Debug.LogWarning("[MainMenuFix] Không tìm thấy gì để sửa. Nếu sếp dùng TextMeshPro, hệ thống có thể chưa quét được.");
        }
    }
}
