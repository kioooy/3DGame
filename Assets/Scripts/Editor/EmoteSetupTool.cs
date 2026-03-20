#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class EmoteSetupTool : EditorWindow
{
    private const string DanceClipPath = "Assets/Animation/Dance.anim";
    private const string WavingClipPath = "Assets/Animation/Waving.fbx";
    private const string ThrowClipPath = "Assets/Animation/Throw.fbx";

    [MenuItem("Tools/Antigravity/Setup Emote For Player")]
    public static void SetupEmote()
    {
        // 1. Lấy GameObject đang được chọn
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng click chọn nhân vật chính (Player) ở bảng Hierarchy trước khi chạy Tool này!", "Đã hiểu");
            return;
        }

        // 2. Lấy Animator
        Animator animator = selectedObj.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Animator trên GameObject (hoặc các object con) bạn vừa chọn.", "Đã hiểu");
            return;
        }

        // 3. Lấy Animator Controller
        AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Animator hiện tại không sử dụng chuẩn AnimatorController (có thể là OverrideController). Vui lòng chọn file .controller gốc.", "Đã hiểu");
            return;
        }

        // 4. Lấy Animation Clip
        AnimationClip danceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DanceClipPath);
        AnimationClip wavingClip = LoadAnimationClipFromFBX(WavingClipPath);
        AnimationClip throwClip = LoadAnimationClipFromFBX(ThrowClipPath);
        
        if (danceClip == null || wavingClip == null || throwClip == null)
        {
            EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy đủ file Animation tại:\n- {DanceClipPath}\n- {WavingClipPath}\n- {ThrowClipPath}\n\nVui lòng kiểm tra lại.", "Đã hiểu");
            return;
        }

        // 5. Thêm Parameters (Trigger) và Setup States
        string[] emoteNames = { "Emote1", "Emote2", "Emote3", "Emote4", "Throw" };
        AnimationClip[] emoteClips = { danceClip, danceClip, wavingClip, null, throwClip }; // Map 1->Dance, 2->Dance, 3->Waving, 4->None, 5->Throw
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Kiểm tra Parameter CancelEmote (Dùng để ngắt Animation khi bật di chuyển)
        bool cancelParamExists = false;
        foreach (var param in controller.parameters)
        {
            if (param.name == "CancelEmote")
            {
                cancelParamExists = true; break;
            }
        }
        if (!cancelParamExists)
        {
            controller.AddParameter("CancelEmote", AnimatorControllerParameterType.Trigger);
        }

        // Lưu xem thao tác thành công bao nhiêu cái
        int successCount = 0;

        for (int i = 0; i < emoteNames.Length; i++)
        {
            string emoteName = emoteNames[i];
            AnimationClip clipToUse = emoteClips[i];
            
            // Bỏ qua nếu không có clip cài đặt (Ví dụ nút None)
            if (clipToUse == null) continue;
            // Kiểm tra xem parameter (VD: Emote1) đã tồn tại chưa
            bool paramExists = false;
            foreach (var param in controller.parameters)
            {
                if (param.name == emoteName)
                {
                    paramExists = true;
                    break;
                }
            }

            if (!paramExists)
            {
                controller.AddParameter(emoteName, AnimatorControllerParameterType.Trigger);
            }

            // Kiểm tra xem State đã tồn tại chưa
            AnimatorState emoteState = null;
            foreach (var state in rootStateMachine.states)
            {
                if (state.state.name == emoteName)
                {
                    emoteState = state.state;
                    break;
                }
            }

            if (emoteState == null)
            {
                // Tạo mới State
                emoteState = rootStateMachine.AddState(emoteName);
                emoteState.motion = clipToUse;

                // Tạo Transition từ Any State vào Emote State
                AnimatorStateTransition anyTr = rootStateMachine.AddAnyStateTransition(emoteState);
                anyTr.hasExitTime = false;
                anyTr.hasFixedDuration = true;
                anyTr.duration = 0.25f; // Thời gian blend mượt
                anyTr.AddCondition(AnimatorConditionMode.If, 0, emoteName);

                // Tạo Transition 1: Từ Emote State quay lại Default State (khi hết 1 vòng clip tự động)
                if (rootStateMachine.defaultState != null)
                {
                    AnimatorStateTransition exitTr = emoteState.AddTransition(rootStateMachine.defaultState);
                    exitTr.hasExitTime = true;
                    exitTr.exitTime = 0.9f;     // Đợi gần chạy xong clip thì chuyển
                    exitTr.hasFixedDuration = true;
                    exitTr.duration = 0.25f;
                    
                    // Tạo Transition 2: Ngắt ngang (CancelEmote) khi nhân vật di chuyển
                    AnimatorStateTransition cancelTr = emoteState.AddTransition(rootStateMachine.defaultState);
                    cancelTr.hasExitTime = false;   // Ngắt ngay lập tức, không đợi hết clip
                    cancelTr.hasFixedDuration = true;
                    cancelTr.duration = 0.1f;
                    cancelTr.AddCondition(AnimatorConditionMode.If, 0, "CancelEmote");
                }

                successCount++;
            }
            else
            {
                // Nếu state đã có, update lại clip phòng trường hợp đổi file
                emoteState.motion = clipToUse;
            }
        }

        // 6. Lưu lại các thay đổi vào file
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        if (successCount > 0)
        {
            EditorUtility.DisplayDialog("Thành công", $"Đã hoàn tất thiết lập {successCount} state Emote vào Animator [{controller.name}] thành công! Bạn có thể ấn nút Play để test.", "Quá đã");
        }
        else
        {
            EditorUtility.DisplayDialog("Thông báo", "Các State Emote này đã có sẵn trong Animator.", "OK");
        }

        // 7. Gắn script EmoteUIManager vào Player nếu chưa có
        EmoteUIManager uiManager = selectedObj.GetComponentInChildren<EmoteUIManager>();
        if (uiManager == null)
        {
            uiManager = selectedObj.AddComponent<EmoteUIManager>();
            EditorUtility.DisplayDialog("Thành công phụ", "Đã gắn tự động script EmoteUIManager lên nhân vật để hiển thị vòng quay.", "OK");
        }
    }
    
    // Hàm phụ trợ tải AnimationClip nằm bên trong file FBX
    private static AnimationClip LoadAnimationClipFromFBX(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }
}
#endif
