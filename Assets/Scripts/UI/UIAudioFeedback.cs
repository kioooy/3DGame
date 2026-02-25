using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton phát âm thanh UI: hover, click, tab switch, slider drag, confirm, error.
/// Tự tạo AudioSource. Không cần AudioClip bên ngoài – tự sinh procedural.
/// </summary>
public class UIAudioFeedback : MonoBehaviour
{
    public static UIAudioFeedback Instance { get; private set; }

    public enum SoundType
    {
        Hover,
        Click,
        Tab,
        SliderTick,
        Confirm,
        Error,
        Open,
        Close
    }

    [Header("Volume")]
    [Range(0f, 1f)] public float uiVolume = 0.45f;

    [Header("Custom Clips (tuỳ chọn – để trống dùng procedural)")]
    public AudioClip clipHover;
    public AudioClip clipClick;
    public AudioClip clipTab;
    public AudioClip clipSlider;
    public AudioClip clipConfirm;
    public AudioClip clipError;
    public AudioClip clipOpen;
    public AudioClip clipClose;

    AudioSource _src;
    AudioSource _srcLow;  // nguồn phụ để phát hover nhẹ hơn

    // Throttle hover sound
    float _lastHoverTime = -1f;
    const float HoverCooldown = 0.08f;

    // Slider throttle
    float _lastSliderTime = -1f;
    const float SliderCooldown = 0.04f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetupAudioSources();
    }

    void SetupAudioSources()
    {
        _src    = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake  = false;
        _src.spatialBlend = 0f; // 2D

        _srcLow = gameObject.AddComponent<AudioSource>();
        _srcLow.playOnAwake  = false;
        _srcLow.spatialBlend = 0f;
        _srcLow.volume       = 0.3f;
    }

    // ──────────────────────────────────────
    //   Static helper
    // ──────────────────────────────────────
    public static void Play(SoundType type)
    {
        if (Instance == null) return;
        Instance.PlaySound(type);
    }

    public static void PlaySlider(float value)
    {
        if (Instance == null) return;
        if (Time.realtimeSinceStartup - Instance._lastSliderTime < SliderCooldown) return;
        Instance._lastSliderTime = Time.realtimeSinceStartup;
        Instance.PlaySliderInternal(value);
    }

    // ──────────────────────────────────────
    //   Internal play logic
    // ──────────────────────────────────────
    void PlaySound(SoundType type)
    {
        switch (type)
        {
            case SoundType.Hover:
                if (Time.realtimeSinceStartup - _lastHoverTime < HoverCooldown) return;
                _lastHoverTime = Time.realtimeSinceStartup;
                if (clipHover) PlayClip(_srcLow, clipHover, uiVolume * 0.4f);
                else PlayProceduralHover();
                break;

            case SoundType.Click:
                if (clipClick) PlayClip(_src, clipClick, uiVolume);
                else PlayProceduralClick();
                break;

            case SoundType.Tab:
                if (clipTab) PlayClip(_src, clipTab, uiVolume * 0.8f);
                else PlayProceduralTab();
                break;

            case SoundType.SliderTick:
                if (clipSlider) PlayClip(_srcLow, clipSlider, uiVolume * 0.3f);
                else PlayProceduralSliderTick(0.5f);
                break;

            case SoundType.Confirm:
                if (clipConfirm) PlayClip(_src, clipConfirm, uiVolume);
                else PlayProceduralConfirm();
                break;

            case SoundType.Error:
                if (clipError) PlayClip(_src, clipError, uiVolume);
                else PlayProceduralError();
                break;

            case SoundType.Open:
                if (clipOpen) PlayClip(_src, clipOpen, uiVolume * 0.7f);
                else PlayProceduralOpen();
                break;

            case SoundType.Close:
                if (clipClose) PlayClip(_src, clipClose, uiVolume * 0.7f);
                else PlayProceduralClose();
                break;
        }
    }

    void PlaySliderInternal(float normalizedValue)
    {
        PlayProceduralSliderTick(normalizedValue);
    }

    void PlayClip(AudioSource src, AudioClip clip, float vol)
    {
        src.PlayOneShot(clip, vol);
    }

    // ──────────────────────────────────────
    //   Procedural Audio Generation
    //   Tạo âm thanh bằng code, không cần file .wav
    // ──────────────────────────────────────

    // Hover: tiếng "tik" nhẹ, pitch cao
    void PlayProceduralHover()
    {
        var clip = GenerateTone(0.04f, 1400f, 1600f, 0.18f, WaveShape.Sine, fadeOut: true);
        _srcLow.PlayOneShot(clip, uiVolume * 0.35f);
    }

    // Click: tiếng "click" cứng ngắn
    void PlayProceduralClick()
    {
        var clip = GenerateTone(0.07f, 900f, 600f, 0.6f, WaveShape.Square, fadeOut: true);
        _src.PlayOneShot(clip, uiVolume * 0.7f);
    }

    // Tab switch: tiếng "woosh" nhẹ
    void PlayProceduralTab()
    {
        var clip = GenerateTone(0.12f, 600f, 900f, 0.45f, WaveShape.Sine, fadeOut: true);
        _src.PlayOneShot(clip, uiVolume * 0.6f);
    }

    // Slider tick: tiếng nhỏ theo pitch (thấp → cao)
    void PlayProceduralSliderTick(float normalizedVal)
    {
        float freq = Mathf.Lerp(400f, 1200f, normalizedVal);
        var clip = GenerateTone(0.03f, freq, freq * 0.9f, 0.25f, WaveShape.Sine, fadeOut: true);
        _srcLow.PlayOneShot(clip, uiVolume * 0.25f);
    }

    // Confirm: hai nốt đi lên (success)
    void PlayProceduralConfirm()
    {
        StartCoroutine(PlaySequence(new float[] { 523f, 659f, 784f }, 0.08f, 0.55f));
    }

    // Error: tiếng "buzz" đi xuống
    void PlayProceduralError()
    {
        StartCoroutine(PlaySequence(new float[] { 400f, 280f }, 0.1f, 0.5f, WaveShape.Square));
    }

    // Open: tiếng sweep lên
    void PlayProceduralOpen()
    {
        var clip = GenerateTone(0.18f, 300f, 700f, 0.4f, WaveShape.Sine, fadeOut: false, fadeIn: true);
        _src.PlayOneShot(clip, uiVolume * 0.5f);
    }

    // Close: tiếng sweep xuống
    void PlayProceduralClose()
    {
        var clip = GenerateTone(0.14f, 650f, 250f, 0.4f, WaveShape.Sine, fadeOut: true);
        _src.PlayOneShot(clip, uiVolume * 0.5f);
    }

    // ──────────────────────────────────────
    //   Audio Generation utils
    // ──────────────────────────────────────
    enum WaveShape { Sine, Square, Triangle }

    AudioClip GenerateTone(float duration, float startFreq, float endFreq, float amplitude,
        WaveShape shape = WaveShape.Sine, bool fadeOut = false, bool fadeIn = false)
    {
        int sampleRate = 44100;
        int samples    = Mathf.RoundToInt(sampleRate * duration);
        var data       = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / samples;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float phase = 2f * Mathf.PI * freq * (float)i / sampleRate;

            float wave;
            switch (shape)
            {
                case WaveShape.Square:
                    wave = Mathf.Sin(phase) >= 0 ? 1f : -1f;
                    break;
                case WaveShape.Triangle:
                    wave = Mathf.Asin(Mathf.Sin(phase)) * (2f / Mathf.PI);
                    break;
                default: // Sine
                    wave = Mathf.Sin(phase);
                    break;
            }

            float env = amplitude;
            if (fadeIn  && t < 0.15f) env *= t / 0.15f;
            if (fadeOut && t > 0.5f)  env *= 1f - (t - 0.5f) / 0.5f;

            data[i] = wave * env;
        }

        var clip = AudioClip.Create("ProceduralTone", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    System.Collections.IEnumerator PlaySequence(float[] freqs, float noteDuration, float amplitude,
        WaveShape shape = WaveShape.Sine)
    {
        foreach (float f in freqs)
        {
            var clip = GenerateTone(noteDuration, f, f, amplitude, shape, fadeOut: true);
            _src.PlayOneShot(clip, uiVolume);
            yield return new WaitForSecondsRealtime(noteDuration * 0.8f);
        }
    }
}
