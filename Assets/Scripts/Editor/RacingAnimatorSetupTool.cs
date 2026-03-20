#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class RacingAnimatorSetupTool : EditorWindow
{
    private AnimationClip idleClip;
    private AnimationClip runClip;
    private AnimationClip jumpClip;
    private string controllerName = "RacingAnimator";

    [MenuItem("Tools/3DGame/Create Racing Animator Controller")]
    public static void ShowWindow()
    {
        GetWindow<RacingAnimatorSetupTool>("Racing Animator Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Racing Minigame Animator Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Kéo thả 3 file Animation (Idle, Run, Jump) của Dế Mèn (hoặc Dế Trũi) vào 3 ô dưới đây rồi bấm nút Tạo Animator Controller.", MessageType.Info);

        idleClip = (AnimationClip)EditorGUILayout.ObjectField("Idle Animation", idleClip, typeof(AnimationClip), false);
        runClip = (AnimationClip)EditorGUILayout.ObjectField("Run Animation", runClip, typeof(AnimationClip), false);
        jumpClip = (AnimationClip)EditorGUILayout.ObjectField("Jump Animation", jumpClip, typeof(AnimationClip), false);

        GUILayout.Space(10);
        controllerName = EditorGUILayout.TextField("Tên Controller Lưu Thành", controllerName);

        GUILayout.Space(10);
        if (GUILayout.Button("TẠO ANIMATOR CONTROLLER", GUILayout.Height(40)))
        {
            CreateController();
        }
    }

    private void CreateController()
    {
        if (idleClip == null || runClip == null || jumpClip == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Sếp phải chọn đủ 3 animation Idle, Run, và Jump nhé!", "OK");
            return;
        }

        string path = $"Assets/Animation/{controllerName}.controller";
        
        if (!AssetDatabase.IsValidFolder("Assets/Animation"))
            AssetDatabase.CreateFolder("Assets", "Animation");

        // Tạo Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // Thêm Parameters giống như trong code RacingPlayer.cs và RacingNPC.cs
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);

        // State Machine gốc
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Tạo 3 States
        AnimatorState idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;

        AnimatorState runState = rootStateMachine.AddState("Run");
        runState.motion = runClip;

        AnimatorState jumpState = rootStateMachine.AddState("Jump");
        jumpState.motion = jumpClip;

        // --- CÀI ĐẶT TRANSITIONS ---

        // Idle <-> Run
        AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
        idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.1f;

        AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
        runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.1f;

        // Any -> Jump
        AnimatorStateTransition anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "IsJumping");
        anyToJump.hasExitTime = false;
        anyToJump.duration = 0.1f;

        // Jump -> Idle
        AnimatorStateTransition jumpToIdle = jumpState.AddTransition(idleState);
        jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsJumping");
        jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        jumpToIdle.hasExitTime = true;
        jumpToIdle.exitTime = 0.8f;
        jumpToIdle.duration = 0.2f;

        // Jump -> Run
        AnimatorStateTransition jumpToRun = jumpState.AddTransition(runState);
        jumpToRun.AddCondition(AnimatorConditionMode.IfNot, 0, "IsJumping");
        jumpToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
        jumpToRun.hasExitTime = true;
        jumpToRun.exitTime = 0.8f;
        jumpToRun.duration = 0.2f;

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Thành công", $"Tạo thành công Racing Animator Controller tại: {path}\nBây giờ Sếp hãy chọn nhân vật trong Scene và gắn file {controllerName}.controller này vào component Animator nhé!", "Tuyệt vời");
        
        // Highlight file vừa tạo
        EditorGUIUtility.PingObject(controller);
    }
}
#endif
