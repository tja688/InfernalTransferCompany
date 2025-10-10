#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

[CustomEditor(typeof(UITweenController))]
public class UITweenControllerEditor : Editor
{
    private static Tween _previewTween;

    private UITweenController C => (UITweenController)target;

    private void OnDisable()
    {
        StopPreview();
    }

    public override void OnInspectorGUI()
    {
        // 标题
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("UI Tween Controller (Advanced V3)", EditorStyles.boldLabel);

        // --- 动画设置 ---
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
            DrawHelp("时长与缓动影响整体“体感速度”。微交互建议 0.15~0.6s；整面板进出 0.6~1.2s。自定义曲线开启后将覆盖 Ease。");
            C.duration = EditorGUILayout.Slider(new GUIContent("Duration (s)", "动画总时长（秒）。"),
                                                C.duration, 0.01f, 10f);
            C.easeType = (Ease)EditorGUILayout.EnumPopup(new GUIContent("Ease", "标准缓动类型。"),
                                                         C.easeType);

            C.useCustomCurve = EditorGUILayout.Toggle(new GUIContent("Use Custom Curve", "启用后优先使用自定义 AnimationCurve。"),
                                                      C.useCustomCurve);
            if (C.useCustomCurve)
            {
                C.customCurve = EditorGUILayout.CurveField(new GUIContent("Animation Curve", "横轴时间 0..1，纵轴进度 0..1。"),
                                                           C.customCurve);
            }

            C.useBezierPath = EditorGUILayout.Toggle(new GUIContent("Use Bezier Path", "沿二次 Bézier（A→C→B）运动。关闭则直线到目标点。"),
                                                     C.useBezierPath);
            C.showPathGizmos = EditorGUILayout.Toggle(new GUIContent("Show Path Gizmos", "在 Scene 视图绘制起点/终点/控制点与曲线，支持拖动控制点。"),
                                                      C.showPathGizmos);
        }

        // --- 颜色设置 ---
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Color / Opacity", EditorStyles.boldLabel);
            DrawHelp("AlphaOnly 走 CanvasGroup（更适合整块 UI 淡入淡出）；GraphicColor 直接改 Image/Text 颜色；Both 两者都改。");
            C.colorMode = (UITweenController.ColorMode)EditorGUILayout.EnumPopup(
                new GUIContent("Color Mode", "选择要改哪种颜色/透明度。"),
                C.colorMode
            );
        }

        // --- Anchor/Pivot 可选 ---
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Anchor & Pivot (Optional)", EditorStyles.boldLabel);
            DrawHelp("通常不建议运行时频繁改 Anchor/Pivot（可能带来布局复杂性）。除非明确需求，再开启此项加入补间。");
            C.tweenAnchorAndPivot = EditorGUILayout.Toggle(new GUIContent("Tween Anchor & Pivot", "将 AnchorMin/AnchorMax/Pivot 也纳入补间。"),
                                                           C.tweenAnchorAndPivot);
        }

        // --- 状态记录动作 ---
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("State Recording", EditorStyles.boldLabel);
            DrawHelp("操作流程建议：\n1) 把物体摆到“初始”样子 → 点 Record Initial\n2) 把物体摆到“目标”样子 → 点 Record Target\n3) Play Preview 查看效果；Stop 后会回到初始。\n必要时可手动 Reset Control Point（重算为 AB 中点）。");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Record Initial", "记录当前为初始状态；并将控制点重算为 AB 中点（刷新起点）。"), GUILayout.Height(28)))
                {
                    Undo.RecordObject(C, "Record Initial");
                    C.RecordInitialState();
                    EditorUtility.SetDirty(C);
                }

                if (GUILayout.Button(new GUIContent("Record Target", "记录当前为目标状态；仅重算控制点（不改起点）。"), GUILayout.Height(28)))
                {
                    Undo.RecordObject(C, "Record Target");
                    C.RecordTargetState();
                    EditorUtility.SetDirty(C);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Reset Control Point", "将控制点重置为 AB 中点；不会修改起点。"), GUILayout.Height(24)))
                {
                    Undo.RecordObject(C, "Reset Control Point");
                    C.ResetControlPoint(false);
                    EditorUtility.SetDirty(C);
                }

                if (GUILayout.Button(new GUIContent("Revert to Initial", "将物体恢复到已记录的初始状态。"), GUILayout.Height(24)))
                {
                    Undo.RecordObject(C.RectTransform, "Revert to Initial");
                    var cg = C.GetComponent<CanvasGroup>();
                    if (cg) Undo.RecordObject(cg, "Revert to Initial");
                    var g = C.GetComponent<Graphic>();
                    if (g) Undo.RecordObject(g, "Revert to Initial");
                    C.RevertToInitialState();
                    EditorUtility.SetDirty(C);
                }
            }
        }

        // --- 预览 ---
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Live Preview (Editor)", EditorStyles.boldLabel);
            DrawHelp("Play：在编辑器中循环预览（不需进入 Play 模式）；Stop：停止并自动回到初始状态。");

            bool isPreviewing = _previewTween != null && _previewTween.IsActive() && !_previewTween.IsComplete();
            GUI.backgroundColor = isPreviewing ? new Color(1.0f, 0.6f, 0.6f) : new Color(0.7f, 1.0f, 0.7f);
            string btn = isPreviewing ? "Stop Preview" : "Play Preview";
            if (GUILayout.Button(btn, GUILayout.Height(32)))
            {
                if (isPreviewing)
                {
                    StopPreview();
                    C.RevertToInitialState();
                }
                else
                {
                    PlayPreview();
                }
            }
            GUI.backgroundColor = Color.white;
        }
    }

    private void DrawHelp(string msg)
    {
        EditorGUILayout.HelpBox(msg, MessageType.Info);
    }

    private void PlayPreview()
    {
        StopPreview();
        // 预览前先回到起点，保证每次体验一致
        C.RevertToInitialState();
        _previewTween = C.CreateAnimationSequence()
                         .SetUpdate(true)            // 编辑器更新
                         .SetLoops(-1, LoopType.Restart)
                         .Play();
    }

    private void StopPreview()
    {
        if (_previewTween != null && _previewTween.IsActive())
            _previewTween.Kill();
        _previewTween = null;
    }

    // --------- Scene 视图路径/控制点操作 ----------
    private void OnSceneGUI()
    {
        if (!C.showPathGizmos) return;

        var rt = C.GetComponent<RectTransform>();
        if (rt == null || rt.parent == null) return;

        // 父变换：RectTransform.anchoredPosition 属于父本地坐标
        var parent = rt.parent as RectTransform;

        // 本地 → 世界
        Vector3 W(Vector2 local) => parent.TransformPoint(local);

        Vector2 a = C.StartPos;
        Vector2 b = C.EndPos;
        Vector2 p = C.pathControlPoint;

        Vector3 wa = W(a);
        Vector3 wb = W(b);
        Vector3 wp = W(p);

        // 画端点
        Handles.color = Color.green;
        Handles.SphereHandleCap(0, wa, Quaternion.identity, HandleUtility.GetHandleSize(wa) * 0.05f, EventType.Repaint);
        Handles.color = Color.red;
        Handles.SphereHandleCap(0, wb, Quaternion.identity, HandleUtility.GetHandleSize(wb) * 0.05f, EventType.Repaint);

        // 控制点可拖拽
        Handles.color = Color.cyan;
        EditorGUI.BeginChangeCheck();
        Vector3 wpNew = Handles.PositionHandle(wp, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(C, "Move Bezier Control Point");
            // 世界 → 父本地
            C.pathControlPoint = parent.InverseTransformPoint(wpNew);
            EditorUtility.SetDirty(C);
        }

        // 控制线
        Handles.color = new Color(0f, 0.8f, 1f, 0.5f);
        Handles.DrawLine(wa, wpNew);
        Handles.DrawLine(wpNew, wb);

        // 绘制二次 Bézier（用三次 API 传同一控制点两次）
        Handles.DrawBezier(wa, wb, wpNew, wpNew, new Color(0.2f, 1f, 1f, 0.8f), null, 2f);
    }
}
#endif
