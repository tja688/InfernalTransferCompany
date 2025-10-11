// MIT License
// Custom inspector & live preview + preset bridge

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DG.Tweening;

[CustomEditor(typeof(UITweenController))]
public class UITweenControllerEditor : Editor
{
    UITweenController C;

    struct Snap
    {
        public Vector2 pos, size, anchorMin, anchorMax, pivot;
        public Vector3 euler;
        public float alpha;
        public Color color;
    }

    Snap _snap;
    bool _hasSnap = false;
    Tween _previewTween;

    void OnEnable()
    {
        C = (UITweenController)target;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SafeStopPreview(inEditorDisable: true);
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        SceneView.duringSceneGui -= OnSceneGUI;
        _previewTween = null;
        _hasSnap = false;
        C = null;
    }

    void OnPlayModeChanged(PlayModeStateChange s) => SafeStopPreview(inEditorDisable: true);

    public override void OnInspectorGUI()
    {
        if (target == null) return;
        if (C == null) C = (UITweenController)target;

        serializedObject.Update();

        // —— 新增：Preset Binding —— //
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Preset Binding", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("boundPreset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoSaveToPreset"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Save Now", "将当前所有参数写入 boundPreset（若已绑定）。"), GUILayout.Height(22)))
                {
                    var p = C.boundPreset;
                    if (p == null) EditorUtility.DisplayDialog("Save Preset", "未绑定 Preset。", "OK");
                    else { Undo.RecordObject(p, "Save Preset"); C.SaveToPreset(p); EditorUtility.SetDirty(p); }
                }
                if (GUILayout.Button(new GUIContent("Load From Preset", "从 boundPreset 读回参数覆盖当前。"), GUILayout.Height(22)))
                {
                    var p = C.boundPreset;
                    if (p == null) EditorUtility.DisplayDialog("Load Preset", "未绑定 Preset。", "OK");
                    else { Undo.RecordObject(C, "Load Preset"); C.LoadFromPreset(p); EditorUtility.SetDirty(C); }
                }
            }
            EditorGUILayout.HelpBox("可把本脚本的配置写入 ScriptableObject（可命名、复用、跨对象播放）。勾选 Auto Save 将在参数变更时自动同步。", MessageType.Info);
        }

        // —— 原有参数区（保持不变） —— //
        EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("delay"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loops"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loopType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("unscaledTime"));

        EditorGUILayout.Space();
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Easing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useCustomCurve"));
            if (C.useCustomCurve)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customCurve"));
            else
                EditorGUILayout.PropertyField(serializedObject.FindProperty("easeType"));
        }

        EditorGUILayout.Space();
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Target B（最终目标）", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetAnchoredPosition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetSizeDelta"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetPivot"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetEulerZ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetAlpha"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetColor"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Capture Target", "将当前对象状态记录为目标 B。"), GUILayout.Height(24)))
                {
                    Undo.RecordObject(C, "Capture Target");
                    C.CaptureTargetFromCurrent();
                    if (C.autoSaveToPreset && C.boundPreset != null) C.SaveToPreset(C.boundPreset, keepPresetName:true);
                    EditorUtility.SetDirty(C);
                }

                if (GUILayout.Button(new GUIContent("Set Pass C = Current", "将必经点 C 设为当前对象位置（父本地）。"), GUILayout.Height(24)))
                {
                    Undo.RecordObject(C, "Set Pass C From Current");
                    C.SetPassPointFromCurrent();
                    if (C.autoSaveToPreset && C.boundPreset != null) C.SaveToPreset(C.boundPreset, keepPresetName:true);
                    EditorUtility.SetDirty(C);
                }

                if (GUILayout.Button(new GUIContent("Pass C = Mid(Current,Target)", "将必经点 C 设为当前与目标位置的中点。"), GUILayout.Height(24)))
                {
                    Undo.RecordObject(C, "Set Pass C Mid");
                    C.SetPassPointToMidCurrentAndTarget();
                    if (C.autoSaveToPreset && C.boundPreset != null) C.SaveToPreset(C.boundPreset, keepPresetName:true);
                    EditorUtility.SetDirty(C);
                }
            }
        }

        EditorGUILayout.Space();
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Pass-Through（途中必经）", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("passThroughPointC"));
            EditorGUILayout.Slider(serializedObject.FindProperty("passTStar"), 0.05f, 0.95f);
            EditorGUILayout.HelpBox("保证在 t* 时刻经过 C；运行时动态反解 Bézier 控制点。", MessageType.Info);
        }

        EditorGUILayout.Space();
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Animate What", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animatePosition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animateSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animateRotationZ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animateAlpha"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animateColor"));
        }

        EditorGUILayout.Space();
        C.showPathGizmos = EditorGUILayout.ToggleLeft("Show Path Gizmos (Scene)", C.showPathGizmos);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Live Preview (Editor)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("预览前快照当前；停止后自动还原（对象存在时）。", MessageType.Info);
            bool isPreviewing = _previewTween != null && _previewTween.IsActive() && !_previewTween.IsComplete();
            GUI.backgroundColor = isPreviewing ? new Color(1f,0.6f,0.6f) : new Color(0.7f,1f,0.7f);
            if (GUILayout.Button(isPreviewing ? "Stop Preview" : "Play Preview", GUILayout.Height(30)))
            {
                if (isPreviewing) SafeStopPreview();
                else SafePlayPreview();
            }
            GUI.backgroundColor = Color.white;
        }

        serializedObject.ApplyModifiedProperties();
    }

    // —— 预览与 Scene 可视化（与你当前版本一致，已做缺失引用防护） —— //

    void SafePlayPreview()
    {
        SafeStopPreview();

        if (C == null) return;
        var rt = C ? C.GetComponent<RectTransform>() : null;
        if (rt == null) return;

        var cg = C.GetComponent<CanvasGroup>();
        var g  = C.GetComponent<UnityEngine.UI.Graphic>();

        _snap.pos = rt.anchoredPosition; _snap.size = rt.sizeDelta;
        _snap.anchorMin = rt.anchorMin; _snap.anchorMax = rt.anchorMax; _snap.pivot = rt.pivot;
        _snap.euler = rt.eulerAngles;
        _snap.alpha = cg ? cg.alpha : (g ? g.color.a : 1f);
        _snap.color = g ? g.color : Color.white;
        _hasSnap = true;

        var seq = C.CreateAnimationSequence();
        if (seq == null) return;

        _previewTween = seq.SetUpdate(true).SetLoops(-1, LoopType.Restart).Play();
    }

    void SafeStopPreview(bool inEditorDisable = false)
    {
        if (_previewTween != null && _previewTween.IsActive()) { try { _previewTween.Kill(); } catch { } }
        _previewTween = null;
        if (!_hasSnap) return;

        if (C != null)
        {
            var rt = C.GetComponent<RectTransform>();
            if (rt != null)
            {
                Undo.RecordObject(rt, "Restore Snapshot");
                rt.anchorMin = _snap.anchorMin; rt.anchorMax = _snap.anchorMax; rt.pivot = _snap.pivot;
                rt.sizeDelta = _snap.size; rt.anchoredPosition = _snap.pos; rt.eulerAngles = _snap.euler;
            }
            var cg = C.GetComponent<CanvasGroup>();
            if (cg != null) { Undo.RecordObject(cg, "Restore Snapshot"); cg.alpha = _snap.alpha; }
            var g = C.GetComponent<UnityEngine.UI.Graphic>();
            if (g != null) { Undo.RecordObject(g, "Restore Snapshot"); g.color = _snap.color; }
        }
        if (!inEditorDisable) _hasSnap = false;
    }

    void OnSceneGUI(SceneView view)
    {
        if (C == null || !C.showPathGizmos) return;
        var rt = C.GetComponent<RectTransform>(); if (rt == null || rt.parent == null) return;
        var parent = rt.parent as RectTransform; if (parent == null) return;
        Vector3 W(Vector2 local) => parent.TransformPoint(local);

        Vector2 A = _hasSnap ? _snap.pos : rt.anchoredPosition;
        Vector2 B = C.TargetPos;
        Vector2 Cc = C.PassPointC;
        float tStar = Mathf.Clamp(C.PassTStar, 0.05f, 0.95f);
        Vector2 P = UITweenController.SolveQuadraticControlPoint(A, B, Cc, tStar);

        Vector3 wa = W(A), wb = W(B), wc = W(Cc), wp = W(P);
        Handles.color = Color.green; Handles.SphereHandleCap(0, wa, Quaternion.identity, HandleUtility.GetHandleSize(wa)*0.05f, EventType.Repaint);
        Handles.color = Color.red;    Handles.SphereHandleCap(0, wb, Quaternion.identity, HandleUtility.GetHandleSize(wb)*0.05f, EventType.Repaint);

        Handles.color = Color.cyan;
        EditorGUI.BeginChangeCheck();
        Vector3 wcNew = Handles.PositionHandle(wc, Quaternion.identity);
        if (EditorGUI.EndChangeCheck() && C != null)
        {
            Undo.RecordObject(C, "Move Pass-Through C");
            C.passThroughPointC = parent.InverseTransformPoint(wcNew);
            if (C.autoSaveToPreset && C.boundPreset != null) C.SaveToPreset(C.boundPreset, keepPresetName:true);
            EditorUtility.SetDirty(C);
        }

        Handles.color = new Color(0f, 0.8f, 1f, 0.5f);
        Handles.DrawLine(wa, wp); Handles.DrawLine(wp, wb);
        Handles.DrawBezier(wa, wb, wp, wp, new Color(0.2f,1f,1f,0.8f), null, 2f);

        Handles.Label(wa, new GUIContent(" A (start: preview snapshot / current)"));
        Handles.Label(wb, new GUIContent(" B (target)"));
        Handles.Label(wcNew,new GUIContent(" C (pass-through)"));
    }
}
#endif
