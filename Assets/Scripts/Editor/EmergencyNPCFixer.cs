using UnityEngine;
using UnityEditor;

public class EmergencyNPCFixer : EditorWindow
{
    [MenuItem("Window/Quest System/3. Emergency Fix NPCs In Scene")]
    public static void FixNPCs()
    {
        FixXenToc();
        FixDeTrui();
        FixConKien();
        FixDeChoat();
        Debug.Log("<color=green>[EmergencyNPCFixer]</color> Đã quét và sửa lỗi cho tất cả 4 NPC chính trong Scene!");
        EditorUtility.DisplayDialog("Thành công", "Đã quét và sửa lỗi Animator/Script cho tất cả NPC trong Scene hiện tại. Vui lòng check Console.", "OK");
    }

    [MenuItem("Window/Quest System/4. Restore Original NPC UI")]
    public static void RestoreOldUI()
    {
        RestoreUI("Xén Tóc", "XenToc");
        RestoreUI("Dế Trũi", "DeTrui");
        RestoreUI("Côn Kiến", "ConKien");
        RestoreUI("Dế Choắt", "DeChoat");
        EditorUtility.DisplayDialog("Khôi phục UI", "Đã xóa InteractionUI rác và tự auto-link lại UI gốc của bạn (nếu có).", "OK");
    }

    static void RestoreUI(string name1, string name2)
    {
        GameObject obj = GameObject.Find(name1) ?? GameObject.Find(name2);
        if (obj == null) return;
        
        // Xoá cái ui cũ do tool tạo
        Transform badUI = obj.transform.Find("InteractionUI");
        if (badUI != null) DestroyImmediate(badUI.gameObject);

        // Tìm 1 UI text/canvas khác để gán lại
        GameObject goodUI = null;
        Canvas[] canvases = obj.GetComponentsInChildren<Canvas>(true);
        if (canvases.Length > 0) goodUI = canvases[0].gameObject;

        if (goodUI != null)
        {
            var x = obj.GetComponent<XenTocNPC>(); if (x != null) x.interactionPromptUI = goodUI;
            var t = obj.GetComponent<DeTruiNPC>(); if (t != null) t.interactionPromptUI = goodUI;
            var k = obj.GetComponent<ConKienNPC>(); if (k != null) k.interactionPromptUI = goodUI;
            var c = obj.GetComponent<DeChoatNPC>(); if (c != null) c.interactionPromptUI = goodUI;
            Debug.Log($"[Restore] Đã khôi phục giao diện {goodUI.name} cho {obj.name}");
        }
        EditorUtility.SetDirty(obj);
    }

    static void FixXenToc()
    {
        GameObject obj = GameObject.Find("Xén Tóc") ?? GameObject.Find("XenToc");
        if (obj == null) return;

        XenTocNPC script = obj.GetComponent<XenTocNPC>();
        if (script == null) script = obj.AddComponent<XenTocNPC>();

        SetupAnimator(obj);
        SetupUI(obj, script);
        EditorUtility.SetDirty(obj);
    }

    static void FixDeTrui()
    {
        GameObject obj = GameObject.Find("Dế Trũi") ?? GameObject.Find("DeTrui");
        if (obj == null) {
            // Thử tìm bằng tag hoặc regex nếu cần, nhưng tạm thời dùng Find.
            return;
        }

        DeTruiNPC script = obj.GetComponent<DeTruiNPC>();
        if (script == null) script = obj.AddComponent<DeTruiNPC>();

        // De Trui cần Rigidbody
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
            rb.mass = 50f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        SetupAnimator(obj);
        SetupUI(obj, null, script);
        EditorUtility.SetDirty(obj);
    }

    static void FixConKien()
    {
        GameObject obj = GameObject.Find("Côn Kiến") ?? GameObject.Find("ConKien");
        if (obj == null) return;

        ConKienNPC script = obj.GetComponent<ConKienNPC>();
        if (script == null) script = obj.AddComponent<ConKienNPC>();

        SetupAnimator(obj);
        SetupUI(obj, null, null, script);
        EditorUtility.SetDirty(obj);
    }

    static void FixDeChoat()
    {
        GameObject obj = GameObject.Find("Dế Choắt") ?? GameObject.Find("DeChoat");
        if (obj == null) return;

        DeChoatNPC script = obj.GetComponent<DeChoatNPC>();
        if (script == null) script = obj.AddComponent<DeChoatNPC>();

        SetupAnimator(obj);
        SetupUI(obj, null, null, null, script);
        EditorUtility.SetDirty(obj);
    }

    static void SetupAnimator(GameObject obj)
    {
        Animator anim = obj.GetComponent<Animator>();
        if (anim == null) anim = obj.AddComponent<Animator>();
        
        if (anim.runtimeAnimatorController == null)
        {
            // Try to find an animator controller with the same name as the object
            string[] guids = AssetDatabase.FindAssets(obj.name + " t:AnimatorController");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                Debug.Log($"[Fixer] Đã gắn AnimatorController tự động cho {obj.name}");
            }
            else
            {
                Debug.LogWarning($"[Fixer] {obj.name} đang thiếu AnimatorController! Hãy gán bằng tay nếu animation bị lỗi.");
            }
        }
    }

    static void SetupUI(GameObject obj, XenTocNPC x = null, DeTruiNPC t = null, ConKienNPC k = null, DeChoatNPC c = null)
    {
        Transform uiTransform = obj.transform.Find("InteractionUI");
        if (uiTransform == null || uiTransform.gameObject == null)
        {
            GameObject ui = new GameObject("InteractionUI");
            ui.transform.SetParent(obj.transform);
            ui.transform.localPosition = new Vector3(0, 2f, 0);
            
            // Adding Canvas turns Transform into RectTransform, destroying the old Transform!
            Canvas canvas = ui.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);
            canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            var text = ui.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = obj.name;
            text.fontSize = 50;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            
            ui.SetActive(false); 
            uiTransform = ui.transform; // Refetch AFTER Canvas has potentially swapped it
        }

        if (x != null && x.interactionPromptUI == null) { x.interactionPromptUI = uiTransform.gameObject; x.animator = obj.GetComponent<Animator>(); }
        if (t != null && t.interactionPromptUI == null) { t.interactionPromptUI = uiTransform.gameObject; t.animator = obj.GetComponent<Animator>(); }
        if (k != null && k.interactionPromptUI == null) { k.interactionPromptUI = uiTransform.gameObject; k.animator = obj.GetComponent<Animator>(); }
        if (c != null && c.interactionPromptUI == null) { c.interactionPromptUI = uiTransform.gameObject; c.animator = obj.GetComponent<Animator>(); }
    }
}
