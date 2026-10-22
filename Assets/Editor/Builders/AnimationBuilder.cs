using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Genera todas las animaciones a partir de las hojas de sprites:
/// Run y Idle en 4 direcciones (blend trees 2D) para Alex y el panel holográfico.
/// Reutiliza player.controller para conservar la referencia del Animator en la escena.
/// </summary>
public static class AnimationBuilder
{
    const float RunFps = 12f;
    const float IdleFps = 4f;
    const float HoloFps = 4f;
    const string PlayerController = BuildPaths.Animation + "/player.controller";
    public const string HoloController = BuildPaths.Animation + "/holo_panel.controller";

    static readonly (string dir, string sheet, Vector2 vector)[] Directions =
    {
        ("Down", BuildPaths.RunDown, Vector2.down),
        ("Up", BuildPaths.RunUp, Vector2.up),
        ("Left", BuildPaths.RunLeft, Vector2.left),
        ("Right", BuildPaths.RunRight, Vector2.right)
    };

    public static Sprite[] LoadFrames(string sheetPath) =>
        AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
            .ToArray();

    public static void BuildAll()
    {
        BuildPaths.EnsureFolder(BuildPaths.Animation);
        BuildPlayer();
        BuildHolo();
        AssetDatabase.SaveAssets();
    }

    static void BuildPlayer()
    {
        var runClips = Directions.Select(d => (d.vector, clip: SaveClip($"Player_Run_{d.dir}", LoadFrames(d.sheet), RunFps))).ToArray();
        // Idle: respiración sutil alternando los fotogramas de apoyo (0 y 3) de cada hoja.
        var idleClips = Directions.Select(d =>
        {
            var frames = LoadFrames(d.sheet);
            return (d.vector, clip: SaveClip($"Player_Idle_{d.dir}", new[] { frames[0], frames[0], frames[3 % frames.Length] }, IdleFps));
        }).ToArray();

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerController)
                         ?? AnimatorController.CreateAnimatorControllerAtPath(PlayerController);
        ResetController(controller);
        foreach (var name in new[] { "MoveX", "MoveY", "LastX", "LastY", "Speed" })
        {
            controller.AddParameter(name, AnimatorControllerParameterType.Float);
        }

        var idleState = controller.CreateBlendTreeInController("Idle", out var idleTree, 0);
        ConfigureTree(idleTree, "LastX", "LastY", idleClips);
        var runState = controller.CreateBlendTreeInController("Run", out var runTree, 0);
        ConfigureTree(runTree, "MoveX", "MoveY", runClips);

        var machine = controller.layers[0].stateMachine;
        machine.defaultState = idleState;
        AddTransition(idleState, runState, AnimatorConditionMode.Greater);
        AddTransition(runState, idleState, AnimatorConditionMode.Less);
        EditorUtility.SetDirty(controller);
    }

    static void BuildHolo()
    {
        var frames = new[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>(BuildPaths.HoloF0),
            AssetDatabase.LoadAssetAtPath<Sprite>(BuildPaths.HoloF1)
        };
        var clip = SaveClip("Holo_Panel_On", frames, HoloFps);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(HoloController)
                         ?? AnimatorController.CreateAnimatorControllerAtPath(HoloController);
        ResetController(controller);
        var state = controller.layers[0].stateMachine.AddState("Holo");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
    }

    static void ResetController(AnimatorController controller)
    {
        foreach (var parameter in controller.parameters) controller.RemoveParameter(parameter);
        var machine = controller.layers[0].stateMachine;
        foreach (var child in machine.states) machine.RemoveState(child.state);
    }

    static void ConfigureTree(BlendTree tree, string paramX, string paramY, (Vector2 vector, AnimationClip clip)[] clips)
    {
        tree.blendType = BlendTreeType.SimpleDirectional2D;
        tree.blendParameter = paramX;
        tree.blendParameterY = paramY;
        foreach (var (vector, clip) in clips) tree.AddChild(clip, vector);
    }

    static void AddTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(mode, 0.01f, "Speed");
    }

    static AnimationClip SaveClip(string name, Sprite[] frames, float fps)
    {
        string path = $"{BuildPaths.Animation}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.frameRate = fps;

        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        }
        // Clave final repetida para que el último fotograma dure lo mismo que los demás.
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }
}
