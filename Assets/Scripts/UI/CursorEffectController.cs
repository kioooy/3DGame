using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển hiệu ứng con trỏ chuột trong game.
/// Đặt vào bất kỳ GameObject nào trong scene (SettingsManager tự tìm).
/// </summary>
public class CursorEffectController : MonoBehaviour
{
    [Header("Trail Effect")]
    [Tooltip("Image Trail theo chuột – assign một Image trong Canvas")]
    public Image trailImage;
    public float trailFadeSpeed  = 6f;
    public float trailRadius     = 18f;

    [Header("Highlight Ring")]
    public Image highlightRing;
    public float highlightPulseSpeed = 2f;

    // ── Internal ──
    bool _trailEnabled;
    bool _highlight;
    bool _smooth;
    float _cursorScale = 1f;
    Vector3 _smoothPos;
    float _pulseTimer;

    // Virtual cursor position (dùng khi smooth bật)
    Vector3 _targetPos;

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;

        // ── Smooth cursor ──
        if (_smooth)
        {
            _smoothPos = Vector3.Lerp(_smoothPos, mousePos, Time.unscaledDeltaTime * 18f);
        }
        else
        {
            _smoothPos = mousePos;
        }

        // ── Trail effect ──
        if (trailImage != null)
        {
            if (_trailEnabled)
            {
                trailImage.gameObject.SetActive(true);
                trailImage.rectTransform.position = Vector3.Lerp(
                    trailImage.rectTransform.position, _smoothPos,
                    Time.unscaledDeltaTime * trailFadeSpeed);

                // Fade alpha
                Color c = trailImage.color;
                c.a = Mathf.Lerp(c.a, 0.6f, Time.unscaledDeltaTime * 8f);
                trailImage.color = c;
            }
            else
            {
                trailImage.gameObject.SetActive(false);
            }
        }

        // ── Highlight ring ──
        if (highlightRing != null)
        {
            if (_highlight)
            {
                highlightRing.gameObject.SetActive(true);
                highlightRing.rectTransform.position = _smoothPos;
                _pulseTimer += Time.unscaledDeltaTime * highlightPulseSpeed;
                float pulse = 0.7f + Mathf.Sin(_pulseTimer) * 0.15f;
                float s = _cursorScale * pulse;
                highlightRing.rectTransform.localScale = Vector3.one * s;
            }
            else
            {
                highlightRing.gameObject.SetActive(false);
            }
        }
    }

    // ──────────────────────────────────────
    //   Public API – gọi từ SettingsManager
    // ──────────────────────────────────────
    public void SetTrailEnabled(bool enabled)
    {
        _trailEnabled = enabled;
        if (trailImage != null && !enabled)
            trailImage.gameObject.SetActive(false);
    }

    public void SetSize(float scale)
    {
        _cursorScale = scale;
        // Nếu dùng custom cursor texture, scale ở đây
    }

    public void SetHighlight(bool enabled)
    {
        _highlight = enabled;
        if (highlightRing != null && !enabled)
            highlightRing.gameObject.SetActive(false);
    }

    public void SetStyle(int styleIndex)
    {
        // 0 = mặc định, 1 = tinh chỉnh, 2 = vòng tròn
        // Có thể swap CursorMode hoặc texture tại đây
        switch (styleIndex)
        {
            case 0: Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); break;
            case 1: Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware); break;
            default: Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); break;
        }
    }

    public void SetSmooth(bool enabled)
    {
        _smooth = enabled;
        if (!enabled) _smoothPos = Input.mousePosition;
    }
}
