#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UITweenTrack))]
public class UITweenTrackEditor : Editor
{
    SerializedProperty _tracksProp;
    SerializedProperty _useUnscaledProp;

    void OnEnable()
    {
        _tracksProp = serializedObject.FindProperty("tracks");
        _useUnscaledProp = serializedObject.FindProperty("useUnscaledIntervals");
    }

    public override void OnInspectorGUI()
    {
        if (serializedObject.isEditingMultipleObjects)
        {
            EditorGUILayout.HelpBox("暂不支持多对象同时编辑 UITweenTrack。", MessageType.Info);
            return;
        }

        serializedObject.Update();

        EditorGUILayout.PropertyField(_useUnscaledProp);
        EditorGUILayout.Space();

        DrawTracks();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawTracks()
    {
        EditorGUILayout.LabelField("轨道设置", EditorStyles.boldLabel);
        if (_tracksProp == null)
        {
            EditorGUILayout.HelpBox("未找到轨道数据。", MessageType.Warning);
            return;
        }

        for (int i = 0; i < _tracksProp.arraySize; i++)
        {
            var trackProp = _tracksProp.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.PropertyField(trackProp.FindPropertyRelative("trackName"), new GUIContent("轨道名称"));

                var intervalProp = trackProp.FindPropertyRelative("uniformInterval");
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(intervalProp, new GUIContent("统一间隔"));
                    if (GUILayout.Button("应用到轨道", GUILayout.Width(100f)))
                    {
                        ApplyUniformInterval(i, intervalProp.floatValue);
                    }
                }

                EditorGUILayout.PropertyField(trackProp.FindPropertyRelative("items"), new GUIContent("轨道元素"), true);
            }
        }

        if (GUILayout.Button("添加新轨道"))
        {
            _tracksProp.arraySize++;
        }
    }

    void ApplyUniformInterval(int trackIndex, float interval)
    {
        foreach (var obj in targets)
        {
            if (obj is UITweenTrack track)
            {
                Undo.RecordObject(track, "Apply Uniform Interval");
                track.ApplyUniformInterval(trackIndex, interval);
                EditorUtility.SetDirty(track);
            }
        }
    }
}
#endif
