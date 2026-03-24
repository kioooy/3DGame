using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Tool tự động phát hiện và gắn đúng script NPC (XenTocNPC / ConKienNPC / DeTruiNPC / DeChoatNPC)
/// dựa trên tên GameObject. Cũng tự clone ChatBubble và InteractionPromptUI từ NPC gốc.
/// Mở từ menu: GDC301 → Gán Script NPC Tự Động
/// </summary>
public class NPCScriptAssignTool : EditorWindow
{
    // Template để clone ChatBubble & Prompt
    private DeTruiNPC   templateDeTrui;
    private ConKienNPC  templateConKien;
    private XenTocNPC   templateXenToc;
    private DeChoatNPC  templateDeChoat;

    private Vector2 scrollPos;
    private List<string> log = new List<string>();

    [MenuItem("GDC301/Gán Script NPC Tự Động")]
    public static void ShowWindow()
    {
        var win = GetWindow<NPCScriptAssignTool>("NPC Script Assigner");
        win.minSize = new Vector2(420, 460);
    }

    private void OnGUI()
    {
        // ── Header ──────────────────────────────────────────────
        EditorGUILayout.LabelField("🦟  Gán Script NPC Tự Động", EditorStyles.whiteLargeLabel);
        EditorGUILayout.HelpBox(
            "Chọn các GameObject NPC trong Hierarchy, sau đó bấm nút bên dưới.\n" +
            "Tool sẽ dựa vào TÊN để gắn đúng script:\n" +
            "  • xentoc / xen → XenTocNPC\n" +
            "  • conkien / kien → ConKienNPC\n" +
            "  • detrui / trui → DeTruiNPC\n" +
            "  • dechoat / choat → DeChoatNPC",
            MessageType.Info);

        GUILayout.Space(8);

        // ── Template references (tùy chọn) ───────────────────────
        EditorGUILayout.LabelField("Template (để clone ChatBubble & PromptUI)", EditorStyles.boldLabel);
        templateDeTrui  = AssignTemplateField("Template Dế Trũi", templateDeTrui);
        templateConKien = AssignTemplateField("Template Côn Kiến", templateConKien);
        templateXenToc  = AssignTemplateField("Template Xén Tóc", templateXenToc);
        templateDeChoat = AssignTemplateField("Template Dế Choắt", templateDeChoat);

        // Auto-fill templates từ scene
        if (GUILayout.Button("Tự Tìm Template Từ Scene", GUILayout.Height(26)))
            AutoFillTemplates();

        GUILayout.Space(10);

        // ── Main button ──────────────────────────────────────────
        GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
        if (GUILayout.Button("▶  Gán Script Cho NPC Đã Chọn", GUILayout.Height(46)))
        {
            log.Clear();
            AssignScripts(Selection.gameObjects);
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(6);

        // Nút bổ sung: scan toàn scene
        GUI.backgroundColor = new Color(0.8f, 0.9f, 1f);
        if (GUILayout.Button("🔍  Scan & Gán Cho TẤT CẢ NPC Trong Scene", GUILayout.Height(36)))
        {
            log.Clear();
            var all = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            List<GameObject> candidates = new List<GameObject>();
            foreach (var go in all)
            {
                // Bỏ qua các GameObject con (như xương, cục mesh bên trong model, ChatBubble, UI, v.v.)
                // Chỉ nhận các Root GameObject hoặc những GameObject nằm ngay dưới Root Scene (thường là model)
                // Hoặc đơn giản: chỉ gán nếu có Animator (model thật) HOẶC là Transform cấp 0
                if (go.transform.parent != null && go.GetComponent<Animator>() == null)
                    continue;

                string n = go.name.ToLower();

                // Loại trừ những cụm từ phụ của xương rig, UI, camera
                if (n.Contains("camera") || n.Contains("rig") || n.Contains("bone") || n.Contains("ui") || n.Contains("canvas") || n.Contains("bubble"))
                    continue;

                if (n.Contains("xen") || n.Contains("kien") || n.Contains("trui") || n.Contains("choat"))
                    candidates.Add(go);
            }
            AssignScripts(candidates.ToArray());
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("Xóa Script Khỏi Toàn Bộ NPC Trong Scene", GUILayout.Height(25)))
        {
            RemoveAllNPCScripts();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);

        // ── Log ──────────────────────────────────────────────────
        EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(160));
        foreach (var line in log)
            EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────
    private T AssignTemplateField<T>(string label, T current) where T : MonoBehaviour
    {
        GameObject currentGO = current != null ? current.gameObject : null;
        GameObject newGO = (GameObject)EditorGUILayout.ObjectField(label, currentGO, typeof(GameObject), true);
        
        if (newGO != null)
        {
            T script = newGO.GetComponent<T>();
            if (script != null) return script;
            
            // Tự động gán script cho đối tượng kéo vào nếu nó chưa có
            script = newGO.AddComponent<T>();
            log.Add($"🔧 Đã tự động thêm Script {typeof(T).Name} vào {newGO.name} khi kéo thủ công.");
            return script;
        }
        return null;
    }
    private void AutoFillTemplates()
    {
        if (templateDeTrui  == null) templateDeTrui  = FindAnyObjectByType<DeTruiNPC>();
        if (templateConKien == null) templateConKien = FindAnyObjectByType<ConKienNPC>();
        if (templateXenToc  == null) templateXenToc  = FindAnyObjectByType<XenTocNPC>();
        if (templateDeChoat == null) templateDeChoat = FindAnyObjectByType<DeChoatNPC>();
        log.Add("✅ Tự tìm template xong.");
        Repaint();
    }

    private void RemoveAllNPCScripts()
    {
        int count = 0;
        var allXenToc = FindObjectsByType<XenTocNPC>(FindObjectsSortMode.None);
        foreach (var c in allXenToc) { DestroyImmediate(c); count++; }

        var allConKien = FindObjectsByType<ConKienNPC>(FindObjectsSortMode.None);
        foreach (var c in allConKien) { DestroyImmediate(c); count++; }

        var allDeTrui = FindObjectsByType<DeTruiNPC>(FindObjectsSortMode.None);
        foreach (var c in allDeTrui) { DestroyImmediate(c); count++; }

        var allDeChoat = FindObjectsByType<DeChoatNPC>(FindObjectsSortMode.None);
        foreach (var c in allDeChoat) { DestroyImmediate(c); count++; }

        log.Add($"🗑 Đã gỡ bỏ {count} script NPC khỏi Scene.");
        Repaint();
    }

    // ─────────────────────────────────────────────────────────────
    private void AssignScripts(GameObject[] objects)
    {
        if (objects == null || objects.Length == 0)
        {
            EditorUtility.DisplayDialog("Chú ý", "Chưa chọn GameObject nào trong Hierarchy!", "OK");
            return;
        }

        int count = 0;

        List<GameObject> allTargets = new List<GameObject>();
        foreach(var obj in objects)
        {
            if (obj == null) continue;
            Transform[] children = obj.GetComponentsInChildren<Transform>(true);
            foreach(var t in children)
            {
                if (!allTargets.Contains(t.gameObject))
                    allTargets.Add(t.gameObject);
            }
        }

        foreach (var obj in allTargets)
        {
            if (obj == null) continue;
            string nameLow = obj.name.ToLower();

            if (nameLow.Contains("camera") || nameLow.Contains("rig") || nameLow.Contains("bone") || nameLow.Contains("ui") || nameLow.Contains("canvas") || nameLow.Contains("bubble"))
                continue;

            bool isXenToc  = nameLow.Contains("xentoc") || nameLow.Contains("xen_toc") || nameLow.Contains("xen");
            bool isConKien = nameLow.Contains("conkien") || nameLow.Contains("con_kien") || nameLow.Contains("kien");
            bool isDeTrui  = nameLow.Contains("detrui")  || nameLow.Contains("de_trui")  || nameLow.Contains("trui");
            bool isDeChoat = nameLow.Contains("dechoat") || nameLow.Contains("de_choat") || nameLow.Contains("choat");

            if (!isXenToc && !isConKien && !isDeTrui && !isDeChoat)
            {
                if (System.Array.IndexOf(objects, obj) >= 0)
                    log.Add($"⏭ Bỏ qua '{obj.name}' – không nhận dạng được.");
                continue;
            }

            Undo.RecordObject(obj, "Assign NPC Script");

            // Xóa các script NPC cũ KHÔNG đúng loại để tránh conflict
            RemoveWrongScripts(obj, isXenToc, isConKien, isDeTrui, isDeChoat);

            // Gắn Components chung (Animator, Rigidbody, Collider, MinimapMarker)
            EnsureBaseComponents(obj);

            // Gắn đúng script NPC
            if (isXenToc)       SetupXenToc(obj);
            else if (isConKien) SetupConKien(obj);
            else if (isDeTrui)  SetupDeTrui(obj);
            else if (isDeChoat) SetupDeChoat(obj);

            EditorUtility.SetDirty(obj);
            count++;
            log.Add($"✅ '{obj.name}' → {GetScriptLabel(isXenToc, isConKien, isDeTrui, isDeChoat)}");
        }

        AssetDatabase.SaveAssets();
        log.Add($"─── Hoàn thành: {count}/{objects.Length} đối tượng ───");
        if (count > 0)
            EditorUtility.DisplayDialog("Xong!", $"Đã gán script cho {count} NPC.", "OK");
        Repaint();
    }

    // ─────────────────────────────────────────────────────────────
    private void RemoveWrongScripts(GameObject obj, bool wantXen, bool wantKien, bool wantTrui, bool wantChoat)
    {
        if (!wantXen)
        {
            var c = obj.GetComponent<XenTocNPC>();
            if (c != null) { Undo.DestroyObjectImmediate(c); log.Add($"  🗑 Xóa XenTocNPC khỏi '{obj.name}'"); }
        }
        if (!wantKien)
        {
            var c = obj.GetComponent<ConKienNPC>();
            if (c != null) { Undo.DestroyObjectImmediate(c); log.Add($"  🗑 Xóa ConKienNPC khỏi '{obj.name}'"); }
        }
        if (!wantTrui)
        {
            var c = obj.GetComponent<DeTruiNPC>();
            if (c != null) { Undo.DestroyObjectImmediate(c); log.Add($"  🗑 Xóa DeTruiNPC khỏi '{obj.name}'"); }
        }
        if (!wantChoat)
        {
            var c = obj.GetComponent<DeChoatNPC>();
            if (c != null) { Undo.DestroyObjectImmediate(c); log.Add($"  🗑 Xóa DeChoatNPC khỏi '{obj.name}'"); }
        }
    }

    private void EnsureBaseComponents(GameObject obj)
    {
        // Animator
        if (obj.GetComponent<Animator>() == null)
        {
            Undo.AddComponent<Animator>(obj);
            log.Add($"  ➕ Thêm Animator vào '{obj.name}'");
        }

        // Rigidbody
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = Undo.AddComponent<Rigidbody>(obj);
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.mass = 50f;
            log.Add($"  ➕ Thêm Rigidbody vào '{obj.name}'");
        }

        // Collider
        if (obj.GetComponent<Collider>() == null)
        {
            var cap = Undo.AddComponent<CapsuleCollider>(obj);
            AutoSizeCollider(obj, cap);
            log.Add($"  ➕ Thêm CapsuleCollider vào '{obj.name}'");
        }

        // MinimapMarker
        MinimapMarker marker = obj.GetComponent<MinimapMarker>();
        if (marker == null)
        {
            marker = Undo.AddComponent<MinimapMarker>(obj);
            marker.markerColor = new Color(0.8f, 0.2f, 0.8f);
            marker.heightOffset = 25f;
        }
    }

    private void AutoSizeCollider(GameObject obj, CapsuleCollider cap)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { cap.height = 1f; cap.center = Vector3.up * 0.5f; cap.radius = 0.3f; return; }

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        float lossyY = Mathf.Max(obj.transform.lossyScale.y, 0.001f);
        float realH  = bounds.size.y;
        cap.height = realH / lossyY;
        cap.center = new Vector3(0, cap.height / 2f, 0);
        cap.radius = cap.height / 3f;
    }

    // ─────────────────────────────────────────────────────────────
    private void SetupXenToc(GameObject obj)
    {
        XenTocNPC script = obj.GetComponent<XenTocNPC>();
        if (script == null) script = Undo.AddComponent<XenTocNPC>(obj);

        script.animator = obj.GetComponent<Animator>();
        CloneChatBubbleAndPrompt(obj, templateXenToc?.chatBubble, templateXenToc?.interactionPromptUI,
            (cb, pr) => { script.chatBubble = cb; script.interactionPromptUI = pr; });
    }

    private void SetupConKien(GameObject obj)
    {
        ConKienNPC script = obj.GetComponent<ConKienNPC>();
        if (script == null) script = Undo.AddComponent<ConKienNPC>(obj);

        script.animator = obj.GetComponent<Animator>();
        CloneChatBubbleAndPrompt(obj, templateConKien?.chatBubble, templateConKien?.interactionPromptUI,
            (cb, pr) => { script.chatBubble = cb; script.interactionPromptUI = pr; });
    }

    private void SetupDeTrui(GameObject obj)
    {
        DeTruiNPC script = obj.GetComponent<DeTruiNPC>();
        if (script == null) script = Undo.AddComponent<DeTruiNPC>(obj);

        script.animator = obj.GetComponent<Animator>();
        script.enableWandering = true;
        script.enableRacing    = true;
        script.enableCaro      = true;

        // Gán LayerMask mặc định
        script.groundLayer   = LayerMask.GetMask("Default", "Ground", "Terrain");
        script.obstacleLayer = LayerMask.GetMask("Default", "Obstacle");

        CloneChatBubbleAndPrompt(obj, templateDeTrui?.chatBubble, templateDeTrui?.interactionPromptUI,
            (cb, pr) => { script.chatBubble = cb; script.interactionPromptUI = pr; });
    }

    private void SetupDeChoat(GameObject obj)
    {
        DeChoatNPC script = obj.GetComponent<DeChoatNPC>();
        if (script == null) script = Undo.AddComponent<DeChoatNPC>(obj);

        script.animator = obj.GetComponent<Animator>();
        CloneChatBubbleAndPrompt(obj, templateDeChoat?.chatBubble, templateDeChoat?.interactionPromptUI,
            (cb, pr) => { script.chatBubble = cb; script.interactionPromptUI = pr; });
    }

    // ─────────────────────────────────────────────────────────────
    /// <summary>Tính chiều cao đỉnh đầu NPC để đặt UI chat đúng chỗ.</summary>
    private float GetHeadOffset(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 1.5f;
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        float lossyY = Mathf.Max(obj.transform.lossyScale.y, 0.001f);
        return (bounds.max.y - obj.transform.position.y + 0.5f) / lossyY;
    }

    private delegate void AssignCallback(ChatBubble chatBubble, GameObject promptUI);

    private void CloneChatBubbleAndPrompt(
        GameObject obj,
        ChatBubble srcBubble, GameObject srcPrompt,
        AssignCallback assign)
    {
        ChatBubble bubbleComp = obj.GetComponentInChildren<ChatBubble>();
        GameObject promptUI   = FindChildByName(obj, "InteractionPrompt");

        float headY = GetHeadOffset(obj);

        if (bubbleComp == null && srcBubble != null)
        {
            GameObject clone = CloneGameObject(srcBubble.gameObject, obj.transform);
            clone.name = "ChatBubble";
            clone.transform.localPosition = new Vector3(0, headY, 0);
            bubbleComp = clone.GetComponent<ChatBubble>();
            log.Add($"  ➕ Clone ChatBubble cho '{obj.name}'");
        }

        if (promptUI == null && srcPrompt != null)
        {
            GameObject clone = CloneGameObject(srcPrompt, obj.transform);
            clone.name = "InteractionPrompt";
            clone.transform.localPosition = new Vector3(0, headY, 0);
            promptUI = clone;
            log.Add($"  ➕ Clone InteractionPrompt cho '{obj.name}'");
        }

        assign(bubbleComp, promptUI);
    }

    private GameObject CloneGameObject(GameObject source, Transform parent)
    {
#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfAnyPrefab(source))
        {
            var prefabSrc = PrefabUtility.GetCorrespondingObjectFromSource(source);
            if (prefabSrc != null)
                return (GameObject)PrefabUtility.InstantiatePrefab(prefabSrc, parent);
        }
#endif
        return Instantiate(source, parent);
    }

    private GameObject FindChildByName(GameObject obj, string name)
    {
        Transform t = obj.transform.Find(name);
        return t != null ? t.gameObject : null;
    }

    private string GetScriptLabel(bool xen, bool kien, bool trui, bool choat)
    {
        if (xen)  return "XenTocNPC";
        if (kien) return "ConKienNPC";
        if (trui) return "DeTruiNPC";
        if (choat) return "DeChoatNPC";
        return "?";
    }
}
