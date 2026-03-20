using UnityEngine;

/// <summary>
/// A marker that is visible only to the MinimapCamera.
/// It dynamically creates a quad above the object that faces upwards.
/// </summary>
public class MinimapMarker : MonoBehaviour
{
    [Tooltip("Color of the icon on the minimap")]
    public Color markerColor = Color.white;

    [Tooltip("Size of the marker")]
    public float markerSize = 2f;

    [Tooltip("How high above the object the marker should float")]
    public float heightOffset = 25f;

    private GameObject markerObject;

    void Start()
    {
        // Ghi đè chỉ số cũ nhỡ các con vật tạo ra trước đó lúc nào cũng bị lưu 3f hay 5f
        if (heightOffset < 20f) 
        {
            heightOffset = 25f;
        }

        CreateMarker();
    }

    void CreateMarker()
    {
        markerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        markerObject.name = "MinimapIcon_" + gameObject.name;
        
        // Tắt ngay lập tức collider để tránh Physics engine báo lỗi trong 1 frame đầu tiên
        Collider col = markerObject.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
            Destroy(col);
        }
        
        // Attach to this object
        markerObject.transform.SetParent(transform, false);
        
        // Position it above the object
        markerObject.transform.localPosition = new Vector3(0, heightOffset, 0);
        
        // Face upwards (so the top-down minimap camera can see it)
        markerObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        
        // Set size
        markerObject.transform.localScale = new Vector3(markerSize, markerSize, 1f);
        
        // Set Layer to MinimapIcon (Layer 8)
        int minimapLayer = LayerMask.NameToLayer("MinimapIcon");
        if (minimapLayer == -1) // Fallback if layer isn't created, though it should be
        {
            minimapLayer = 8;
        }
        markerObject.layer = minimapLayer;
        
        // Set Material to a basic unlit color nhưng bỏ qua cản trở Z (Xuyên địa hình)
        Renderer rend = markerObject.GetComponent<Renderer>();
        if (rend != null)
        {
            // Lấy Shader tuỳ chỉnh (ZTest Always) mà ta vừa tạo ở Assets/Shaders/MinimapOverlayMarker.shader
            Shader overlayShader = Shader.Find("Hidden/MinimapOverlayMarker");
            if (overlayShader != null)
            {
                Material mat = new Material(overlayShader);
                mat.color = markerColor;
                rend.material = mat;
            }
            else
            {
                // Fallback cứu cánh nếu lỡ quên tạo file Shader
                Material mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = markerColor;
                rend.material = mat;
            }
        }
    }
    
    // Allow updating color dynamically
    public void SetColor(Color newColor)
    {
        markerColor = newColor;
        if (markerObject != null)
        {
            Renderer rend = markerObject.GetComponent<Renderer>();
            if (rend != null && rend.material != null)
            {
                rend.material.color = markerColor;
                if (rend.material.HasProperty("_EmissionColor"))
                {
                    rend.material.SetColor("_EmissionColor", markerColor);
                }
            }
        }
    }
}
