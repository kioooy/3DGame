using UnityEngine;
using System.Collections;

/// <summary>
/// Quản lý nhạc nền theo phong cách Minecraft:
/// - Nhạc yên tĩnh, ambient, phát ngẫu nhiên với khoảng nghỉ giữa các bài
/// - Fade in / Fade out mượt mà
/// - Hỗ trợ nhiều track
/// - Tự động tải nhạc và tự chạy mà không cần setup trên Scene.
/// </summary>
public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance { get; private set; }

    // Tự động kích hoạt khi chạy game, không bắt buộc thả vào Scene
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInitialize()
    {
        // Tránh tạo thêm nếu trên Scene đã có sẵn 1 cái người dùng kéo vào
        if (FindFirstObjectByType<BackgroundMusicManager>() == null && Instance == null)
        {
            GameObject bgmObj = new GameObject("BackgroundMusicManager_Auto");
            bgmObj.AddComponent<BackgroundMusicManager>();
        }
    }

    [Header("Music Tracks (kéo file nhạc vào đây hoặc nó tự động tải từ Resources/Music_BGM)")]
    public AudioClip[] musicTracks;

    [Header("Music Settings")]
    [Tooltip("Thời gian nghỉ tối thiểu giữa 2 bài (giây)")]
    public float minSilenceTime = 1f;
    [Tooltip("Thời gian nghỉ tối đa giữa 2 bài (giây)")]
    public float maxSilenceTime = 3f;
    [Tooltip("Âm lượng tối đa (0-1)")]
    [Range(0f, 1f)]
    public float maxVolume = 0.4f;
    [Tooltip("Thời gian fade in / fade out (giây)")]
    public float fadeDuration = 3f;

    private AudioSource audioSource;
    private int lastPlayedIndex = -1;
    private Coroutine currentFadeCoroutine;
    private bool isDucked = false;
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

        // Tự động load nhạc nền tĩnh định sẵn nếu ở Inspector chưa gán
        if (musicTracks == null || musicTracks.Length == 0)
        {
            AudioClip mainBgm = Resources.Load<AudioClip>("Music_BGM/Jungle (7) Loop");
            if (mainBgm != null)
            {
                musicTracks = new AudioClip[] { mainBgm };
                Debug.Log($"[BackgroundMusicManager] Đã tải bài nhạc nền chính: {mainBgm.name}");
            }
            else
            {
                musicTracks = Resources.LoadAll<AudioClip>("Music_BGM");
            }
        }
    }

    void Start()
    {
        if (musicTracks != null && musicTracks.Length > 0)
        {
            // Bắt đầu sau một khoảng im lặng rất ngắn
            float initialDelay = 1f;
            StartCoroutine(MusicLoop(initialDelay));
        }
        else
        {
            Debug.LogWarning("[BackgroundMusicManager] Chưa có track nhạc! Hãy đảm bảo thả file nhạc vào mục Assets/Resources/Music_BGM.");
        }
    }

    IEnumerator MusicLoop(float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);

        // Nếu chỉ có 1 track → loop thẳng, không cần silence giữa bài
        if (musicTracks.Length == 1)
        {
            audioSource.clip = musicTracks[0];
            audioSource.loop = true;
            audioSource.Play();
            float targetVol = isDucked ? 0.05f : maxVolume;
            if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = StartCoroutine(FadeVolume(0f, targetVol, fadeDuration));
            yield return currentFadeCoroutine;
            yield break; // Xong — AudioSource tự loop mãi
        }

        // Nhiều track: cycling với khoảng nghỉ Minecraft
        while (true)
        {
            int index = PickRandomTrack();
            if (index < 0) yield break;

            AudioClip clip = musicTracks[index];
            lastPlayedIndex = index;

            // Fade in
            audioSource.clip = clip;
            audioSource.loop = false;
            audioSource.Play();

            float targetVol = isDucked ? 0.05f : maxVolume;
            if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = StartCoroutine(FadeVolume(audioSource.volume, targetVol, fadeDuration));
            yield return currentFadeCoroutine;

            // Phát đến hết bài (clamp để không âm)
            float playDuration = Mathf.Max(0f, clip.length - fadeDuration * 2f);
            if (playDuration > 0f)
                yield return new WaitForSeconds(playDuration);

            // Fade out
            if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = StartCoroutine(FadeVolume(audioSource.volume, 0f, fadeDuration));
            yield return currentFadeCoroutine;

            audioSource.Stop();

            // Nghỉ im lặng kiểu Minecraft
            float silence = Random.Range(minSilenceTime, maxSilenceTime);
            yield return new WaitForSeconds(silence);
        }
    }

    IEnumerator FadeVolume(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        audioSource.volume = to;
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

    public void PauseMusic()
    {
        StopAllCoroutines();
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeVolume(audioSource.volume, 0f, 1f));
    }

    public void ResumeMusic()
    {
        StopAllCoroutines();
        StartCoroutine(MusicLoop(1f));
    }

    public void StopMusic()
    {
        StopAllCoroutines();
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeVolume(audioSource.volume, 0f, fadeDuration));
    }

    public void DuckAudio(float targetVolume = 0.05f, float duration = 0.5f)
    {
        isDucked = true;
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeVolume(audioSource.volume, targetVolume, duration));
    }

    public void RestoreAudio(float duration = 0.5f)
    {
        isDucked = false;
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeVolume(audioSource.volume, maxVolume, duration));
    }
}
