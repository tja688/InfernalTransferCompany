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
        public Vector2 pos, size;
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
    }

    void OnPlayModeChanged(PlayModeStateChange s) => SafeStopPreview(inEditorDisable: true);

    public override void OnInspectorGUI()
    {
        if (target == null) return;
        C = (UITweenController)target;
        serializedObject.Update();

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Preset Binding", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("boundPreset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoSaveToPreset"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Save Now", "將當前所有參數寫入 boundPreset。"), GUILayout.Height(22)))
                {
                    if (C.boundPreset != null) { Undo.RecordObject(C.boundPreset, "Save Preset"); C.SaveToPreset(C.boundPreset); }
                }
                if (GUILayout.Button(new GUIContent("Load From Preset", "從 boundPreset 讀回參數。"), GUILayout.Height(22)))
                {
                    if (C.boundPreset != null) { Undo.RecordObject(C, "Load Preset"); C.LoadFromPreset(C.boundPreset); }
                }
            }
        }

        EditorGUILayout.Space();
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Mode", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useRelativeMode"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (C.useRelativeMode)
                {
                    C.easeType = Ease.OutBack;
                    C.useCustomCurve = false;
                }
            }

            if (C.useRelativeMode)
            {
                EditorGUILayout.HelpBox("相對模式：所有變換屬性均為【偏移量】。此模式下為直線運動。", MessageType.Info);

                if (GUILayout.Button("復位偏移量 (Reset Offsets)"))
                {
                    Undo.RecordObject(C, "Reset Tween Offsets");
                    C.targetAnchoredPosition = Vector2.zero;
                    C.targetSizeDelta = Vector2.zero;
                    C.targetEulerZ = 0f;
                    EditorUtility.SetDirty(C);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useBezierPath"));
                if (C.useBezierPath)
                {
                    EditorGUILayout.HelpBox("絕對模式+曲線路徑：可拖動Gizmo調節運動軌跡。", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("絕對模式+直線路徑：點對點的直線運動。", MessageType.Info);
                }
            }
        }

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
            string targetLabel = C.useRelativeMode ? "Target B (Offsets)" : "Target B (Absolute)";
            EditorGUILayout.LabelField(targetLabel, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetAnchoredPosition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetSizeDelta"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetPivot"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetEulerZ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetAlpha"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetColor"));
            
            // ==================== 修正區域：將按鈕組加回來 ====================
            // 這些按鈕只在絕對模式下有意義
            if (!C.useRelativeMode)
            {
                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("Capture Target", "將當前對象狀態記錄為目標 B。"), GUILayout.Height(24)))
                    {
                        Undo.RecordObject(C, "Capture Target");
                        C.CaptureTargetFromCurrent();
                        EditorUtility.SetDirty(C);
                    }
                    
                    // 只有在啟用貝塞爾曲線時，設置C點的按鈕才有意義
                    if (C.useBezierPath)
                    {
                        if (GUILayout.Button(new GUIContent("Set Pass C = Current", "將必經點 C 設為當前對象位置。"), GUILayout.Height(24)))
                        {
                            Undo.RecordObject(C, "Set Pass C From Current");
                            C.SetPassPointFromCurrent();
                            EditorUtility.SetDirty(C);
                        }

                        if (GUILayout.Button(new GUIContent("Pass C = Mid", "將必經點 C 設為當前與目標位置的中點。"), GUILayout.Height(24)))
                        {
                            Undo.RecordObject(C, "Set Pass C Mid");
                            C.SetPassPointToMidCurrentAndTarget();
                            EditorUtility.SetDirty(C);
                        }
                    }
                }
            }
            // =================================================================
        }

        if (!C.useRelativeMode && C.useBezierPath)
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Pass-Through C (Bézier Path)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("passThroughPointC"));
                EditorGUILayout.Slider(serializedObject.FindProperty("passTStar"), 0.05f, 0.95f);
            }
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
        EditorGUILayout.PropertyField(serializedObject.FindProperty("showPathGizmos"));

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Live Preview", EditorStyles.boldLabel);
            bool isPreviewing = _previewTween != null && _previewTween.IsActive();
            GUI.backgroundColor = isPreviewing ? Color.red : Color.green;
            if (GUILayout.Button(isPreviewing ? "Stop Preview" : "Play Preview", GUILayout.Height(30)))
            {
                if (isPreviewing) SafeStopPreview();
                else SafePlayPreview();
            }
            GUI.backgroundColor = Color.white;
        }

        serializedObject.ApplyModifiedProperties();
    }
    
    // ... (SafePlayPreview, SafeStopPreview, OnSceneGUI methods remain the same) ...

    void SafePlayPreview()
    {
        SafeStopPreview();
        var rt = C.GetComponent<RectTransform>();
        if (rt == null) return;
        var cg = C.GetComponent<CanvasGroup>();
        var g = C.GetComponent<UnityEngine.UI.Graphic>();

        _snap.pos = rt.anchoredPosition; _snap.size = rt.sizeDelta; _snap.euler = rt.eulerAngles;
        _snap.alpha = cg ? cg.alpha : (g ? g.color.a : 1f);
        _snap.color = g ? g.color : Color.white;
        _hasSnap = true;

        _previewTween = C.CreateAnimationSequence().SetUpdate(true).SetLoops(-1, LoopType.Yoyo).Play();
    }

    void SafeStopPreview(bool inEditorDisable = false)
    {
        if (_previewTween != null && _previewTween.IsActive()) _previewTween.Kill();
        if (!_hasSnap || C == null) return;

        var rt = C.GetComponent<RectTransform>();
        if (rt != null)
        {
            Undo.RecordObject(rt, "Restore Preview");
            rt.anchoredPosition = _snap.pos; rt.sizeDelta = _snap.size; rt.eulerAngles = _snap.euler;
        }

        var cg = C.GetComponent<CanvasGroup>();
        if (cg != null) { Undo.RecordObject(cg, "Restore Snapshot"); cg.alpha = _snap.alpha; }
        var g = C.GetComponent<UnityEngine.UI.Graphic>();
        if (g != null) { Undo.RecordObject(g, "Restore Snapshot"); g.color = _snap.color; }

        if (!inEditorDisable) _hasSnap = false;
    }

    void OnSceneGUI(SceneView view)
    {
        if (C == null || !C.showPathGizmos || C.useRelativeMode || !C.useBezierPath)
        {
            return;
        }

        var rt = C.GetComponent<RectTransform>();
        var parent = rt.parent as RectTransform;
        if (rt == null || parent == null) return;
        Vector3 W(Vector2 local) => parent.TransformPoint(local);

        Vector2 A = _hasSnap ? _snap.pos : rt.anchoredPosition;
        Vector2 B = C.TargetPos;
        Vector2 Cc = C.PassPointC;

        float tStar = Mathf.Clamp(C.PassTStar, 0.05f, 0.95f);
        Vector2 P = UITweenController.SolveQuadraticControlPoint(A, B, Cc, tStar);

        Vector3 wa = W(A), wb = W(B), wc = W(Cc), wp = W(P);
        Handles.color = Color.green; Handles.SphereHandleCap(0, wa, Quaternion.identity, HandleUtility.GetHandleSize(wa) * 0.05f, EventType.Repaint);
        Handles.color = Color.red; Handles.SphereHandleCap(0, wb, Quaternion.identity, HandleUtility.GetHandleSize(wb) * 0.05f, EventType.Repaint);

        Handles.color = Color.cyan;
        EditorGUI.BeginChangeCheck();
        Vector3 wcNew = Handles.PositionHandle(wc, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(C, "Move Pass-Through C");
            C.passThroughPointC = parent.InverseTransformPoint(wcNew);
            EditorUtility.SetDirty(C);
        }

        Handles.color = new Color(0f, 0.8f, 1f, 0.5f);
        Handles.DrawLine(wa, wp); Handles.DrawLine(wp, wb);
        Handles.DrawBezier(wa, wb, wp, wp, new Color(0.2f, 1f, 1f, 0.8f), null, 2f);

        Handles.Label(wa, " A (Start)");
        Handles.Label(wb, " B (Target)");
        Handles.Label(wcNew, " C (Pass-through)");
    }
}
#endif