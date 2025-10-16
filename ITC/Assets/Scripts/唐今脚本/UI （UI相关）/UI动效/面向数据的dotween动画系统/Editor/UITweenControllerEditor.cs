#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

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
    bool _hasSnap = false; // 是否已经捕获快照并进入预览模式
    Tween _previewTween;
    List<Action> _restoreActions = new List<Action>();

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

        // --- Inspector 面板 ---
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

        EditorGUILayout.Space();
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Secondary Animations", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("secondaryTweens"), true);
        }

        EditorGUILayout.Space();
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Timeline Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("timelineEvents"), true);
        }

        // ======= 可视化时间轴 =======
        EditorGUILayout.Space();
        DrawTimelineEditor();

        EditorGUILayout.Space();
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Live Preview", EditorStyles.boldLabel);
            GUI.backgroundColor = _hasSnap ? Color.red : Color.green;
            if (GUILayout.Button(_hasSnap ? "Stop Preview" : "Play Preview", GUILayout.Height(30)))
            {
                if (_hasSnap) SafeStopPreview();
                else SafePlayPreview();
            }
            GUI.backgroundColor = Color.white;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTimelineEditor()
    {
        EditorGUILayout.Space();
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Visual Timeline Editor", EditorStyles.boldLabel);

            float totalDuration = Mathf.Max(0f, C.duration);
            if (totalDuration <= 0f)
            {
                EditorGUILayout.HelpBox("Duration must be greater than 0 to display the timeline.", MessageType.Info);
                return;
            }

            Rect timelineRect = EditorGUILayout.GetControlRect(false, 160f);
            EditorGUI.DrawRect(timelineRect, new Color(0.12f, 0.12f, 0.12f));

            Handles.BeginGUI();
            DrawTimelineGrid(timelineRect, totalDuration);

            int curveIndex = 0;
            if (C.animatePosition)
                DrawPropertyCurve(timelineRect, totalDuration, "Position", Color.cyan, curveIndex++, GetValueAtTime);
            if (C.animateSize)
                DrawPropertyCurve(timelineRect, totalDuration, "Size", Color.yellow, curveIndex++, GetValueAtTime);
            if (C.animateRotationZ)
                DrawPropertyCurve(timelineRect, totalDuration, "Rotation", Color.magenta, curveIndex++, GetValueAtTime);
            if (C.animateAlpha)
                DrawPropertyCurve(timelineRect, totalDuration, "Alpha", Color.green, curveIndex++, GetValueAtTime);
            if (C.animateColor)
                DrawPropertyCurve(timelineRect, totalDuration, "Color", Color.white, curveIndex++, GetValueAtTime);

            // 副轨道：曲线+左上角图例一次性标注
            var secondaryProp = serializedObject.FindProperty("secondaryTweens");
            if (secondaryProp != null && secondaryProp.isArray)
            {
                for (int i = 0; i < secondaryProp.arraySize; i++)
                {
                    Color curveColor = Color.HSVToRGB((i * 0.2f) % 1f, 0.8f, 1f);
                    DrawSecondaryCurve(timelineRect, totalDuration, secondaryProp.GetArrayElementAtIndex(i), curveColor, curveIndex++);
                }
            }

            // 精简副轨道时间标记（仅细线）
            DrawSecondaryMarkers(timelineRect, totalDuration, secondaryProp, new Color(1f, 0.8f, 0f, 0.9f));

            // 事件节点：仅竖线，不再标注名称
            var eventProp = serializedObject.FindProperty("timelineEvents");
            DrawTimelineEventMarkers(timelineRect, totalDuration, eventProp, new Color(0.2f, 1f, 0.4f, 0.9f));

            Handles.EndGUI();
        }
    }

    private void DrawTimelineGrid(Rect rect, float duration)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.1f);
        Handles.DrawSolidRectangleWithOutline(rect, new Color(1f, 1f, 1f, 0.05f), new Color(1f, 1f, 1f, 0.15f));

        int gridLines = 10;
        for (int i = 0; i <= gridLines; i++)
        {
            float normalized = i / (float)gridLines;
            float x = Mathf.Lerp(rect.x, rect.xMax, normalized);
            Handles.color = new Color(1f, 1f, 1f, 0.08f);
            Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.yMax));

            GUI.Label(new Rect(x - 20f, rect.y - 18f, 50f, 16f), (duration * normalized).ToString("F2"), EditorStyles.whiteMiniLabel);
        }
        Handles.color = Color.white;
    }

    private void DrawPropertyCurve(Rect rect, float duration, string label, Color color, int index, Func<float, float> evaluate)
    {
        const int steps = 60;
        Vector3[] points = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float normalizedTime = i / (float)steps;
            float value = Mathf.Clamp01(evaluate(normalizedTime));
            float x = Mathf.Lerp(rect.x, rect.xMax, normalizedTime);
            float y = Mathf.Lerp(rect.yMax - 8f, rect.y + 24f, value);
            points[i] = new Vector3(x, y);
        }

        Handles.color = color;
        Handles.DrawAAPolyLine(2f, points);

        var originalColor = GUI.color;
        GUI.color = color;
        GUI.Label(new Rect(rect.x + 6f, rect.y + 6f + index * 16f, 160f, 16f), label, EditorStyles.whiteMiniLabel);
        GUI.color = originalColor;
    }

    // —— 精简后的副轨道曲线：仅左上角图例标注一次，不跟随曲线重复显示 —— 
    private void DrawSecondaryCurve(Rect rect, float totalDuration, SerializedProperty secondaryTweenProp, Color color, int legendIndex)
    {
        if (secondaryTweenProp == null) return;

        float startTime = secondaryTweenProp.FindPropertyRelative("startTime")?.floatValue ?? 0f;
        float duration = secondaryTweenProp.FindPropertyRelative("duration")?.floatValue ?? 0f;
        SerializedProperty easeProp = secondaryTweenProp.FindPropertyRelative("easeType");
        Ease easeType = easeProp != null ? (Ease)easeProp.enumValueIndex : Ease.Linear;

        // 简洁模块名：优先使用 propertyType 的显示名
        string label;
        var propTypeProp = secondaryTweenProp.FindPropertyRelative("propertyType");
        if (propTypeProp != null && propTypeProp.enumDisplayNames != null && propTypeProp.enumValueIndex >= 0)
            label = propTypeProp.enumDisplayNames[propTypeProp.enumValueIndex];
        else
            label = "Secondary";

        if (duration <= 0f) return;

        const int steps = 50;
        Vector3[] points = new Vector3[steps + 1];

        // 采样以做Y轴归一化
        float minValue = float.MaxValue, maxValue = float.MinValue;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float v = DOVirtual.EasedValue(0f, 1f, t, easeType);
            if (v < minValue) minValue = v;
            if (v > maxValue) maxValue = v;
        }
        if (Mathf.Approximately(maxValue, minValue)) maxValue = minValue + 1f;

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float eased = DOVirtual.EasedValue(0f, 1f, t, easeType);
            float y01 = Mathf.InverseLerp(minValue, maxValue, eased);
            float absoluteTime = startTime + duration * t;
            float x = TimeToPixel(rect, totalDuration, absoluteTime);
            float y = Mathf.Lerp(rect.yMax - 8f, rect.y + 24f, y01);
            points[i] = new Vector3(x, y);
        }

        Handles.color = color;
        Handles.DrawAAPolyLine(2f, points);

        // 左上角图例：一次且仅一次
        var originalColor = GUI.color;
        GUI.color = color;
        GUI.Label(new Rect(rect.x + 6f, rect.y + 6f + legendIndex * 16f, 160f, 16f), label, EditorStyles.whiteMiniLabel);
        GUI.color = originalColor;
    }

    // —— 精简副轨道时间标记：仅细线，无名称、无粗矩形 —— 
    private void DrawSecondaryMarkers(Rect rect, float duration, SerializedProperty listProp, Color color)
    {
        if (listProp == null || !listProp.isArray) return;

        for (int i = 0; i < listProp.arraySize; i++)
        {
            var element = listProp.GetArrayElementAtIndex(i);
            float start = Mathf.Max(0f, element.FindPropertyRelative("startTime")?.floatValue ?? 0f);
            float span  = Mathf.Max(0f, element.FindPropertyRelative("duration")?.floatValue ?? 0f);
            float xStart = TimeToPixel(rect, duration, start);
            float xEnd   = TimeToPixel(rect, duration, start + span);

            Handles.color = new Color(color.r, color.g, color.b, 0.35f);
            Handles.DrawLine(new Vector3(xStart, rect.yMax - 6f), new Vector3(xEnd, rect.yMax - 6f));
        }
        Handles.color = Color.white;
    }

    // —— 精简事件标记：只画竖线，不显示名称 —— 
    private void DrawTimelineEventMarkers(Rect rect, float duration, SerializedProperty listProp, Color color)
    {
        if (listProp == null || !listProp.isArray) return;

        Handles.color = color;
        for (int i = 0; i < listProp.arraySize; i++)
        {
            var element = listProp.GetArrayElementAtIndex(i);
            float fireTime = Mathf.Max(0f, element.FindPropertyRelative("fireTime")?.floatValue ?? 0f);
            float x = TimeToPixel(rect, duration, fireTime);
            Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.yMax));
        }
        Handles.color = Color.white;
    }

    private float GetValueAtTime(float normalizedTime)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);
        if (C.useCustomCurve && C.customCurve != null)
            return Mathf.Clamp01(C.customCurve.Evaluate(normalizedTime));
        return Mathf.Clamp01(DOVirtual.EasedValue(0f, 1f, normalizedTime, C.easeType));
    }

    private float TimeToPixel(Rect rect, float duration, float time)
    {
        if (duration <= 0f) return rect.x;
        float normalized = Mathf.Clamp01(time / duration);
        return Mathf.Lerp(rect.x, rect.xMax, normalized);
    }

    void SafePlayPreview()
    {
        SafeStopPreview();
        var rt = C.GetComponent<RectTransform>();
        if (rt == null) return;
        var cg = C.GetComponent<CanvasGroup>();
        var g  = C.GetComponent<UnityEngine.UI.Graphic>();

        // 捕获快照
        _snap.pos = rt.anchoredPosition;
        _snap.size = rt.sizeDelta;
        _snap.euler = rt.eulerAngles;
        _snap.alpha = cg ? cg.alpha : (g ? g.color.a : 1f);
        _snap.color = g ? g.color : Color.white;
        _hasSnap = true;

        _restoreActions.Clear();

        if (C.timelineEvents != null)
        {
            foreach (var timelineEvent in C.timelineEvents)
            {
                if (timelineEvent == null) continue;
                if (timelineEvent.eventType == TimelineEventType.ChangeSprite && timelineEvent.targetImage != null)
                {
                    var targetImage = timelineEvent.targetImage;
                    Sprite originalSprite = targetImage.sprite;
                    _restoreActions.Add(() =>
                    {
                        if (targetImage == null) return;
                        Undo.RecordObject(targetImage, "Restore Timeline Event Sprite");
                        targetImage.sprite = originalSprite;
                    });
                }
            }
        }

        bool scaleCaptured = false;
        if (C.secondaryTweens != null)
        {
            foreach (var secTween in C.secondaryTweens)
            {
                if (secTween == null) continue;
                if (secTween.propertyType == SecondaryTweenType.Scale && !scaleCaptured)
                {
                    Vector3 originalScale = rt.localScale;
                    _restoreActions.Add(() =>
                    {
                        if (rt == null) return;
                        Undo.RecordObject(rt, "Restore Secondary Scale");
                        rt.localScale = originalScale;
                    });
                    scaleCaptured = true;
                }
            }
        }

        _previewTween = C.CreateAnimationSequence()
                         .SetUpdate(true)
                         .SetLoops(1)
                         .Play();
    }
    
    void SafeStopPreview(bool inEditorDisable = false)
    {
        if (_previewTween != null && _previewTween.IsActive()) _previewTween.Kill();

        if (!_hasSnap || C == null) return;

        var rt = C.GetComponent<RectTransform>();
        if (rt != null)
        {
            Undo.RecordObject(rt, "Restore Preview");
            rt.anchoredPosition = _snap.pos;
            rt.sizeDelta = _snap.size;
            rt.eulerAngles = _snap.euler;
        }

        var cg = C.GetComponent<CanvasGroup>();
        if (cg != null) { Undo.RecordObject(cg, "Restore Snapshot"); cg.alpha = _snap.alpha; }
        
        var g = C.GetComponent<UnityEngine.UI.Graphic>();
        if (g != null) { Undo.RecordObject(g, "Restore Snapshot"); g.color = _snap.color; }

        if (_restoreActions != null)
        {
            foreach (var action in _restoreActions) action?.Invoke();
            _restoreActions.Clear();
        }

        if (!inEditorDisable) _hasSnap = false;
    }

    void OnSceneGUI(SceneView view)
    {
        if (C == null || !C.showPathGizmos || C.useRelativeMode || !C.useBezierPath) return;

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
        Handles.color = Color.red;   Handles.SphereHandleCap(0, wb, Quaternion.identity, HandleUtility.GetHandleSize(wb) * 0.05f, EventType.Repaint);

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
