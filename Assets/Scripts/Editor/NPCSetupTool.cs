using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class NPCSetupTool : EditorWindow
{
    private AnimationClip idleClip;
    private AnimationClip walkClip;
    private Avatar avatarObj;

    [MenuItem("GDC301/Setup NPC (Wandering)")]
    public static void ShowWindow()
    {
        GetWindow<NPCSetupTool>("NPC Setup Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Cài đặt tự động hệ thống NPC cho các Model trong Hierarchy", EditorStyles.boldLabel);
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Chọn các nhân vật (Kiến, Xén Tóc...) ở Hierarchy, sau đó bấm nút bên dưới.", MessageType.Info);
        
        GUILayout.Space(10);
        GUILayout.Label("Animation Setup (Sẽ tự động tạo Controller)", EditorStyles.boldLabel);
        idleClip = (AnimationClip)EditorGUILayout.ObjectField("Idle Clip", idleClip, typeof(AnimationClip), false);
        walkClip = (AnimationClip)EditorGUILayout.ObjectField("Walk/Run Clip", walkClip, typeof(AnimationClip), false);
        avatarObj = (Avatar)EditorGUILayout.ObjectField("Avatar (Tùy chọn)", avatarObj, typeof(Avatar), false);

        GUILayout.Space(10);
        if (GUILayout.Button("Setup Selected NPCs", GUILayout.Height(40)))
        {
            SetupSelectedNPCs();
        }
    }

    private void SetupSelectedNPCs()
    {
        GameObject[] selectedObjs = Selection.gameObjects;
        if (selectedObjs.Length == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Hãy chọn ít nhất 1 GameObject trong Hierarchy!", "OK");
            return;
        }

        // Tìm một DeTruiNPC có sẵn trong cảnh để mượn Prefab ChatBubble và PromptUI
        DeTruiNPC templateNPC = Object.FindAnyObjectByType<DeTruiNPC>();

        foreach (GameObject obj in selectedObjs)
        {
            // Tránh setup đè lên chính thằng template gốc 
            if (templateNPC != null && obj == templateNPC.gameObject) continue;

            Undo.RecordObject(obj, "Setup NPC");

            // 1. Animator
            Animator anim = obj.GetComponent<Animator>();
            if (anim == null) anim = Undo.AddComponent<Animator>(obj);

            // AUTO-ASSIGN AVATAR (Tự động tìm kiếm Avatar xương của chính FBX đó)
            if (anim.avatar == null)
            {
                if (avatarObj != null)
                {
                    anim.avatar = avatarObj;
                }
                else
                {
                    // Truy vết file FBX hoặc Prefab gốc chứa Model này
                    string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        // Lục tung tất cả object con trong FBX để kiếm file Avatar
                        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                        foreach (Object a in assets)
                        {
                            if (a is Avatar avt)
                            {
                                anim.avatar = avt;
                                break;
                            }
                        }
                    }
                }
            }
            
            // TỰ ĐỘNG TẠO VÀ GÁN ANIMATOR CONTROLLER NẾU NGƯỜI DÙNG ĐÃ TRUYỀN VÀO 2 CLIP TỪ GIAO DIỆN
            if (idleClip != null && walkClip != null)
            {
                // Tạo thư mục nếu chưa có
                if (!AssetDatabase.IsValidFolder("Assets/Animation"))
                {
                    AssetDatabase.CreateFolder("Assets", "Animation");
                }

                // Xây dựng đường dẫn (Tránh trùng file thì thêm số Random hoặc ID)
                string controllerPath = $"Assets/Animation/{obj.name}_Controller_{obj.GetInstanceID()}.controller";
                
                // Tạo file Controller
                AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

                // Thêm Biến bool "IsRunning" (Để khớp với Script DeTruiNPC)
                controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);

                // Lấy Layer mặc định
                AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

                // Tạo State Idle (Mặc định)
                AnimatorState idleState = rootStateMachine.AddState("Idle");
                idleState.motion = idleClip;

                // Tạo State Chạy rảo bước
                AnimatorState runState = rootStateMachine.AddState("Walk"); // Hoặc tên là Run/Walk tùy ý
                runState.motion = walkClip;

                // Trỏ đường kết nối (Transition) Idle -> Run
                AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
                idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
                idleToRun.duration = 0.25f;

                // Trỏ đường kết nối (Transition) Run -> Idle
                AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
                runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
                runToIdle.duration = 0.25f;

                // Gán bộ điều khiển vừa đúc xong vào con bọ
                anim.runtimeAnimatorController = controller;
            }

            // 1.5. Minimap Marker
            MinimapMarker marker = obj.GetComponent<MinimapMarker>();
            if (marker == null) marker = Undo.AddComponent<MinimapMarker>(obj);
            marker.markerColor = new Color(0.8f, 0.2f, 0.8f); // Màu tím nhạt cho NPC dạo phố
            marker.heightOffset = 25f; // Bắn hẳn lên cao 25m mây xanh để khỏi bị model nhân vật cản tầm nhìn khi lại gần

            // 2. Rigidbody
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null) rb = Undo.AddComponent<Rigidbody>(obj);
            rb.constraints = RigidbodyConstraints.FreezeRotation; // Không để NPC ngã
            rb.mass = 50f;

            // 3. Collider
            Collider col = obj.GetComponent<Collider>();
            if (col == null)
            {
                CapsuleCollider cap = Undo.AddComponent<CapsuleCollider>(obj);
                cap.height = 1.0f;
                cap.center = new Vector3(0, 0.5f, 0); // Kích thước body mặc định của kiến/xén tóc
                cap.radius = 0.3f;
            }

            // 4. DeTruiNPC (Đa năng)
            DeTruiNPC npcScript = obj.GetComponent<DeTruiNPC>();
            if (npcScript == null) npcScript = Undo.AddComponent<DeTruiNPC>(obj);

            npcScript.npcName = obj.name;
            npcScript.enableWandering = true; // Bật đi dạo mặc định
            
            // XÉT ĐẶC ĐIỂM HỘI THOẠI & MINIGAME THEO TÊN NHÂN VẬT
            string nameLower = obj.name.ToLower();
            if (nameLower.Contains("detrui"))
            {
                npcScript.enableRacing = true;
                npcScript.enableCaro = false;
                npcScript.enableArmWrestling = false;
                npcScript.dialogue = new string[] {
                    "Xin chào người anh em! Tôi là Dế Trũi.",
                    "Lâu rồi không có ai đến thăm khu vườn này.",
                    "Cậu có muốn đọ sức một chút không?"
                };
            }
            else if (nameLower.Contains("kien"))
            {
                npcScript.enableRacing = false;
                npcScript.enableCaro = false;
                npcScript.enableArmWrestling = false;
                npcScript.dialogue = new string[] {
                    "Hây dô! Tôi là Kiến thợ đây.",
                    "Khuân vác suốt ngày mệt quá đi mất.",
                    "Này, cậu nhớ cẩn thận mấy tảng đá rơi đấy nhé!"
                };
            }
            else if (nameLower.Contains("dechoat"))
            {
                npcScript.enableRacing = false;
                npcScript.enableCaro = true;
                npcScript.enableArmWrestling = false;
                npcScript.dialogue = new string[] {
                    "Chào người anh em... Tôi dạo này ốm yếu quá.",
                    "Nhưng cái vụ chơi cờ Caro thì đầu óc tôi vẫn còn nhạy bén lắm nhé!",
                    "Khụ khụ... cậu có muốn chơi một ván cho đầu óc thư giãn không?"
                };
            }
            else if (nameLower.Contains("xentoc"))
            {
                npcScript.enableRacing = false;
                npcScript.enableCaro = false;
                npcScript.enableArmWrestling = true;
                npcScript.dialogue = new string[] {
                    "Khà khà, ta là Xén Tóc lực lưỡng đây!",
                    "Trên đời này chỉ có sức mạnh mới giải quyết được vấn đề.",
                    "Thấy hàm răng ta không? Dám gồng tay với ta không, hả nhóc?"
                };
            }
            else
            {
                npcScript.enableRacing = false;
                npcScript.enableCaro = false;
                npcScript.enableArmWrestling = false;
                npcScript.dialogue = new string[] {
                    "Xin chào! Tôi là " + obj.name + ".",
                    "Hôm nay trời đẹp nhỉ!",
                    "Cứ đi dạo loanh quanh quanh đây thôi."
                };
            }

            // TÍNH TOÁN VỊ TRÍ CHIỀU CAO CỦA KHUNG CHAT PHÙ HỢP CƠ THỂ CON BỌ
            float uiHeightOffset = 1.5f; // Mặc định
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                
                // bounds.max.y là độ cao tuyệt đối của đỉnh đầu so với map
                float headWorldY = bounds.max.y;
                float objWorldY = obj.transform.position.y;
                
                // Độ cao Tương đối của vùng trên đỉnh đầu
                float localDeltaY = (headWorldY - objWorldY) + 0.5f; // cơi thêm 0.5m khoảng trống
                
                // Quy đổi về không gian cục bộ (Phòng trường hợp scale của object bị chỉnh sửa lung tung kiểu nhân 100)
                float lossyY = obj.transform.lossyScale.y;
                if (lossyY > 0)
                {
                    uiHeightOffset = localDeltaY / lossyY;
                }
                
                // Sẵn tiện căn chỉnh lại kích thước CapsuleCollider theo đúng chiều cao thực tế của con bọ
                if (col is CapsuleCollider capSetup && lossyY > 0)
                {
                    float realHeight = headWorldY - bounds.min.y;
                    if (realHeight > 0.1f)
                    {
                        capSetup.height = realHeight / lossyY;
                        capSetup.center = new Vector3(0, capSetup.height / 2f, 0);
                        capSetup.radius = capSetup.height / 3f;
                    }
                }
            }

            // 5. Clone ChatBubble & PromptUI từ Template (Nếu chưa có)
            if (templateNPC != null)
            {
                // Clone Chat Bubble
                if (npcScript.chatBubble == null && templateNPC.chatBubble != null)
                {
                    GameObject clonedBubble = null;
#if UNITY_EDITOR
                    if (PrefabUtility.IsPartOfAnyPrefab(templateNPC.chatBubble.gameObject))
                    {
                        clonedBubble = (GameObject)PrefabUtility.InstantiatePrefab(
                            PrefabUtility.GetCorrespondingObjectFromSource(templateNPC.chatBubble.gameObject), obj.transform);
                    }
#endif
                    if (clonedBubble == null) clonedBubble = Instantiate(templateNPC.chatBubble.gameObject, obj.transform);
                    
                    clonedBubble.name = "ChatBubble";
                    // Đẩy lên theo cái chiều cao chuẩn xác với tỉ lệ Model
                    clonedBubble.transform.localPosition = new Vector3(0, uiHeightOffset, 0); 
                    npcScript.chatBubble = clonedBubble.GetComponent<ChatBubble>();
                }

                // Clone Interaction Prompt
                if (npcScript.interactionPromptUI == null && templateNPC.interactionPromptUI != null)
                {
                    GameObject clonedPrompt = null;
#if UNITY_EDITOR
                    if (PrefabUtility.IsPartOfAnyPrefab(templateNPC.interactionPromptUI.gameObject))
                    {
                        clonedPrompt = (GameObject)PrefabUtility.InstantiatePrefab(
                            PrefabUtility.GetCorrespondingObjectFromSource(templateNPC.interactionPromptUI.gameObject), obj.transform);
                    }
#endif
                    if (clonedPrompt == null) clonedPrompt = Instantiate(templateNPC.interactionPromptUI.gameObject, obj.transform);
                    
                    clonedPrompt.name = "InteractionPrompt";
                    clonedPrompt.transform.localPosition = new Vector3(0, uiHeightOffset, 0);
                    npcScript.interactionPromptUI = clonedPrompt;
                }
            }

            // Cấu hình layer (nếu mặt đất là mask mặc định)
            npcScript.groundLayer = LayerMask.GetMask("Default", "Ground", "Terrain"); // Setup một mask căn bản đỡ rớt
            npcScript.animator = anim;

            EditorUtility.SetDirty(obj);
        }

        EditorUtility.DisplayDialog("Setup NPC Thành công", $"Đã tự động cài đặt hệ thống Cấu Hình NPC (Wandering) cho {selectedObjs.Length} sinh vật!", "OK");
    }
}
