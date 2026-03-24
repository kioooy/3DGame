#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

/// <summary>
/// Tool re-encode video intro sang H.264 Baseline Profile
/// để Unity VideoPlayer hiển thị đúng (tránh màn hình trắng).
/// Menu: Tools > Fix Intro Video Encoding
/// </summary>
public class IntroVideoFixTool : EditorWindow
{
    private const string VIDEO_PATH = "Assets/Audio 1/pWF2AmF0GwAAAZ0OCBveYWYAYXUCYXMaADdiPg.mp4";
    private string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
    private bool isProcessing = false;
    private string statusMessage = "";

    [MenuItem("Tools/Fix Intro Video Encoding")]
    public static void ShowWindow()
    {
        var w = GetWindow<IntroVideoFixTool>("Fix Intro Video");
        w.minSize = new Vector2(420, 280);
        w.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("🎬 Fix Intro Video cho Unity VideoPlayer", EditorStyles.boldLabel);
        GUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Video bị màn hình trắng do encode H.264 không phải Baseline Profile.\n" +
            "Tool này dùng FFmpeg để re-encode sang định dạng Unity đọc được.",
            MessageType.Info);

        GUILayout.Space(10);

        // Kiểm tra video gốc
        string absVideoPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", VIDEO_PATH));
        bool videoExists = File.Exists(absVideoPath);
        EditorGUILayout.LabelField("File video:", VIDEO_PATH);
        EditorGUILayout.LabelField("Trạng thái:", videoExists ? "✅ Tìm thấy" : "❌ Không tìm thấy");

        GUILayout.Space(10);
        GUILayout.Label("Đường dẫn FFmpeg:", EditorStyles.boldLabel);
        ffmpegPath = EditorGUILayout.TextField(ffmpegPath);

        if (GUILayout.Button("📂 Tìm FFmpeg tự động"))
        {
            FindFFmpegAuto();
        }

        bool ffmpegExists = File.Exists(ffmpegPath);
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Trạng thái FFmpeg:", ffmpegExists ? "✅ Tìm thấy" : "❌ Không tìm thấy - Cần cài FFmpeg");
        EditorGUI.indentLevel--;

        if (!ffmpegExists)
        {
            EditorGUILayout.HelpBox(
                "Bạn cần cài FFmpeg:\n" +
                "1. Tải tại: https://ffmpeg.org/download.html\n" +
                "2. Giải nén vào C:\\ffmpeg\\\n" +
                "3. Nhập đường dẫn ffmpeg.exe vào ô trên.",
                MessageType.Warning);
        }

        GUILayout.Space(10);
        GUI.enabled = videoExists && ffmpegExists && !isProcessing;

        if (GUILayout.Button("🔄 Re-encode Video (Fix màn hình trắng)", GUILayout.Height(40)))
        {
            ReencodeVideo(absVideoPath);
        }

        GUI.enabled = true;

        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Space(5);
            EditorGUILayout.HelpBox(statusMessage, isProcessing ? MessageType.Info : MessageType.None);
        }
    }

    void FindFFmpegAuto()
    {
        string[] candidates = {
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe",
            @"C:\tools\ffmpeg\bin\ffmpeg.exe",
        };

        // Thử tìm trong PATH
        try
        {
            var proc = Process.Start(new ProcessStartInfo("where", "ffmpeg")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            string output = proc.StandardOutput.ReadLine();
            if (!string.IsNullOrEmpty(output) && File.Exists(output.Trim()))
            {
                ffmpegPath = output.Trim();
                statusMessage = "✅ Tìm thấy FFmpeg trong PATH: " + ffmpegPath;
                return;
            }
        }
        catch { }

        foreach (var c in candidates)
        {
            if (File.Exists(c)) { ffmpegPath = c; statusMessage = "✅ Tìm thấy: " + c; return; }
        }

        statusMessage = "❌ Không tìm thấy FFmpeg tự động. Hãy nhập đường dẫn thủ công.";
    }

    void ReencodeVideo(string inputPath)
    {
        // Output file: thêm "_fixed" vào tên
        string dir    = Path.GetDirectoryName(inputPath);
        string name   = Path.GetFileNameWithoutExtension(inputPath);
        string outPath = Path.Combine(dir, name + "_fixed.mp4");

        // FFmpeg args: re-encode H.264 Baseline + AAC audio, tương thích Unity
        string args = $"-y -i \"{inputPath}\" -c:v libx264 -profile:v baseline -level 3.0 " +
                      $"-preset medium -crf 23 -pix_fmt yuv420p -c:a aac -b:a 192k \"{outPath}\"";

        isProcessing = true;
        statusMessage = "⏳ Đang re-encode... vui lòng chờ.";
        Repaint();

        try
        {
            var psi = new ProcessStartInfo(ffmpegPath, args)
            {
                UseShellExecute        = false,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
            };
            var proc = Process.Start(psi);
            // Timeout 5 phút
            proc.WaitForExit(300_000);

            if (proc.ExitCode == 0 && File.Exists(outPath))
            {
                // Ghi đè file gốc bằng file đã fix
                File.Delete(inputPath);
                File.Move(outPath, inputPath);

                AssetDatabase.Refresh();
                statusMessage = "✅ Hoàn tất! File đã được re-encode. Chạy lại game để kiểm tra.";
                UnityEngine.Debug.Log("[IntroVideoFix] Re-encode thành công: " + inputPath);
            }
            else
            {
                string err = proc.StandardError.ReadToEnd();
                statusMessage = "❌ FFmpeg lỗi (exit " + proc.ExitCode + "):\n" + err.Substring(0, Mathf.Min(err.Length, 300));
                UnityEngine.Debug.LogError("[IntroVideoFix] FFmpeg error:\n" + err);
            }
        }
        catch (System.Exception ex)
        {
            statusMessage = "❌ Lỗi chạy FFmpeg: " + ex.Message;
            UnityEngine.Debug.LogError("[IntroVideoFix] Exception: " + ex);
        }
        finally
        {
            isProcessing = false;
            Repaint();
        }
    }
}
#endif
