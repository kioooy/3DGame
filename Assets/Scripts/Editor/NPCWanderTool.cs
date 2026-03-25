using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Tool: Gán / Gỡ NPCWanderController cho các NPC chọn từ Hierarchy.
/// Mở qua menu Tools > NPC Wander Tool.
/// </summary>
public class NPCWanderTool : EditorWindow
{
    // ── Cài đặt mặc định ──────────────────────────────────────────
    private float _wanderRadius   = 6f;
    private float _moveSpeed      = 2.5f;
    private float _rotSpeed       = 180f;
    private float _minWait        = 1.5f;
    private float _maxWait        = 4f;

    private Vector2 _scroll;
    private GUIStyle _headerStyle;
    private GUIStyle _npcBoxStyle;

    // ── Mở cửa sổ ─────────────────────────────────────────────────
    [MenuItem("Tools/🐛 NPC Wander Tool")]
    public static void OpenWindow()
    {
        NPCWanderTool win = GetWindow<NPCWanderTool>("NPC Wander Tool");
        win.minSize = new Vector2(340, 440);
        win.Show();
    }

    // ── Vẽ UI ──────────────────────────────────────────────────────
    private void OnGUI()
    {
        InitStyles();

        // ─ Tiêu đề
        EditorGUILayout.Space(6);
        GUILayout.Label("🐛  NPC Wander Tool", _headerStyle);
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox("Chọn các NPC trong Hierarchy, cấu hình tham số rồi bấm Gán / Gỡ.", MessageType.Info);
        EditorGUILayout.Space(8);

        // ─ Cài đặt mặc định
        GUILayout.Label("⚙️  Tham Số Mặc Định", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _wanderRadius = EditorGUILayout.FloatField("Bán kính lang thang (m)", _wanderRadius);
            _moveSpeed    = EditorGUILayout.FloatField("Tốc độ di chuyển (m/s)",  _moveSpeed);
            _rotSpeed     = EditorGUILayout.FloatField("Tốc độ xoay (°/s)",       _rotSpeed);
            EditorGUILayout.MinMaxSlider(
                new GUIContent($"Thời gian dừng ({_minWait:F1}s – {_maxWait:F1}s)"),
                ref _minWait, ref _maxWait, 0.5f, 15f);
        }

        EditorGUILayout.Space(10);

        // ─ Danh sách NPC đang chọn
        GameObject[] selected = Selection.gameObjects;
        GUILayout.Label($"📋  NPC Đã Chọn ({selected.Length})", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(160));
        if (selected.Length == 0)
        {
            EditorGUILayout.HelpBox("Chưa chọn NPC nào trong Hierarchy.", MessageType.Warning);
        }
        else
        {
            foreach (var go in selected)
            {
                bool hasWander = go.GetComponent<NPCWanderController>() != null;
                using (new EditorGUILayout.HorizontalScope(_npcBoxStyle))
                {
                    GUILayout.Label(hasWander ? "🟢" : "⚪", GUILayout.Width(20));
                    GUILayout.Label(go.name, GUILayout.ExpandWidth(true));
                    GUI.color = hasWander ? Color.yellow : Color.green;
                    if (GUILayout.Button(hasWander ? "Cấu hình" : "+ Gán", GUILayout.Width(75)))
                    {
                        ApplyToSingle(go);
                    }
                    GUI.color = hasWander ? new Color(1f,0.4f,0.4f) : Color.gray;
                    if (GUILayout.Button("✕ Gỡ", GUILayout.Width(55)))
                    {
                        RemoveFromSingle(go);
                    }
                    GUI.color = Color.white;
                }
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);

        // ─ Nút hành động hàng loạt
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.4f, 0.85f, 0.4f);
            if (GUILayout.Button("✅  Gán Tất Cả", GUILayout.Height(34)))
                ApplyToAll(selected);

            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑  Gỡ Tất Cả", GUILayout.Height(34)))
                RemoveFromAll(selected);

            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(6);

        // ─ Nút mở scene view ping
        if (selected.Length == 1 && selected[0].GetComponent<NPCWanderController>() != null)
        {
            if (GUILayout.Button("🎯  Ping trong Scene View"))
                EditorGUIUtility.PingObject(selected[0]);
        }

        // Auto-repaint khi chọn thay đổi
        Repaint();
    }

    // ── Logic gán ─────────────────────────────────────────────────
    private void ApplyToSingle(GameObject go)
    {
        Undo.RecordObject(go, "Add NPCWanderController");

        NPCWanderController wander = go.GetComponent<NPCWanderController>();
        if (wander == null)
            wander = Undo.AddComponent<NPCWanderController>(go);

        wander.wanderRadius  = _wanderRadius;
        wander.moveSpeed     = _moveSpeed;
        wander.rotationSpeed = _rotSpeed;
        wander.minWaitTime   = _minWait;
        wander.maxWaitTime   = _maxWait;

        EditorUtility.SetDirty(go);
        Debug.Log($"[NPCWanderTool] ✅ Đã gán NPCWanderController cho '{go.name}'");
    }

    private void RemoveFromSingle(GameObject go)
    {
        NPCWanderController wander = go.GetComponent<NPCWanderController>();
        if (wander != null)
        {
            Undo.DestroyObjectImmediate(wander);
            Debug.Log($"[NPCWanderTool] 🗑 Đã gỡ NPCWanderController khỏi '{go.name}'");
        }
    }

    private void ApplyToAll(GameObject[] targets)
    {
        if (targets.Length == 0) { EditorUtility.DisplayDialog("Thông báo", "Chưa chọn NPC nào!", "OK"); return; }
        foreach (var go in targets) ApplyToSingle(go);
    }

    private void RemoveFromAll(GameObject[] targets)
    {
        if (targets.Length == 0) { EditorUtility.DisplayDialog("Thông báo", "Chưa chọn NPC nào!", "OK"); return; }
        foreach (var go in targets) RemoveFromSingle(go);
    }

    // ── Khởi tạo Style ────────────────────────────────────────────
    private void InitStyles()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 15,
                alignment = TextAnchor.MiddleCenter,
                margin    = new RectOffset(0, 0, 4, 4)
            };
        }

        if (_npcBoxStyle == null)
        {
            _npcBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(6, 6, 4, 4),
                margin  = new RectOffset(0, 0, 2, 2)
            };
        }
    }

    // Repaint khi selection thay đổi
    private void OnSelectionChange() => Repaint();
}
