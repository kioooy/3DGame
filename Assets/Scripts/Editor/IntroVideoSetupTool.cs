using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class IntroVideoSetupTool : EditorWindow
{
    [MenuItem("Window/Game Setup/Setup Intro Video UI")]
    public static void CreateIntroVideoHierarchy()
    {
        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.name != "StylizedNatureLite_Demo")
        {
            if (!EditorUtility.DisplayDialog("Kiểm tra Scene", "Hệ thống phát hiện sếp không ở Scene StylizedNatureLite_Demo. Sếp có chắc muốn cài Intro Video vào Scene này không?", "Cứ cài", "Thôi bỏ"))
            {
                return;
            }
        }

        string videoPath = "Assets/Audio 1/pWF2AmF0GwAAAZ0OCBveYWYAYXUCYXMaADdiPg.mp4";
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(videoPath);
        if (clip == null)
        {
            Debug.LogError($"[IntroVideoSetup] Không tìm thấy tệp video tại: {videoPath} - Hãy kiểm tra lại đường dẫn!");
        }

        GameObject oldCanvas = GameObject.Find("IntroVideoCanvas");
        if (oldCanvas != null) DestroyImmediate(oldCanvas);

        GameObject canvasGeo = new GameObject("IntroVideoCanvas");
        Canvas canvas = canvasGeo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; 
        
        CanvasScaler scaler = canvasGeo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGeo.AddComponent<GraphicRaycaster>(); 

        GameObject bgGeo = new GameObject("Background");
        bgGeo.transform.SetParent(canvasGeo.transform, false);
        RawImage bgTarget = bgGeo.AddComponent<RawImage>();
        bgTarget.color = Color.white; 
        
        RectTransform bgRT = bgGeo.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        VideoPlayer vp = bgGeo.AddComponent<VideoPlayer>();
        vp.clip = clip;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.playOnAwake = true;
        vp.isLooping = false;
        vp.audioOutputMode = VideoAudioOutputMode.Direct; 

        GameObject btnGeo = new GameObject("SkipButton");
        btnGeo.transform.SetParent(canvasGeo.transform, false);
        Image btnImg = btnGeo.AddComponent<Image>();
        btnImg.color = new Color(0, 0, 0, 0.7f); 
        Button btn = btnGeo.AddComponent<Button>();

        RectTransform btnRT = btnGeo.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0); 
        btnRT.anchorMax = new Vector2(1, 0);
        btnRT.pivot = new Vector2(1, 0);
        btnRT.anchoredPosition = new Vector2(-50, 50); 
        btnRT.sizeDelta = new Vector2(250, 70);

        GameObject textGeo = new GameObject("Text");
        textGeo.transform.SetParent(btnGeo.transform, false);
        UnityEngine.UI.Text txt = textGeo.AddComponent<UnityEngine.UI.Text>();
        txt.text = "BỎ QUA KHÚC NÀY  >>";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        RectTransform textRT = textGeo.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        IntroVideoController controller = canvasGeo.AddComponent<IntroVideoController>();
        controller.videoCanvas = canvasGeo;
        controller.videoPlayer = vp;
        controller.displayImage = bgTarget;
        controller.skipButton = btn;

        EditorSceneManager.MarkSceneDirty(currentScene);
        Debug.Log("[IntroVideoSetup] Đã SETUP thành công Video Intro! \nSếp hãy lưu lại Scene và bật Play để tận hưởng thành quả.");
    }
}
