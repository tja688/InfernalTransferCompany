#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DG.Tweening;

[CustomEditor(typeof(UITweenController))]
public class UITweenControllerEditor : Editor
{
    private static Tween _previewTween;
    // 序列化屬性
    private SerializedProperty durationProp, useAnimationCurveProp, easeTypeProp, customEaseCurveProp, animateColorProp, usePathProp;
    private Transform pathRefObject;

    private void OnEnable()
    {
        durationProp = serializedObject.FindProperty("duration");
        useAnimationCurveProp = serializedObject.FindProperty("useAnimationCurve");
        easeTypeProp = serializedObject.FindProperty("easeType");
        customEaseCurveProp = serializedObject.FindProperty("customEaseCurve");
        animateColorProp = serializedObject.FindProperty("animateColor");
        usePathProp = serializedObject.FindProperty("usePath");
    }
    
    private void OnDisable()
    {
        StopPreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        UITweenController controller = (UITweenController)target;

        // --- 動畫設定 ---
        EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(durationProp);
        EditorGUILayout.PropertyField(animateColorProp);
        
        // --- 緩動設定 ---
        EditorGUILayout.PropertyField(useAnimationCurveProp);
        if (useAnimationCurveProp.boolValue)
        {
            EditorGUILayout.PropertyField(customEaseCurveProp);
        }
        else
        {
            EditorGUILayout.PropertyField(easeTypeProp);
        }

        // --- 路徑設定 ---
        EditorGUILayout.PropertyField(usePathProp);
        if (usePathProp.boolValue)
        {
            EditorGUILayout.LabelField("Path Controls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("啟用路徑模式後，請在Scene視圖中直接拖動控制點來調整運動軌跡。", MessageType.Info);
            pathRefObject = (Transform)EditorGUILayout.ObjectField("途經點參考物件", pathRefObject, typeof(Transform), true);
            if (pathRefObject != null)
            {
                if (GUILayout.Button("從參考物件烘焙途經點"))
                {
                    Vector2 localPoint = WorldToCanvasLocal(controller.RectTransform, pathRefObject.position);
                    Undo.RecordObject(controller, "Bake Path Control Point");
                    controller.SetControlPoint(localPoint);
                    EditorUtility.SetDirty(controller);
                    pathRefObject = null;
                }
            }
            if (GUILayout.Button("重置路徑為直線"))
            {
                Undo.RecordObject(controller, "Reset Path");
                controller.ResetControlPoint();
                EditorUtility.SetDirty(controller);
            }
        }
        
        serializedObject.ApplyModifiedProperties();

        // --- 狀態控制 ---
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("State & Preview", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Record Initial State", GUILayout.Height(30)))
        {
            Undo.RecordObject(controller, "Record Initial State");
            controller.RecordInitialState();
            EditorUtility.SetDirty(controller);
        }
        if (GUILayout.Button("Record Target State", GUILayout.Height(30)))
        {
            Undo.RecordObject(controller, "Record Target State");
            controller.RecordTargetState();
            EditorUtility.SetDirty(controller);
        }

        // --- 預覽控制 ---
        bool isPreviewing = _previewTween != null && _previewTween.IsActive() && !_previewTween.IsComplete();
        GUI.backgroundColor = isPreviewing ? new Color(1.0f, 0.6f, 0.6f) : new Color(0.7f, 1.0f, 0.7f);
        string buttonText = isPreviewing ? "Stop Preview" : "Play Preview";
        if (GUILayout.Button(buttonText, GUILayout.Height(35)))
        {
            if (isPreviewing) { StopPreview(); controller.RevertToInitialState(); }
            else { PlayPreview(controller); }
        }
        GUI.backgroundColor = Color.white;
        
        if (GUILayout.Button("Revert to Initial State"))
        {
            Undo.RecordObject(controller.RectTransform, "Revert to Initial State");
            controller.RevertToInitialState();
        }
    }
    
    private void OnSceneGUI()
    {
        UITweenController controller = (UITweenController)target;
        if (!controller.usePath) return;

        RectTransform rt = controller.RectTransform;
        
        Vector3 startPosWorld = rt.TransformPoint(controller.GetStartPos());
        Vector3 targetPosWorld = rt.TransformPoint(controller.GetTargetPos());
        Vector3 controlPosWorld = rt.TransformPoint(controller.GetControlPoint());

        EditorGUI.BeginChangeCheck();
        Vector3 newControlPosWorld = Handles.PositionHandle(controlPosWorld, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(controller, "Move Path Control Point");
            controller.SetControlPoint(WorldToCanvasLocal(rt, newControlPosWorld));
            EditorUtility.SetDirty(controller);
        }

        Handles.color = Color.gray;
        Handles.DrawLine(startPosWorld, newControlPosWorld);
        Handles.DrawLine(targetPosWorld, newControlPosWorld);
        
        Handles.color = Color.white;
        Handles.DrawAAPolyLine(3, startPosWorld, targetPosWorld);

        Handles.DrawBezier(startPosWorld, targetPosWorld, newControlPosWorld, newControlPosWorld, Color.green, null, 2f);
        
        Handles.Label(startPosWorld + Vector3.up * 10, "Start");
        Handles.Label(targetPosWorld + Vector3.up * 10, "Target");
        Handles.Label(newControlPosWorld + Vector3.up * 10, "Control Point");
    }

    private Vector2 WorldToCanvasLocal(RectTransform rt, Vector3 worldPos)
    {
        if (rt.parent == null) return worldPos;
        return rt.parent.InverseTransformPoint(worldPos);
    }
    
    private void PlayPreview(UITweenController controller)
    {
        StopPreview();
        _previewTween = controller.CreateAnimationSequence();
        _previewTween.SetUpdate(true).SetLoops(-1, LoopType.Restart).Play();
    }

    private void StopPreview()
    {
        if (_previewTween != null && _previewTween.IsActive())
        {
            _previewTween.Kill();
        }
        _previewTween = null;
    }
}
#endif