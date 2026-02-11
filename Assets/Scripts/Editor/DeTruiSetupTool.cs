using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public class DeTruiSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup DeTrui Animator")]
    public static void SetupAnimator()
    {
        // 1. Find the Controller
        string[] controllerGuids = AssetDatabase.FindAssets("t:AnimatorController DeTruiController");
        if (controllerGuids.Length == 0)
        {
            Debug.LogError("Could not find 'DeTruiController'. Please create it first.");
            return;
        }
        string controllerPath = AssetDatabase.GUIDToAssetPath(controllerGuids[0]);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        // 2. Find Animations
        // We look for clips with "Idle" and "Talk" in their names, preferring Kevin Iglesias assets if possible
        AnimationClip idleClip = FindClip("Idle01"); 
        AnimationClip talkClip = FindClip("Talk01");

        if (idleClip == null) idleClip = FindClip("Idle");
        if (talkClip == null) talkClip = FindClip("Talk");

        if (talkClip == null || idleClip == null)
        {
            Debug.LogError($"Could not find all animations. Idle: {idleClip}, Talk: {talkClip}");
            return;
        }

        // 3. Setup Controller
        // Clear existing layers to start fresh or just add to Base Layer
        AnimatorControllerLayer rootLayer = controller.layers[0];
        AnimatorStateMachine rootStateMachine = rootLayer.stateMachine;

        // Add Parameters
        AddParameter(controller, "Idle", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "Talk", AnimatorControllerParameterType.Trigger);

        // Add States
        AnimatorState idleState = FindOrCreateState(rootStateMachine, "Idle", idleClip);
        AnimatorState talkState = FindOrCreateState(rootStateMachine, "Talk", talkClip);

        // Add Transitions
        // Idle -> Talk
        var toTalk = idleState.AddTransition(talkState);
        toTalk.AddCondition(AnimatorConditionMode.If, 0, "Talk");
        toTalk.duration = 0.2f;
        toTalk.hasExitTime = false;

        // Talk -> Idle
        var toIdle = talkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.If, 0, "Idle");
        toIdle.duration = 0.2f;
        toIdle.hasExitTime = false; // or true if we want to finish talking animation

        rootStateMachine.defaultState = idleState;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log($"Successfully setup DeTruiController at {controllerPath} with Idle: {idleClip.name} and Talk: {talkClip.name}");
    }

    static AnimationClip FindClip(string name)
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip " + name);
        if (guids.Length == 0) return null;
        
        // Prefer assets in "Kevin Iglesias" folder if possible
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Kevin Iglesias"))
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }
        
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        bool exists = false;
        foreach (var param in controller.parameters)
        {
            if (param.name == name)
            {
                exists = true; 
                break;
            }
        }
        if (!exists) controller.AddParameter(name, type);
    }

    static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string name, AnimationClip clip)
    {
        // Check if state exists
        foreach (var state in sm.states)
        {
            if (state.state.name == name)
            {
                state.state.motion = clip;
                return state.state;
            }
        }
        // Create new
        var newState = sm.AddState(name);
        newState.motion = clip;
        return newState;
    }
}
