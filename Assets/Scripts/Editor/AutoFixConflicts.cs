using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

[InitializeOnLoad]
public class AutoFixConflicts
{
    static AutoFixConflicts()
    {
        EditorApplication.delayCall += FixConflicts;
    }

    static void FixConflicts()
    {
        if (EditorPrefs.GetBool("AutoFixConflictsDone", false)) return;
        EditorPrefs.SetBool("AutoFixConflictsDone", true);

        string folderPath = "Assets/Environment/JC_StylizedNature_Lite/Scenes";
        if (!Directory.Exists(folderPath)) return;

        string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        int count = 0;
        foreach (string file in files)
        {
            if (file.EndsWith(".scenetemplate") || file.EndsWith(".unity"))
            {
                try
                {
                    string content = File.ReadAllText(file);
                    if (content.Contains("<<<<<<< HEAD"))
                    {
                        // Dùng Regex tìm và giữ lại bản code của HEAD, xóa đi đoạn ======= đến >>>>>>>
                        string pattern = @"<<<<<<< HEAD\r?\n(.*?)=======\r?\n.*?\r?\n>>>>>>> [^\r\n]*\r?\n";
                        string newContent = Regex.Replace(content, pattern, "$1", RegexOptions.Singleline);
                        
                        if (newContent != content)
                        {
                            File.WriteAllText(file, newContent, System.Text.Encoding.UTF8);
                            count++;
                            Debug.Log("[AutoFix] Đã sửa lỗi Merge Conflict trong file: " + file);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Không thể sửa file " + file + ": " + e.Message);
                }
            }
        }
        
        if (count > 0)
        {
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>✅ Đã tự động dọn dẹp xong {count} file bị lỗi chữ Git (Merge Conflict) trong thư mục Demo!</color>");
        }
    }
}
