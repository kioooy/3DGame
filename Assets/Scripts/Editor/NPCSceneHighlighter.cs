using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Vẽ tên NPC trong Scene View của Unity Editor (không cần Play).
/// Kích hoạt qua Tools > NPC Scene Highlighter.
/// </summary>
[InitializeOnLoad]
public static class NPCSceneHighlighter
{
    private static bool _enabled = true;
    private static Color _labelBg    = new Color(0.05f, 0.05f, 0.15f, 0.82f);
    private static Color _labelColor = new Color(1f, 0.92f, 0.3f);
    private static float _heightOffset = 2.4f;

    // Danh sách script NPC cần highlight (tên class)
    private static readonly string[] NpcScriptNames = {
        "ConKienNPC", "DeTruiNPC", "DeChoatNPC", "XenTocNPC", "VeSauNPC", "NPCNameTag"
    };

    static NPCSceneHighlighter()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        // Đọc trạng thái từ lần trước
        _enabled = EditorPrefs.GetBool("NPCSceneHighlighter_Enabled", true);
    }

    // ── Menu mở / tắt ─────────────────────────────────────────────
    [MenuItem("Tools/🏷️  NPC Scene Highlighter  %#h")]   // Ctrl+Shift+H
    public static void ToggleHighlighter()
    {
        _enabled = !_enabled;
        EditorPrefs.SetBool("NPCSceneHighlighter_Enabled", _enabled);
        string state = _enabled ? "BẬT ✅" : "TẮT ❌";
        Debug.Log($"[NPCSceneHighlighter] {state}");
        SceneView.RepaintAll();
    }

    [MenuItem("Tools/🏷️  NPC Scene Highlighter  %#h", true)]
    public static bool ToggleValidate()
    {
        Menu.SetChecked("Tools/🏷️  NPC Scene Highlighter  %#h", _enabled);
        return true;
    }

    // ── Vẽ trong Scene View ────────────────────────────────────────
    private static void OnSceneGUI(SceneView sv)
    {
        if (!_enabled) return;

        foreach (GameObject go in GetAllNPCsInScene())
        {
            if (go == null) continue;

            string label   = GetNPCLabel(go);
            Vector3 worldPos = go.transform.position + Vector3.up * _heightOffset;

            // ── Vẽ background box kiểu cũ qua Handles.BeginGUI
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
            bool isSelected   = ArrayUtility.Contains(Selection.gameObjects, go);

            Handles.BeginGUI();
            {
                GUIStyle style = new GUIStyle(GUI.skin.box)
                {
                    fontSize  = isSelected ? 13 : 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    padding   = new RectOffset(8, 8, 3, 3)
                };
                style.normal.textColor = isSelected
                    ? new Color(0.3f, 1f, 0.5f)  // Xanh lá khi chọn
                    : _labelColor;

                GUIContent content  = new GUIContent(label);
                Vector2     size    = style.CalcSize(content);
                Rect        rect    = new Rect(screenPos.x - size.x * 0.5f,
                                               screenPos.y - size.y - 4f,
                                               size.x, size.y);

                // Background mờ
                GUI.color = isSelected ? new Color(0.1f, 0.3f, 0.1f, 0.88f) : _labelBg;
                GUI.Box(rect, GUIContent.none);
                GUI.color = Color.white;

                // Text
                GUI.Label(rect, content, style);
            }
            Handles.EndGUI();

            // Đường kẻ từ pivot lên label
            Handles.color = isSelected
                ? new Color(0.3f, 1f, 0.5f, 0.7f)
                : new Color(1f, 0.92f, 0.3f, 0.4f);
            Handles.DrawLine(go.transform.position, worldPos);

            // Vòng tròn orbit khi chọn
            if (isSelected)
            {
                Handles.color = new Color(0.3f, 1f, 0.5f, 0.25f);
                Handles.DrawSolidDisc(go.transform.position, Vector3.up, 0.7f);
                Handles.color = new Color(0.3f, 1f, 0.5f, 0.8f);
                Handles.DrawWireDisc(go.transform.position, Vector3.up, 0.7f);
            }
        }
    }

    // ── Lấy label cho NPC ─────────────────────────────────────────
    private static string GetNPCLabel(GameObject go)
    {
        // Ưu tiên NPCNameTag.displayName
        var tag = go.GetComponent<NPCNameTag>();
        if (tag != null && !string.IsNullOrEmpty(tag.displayName))
            return $"👤 {tag.displayName}";

        // Thử GetDisplayName()
        foreach (var comp in go.GetComponents<MonoBehaviour>())
        {
            if (comp == null) continue;
            var method = comp.GetType().GetMethod("GetDisplayName");
            if (method != null)
            {
                var result = method.Invoke(comp, null);
                if (result is string s && !string.IsNullOrEmpty(s))
                    return $"👤 {s}";
            }
        }

        return $"👤 {go.name}";
    }

    // ── Thu thập tất cả NPC trong Scene ───────────────────────────
    private static List<GameObject> GetAllNPCsInScene()
    {
        var result = new List<GameObject>();

        // Lấy theo NPCNameTag (nếu sếp gắn)
        foreach (var tag in Object.FindObjectsByType<NPCNameTag>(FindObjectsSortMode.None))
            if (!result.Contains(tag.gameObject)) result.Add(tag.gameObject);

        // Lấy theo tên class NPC
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            string typeName = mb.GetType().Name;
            foreach (string npcName in NpcScriptNames)
            {
                if (typeName == npcName && !result.Contains(mb.gameObject))
                {
                    result.Add(mb.gameObject);
                    break;
                }
            }
        }

        return result;
    }
}
