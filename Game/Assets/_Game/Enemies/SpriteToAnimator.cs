using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SpriteToAnimator : EditorWindow
{
    private string baseName = "Orc";
    private int frameRate = 8;

    [System.Serializable]
    public class ActionConfig
    {
        public string name; 
        public string triggerName; 
        public Texture2D sheet;
        public int rows = 4;    
        public int columns = 4; 
        public bool loop = true;
    }

    // --- YOUR SPECIFIC CONFIGURATION ---
    public ActionConfig idle = new ActionConfig { name = "Idle", triggerName = "DoIdle", rows = 4, columns = 4, loop = true };
    public ActionConfig walk = new ActionConfig { name = "Walk", triggerName = "DoWalk", rows = 4, columns = 6, loop = true };
    public ActionConfig run = new ActionConfig { name = "Run", triggerName = "DoRun", rows = 4, columns = 8, loop = true };
    public ActionConfig attack = new ActionConfig { name = "Attack", triggerName = "Attack", rows = 4, columns = 8, loop = false };
    public ActionConfig death = new ActionConfig { name = "Death", triggerName = "Die", rows = 4, columns = 8, loop = false };

    [MenuItem("Tools/Animator Final (V6)")]
    public static void ShowWindow() { GetWindow<SpriteToAnimator>("Animator V6"); }

    void OnGUI()
    {
        GUILayout.Label("Directional Death & Triggers", EditorStyles.boldLabel);
        baseName = EditorGUILayout.TextField("Character Name", baseName);
        frameRate = EditorGUILayout.IntField("Frame Rate", frameRate);

        GUILayout.Space(10);
        DrawConfigField(idle);
        DrawConfigField(walk);
        DrawConfigField(run);
        DrawConfigField(attack);
        DrawConfigField(death);

        GUILayout.Space(20);
        if (GUILayout.Button("GENERATE ANIMATOR", GUILayout.Height(40)))
        {
            CreateAnimator();
        }
    }

    void DrawConfigField(ActionConfig config)
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label($"{config.name} [{config.rows} Rows x {config.columns} Cols]", EditorStyles.boldLabel);
        config.sheet = (Texture2D)EditorGUILayout.ObjectField(config.sheet, typeof(Texture2D), false);
        
        if (config.sheet != null) {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Rows:", GUILayout.Width(35)); config.rows = EditorGUILayout.IntField(config.rows, GUILayout.Width(30));
            GUILayout.Label("Cols:", GUILayout.Width(35)); config.columns = EditorGUILayout.IntField(config.columns, GUILayout.Width(30));
            config.loop = EditorGUILayout.ToggleLeft("Loop", config.loop, GUILayout.Width(50));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }

    void CreateAnimator()
    {
        if (idle.sheet == null && walk.sheet == null) { Debug.LogError("Need Idle or Walk!"); return; }

        string path = AssetDatabase.GetAssetPath(idle.sheet ?? walk.sheet);
        string folderPath = Path.GetDirectoryName(path);
        string controllerPath = Path.Combine(folderPath, baseName + "_Controller.controller");
        
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        
        // 1. Add Directional Params
        controller.AddParameter("Horizontal", AnimatorControllerParameterType.Float);
        controller.AddParameter("Vertical", AnimatorControllerParameterType.Float);
        
        // 2. Add Triggers
        AddParamIfSheetExists(controller, idle);
        AddParamIfSheetExists(controller, walk);
        AddParamIfSheetExists(controller, run);
        AddParamIfSheetExists(controller, attack);
        AddParamIfSheetExists(controller, death);

        // 3. Create States
        ProcessAction(controller, folderPath, idle);
        ProcessAction(controller, folderPath, walk);
        ProcessAction(controller, folderPath, run);
        ProcessAction(controller, folderPath, attack);
        ProcessAction(controller, folderPath, death);

        Debug.Log($"<color=green>SUCCESS!</color> Animator created at {controllerPath}");
    }

    void AddParamIfSheetExists(AnimatorController c, ActionConfig config)
    {
        if(config.sheet != null) c.AddParameter(config.triggerName, AnimatorControllerParameterType.Trigger);
    }

    void ProcessAction(AnimatorController controller, string folder, ActionConfig config)
    {
        if (config.sheet == null) return;

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState state = sm.AddState(config.name);
        List<Sprite> sprites = LoadSprites(config.sheet);

        // Logic: If Rows == 4, we make a Blend Tree (Directional)
        // This now applies to DEATH as well, since you set Rows=4 for it.
        if (config.rows == 4) {
            var clips = new List<AnimationClip> {
                CreateClip(folder, $"{baseName}_{config.name}_Down", sprites, 0, config.columns, config.loop),
                CreateClip(folder, $"{baseName}_{config.name}_Up", sprites, 1, config.columns, config.loop),
                CreateClip(folder, $"{baseName}_{config.name}_Left", sprites, 2, config.columns, config.loop),
                CreateClip(folder, $"{baseName}_{config.name}_Right", sprites, 3, config.columns, config.loop)
            };
            state.motion = CreateBlendTree(controller, config.name + "_Blend", clips);
        } else {
            state.motion = CreateClip(folder, $"{baseName}_{config.name}", sprites, 0, config.columns, config.loop);
        }

        // Trigger Connection (Any State -> This)
        var transition = sm.AddAnyStateTransition(state);
        transition.AddCondition(AnimatorConditionMode.If, 0, config.triggerName);
        transition.duration = 0; 
        transition.canTransitionToSelf = false; 
    }

    AnimationClip CreateClip(string f, string n, List<Sprite> s, int r, int c, bool l) {
        AnimationClip clip = new AnimationClip { frameRate = frameRate };
        AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings { loopTime = l });
        ObjectReferenceKeyframe[] kf = new ObjectReferenceKeyframe[c];
        for (int i=0; i<c; i++) { 
            int idx = r*c+i; 
            if(idx<s.Count) kf[i] = new ObjectReferenceKeyframe { time=i*(1f/frameRate), value=s[idx] }; 
        }
        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { type=typeof(SpriteRenderer), path="", propertyName="m_Sprite"}, kf);
        AssetDatabase.CreateAsset(clip, Path.Combine(f, n+".anim"));
        return clip;
    }

    BlendTree CreateBlendTree(AnimatorController c, string n, List<AnimationClip> clips) {
        BlendTree t; c.CreateBlendTreeInController(n, out t); 
        t.blendType = BlendTreeType.SimpleDirectional2D;
        t.blendParameter = "Horizontal"; t.blendParameterY = "Vertical";
        t.AddChild(clips[0], new Vector2(0, -1)); t.AddChild(clips[1], new Vector2(0, 1));
        t.AddChild(clips[2], new Vector2(-1, 0)); t.AddChild(clips[3], new Vector2(1, 0));
        return t;
    }

    List<Sprite> LoadSprites(Texture2D s) {
        return AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(s)).OfType<Sprite>().OrderBy(x=>EditorUtility.NaturalCompare(x.name,x.name)).ToList();
    }
}