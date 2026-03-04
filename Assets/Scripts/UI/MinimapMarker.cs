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
    public float heightOffset = 5f;

    private GameObject markerObject;

    void Start()
    {
        CreateMarker();
    }

    void CreateMarker()
    {
        markerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        markerObject.name = "MinimapIcon_" + gameObject.name;
        
        // Remove collider so it doesn't interfere with physics
        Destroy(markerObject.GetComponent<Collider>());
        
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
        
        // Set Material to a basic unlit color
        Renderer rend = markerObject.GetComponent<Renderer>();
        if (rend != null)
        {
            // Use an unlit shader if possible so it's brightly colored
            Shader unlitColorShader = Shader.Find("Unlit/Color");
            if (unlitColorShader != null)
            {
                Material mat = new Material(unlitColorShader);
                mat.color = markerColor;
                rend.material = mat;
            }
            else
            {
                // Fallback to standard shader with emission
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = markerColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", markerColor);
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
