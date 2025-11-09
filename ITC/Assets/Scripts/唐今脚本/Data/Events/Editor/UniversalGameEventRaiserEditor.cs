using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UniversalGameEventRaiser 的自定义Inspector编辑器。
/// 根据关联的事件类型动态显示对应的参数字段。
/// </summary>
[CustomEditor(typeof(UniversalGameEventRaiser))]
[CanEditMultipleObjects]
public class UniversalGameEventRaiserEditor : Editor
{
    private SerializedProperty _eventAssetProperty;
    private SerializedProperty _payloadDataProperty;
    private SerializedProperty _intValueProperty;
    private SerializedProperty _floatValueProperty;
    private SerializedProperty _boolValueProperty;
    private SerializedProperty _stringValueProperty;
    private SerializedProperty _vector3ValueProperty;
    private SerializedProperty _objectValueProperty;

    private Type _detectedPayloadType;
    private bool _showPayloadFields = true;

    private void OnEnable()
    {
        _eventAssetProperty = serializedObject.FindProperty("_eventAsset");
        _payloadDataProperty = serializedObject.FindProperty("_payloadData");
        
        if (_payloadDataProperty != null)
        {
            _intValueProperty = _payloadDataProperty.FindPropertyRelative("_intValue");
            _floatValueProperty = _payloadDataProperty.FindPropertyRelative("_floatValue");
            _boolValueProperty = _payloadDataProperty.FindPropertyRelative("_boolValue");
            _stringValueProperty = _payloadDataProperty.FindPropertyRelative("_stringValue");
            _vector3ValueProperty = _payloadDataProperty.FindPropertyRelative("_vector3Value");
            _objectValueProperty = _payloadDataProperty.FindPropertyRelative("_objectValue");
        }

        UpdateDetectedType();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("通用事件触发器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 事件资源字段
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_eventAssetProperty, new GUIContent("事件资源", "要触发的事件资源（ScriptableObject）"));
        if (EditorGUI.EndChangeCheck())
        {
            UpdateDetectedType();
        }

        EditorGUILayout.Space();

        // 显示检测到的事件类型信息
        if (_eventAssetProperty.objectReferenceValue != null)
        {
            if (_detectedPayloadType != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("检测到的事件类型:", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"参数类型: {_detectedPayloadType.Name}", EditorStyles.boldLabel);
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("无法识别事件类型。请确保事件资源继承自 BaseGameEvent<T>。", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请先分配一个事件资源。", MessageType.Info);
        }

        EditorGUILayout.Space();

        // 参数配置区域
        if (_eventAssetProperty.objectReferenceValue != null && _detectedPayloadType != null)
        {
            _showPayloadFields = EditorGUILayout.Foldout(_showPayloadFields, "参数配置", true);
            
            if (_showPayloadFields)
            {
                EditorGUI.indentLevel++;
                DrawPayloadField();
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.Space();

        // 测试按钮
        if (_eventAssetProperty.objectReferenceValue != null && _detectedPayloadType != null)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("触发事件 (运行时)", GUILayout.Height(30)))
            {
                ((UniversalGameEventRaiser)target).Raise();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("触发事件功能仅在运行时可用。", MessageType.Info);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 根据检测到的类型绘制对应的参数字段。
    /// </summary>
    private void DrawPayloadField()
    {
        if (_payloadDataProperty == null) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (_detectedPayloadType == typeof(int))
        {
            EditorGUILayout.PropertyField(_intValueProperty, new GUIContent("Int 参数值"));
        }
        else if (_detectedPayloadType == typeof(float))
        {
            EditorGUILayout.PropertyField(_floatValueProperty, new GUIContent("Float 参数值"));
        }
        else if (_detectedPayloadType == typeof(bool))
        {
            EditorGUILayout.PropertyField(_boolValueProperty, new GUIContent("Bool 参数值"));
        }
        else if (_detectedPayloadType == typeof(string))
        {
            EditorGUILayout.PropertyField(_stringValueProperty, new GUIContent("String 参数值"));
        }
        else if (_detectedPayloadType == typeof(Vector3))
        {
            EditorGUILayout.PropertyField(_vector3ValueProperty, new GUIContent("Vector3 参数值"));
        }
        else if (_detectedPayloadType == typeof(UnityEngine.Object) || typeof(UnityEngine.Object).IsAssignableFrom(_detectedPayloadType))
        {
            EditorGUILayout.PropertyField(_objectValueProperty, new GUIContent("Object 参数值"), true);
        }
        else
        {
            // 自定义类型
            EditorGUILayout.HelpBox($"自定义类型: {_detectedPayloadType.Name}\n" +
                                   "对于自定义类型，请使用代码调用 Raise(object) 方法传入参数。", 
                                   MessageType.Info);
            
            // 如果是Unity Object的子类，仍然显示Object字段
            if (typeof(UnityEngine.Object).IsAssignableFrom(_detectedPayloadType))
            {
                EditorGUILayout.PropertyField(_objectValueProperty, new GUIContent("Object 参数值"), true);
            }
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 更新检测到的事件类型。
    /// </summary>
    private void UpdateDetectedType()
    {
        _detectedPayloadType = null;

        if (_eventAssetProperty.objectReferenceValue == null)
        {
            return;
        }

        ScriptableObject eventAsset = _eventAssetProperty.objectReferenceValue as ScriptableObject;
        if (eventAsset == null)
        {
            return;
        }

        Type assetType = eventAsset.GetType();
        Type baseType = assetType.BaseType;

        // 查找 BaseGameEvent<T> 基类
        while (baseType != null)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(BaseGameEvent<>))
            {
                _detectedPayloadType = baseType.GetGenericArguments()[0];
                break;
            }
            baseType = baseType.BaseType;
        }
    }
}

