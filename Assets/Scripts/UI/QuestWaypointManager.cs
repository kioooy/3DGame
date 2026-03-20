using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestWaypointManager : MonoBehaviour
{
    public static QuestWaypointManager Instance;

    [Header("UI References")]
    public RectTransform pointerUI; // Cả cụm Icon
    public RectTransform arrowIcon; // Hình mũi tên xoay
    public TextMeshProUGUI distanceText; // Khoảng cách
    
    public Transform currentTarget;
    private Camera mainCamera;

    [Header("Settings")]
    public float edgeBuffer = 50f; // Khoảng cách tới mép màn hình

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (pointerUI != null) pointerUI.gameObject.SetActive(false);
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
        if (pointerUI != null && currentTarget != null)
            pointerUI.gameObject.SetActive(true);
    }

    public void ClearTarget()
    {
        currentTarget = null;
        if (pointerUI != null) pointerUI.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (currentTarget == null || pointerUI == null)
        {
            if (pointerUI != null && pointerUI.gameObject.activeSelf)
                pointerUI.gameObject.SetActive(false);
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        if (!pointerUI.gameObject.activeSelf) pointerUI.gameObject.SetActive(true);

        Vector3 targetPos = currentTarget.position + Vector3.up * 1.5f; // Trỏ cao hơn bề mặt đất
        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPos);
        bool isBehind = screenPos.z < 0;

        if (isBehind)
        {
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
            // Xa ra mép màn hình
            if (screenPos.x > Screen.width/2) screenPos.x = Screen.width + 1000;
            else screenPos.x = -1000;
        }

        Vector3 center = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Vector3 dir = (screenPos - center).normalized;

        bool isOffScreen = screenPos.x <= edgeBuffer || screenPos.x >= Screen.width - edgeBuffer ||
                           screenPos.y <= edgeBuffer || screenPos.y >= Screen.height - edgeBuffer || isBehind;

        if (isOffScreen)
        {
            // Bám theo mép viền
            float angle = Mathf.Atan2(dir.y, dir.x);
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            float m = cos / sin;
            Vector3 screenBounds = center * 1f;
            screenBounds.x -= edgeBuffer;
            screenBounds.y -= edgeBuffer;

            Vector3 newPos = center;
            if (cos > 0) newPos = new Vector3(screenBounds.x, screenBounds.x * m, 0);
            else newPos = new Vector3(-screenBounds.x, -screenBounds.x * m, 0);

            if (newPos.y > screenBounds.y) newPos = new Vector3(screenBounds.y / m, screenBounds.y, 0);
            else if (newPos.y < -screenBounds.y) newPos = new Vector3(-screenBounds.y / m, -screenBounds.y, 0);

            newPos += center;
            pointerUI.position = newPos;

            // Xoay hình bên trong
            if (arrowIcon != null)
                arrowIcon.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
        }
        else
        {
            // Mục tiêu trong tầm nhìn màn hình, trỏ thẳng vào
            pointerUI.position = screenPos;
            if (arrowIcon != null)
                arrowIcon.localRotation = Quaternion.Euler(0, 0, -90); // Mũi tên chỉ xuống mục tiêu
        }

        if (distanceText != null)
        {
            float dist = Vector3.Distance(mainCamera.transform.position, currentTarget.position);
            distanceText.text = Mathf.RoundToInt(dist) + "m";
            distanceText.transform.rotation = Quaternion.identity; // Text Không lộn ngược
        }
    }
}
