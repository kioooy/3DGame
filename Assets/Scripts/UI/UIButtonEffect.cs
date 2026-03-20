using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Gắn vào bất kỳ Button/Slider/Toggle nào để thêm hiệu ứng hover + click sinh động.
/// Tự động tìm UIAudioFeedback trong scene để phát âm thanh.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIButtonEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Animation")]
    [Tooltip("Scale khi hover (1.0 = bình thường)")]
    public float hoverScale     = 1.08f;
    [Tooltip("Scale khi nhấn")]
    public float pressScale     = 0.93f;
    [Tooltip("Tốc độ animation")]
    public float animSpeed      = 12f;

    [Header("Color Tint")]
    [Tooltip("Màu overlay khi hover (alpha = cường độ)")]
    public Color hoverTint      = new Color(1f, 1f, 1f, 0.12f);
    [Tooltip("Màu overlay khi nhấn")]
    public Color pressTint      = new Color(0f, 0f, 0f, 0.15f);

    [Header("Glow / Outline Highlight")]
    [Tooltip("Bật glow khi hover")]
    public bool enableGlow      = true;
    public Color glowColor      = new Color(0.4f, 1f, 0.6f, 0.7f);

    [Header("Ripple Effect")]
    [Tooltip("Bật ripple khi click")]
    public bool enableRipple    = true;
    public Color rippleColor    = new Color(1f, 1f, 1f, 0.35f);

    [Header("Audio")]
    public bool playHoverSound  = true;
    public bool playClickSound  = true;

    // ── Internal ──
    RectTransform _rect;
    Graphic _graphic;
    Outline _outline;
    Color _originalTint = Color.white;
    Vector3 _targetScale = Vector3.one;
    bool _isHovered, _isPressed;
    bool _initialized;

    void Awake()
    {
        _rect    = GetComponent<RectTransform>();
        _graphic = GetComponent<Graphic>();
        _outline = GetComponent<Outline>();
        _originalTint = (_graphic != null) ? _graphic.color : Color.white;
        _initialized = true;
    }

    void OnEnable()
    {
        // Re-sync color mỗi lần enable (sau khi SettingsUI refresh màu tab)
        if (_initialized && _graphic != null && !_isHovered && !_isPressed)
            _originalTint = _graphic.color;
    }

    void Update()
    {
        // Smooth scale lerp
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, animSpeed * Time.unscaledDeltaTime);
    }

    // ──────────────────────────────────────
    //   Pointer Events
    // ──────────────────────────────────────
    public void OnPointerEnter(PointerEventData _)
    {
        _isHovered = true;
        _targetScale = Vector3.one * hoverScale;

        if (_graphic != null)
            _graphic.color = BlendColor(_originalTint, hoverTint);

        if (enableGlow && _outline != null)
        {
            _outline.enabled = true;
            _outline.effectColor = glowColor;
            _outline.effectDistance = new Vector2(3, -3);
        }

        if (playHoverSound)
            UIAudioFeedback.Play(UIAudioFeedback.SoundType.Hover);
    }

    public void OnPointerExit(PointerEventData _)
    {
        _isHovered = false;
        _isPressed = false;
        _targetScale = Vector3.one;

        if (_graphic != null)
            _graphic.color = _originalTint;

        if (_outline != null)
            _outline.enabled = false;
    }

    public void OnPointerDown(PointerEventData _)
    {
        _isPressed = true;
        _targetScale = Vector3.one * pressScale;

        if (_graphic != null)
            _graphic.color = BlendColor(_originalTint, pressTint);

        if (enableRipple)
            SpawnRipple();

        if (playClickSound)
            UIAudioFeedback.Play(UIAudioFeedback.SoundType.Click);
    }

    public void OnPointerUp(PointerEventData _)
    {
        _isPressed = false;
        _targetScale = _isHovered ? Vector3.one * hoverScale : Vector3.one;

        if (_graphic != null)
            _graphic.color = _isHovered ? BlendColor(_originalTint, hoverTint) : _originalTint;
    }

    // ──────────────────────────────────────
    //   Ripple Effect
    // ──────────────────────────────────────
    void SpawnRipple()
    {
        var rippleGO = new GameObject("Ripple");
        rippleGO.transform.SetParent(transform, false);
        rippleGO.transform.SetAsFirstSibling();

        var img = rippleGO.AddComponent<Image>();
        img.color = rippleColor;
        img.raycastTarget = false;
        // Không dùng sprite – hình vuông mờ đủ đẹp cho ripple effect

        var rRect = rippleGO.GetComponent<RectTransform>();
        rRect.anchorMin = rRect.anchorMax = new Vector2(0.5f, 0.5f);
        rRect.pivot     = new Vector2(0.5f, 0.5f);
        rRect.sizeDelta = Vector2.zero;

        StartCoroutine(AnimateRipple(rippleGO, img, rRect));
    }

    IEnumerator AnimateRipple(GameObject go, Image img, RectTransform rRect)
    {
        float t = 0f;
        // Đảm bảo maxSize luôn > 0 kể cả khi rect chưa layout xong
        float maxSize = Mathf.Max(_rect.rect.width, _rect.rect.height, 60f) * 2f;
        Color startColor = rippleColor;
        Color endColor = new Color(rippleColor.r, rippleColor.g, rippleColor.b, 0f);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 3f;
            float ease = 1f - (1f - Mathf.Clamp01(t)) * (1f - Mathf.Clamp01(t));
            rRect.sizeDelta = Vector2.one * Mathf.Lerp(0, maxSize, ease);
            img.color = Color.Lerp(startColor, endColor, Mathf.Clamp01(t));
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // ──────────────────────────────────────
    //   Helpers
    // ──────────────────────────────────────
    Color BlendColor(Color base_, Color tint)
    {
        return new Color(
            Mathf.Clamp01(base_.r + (tint.r - 0.5f) * 2f * tint.a),
            Mathf.Clamp01(base_.g + (tint.g - 0.5f) * 2f * tint.a),
            Mathf.Clamp01(base_.b + (tint.b - 0.5f) * 2f * tint.a),
            base_.a
        );
    }

    // ──────────────────────────────────────
    //   Public API – gọi từ bên ngoài
    // ──────────────────────────────────────
    /// <summary>Chơi animation bounce nhẹ (gọi khi cập nhật thành công)</summary>
    public void PlayBounce()
    {
        StopAllCoroutines();
        StartCoroutine(BounceCoroutine());
    }

    IEnumerator BounceCoroutine()
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 6f;
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
            transform.localScale = Vector3.one * s;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}
