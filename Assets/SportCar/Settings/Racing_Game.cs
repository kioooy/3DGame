#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class Startup
{
    static Startup()    
    {
        EditorPrefs.SetInt("showCounts_sportcarcgbr", EditorPrefs.GetInt("showCounts_sportcarcgbr") + 1);

        if (EditorPrefs.GetInt("showCounts_sportcarcgbr") == 1)       
        {
            Application.OpenURL("https://assetstore.unity.com/packages/slug/353370");
            // System.IO.File.Delete("Assets/SportCar/Racing_Game.cs");
        }
    }     
}
#endif
