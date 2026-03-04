using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public class DeTruiSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup DeTrui Animator")]
    public static void ShowWindow()
    {
        // 1. Tìm hoặc tạo Animator Controller
        string[] controllerGuids = AssetDatabase.FindAssets("t:AnimatorController DeTruiController");
        AnimatorController controller;

        if (controllerGuids.Length == 0)
        {
            // Trải qua việc tạo mới nếu chưa tồn tại
            string folderPath = "Assets/Animations";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }
            controller = AnimatorController.CreateAnimatorControllerAtPath($"{folderPath}/DeTruiController.controller");
            Debug.Log($"Created new Animator Controller at {folderPath}/DeTruiController.controller");
        }
        else
        {
            string controllerPath = AssetDatabase.GUIDToAssetPath(controllerGuids[0]);
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        }

        // 2. Tìm các Animation Clip mẫu trong dự án (Bạn có thể điều chỉnh lại tên phù hợp)
        AnimationClip idleClip = FindClip("Idle"); 
        AnimationClip talkClip = FindClip("Talk");
        AnimationClip runClip = FindClip("Run", "Walk"); // Cố tìm Run, không có thì tìm Walk

        if (idleClip == null || talkClip == null || runClip == null)
        {
            Debug.LogError($"[DeTruiSetupTool] Không tìm thấy đủ Animation Clips! \nIdle: {idleClip?.name}\nTalk: {talkClip?.name}\nRun: {runClip?.name}\nVui lòng đặt tên Clip của bạn có chứa các từ khóa này để Tool tìm được.");
            return;
        }

        // 3. Setup Layers và Parameters
        AnimatorControllerLayer rootLayer = controller.layers[0];
        AnimatorStateMachine rootStateMachine = rootLayer.stateMachine;

        AddParameter(controller, "Idle", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "Talk", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "IsRunning", AnimatorControllerParameterType.Bool);

        // 4. Khởi tạo/Tìm States
        AnimatorState idleState = FindOrCreateState(rootStateMachine, "Idle", idleClip);
        AnimatorState talkState = FindOrCreateState(rootStateMachine, "Talk", talkClip);
        AnimatorState runState = FindOrCreateState(rootStateMachine, "Run", runClip);

        // Xóa toàn bộ transitions cũ để làm mới tránh trùng lặp
        ClearTransitions(rootStateMachine);

        // 5. Kết nối mũi tên (Transitions)

        // Idle <-> Run
        var idleToRun = idleState.AddTransition(runState);
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
        idleToRun.duration = 0.15f;
        idleToRun.hasExitTime = false;

        var runToIdle = runState.AddTransition(idleState);
        runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        runToIdle.duration = 0.15f;
        runToIdle.hasExitTime = false;

        // Idle -> Talk
        var idleToTalk = idleState.AddTransition(talkState);
        idleToTalk.AddCondition(AnimatorConditionMode.If, 0, "Talk");
        idleToTalk.duration = 0.25f;
        idleToTalk.hasExitTime = false;

        // Talk -> Idle
        var talkToIdle = talkState.AddTransition(idleState);
        talkToIdle.AddCondition(AnimatorConditionMode.If, 0, "Idle");
        talkToIdle.duration = 0.25f;
        talkToIdle.hasExitTime = false;

        // 6. Hoàn tất lưu lại
        rootStateMachine.defaultState = idleState;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // 7. Tự động áp dụng Controller vào cảnh nếu tìm thấy Dế Trũi
        DeTruiNPC npc = FindFirstObjectByType<DeTruiNPC>();
        if (npc != null)
        {
            Animator anim = npc.GetComponent<Animator>();
            if (anim != null)
            {
                anim.runtimeAnimatorController = controller;
                EditorUtility.SetDirty(npc);
            }
        }

        EditorUtility.DisplayDialog("Thành công", $"Đã setup toàn bộ hệ thống Animator cho Dế Trũi!\n\nBao gồm Idle, Talk và IsRunning.\nController đã tự động gắn vào nhân vật trên map (nếu có).", "OK");
        Debug.Log("[DeTruiSetupTool] ✅ Hoàn tất setup Animator Controller!");
    }

    // -- Hàm hỗ trợ --
    static AnimationClip FindClip(params string[] names)
    {
        foreach (string name in names)
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip " + name);
            if (guids.Length > 0)
            {
                // Ưu tiên Asset trong thư mục xịn (VD: Kevin Iglesias)
                foreach (var guid in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (p.Contains("Kevin") || p.Contains("Mixamo")) return AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                }
                // Nếu không có, lấy đại cái đầu tiên
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }
        return null;
    }

    static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var param in controller.parameters) if (param.name == name) return;
        controller.AddParameter(name, type);
    }

    static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string name, AnimationClip clip)
    {
        foreach (var checkState in sm.states)
        {
            if (checkState.state.name == name)
            {
                checkState.state.motion = clip;
                return checkState.state;
            }
        }
        var newState = sm.AddState(name);
        newState.motion = clip;
        return newState;
    }

    static void ClearTransitions(AnimatorStateMachine sm)
    {
        foreach (var state in sm.states)
        {
            state.state.transitions = new AnimatorStateTransition[0];
        }
        sm.anyStateTransitions = new AnimatorStateTransition[0];
    }
}
