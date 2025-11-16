using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UniversalGameEventListener 的自定义Inspector编辑器。
/// 根据关联的事件类型动态显示对应的响应事件配置。
/// </summary>
[CustomEditor(typeof(UniversalGameEventListener))]
[CanEditMultipleObjects]
public class UniversalGameEventListenerEditor : Editor
{
    private SerializedProperty _eventAssetProperty;
    private SerializedProperty _useUnityEventProperty;
    private SerializedProperty _intResponseProperty;
    private SerializedProperty _floatResponseProperty;
    private SerializedProperty _boolResponseProperty;
    private SerializedProperty _stringResponseProperty;
    private SerializedProperty _vector3ResponseProperty;
    private SerializedProperty _objectResponseProperty;
    private SerializedProperty _customResponseProperty;

    private Type _detectedPayloadType;
    private bool _showResponseFields = true;

    private void OnEnable()
    {
        _eventAssetProperty = serializedObject.FindProperty("_eventAsset");
        _useUnityEventProperty = serializedObject.FindProperty("_useUnityEvent");
        _intResponseProperty = serializedObject.FindProperty("_intResponse");
        _floatResponseProperty = serializedObject.FindProperty("_floatResponse");
        _boolResponseProperty = serializedObject.FindProperty("_boolResponse");
        _stringResponseProperty = serializedObject.FindProperty("_stringResponse");
        _vector3ResponseProperty = serializedObject.FindProperty("_vector3Response");
        _objectResponseProperty = serializedObject.FindProperty("_objectResponse");
        _customResponseProperty = serializedObject.FindProperty("_customResponse");

        UpdateDetectedType();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("通用事件监听器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 事件资源字段
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_eventAssetProperty, new GUIContent("事件资源", "要监听的事件资源（ScriptableObject）"));
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

        // UnityEvent 开关
        EditorGUILayout.PropertyField(_useUnityEventProperty, new GUIContent("使用 UnityEvent 响应"));

        EditorGUILayout.Space();

        // 响应配置区域
        if (_eventAssetProperty.objectReferenceValue != null && _detectedPayloadType != null && _useUnityEventProperty.boolValue)
        {
            _showResponseFields = EditorGUILayout.Foldout(_showResponseFields, "响应事件配置", true);
            
            if (_showResponseFields)
            {
                EditorGUI.indentLevel++;
                DrawResponseField();
                EditorGUI.indentLevel--;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 根据检测到的类型绘制对应的响应事件字段。
    /// </summary>
    private void DrawResponseField()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (_detectedPayloadType == typeof(int))
        {
            EditorGUILayout.PropertyField(_intResponseProperty, new GUIContent("Int 响应事件"), true);
        }
        else if (_detectedPayloadType == typeof(float))
        {
            EditorGUILayout.PropertyField(_floatResponseProperty, new GUIContent("Float 响应事件"), true);
        }
        else if (_detectedPayloadType == typeof(bool))
        {
            EditorGUILayout.PropertyField(_boolResponseProperty, new GUIContent("Bool 响应事件"), true);
        }
        else if (_detectedPayloadType == typeof(string))
        {
            EditorGUILayout.PropertyField(_stringResponseProperty, new GUIContent("String 响应事件"), true);
        }
        else if (_detectedPayloadType == typeof(Vector3))
        {
            EditorGUILayout.PropertyField(_vector3ResponseProperty, new GUIContent("Vector3 响应事件"), true);
        }
        else if (_detectedPayloadType == typeof(UnityEngine.Object) || typeof(UnityEngine.Object).IsAssignableFrom(_detectedPayloadType))
        {
            EditorGUILayout.PropertyField(_objectResponseProperty, new GUIContent("Object 响应事件"), true);
        }
        else
        {
            // 自定义类型
            EditorGUILayout.HelpBox($"自定义类型: {_detectedPayloadType.Name}\n" +
                                   "对于自定义类型，监听器会自动创建对应的监听组件。\n" +
                                   "您可以通过继承 BaseGameEventListener<T> 来创建自定义响应。", 
                                   MessageType.Info);
            
            // 如果是Unity Object的子类，仍然显示Object响应
            if (typeof(UnityEngine.Object).IsAssignableFrom(_detectedPayloadType))
            {
                EditorGUILayout.PropertyField(_objectResponseProperty, new GUIContent("Object 响应事件"), true);
            }
            else
            {
                EditorGUILayout.PropertyField(_customResponseProperty, new GUIContent("通用响应事件"), true);
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

