using UnityEngine;
using UnityEditor;

public class EnvironmentSetupTool
{
    [MenuItem("Quest System/6. Setup Environment (Fog & Ambient)")]
    public static void SetupEnvironment()
    {
        // 1. Cài đặt Sương Mù Toàn Cục (Global Fog)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.015f;
        // Chỉnh sương mù ngả xanh rêu để hợp với rừng hoang dã
        RenderSettings.fogColor = new Color(0.18f, 0.25f, 0.20f, 1f); 
        
        Debug.Log("<color=green>[EnvironmentSetup]</color> Đã kích hoạt sương mù xanh bí ẩn (Global Fog).");

        // 2. Cài đặt Nguồn phát âm thanh nền (Ambient Noise)
        GameObject ambientObj = GameObject.Find("AmbientNoiseAudio");
        if (ambientObj == null)
        {
            ambientObj = new GameObject("AmbientNoiseAudio");
            AudioSource source = ambientObj.AddComponent<AudioSource>();
            
            // Lặp lại liên tục, âm lượng nền nhỏ để không lấn át nhạc BGM
            source.loop = true;
            source.volume = 0.4f; 
            source.playOnAwake = true;
            
            Undo.RegisterCreatedObjectUndo(ambientObj, "Create Ambient Audio");
            
            Debug.Log("<color=green>[EnvironmentSetup]</color> Đã tạo nguồn phát AmbientNoiseAudio.");
            EditorUtility.DisplayDialog("Setup Môi Trường", "Đã chèn Sương mù và tạo Loa Âm thanh nền.\n\nHãy bấm vào Object 'AmbientNoiseAudio' trên Hierarchy và kéo thả file âm thanh (tiếng gió / râm ran dế kêu) vào khe AudioClip của AudioSource!", "OK");
        }
        else
        {
            Debug.Log("<color=yellow>[EnvironmentSetup]</color> Nguồn phát Môi trường đã tồn tại.");
            EditorUtility.DisplayDialog("Setup Môi Trường", "Object AmbientNoiseAudio đã tồn tại trong Scene.", "OK");
        }
    }
}
