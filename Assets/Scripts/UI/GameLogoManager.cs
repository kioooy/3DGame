using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý việc tự động hiển thị Logo lên góc màn hình game.
/// Không cần kéo thả vào Scene. Chỉ cần có ảnh tên "GameLogo" trong thư mục Resources.
/// </summary>
public class GameLogoManager : MonoBehaviour
{
    // Tự động kích hoạt khi chạy game
    /*
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInitialize()
    {
        GameObject logoObj = new GameObject("GameLogoManager_Auto");
        DontDestroyOnLoad(logoObj); // Giữ Logo không bị mất khi đổi Scene
        logoObj.AddComponent<GameLogoManager>();
    }
    */

    void Start()
    {
        // 1. Tải ảnh từ file Assets/Resources/GameLogo.png (hoặc .jpg)
        // Dùng Texture2D để người dùng không cần phải đổi định dạng ảnh thủ công
        Texture2D logoTex = Resources.Load<Texture2D>("GameLogo");
        
        if (logoTex != null)
        {
            CreateLogoUI(logoTex);
        }
        else
        {
            Debug.LogWarning("[GameLogoManager] Không tìm thấy ảnh logo. Vui lòng thả ảnh vào thư mục Assets/Resources/ và đổi tên file thành 'GameLogo' (chữ G và L viết hoa).");
        }
    }

    void CreateLogoUI(Texture2D tex)
    {
        // 2. Tạo Canvas chứa Logo
        GameObject canvasObj = new GameObject("LogoCanvas");
        DontDestroyOnLoad(canvasObj);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Ưu tiên hiển thị trên cùng, đè lên mọi thứ khác
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // 3. Tạo Object chứa Ảnh
        GameObject imageObj = new GameObject("LogoImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        Image logoImage = imageObj.AddComponent<Image>();
        
        // Đổi ảnh Texture2D thành định dạng Sprite mà UI Unity yêu cầu
        Sprite logoSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        logoImage.sprite = logoSprite;
        
        // Tắt raycast để Logo không cản trở việc click chuột vào các nút khác trong game
        logoImage.raycastTarget = false;

        // 4. Căn chỉnh vị trí (Mặc định: Góc trên Cùng, Bên Trái)
        RectTransform rect = logoImage.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -20); // Cách mép trên và mép trái 20px
        
        // 5. Chỉnh lại kích thước hiển thị (Giữ đúng tỷ lệ ảnh gốc)
        float targetWidth = 150f; // Độ lớn của logo (có thể chỉnh to nhỏ ở đây)
        float ratio = (float)tex.height / tex.width;
        rect.sizeDelta = new Vector2(targetWidth, targetWidth * ratio);
    }
}
