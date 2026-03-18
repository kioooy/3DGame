using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool tự động tìm và gán AnimationClip cho chức năng cưỡi Xén Tóc (Flying và Sitting)
/// Chạy tự động khi Unity compile xong.
/// </summary>
[InitializeOnLoad]
public class MountAnimationSetupTool
{
    static MountAnimationSetupTool()
    {
        EditorApplication.delayCall += SetupAnimations;
    }

    [MenuItem("Tools/Setup Mount Animations")]
    public static void SetupAnimations()
    {
        var controllers = Object.FindObjectsByType<MountXenTocController>(FindObjectsSortMode.None);
        if (controllers.Length == 0) return;

        string flyingPath = "Assets/Animation/Paladin J Nordstrom_Flying.fbx";
        string sittingPath = "Assets/Animation/Paladin J Nordstrom_Male Sitting Pose.fbx";

        AnimationClip flyingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(flyingPath);
        AnimationClip sittingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(sittingPath);

        bool changed = false;

        foreach (var ctrl in controllers)
        {
            if (ctrl.xenTocFlyingClip != flyingClip || ctrl.playerSittingClip != sittingClip)
            {
                ctrl.xenTocFlyingClip = flyingClip;
                ctrl.playerSittingClip = sittingClip;
                EditorUtility.SetDirty(ctrl);
                changed = true;
            }
        }

        if (changed)
        {
            Debug.Log("[MountAnimationSetupTool] 🪲 Đã tự động gán hoạt ảnh Flying và Sitting cho MountXenTocController trên Scene.");
        }
    }
}
