using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Demen.Quests
{
    public class DemenQuestUIManager : MonoBehaviour
    {
        public static DemenQuestUIManager Instance { get; private set; }

        [Header("UI Elements")]
        public GameObject questPanel;
        public Transform questListContainer; // ScrollView Content parent

        [Header("Waypoint Settings")]
        public float waypointHeight = 80f;
        public float waypointClearDistance = 5f;
        private GameObject activeWaypoint;
        private Transform trackedTarget;

        // Keep track of spawned quest row UI objects
        private List<GameObject> spawnedRows = new List<GameObject>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("[DemenQuestUIManager] Singleton initialized.");
            }
            else
            {
                Debug.LogWarning("[DemenQuestUIManager] Multiple instances detected!");
            }
        }

        private void Update()
        {
            // Check if player reached the waypoint
            if (activeWaypoint != null && activeWaypoint.activeSelf && trackedTarget != null)
            {
                Transform player = FindPlayerTransform();
                if (player != null)
                {
                    Vector2 p1 = new Vector2(player.position.x, player.position.z);
                    Vector2 p2 = new Vector2(activeWaypoint.transform.position.x, activeWaypoint.transform.position.z);
                    if (Vector2.Distance(p1, p2) <= waypointClearDistance)
                    {
                        activeWaypoint.SetActive(false);
                        trackedTarget = null;
                    }
                }
            }
        }

        public void ToggleQuestPanel()
        {
            if (questPanel != null)
            {
                bool isActive = !questPanel.activeSelf;
                questPanel.SetActive(isActive);
                Debug.Log($"[DemenQuestUIManager] Panel toggled: {isActive}");
                if (isActive) RefreshQuestUI();
            }
            else
            {
                Debug.LogError("[DemenQuestUIManager] questPanel is NULL! Assign it in Inspector.");
            }
        }

        public void RefreshQuestUI()
        {
            if (DemenQuestManager.Instance == null) return;

            // Clear old rows
            foreach (var row in spawnedRows)
            {
                if (row != null) Destroy(row);
            }
            spawnedRows.Clear();

            // If no container assigned, try to find or create one
            if (questListContainer == null)
            {
                EnsureUIStructure();
                if (questListContainer == null) return;
            }

            var activeQuests = DemenQuestManager.Instance.GetActiveQuests();

            // Create header
            CreateTextRow("<color=#38BDF8><size=120%><b>NHIEM VU</b></size></color>", 40f);
            CreateTextRow("<size=40%> </size>", 10f);

            if (activeQuests.Count == 0)
            {
                CreateTextRow("<i><size=85%>Hay kham pha the gioi!</size></i>", 30f);
            }
            else
            {
                foreach (var quest in activeQuests)
                {
                    CreateQuestButton(quest);
                }
            }
        }

        /// <summary>
        /// Create a simple text row (non-clickable)
        /// </summary>
        private void CreateTextRow(string richText, float height)
        {
            GameObject row = new GameObject("QuestTextRow", typeof(RectTransform));
            row.transform.SetParent(questListContainer, false);

            var rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, height);

            var layoutElem = row.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = height;
            layoutElem.flexibleWidth = 1;

            var tmp = row.AddComponent<TextMeshProUGUI>();
            tmp.text = richText;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            spawnedRows.Add(row);
        }

        /// <summary>
        /// Create a clickable quest button with name+description
        /// </summary>
        private void CreateQuestButton(DemenQuestData quest)
        {
            // Main row container
            GameObject row = new GameObject("QuestRow_" + quest.questId, typeof(RectTransform));
            row.transform.SetParent(questListContainer, false);

            var rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 70f);

            var layoutElem = row.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 70f;
            layoutElem.flexibleWidth = 1;

            // Background image (semi-transparent for hover feel)
            var bg = row.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.2f, 0.3f, 0.6f);

            // Button component for click
            var btn = row.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.2f, 0.3f, 0.6f);
            colors.highlightedColor = new Color(0.25f, 0.4f, 0.6f, 0.8f);
            colors.pressedColor = new Color(0.1f, 0.5f, 0.3f, 0.9f);
            colors.selectedColor = new Color(0.2f, 0.35f, 0.5f, 0.7f);
            btn.colors = colors;

            // Quest name text
            GameObject nameObj = new GameObject("QuestName", typeof(RectTransform));
            nameObj.transform.SetParent(row.transform, false);
            var nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0.5f);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = new Vector2(12, 0);
            nameRT.offsetMax = new Vector2(-10, -4);

            var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = "<b>" + quest.questName + "</b>";
            nameTMP.fontSize = 16;
            nameTMP.color = new Color(0.9f, 0.95f, 1f);
            nameTMP.alignment = TextAlignmentOptions.Left;
            nameTMP.textWrappingMode = TextWrappingModes.NoWrap;
            nameTMP.overflowMode = TextOverflowModes.Ellipsis;

            // Quest description text
            GameObject descObj = new GameObject("QuestDesc", typeof(RectTransform));
            descObj.transform.SetParent(row.transform, false);
            var descRT = descObj.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0, 0);
            descRT.anchorMax = new Vector2(1, 0.5f);
            descRT.offsetMin = new Vector2(12, 4);
            descRT.offsetMax = new Vector2(-10, 0);

            var descTMP = descObj.AddComponent<TextMeshProUGUI>();
            descTMP.text = quest.description;
            descTMP.fontSize = 12;
            descTMP.color = new Color(0.7f, 0.75f, 0.85f);
            descTMP.alignment = TextAlignmentOptions.Left;
            descTMP.textWrappingMode = TextWrappingModes.Normal;
            descTMP.overflowMode = TextOverflowModes.Ellipsis;

            // Navigate icon hint (right side)
            GameObject iconObj = new GameObject("NavIcon", typeof(RectTransform));
            iconObj.transform.SetParent(row.transform, false);
            var iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(1, 0);
            iconRT.anchorMax = new Vector2(1, 1);
            iconRT.sizeDelta = new Vector2(40, 0);
            iconRT.anchoredPosition = new Vector2(-20, 0);

            var iconTMP = iconObj.AddComponent<TextMeshProUGUI>();
            iconTMP.text = ">";
            iconTMP.fontSize = 22;
            iconTMP.color = new Color(0.4f, 0.8f, 1f);
            iconTMP.alignment = TextAlignmentOptions.Center;

            // Click handler -> create waypoint pillar at NPC
            DemenQuestData capturedQuest = quest;
            btn.onClick.AddListener(() => OnQuestClicked(capturedQuest));

            spawnedRows.Add(row);
        }

        /// <summary>
        /// Handle quest row click: create waypoint pillar to the quest NPC
        /// </summary>
        private void OnQuestClicked(DemenQuestData quest)
        {
            Debug.Log($"[DemenQuestUIManager] Quest clicked: {quest.questName}");

            Transform target = quest.targetLocation;

            // If no target assigned, try to find NPC by quest ID pattern
            if (target == null)
            {
                target = TryFindNPCByQuestId(quest.questId);
            }

            if (target == null)
            {
                Debug.LogWarning($"[DemenQuestUIManager] No target found for quest: {quest.questId}");
                return;
            }

            // Create/move waypoint pillar
            CreateWaypointAtTarget(target);
            trackedTarget = target;

            // Close quest panel after clicking
            if (questPanel != null) questPanel.SetActive(false);
        }

        /// <summary>
        /// Try to find related NPC by quest ID naming convention
        /// e.g. "talk_detrui" -> find GameObject containing "DeTrui" or "detrui"
        /// </summary>
        private Transform TryFindNPCByQuestId(string questId)
        {
            // Extract NPC name from quest ID (e.g. "talk_xentoc" -> "xentoc")
            string[] parts = questId.Split('_');
            if (parts.Length < 2) return null;

            string npcHint = parts[parts.Length - 1].ToLower();

            // Search all NPC-like objects in scene
            MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var script in allScripts)
            {
                string objName = script.gameObject.name.ToLower();
                string typeName = script.GetType().Name.ToLower();
                if (objName.Contains(npcHint) || typeName.Contains(npcHint))
                {
                    // Check if this looks like an NPC (has relevant NPC script)
                    if (typeName.Contains("npc") || typeName.Contains(npcHint))
                    {
                        Debug.Log($"[DemenQuestUIManager] Auto-found NPC: {script.gameObject.name} for quest {questId}");
                        return script.transform;
                    }
                }
            }

            // Fallback: search by GameObject name containing the hint
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name.ToLower().Contains(npcHint) && obj.activeInHierarchy)
                {
                    Debug.Log($"[DemenQuestUIManager] Fallback found: {obj.name} for quest {questId}");
                    return obj.transform;
                }
            }

            return null;
        }

        /// <summary>
        /// Create or move the waypoint pillar to target position
        /// </summary>
        private void CreateWaypointAtTarget(Transform target)
        {
            if (activeWaypoint != null)
            {
                Destroy(activeWaypoint);
                activeWaypoint = null;
            }

            // Create tall glowing pillar
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "QuestWaypoint";
            Destroy(pillar.GetComponent<Collider>());

            pillar.transform.localScale = new Vector3(1.2f, waypointHeight, 1.2f);
            pillar.transform.position = target.position;

            // Warm light yellow material with glow
            Material mat = new Material(Shader.Find("Standard"));
            Color lightYellow = new Color(1f, 0.95f, 0.6f, 0.4f);
            mat.color = lightYellow;

            // Transparent mode
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            // Emission glow
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.92f, 0.5f, 1f) * 1.5f);

            pillar.GetComponent<MeshRenderer>().sharedMaterial = mat;
            activeWaypoint = pillar;
        }

        /// <summary>
        /// Make sure the UI structure exists (panel + scrollview + content container)
        /// </summary>
        private void EnsureUIStructure()
        {
            if (questPanel == null)
            {
                Debug.LogError("[DemenQuestUIManager] questPanel not assigned!");
                return;
            }

            // Try to find existing content container
            Transform existing = questPanel.transform.Find("Scroll View/Viewport/Content");
            if (existing != null)
            {
                questListContainer = existing;
                return;
            }

            // Create ScrollView structure inside questPanel
            // ScrollView
            GameObject scrollView = new GameObject("Scroll View", typeof(RectTransform));
            scrollView.transform.SetParent(questPanel.transform, false);
            var svRT = scrollView.GetComponent<RectTransform>();
            svRT.anchorMin = Vector2.zero;
            svRT.anchorMax = Vector2.one;
            svRT.offsetMin = new Vector2(10, 10);
            svRT.offsetMax = new Vector2(-10, -50); // Leave space for title

            var scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            // Viewport
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollView.transform, false);
            var vpRT = viewport.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.sizeDelta = Vector2.zero;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            var vpMask = viewport.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = Color.white;

            scrollRect.viewport = vpRT;

            // Content
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.sizeDelta = new Vector2(0, 0);

            var cLayout = content.AddComponent<VerticalLayoutGroup>();
            cLayout.spacing = 6f;
            cLayout.padding = new RectOffset(5, 5, 5, 5);
            cLayout.childForceExpandWidth = true;
            cLayout.childForceExpandHeight = false;
            cLayout.childControlWidth = true;
            cLayout.childControlHeight = true;

            var cSizeFitter = content.AddComponent<ContentSizeFitter>();
            cSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = cRT;

            questListContainer = content.transform;
        }

        private Transform FindPlayerTransform()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.transform : null;
        }
    }
}
