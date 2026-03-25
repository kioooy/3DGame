using UnityEngine;
using TMPro;

/// <summary>
/// Hiển thị tên nhân vật nổi trên đầu NPC trong game (world-space).
/// Gắn vào NPC GameObject cùng với script NPC. Tự động lấy tên từ GetDisplayName() nếu có.
/// </summary>
[ExecuteAlways]
public class NPCNameTag : MonoBehaviour
{
    [Header("Tên Hiển Thị")]
    [Tooltip("Để trống để tự lấy từ GetDisplayName() của NPC script")]
    public string displayName = "";

    [Header("Vị Trí")]
    [Tooltip("Độ cao của tag so với pivot NPC (m)")]
    public float heightOffset = 2.4f;

    [Header("Kiểu Dáng")]
    public Color nameColor       = new Color(1f, 0.92f, 0.3f);   // Vàng sáng
    public Color bgColor         = new Color(0f, 0f, 0f, 0.55f);
    public int   fontSize        = 22;
    public bool  alwaysFaceCamera = true;

    [Header("Khoảng Cách Ẩn")]
    [Tooltip("Ẩn tag khi player ở xa hơn mức này (0 = luôn hiện)")]
    public float hideDistance    = 20f;

    // ── Runtime state
    private GameObject  _tagRoot;
    private TextMeshPro _tmp;
    private SpriteRenderer _bg;
    private Camera      _cam;

    // ──────────────────────────────────────────────────────────────
    void Start()
    {
        BuildTag();
    }

    void OnEnable()  { if (_tagRoot == null) BuildTag(); _tagRoot?.SetActive(true); }
    void OnDisable() { _tagRoot?.SetActive(false); }
    void OnDestroy() { if (_tagRoot != null) DestroyImmediate(_tagRoot); }

    // ──────────────────────────────────────────────────────────────
    void BuildTag()
    {
        // Tạo child object chứa tag
        _tagRoot = new GameObject($"_NameTag_{gameObject.name}");
        _tagRoot.transform.SetParent(transform, false);
        _tagRoot.transform.localPosition = new Vector3(0, heightOffset, 0);

        // TextMeshPro 3D
        _tmp                 = _tagRoot.AddComponent<TextMeshPro>();
        _tmp.text            = ResolveDisplayName();
        _tmp.fontSize        = fontSize;
        _tmp.color           = nameColor;
        _tmp.alignment       = TextAlignmentOptions.Center;
        _tmp.outlineWidth    = 0.2f;
        _tmp.outlineColor    = Color.black;
        _tmp.fontStyle       = FontStyles.Bold;
        _tmp.sortingOrder    = 10;
        _tmp.rectTransform.sizeDelta = new Vector2(4, 0.8f);

        // Không bắt shadow/light
        _tagRoot.layer = LayerMask.NameToLayer("UI");
    }

    // ──────────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (_tmp == null) return;

        // Cập nhật tên nếu chưa chắc
        _tmp.text  = ResolveDisplayName();
        _tmp.color = nameColor;

        // Tìm camera chính
        if (_cam == null) _cam = Camera.main;

        // Quay nhìn về camera
        if (alwaysFaceCamera && _cam != null)
        {
            _tagRoot.transform.rotation = _cam.transform.rotation;
        }

        // Ẩn theo khoảng cách
        if (hideDistance > 0f && _cam != null)
        {
            float dist = Vector3.Distance(_cam.transform.position, transform.position);
            _tagRoot.SetActive(dist <= hideDistance);
        }
    }

    // ──────────────────────────────────────────────────────────────
    string ResolveDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;

        // Thử gọi GetDisplayName() từ NPC script bất kỳ
        var method = GetComponent<MonoBehaviour>()?.GetType().GetMethod("GetDisplayName");
        if (method != null)
        {
            var result = method.Invoke(GetComponent<MonoBehaviour>(), null);
            if (result is string s && !string.IsNullOrEmpty(s)) return s;
        }

        // Thử lấy field npcName
        var field = GetComponent<MonoBehaviour>()?.GetType().GetField("npcName");
        if (field != null)
        {
            var v = field.GetValue(GetComponent<MonoBehaviour>());
            if (v is string sn && !string.IsNullOrEmpty(sn)) return sn;
        }

        return gameObject.name;
    }

    // ── Gizmo trong Editor: dot màu trên NPC
    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        Gizmos.color = nameColor;
        Gizmos.DrawSphere(transform.position + Vector3.up * heightOffset, 0.08f);
#endif
    }
}
