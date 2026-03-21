using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quản lý Toast Notification (Thông báo trượt chữ) khi vô tình phát hiện được
/// côn trùng mới và được chép vào Bestiary. Giúp gameplay không bị gián đoạn.
/// </summary>
public class EncyclopediaNotificationUI : MonoBehaviour
{
    public static EncyclopediaNotificationUI Instance { get; private set; }

    [Header("UI Components")]
    public RectTransform container;
    public TextMeshProUGUI titleTxt; // Thường ghi "Sinh vật mới:"
    public TextMeshProUGUI insectNameTxt;
    public Image insectIcon;

    [Header("Animation")]
    public float showDuration = 3f;
    private Vector2 hiddenPos = new Vector2(-400, -80);
    private Vector2 shownPos = new Vector2(20, 20);

    private Queue<InsectData> _queue = new Queue<InsectData>();
    private bool _isShowing = false;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        if (container != null) 
        {
            container.pivot = new Vector2(0, 0); // Neo góc dưới bên trái
            container.anchorMin = new Vector2(0, 0);
            container.anchorMax = new Vector2(0, 0);
            container.anchoredPosition = hiddenPos;
        }
        
        if (EncyclopediaManager.Instance != null)
        {
            EncyclopediaManager.Instance.OnInsectUnlocked += HandleUnlock;
        }
    }

    void OnDestroy()
    {
        if (EncyclopediaManager.Instance != null)
        {
            EncyclopediaManager.Instance.OnInsectUnlocked -= HandleUnlock;
        }
    }

    private void HandleUnlock(InsectData data)
    {
        _queue.Enqueue(data);
        if (!_isShowing && gameObject.activeInHierarchy) 
            StartCoroutine(ProcessQueue());
    }

    IEnumerator ProcessQueue()
    {
        _isShowing = true;
        while (_queue.Count > 0)
        {
            var data = _queue.Dequeue();
            if (insectNameTxt != null) insectNameTxt.text = data.insectName;
            if (insectIcon != null && data.unlockedSprite != null) insectIcon.sprite = data.unlockedSprite;

            // Slide In
            float t = 0;
            while (t < 1)
            {
                t += Time.unscaledDeltaTime * 3f; // Dùng unscaled để lỡ pause vẫn trượt
                container.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, EaseOut(Mathf.Clamp01(t)));
                yield return null;
            }

            // Có thể play 1 sound TING nho nhỏ báo hiệu 
            if (UIAudioFeedback.Instance != null) UIAudioFeedback.Play(UIAudioFeedback.SoundType.Confirm); 

            // Wait x seconds
            yield return new WaitForSecondsRealtime(showDuration);

            // Slide Out
            t = 0;
            while (t < 1)
            {
                t += Time.unscaledDeltaTime * 3f;
                container.anchoredPosition = Vector2.Lerp(shownPos, hiddenPos, EaseIn(Mathf.Clamp01(t)));
                yield return null;
            }
        }
        _isShowing = false;
    }

    float EaseOut(float t) => 1 - Mathf.Pow(1 - t, 3);
    float EaseIn(float t) => t * t * t;
}
