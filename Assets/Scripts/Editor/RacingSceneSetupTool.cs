#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class RacingSceneSetupTool : EditorWindow
{
    [MenuItem("Tools/3DGame/Setup Racing Minigame Scene")]
    public static void SetupScene()
    {
        // 1. Ask save current scene
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // 2. Create New Scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 3. Set up Environment (Track)
        GameObject trackParent = new GameObject("--- Track ---");
        
        Material defaultMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (defaultMat.shader == null) defaultMat = new Material(Shader.Find("Standard")); // Fallback
        
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(trackParent.transform);
        ground.transform.position = new Vector3(0, -0.5f, 50f);
        ground.transform.localScale = new Vector3(20f, 1f, 120f);
        ground.GetComponent<Renderer>().sharedMaterial = new Material(defaultMat) { color = new Color(0.2f, 0.6f, 0.2f) };

        GameObject startLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        startLine.name = "StartLine";
        startLine.transform.SetParent(trackParent.transform);
        startLine.transform.position = new Vector3(0, 0.01f, 0);
        startLine.transform.localScale = new Vector3(20f, 0.1f, 2f);
        startLine.GetComponent<Renderer>().sharedMaterial = new Material(defaultMat) { color = Color.white };

        GameObject finishLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finishLine.name = "FinishLine";
        finishLine.transform.SetParent(trackParent.transform);
        finishLine.transform.position = new Vector3(0, 0.01f, 100f);
        finishLine.transform.localScale = new Vector3(20f, 0.1f, 2f);
        finishLine.GetComponent<Renderer>().sharedMaterial = new Material(defaultMat) { color = Color.red };
        
        GameObject pStartPos = new GameObject("PlayerStartPos");
        pStartPos.transform.SetParent(trackParent.transform);
        pStartPos.transform.position = new Vector3(-3, 0.01f, 0);

        GameObject nStartPos = new GameObject("NPCStartPos");
        nStartPos.transform.SetParent(trackParent.transform);
        nStartPos.transform.position = new Vector3(3, 0.01f, 0);

        Material obstacleMat = new Material(defaultMat) { color = new Color(0.6f, 0.3f, 0.1f) };

        // Tạo chướng ngại vật (10 cái) dọc theo đường đua của cả 2 bên (Trái x = -3, Phải x = 3)
        for (int i = 1; i <= 8; i++)
        {
            float zPos = i * 10f + Random.Range(-2f, 2f);
            
            // Lane trái (Player)
            GameObject obsL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obsL.name = "Obstacle_Player_" + i;
            obsL.transform.SetParent(trackParent.transform);
            obsL.transform.position = new Vector3(-3, 0.5f, zPos);
            obsL.transform.localScale = new Vector3(2f, 1f, 0.5f);
            obsL.GetComponent<Renderer>().sharedMaterial = obstacleMat;
            obsL.GetComponent<Collider>().isTrigger = true;
            
            // Lane phải (NPC)
            GameObject obsR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obsR.name = "Obstacle_NPC_" + i;
            obsR.transform.SetParent(trackParent.transform);
            obsR.transform.position = new Vector3(3, 0.5f, zPos + Random.Range(-1f, 1f)); // Random lệch xíu so với player
            obsR.transform.localScale = new Vector3(2f, 1f, 0.5f);
            obsR.GetComponent<Renderer>().sharedMaterial = obstacleMat;
            obsR.GetComponent<Collider>().isTrigger = true;
        }

        // 3.5 Set up Grandstands & Audience
        GameObject stadiumParent = new GameObject("--- Stadium & Audience ---");
        Material grandstandMat = new Material(defaultMat) { color = new Color(0.4f, 0.4f, 0.4f) };

        // Tạo 3 bậc khán đài ở 2 bên đường đua
        for (int side = -1; side <= 1; side += 2) // side = -1 (trái), side = 1 (phải)
        {
            for (int step = 0; step < 3; step++)
            {
                GameObject bleacher = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bleacher.name = $"Bleacher_{(side == -1 ? "Left" : "Right")}_Step{step}";
                bleacher.transform.SetParent(stadiumParent.transform);
                
                // Trái: -13, -15, -17. Phải: 13, 15, 17
                float xPos = side * (13f + step * 2f);
                float yPos = 0.5f + step * 1f;
                
                bleacher.transform.position = new Vector3(xPos, yPos, 50f);
                bleacher.transform.localScale = new Vector3(2f, 1f, 120f);
                bleacher.GetComponent<Renderer>().sharedMaterial = grandstandMat;

                // Random sinh khán giả (Capsules) dọc theo khán đài
                for (float z = -5f; z <= 105f; z += Random.Range(2f, 4f))
                {
                    if (Random.value > 0.3f) // 70% có người ngồi
                    {
                        GameObject audience = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        audience.name = "Audience_NPC";
                        audience.transform.SetParent(stadiumParent.transform);
                        audience.transform.position = new Vector3(xPos, yPos + 1f, z);
                        audience.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                        
                        // Màu ngẫu nhiên cho khán giả
                        Color randomColor = new Color(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), Random.Range(0.2f, 1f));
                        audience.GetComponent<Renderer>().sharedMaterial = new Material(defaultMat) { color = randomColor };
                        
                        // Xoá collider của khán giả để nhẹ game
                        DestroyImmediate(audience.GetComponent<Collider>());
                    }
                }
            }
        }

        // 3.6 Set up Forest Background (Che chân trời)
        GameObject forestParent = new GameObject("--- Forest Background ---");
        Material trunkMat = new Material(defaultMat) { color = new Color(0.35f, 0.2f, 0.1f) }; // Nâu gỗ
        Material leafMat = new Material(defaultMat) { color = new Color(0.1f, 0.5f, 0.15f) }; // Xanh lá đậm

        // Trồng rừng 2 bên (Trái x = -25 đến -40, Phải x = 25 đến 40)
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 60; i++) // 60 cây mỗi bên
            {
                float tX = side * Random.Range(25f, 40f);
                float tZ = Random.Range(-20f, 130f);
                
                // Thân cây (Cylinder)
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Tree_Trunk";
                trunk.transform.SetParent(forestParent.transform);
                float trunkHeight = Random.Range(4f, 8f);
                trunk.transform.position = new Vector3(tX, trunkHeight / 2f, tZ);
                trunk.transform.localScale = new Vector3(1f, trunkHeight / 2f, 1f);
                trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
                DestroyImmediate(trunk.GetComponent<Collider>());

                // Tán cây (Sphere)
                int leafClusters = Random.Range(2, 4);
                for (int j = 0; j < leafClusters; j++)
                {
                    GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    leaves.name = "Tree_Leaves";
                    leaves.transform.SetParent(trunk.transform);
                    
                    Vector3 leafOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1.5f), Random.Range(-1f, 1f));
                    leaves.transform.localPosition = Vector3.up + leafOffset;
                    
                    float leafSize = Random.Range(2f, 4f);
                    leaves.transform.localScale = new Vector3(leafSize, leafSize, leafSize) / trunk.transform.localScale.y; // bù trừ scale của cha
                    leaves.GetComponent<Renderer>().sharedMaterial = leafMat;
                    DestroyImmediate(leaves.GetComponent<Collider>());
                }
            }
        }

        // Đảm bảo Skybox đang bật (DefaultGameObjects đã có nhưng ta củng cố Cài Đặt)
        if (RenderSettings.skybox == null)
        {
            Material skyMat = new Material(Shader.Find("Skybox/Procedural"));
            skyMat.SetColor("_SkyTint", new Color(0.5f, 0.7f, 1f));
            skyMat.SetColor("_GroundColor", new Color(0.3f, 0.3f, 0.3f));
            RenderSettings.skybox = skyMat;
        }

        // 4. Set up Racers
        GameObject racersParent = new GameObject("--- Racers ---");

        GameObject playerRacer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerRacer.name = "PLAYER_RACER (Replace Me)";
        playerRacer.transform.SetParent(racersParent.transform);
        playerRacer.transform.position = new Vector3(-3, 1, 0); // Đẩy xa ra
        playerRacer.GetComponent<Renderer>().sharedMaterial = new Material(defaultMat) { color = Color.blue };
        RacingPlayer playerScript = playerRacer.AddComponent<RacingPlayer>();
        
        Rigidbody prb = playerRacer.AddComponent<Rigidbody>();
        prb.useGravity = false;
        prb.isKinematic = true;

        GameObject npcRacer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npcRacer.name = "NPC_RACER (Replace Me)";
        npcRacer.transform.SetParent(racersParent.transform);
        npcRacer.transform.position = new Vector3(3, 1, 0); // Đẩy xa ra
        npcRacer.GetComponent<Renderer>().sharedMaterial = new Material(defaultMat) { color = Color.yellow };
        RacingNPC npcScript = npcRacer.AddComponent<RacingNPC>();
        
        Rigidbody nrb = npcRacer.AddComponent<Rigidbody>();
        nrb.useGravity = false;
        nrb.isKinematic = true;

        // 5. Camera Setup (DETACHED)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.SetParent(null); // Tách khỏi Player
            mainCam.transform.position = new Vector3(-3f, 4f, -6f);
            mainCam.transform.rotation = Quaternion.Euler(20f, 0, 0);
            
            RacingCamera rCam = mainCam.gameObject.AddComponent<RacingCamera>();
            rCam.target = playerRacer.transform;
            rCam.offset = new Vector3(0, 4f, -6f);
        }

        // 6. UI Setup
        GameObject uiParent = new GameObject("--- UI System ---");
        
        GameObject canvasObj = new GameObject("RacingCanvas");
        canvasObj.transform.SetParent(uiParent.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Event System
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.transform.SetParent(uiParent.transform);
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // Texts
        GameObject countdownObj = CreateTextObj("CountdownText", canvasObj.transform, 100, Color.yellow, "3");
        countdownObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);

        string instructions = "BẤM LUÂN PHIÊN [Trái]/[Phải] HOẶC [A]/[D] ĐỂ CHẠY!\nNHẤN [SPACE] ĐỂ NHẢY QUA RÀO!";
        GameObject instructionsObj = CreateTextObj("InstructionsText", canvasObj.transform, 40, Color.white, instructions);
        instructionsObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);

        // End Panel
        GameObject endPanelObj = new GameObject("EndPanel");
        endPanelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = endPanelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        RectTransform panelRect = endPanelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one; 
        panelRect.sizeDelta = Vector2.zero;
        
        GameObject resultObj = CreateTextObj("ResultText", endPanelObj.transform, 80, Color.green, "CHIẾN THẮNG!");
        resultObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);

        // Nút Chơi lại
        GameObject playAgainBtnObj = DefaultControls.CreateButton(new DefaultControls.Resources());
        playAgainBtnObj.name = "PlayAgainButton";
        playAgainBtnObj.transform.SetParent(endPanelObj.transform, false);
        playAgainBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        playAgainBtnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);
        playAgainBtnObj.GetComponentInChildren<Text>().text = "Chơi Lại";
        playAgainBtnObj.GetComponentInChildren<Text>().fontSize = 20;

        // Nút Quay Về
        GameObject buttonObj = DefaultControls.CreateButton(new DefaultControls.Resources());
        buttonObj.name = "ReturnButton";
        buttonObj.transform.SetParent(endPanelObj.transform, false);
        buttonObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
        buttonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);
        buttonObj.GetComponentInChildren<Text>().text = "Quay Lại Game";
        buttonObj.GetComponentInChildren<Text>().fontSize = 20;

        // 7. GameManager Setup
        GameObject gmObj = new GameObject("RacingGameManager");
        RacingMinigameManager gm = gmObj.AddComponent<RacingMinigameManager>();
        
        gm.playerRacer = playerScript;
        gm.npcRacer = npcScript;
        gm.playerStartPos = pStartPos.transform;
        gm.npcStartPos = nStartPos.transform;
        gm.finishLine = finishLine.transform;
        gm.countdownText = countdownObj.GetComponent<TextMeshProUGUI>();
        gm.resultText = resultObj.GetComponent<TextMeshProUGUI>();
        gm.instructionsText = instructionsObj.GetComponent<TextMeshProUGUI>();
        gm.endPanel = endPanelObj;

        // Hook up buttons
        UnityEngine.UI.Button returnBtn = buttonObj.GetComponent<UnityEngine.UI.Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(returnBtn.onClick, gm.ReturnToMainScene);
        
        UnityEngine.UI.Button retryBtn = playAgainBtnObj.GetComponent<UnityEngine.UI.Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(retryBtn.onClick, gm.PlayAgain);

        // 8. Save
        string savePath = "Assets/Scenes/RacingMinigame.unity";
        
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(newScene, savePath);
        Debug.Log($"Scene saved successfully at {savePath}. Please add it to your Build Settings.");
    }

    private static GameObject CreateTextObj(string name, Transform parent, int fontSize, Color color, string initialText)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = initialText;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1000, 200);
        
        return obj;
    }
}
#endif
