using UnityEngine;
using System.Collections;

/// <summary>
/// Quản lý nhạc nền theo phong cách Minecraft:
/// - Nhạc yên tĩnh, ambient, phát ngẫu nhiên với khoảng nghỉ giữa các bài
/// - Fade in / Fade out mượt mà
/// - Hỗ trợ nhiều track
/// </summary>
public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance { get; private set; }

    [Header("Music Tracks (kéo file nhạc vào đây)")]
    public AudioClip[] musicTracks;

    [Header("Minecraft-Style Settings")]
    [Tooltip("Thời gian nghỉ tối thiểu giữa 2 bài (giây) - Minecraft style: im lặng lâu")]
    public float minSilenceTime = 60f;
    [Tooltip("Thời gian nghỉ tối đa giữa 2 bài (giây)")]
    public float maxSilenceTime = 180f;
    [Tooltip("Âm lượng tối đa (0-1)")]
    [Range(0f, 1f)]
    public float maxVolume = 0.4f;
    [Tooltip("Thời gian fade in / fade out (giây)")]
    public float fadeDuration = 3f;

    private AudioSource audioSource;
    private int lastPlayedIndex = -1;
    private bool isFading = false;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Giữ nhạc khi đổi scene

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.volume = 0f;
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    void Start()
    {
        if (musicTracks != null && musicTracks.Length > 0)
        {
            // Bắt đầu sau một khoảng im lặng ngắn khi mới vào game (Minecraft style)
            float initialDelay = Random.Range(5f, 20f);
            StartCoroutine(MusicLoop(initialDelay));
        }
        else
        {
            Debug.LogWarning("BackgroundMusicManager: Chưa có track nhạc! Hãy kéo file .mp3/.ogg vào musicTracks.");
        }
    }

    IEnumerator MusicLoop(float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            // Chọn bài ngẫu nhiên, không lặp lại bài vừa phát
            int index = PickRandomTrack();
            if (index < 0) yield break;

            AudioClip clip = musicTracks[index];
            lastPlayedIndex = index;

            // Fade in
            audioSource.clip = clip;
            audioSource.Play();
            yield return StartCoroutine(FadeVolume(0f, maxVolume, fadeDuration));

            // Phát đến hết bài
            float playDuration = clip.length - fadeDuration * 2f;
            if (playDuration > 0f)
                yield return new WaitForSeconds(playDuration);

            // Fade out
            yield return StartCoroutine(FadeVolume(maxVolume, 0f, fadeDuration));
            audioSource.Stop();

            // Nghỉ im lặng kiểu Minecraft
            float silence = Random.Range(minSilenceTime, maxSilenceTime);
            yield return new WaitForSeconds(silence);
        }
    }

    IEnumerator FadeVolume(float from, float to, float duration)
    {
        isFading = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        audioSource.volume = to;
        isFading = false;
    }

    int PickRandomTrack()
    {
        if (musicTracks == null || musicTracks.Length == 0) return -1;
        if (musicTracks.Length == 1) return 0;

        int index;
        int tries = 0;
        do
        {
            index = Random.Range(0, musicTracks.Length);
            tries++;
        } while (index == lastPlayedIndex && tries < 10);

        return index;
    }

    // API công khai
    public void SetVolume(float vol) => maxVolume = Mathf.Clamp01(vol);

    public void StopMusic()
    {
        StopAllCoroutines();
        StartCoroutine(FadeVolume(audioSource.volume, 0f, fadeDuration));
    }
}
