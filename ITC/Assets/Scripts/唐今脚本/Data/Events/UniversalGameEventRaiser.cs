using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 通用事件触发器，可以自动识别并触发任何类型的GameEvent。
/// 支持：int, float, bool, string, Vector3, Object, 以及自定义类型。
/// 会根据关联的事件类型自动显示对应的参数配置。
/// </summary>
public class UniversalGameEventRaiser : MonoBehaviour
{
    [Header("事件配置")]
    [SerializeField]
    [Tooltip("要触发的事件资源（ScriptableObject）")]
    private ScriptableObject _eventAsset;

    [Header("参数配置")]
    [SerializeField]
    [Tooltip("根据事件类型自动显示对应的参数字段")]
    private EventPayloadData _payloadData = new EventPayloadData();

    private Type _eventType;
    private Type _payloadType;
    private MethodInfo _raiseMethod;

    /// <summary>
    /// 当前关联的事件资源。
    /// </summary>
    public ScriptableObject EventAsset
    {
        get => _eventAsset;
        set
        {
            _eventAsset = value;
            AnalyzeEventType();
        }
    }

    /// <summary>
    /// 检测到的事件参数类型。
    /// </summary>
    public Type DetectedPayloadType => _payloadType;

    private void OnValidate()
    {
        AnalyzeEventType();
    }

    /// <summary>
    /// 分析事件类型，确定其泛型参数。
    /// </summary>
    private void AnalyzeEventType()
    {
        _eventType = null;
        _payloadType = null;
        _raiseMethod = null;

        if (_eventAsset == null)
        {
            return;
        }

        Type assetType = _eventAsset.GetType();
        Type baseType = assetType.BaseType;

        // 查找 BaseGameEvent<T> 基类
        while (baseType != null)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(BaseGameEvent<>))
            {
                _eventType = baseType;
                _payloadType = baseType.GetGenericArguments()[0];
                break;
            }
            baseType = baseType.BaseType;
        }

        if (_payloadType == null)
        {
            Debug.LogWarning($"{name}: 无法识别事件类型，事件资源可能不是 BaseGameEvent<T> 的子类。", this);
            return;
        }

        // 获取 Raise 方法
        _raiseMethod = _eventType.GetMethod("Raise", new[] { _payloadType });
    }

    /// <summary>
    /// 使用配置的默认参数触发事件。
    /// </summary>
    public void Raise()
    {
        if (_eventAsset == null)
        {
            Debug.LogWarning($"{name}: 未配置事件资源，无法触发。", this);
            return;
        }

        if (_raiseMethod == null)
        {
            Debug.LogWarning($"{name}: 无法找到 Raise 方法，事件类型可能不正确。", this);
            return;
        }

        object payload = _payloadData.GetPayloadForType(_payloadType);
        if (payload == null)
        {
            Debug.LogWarning($"{name}: 无法获取有效的事件参数。", this);
            return;
        }

        _raiseMethod.Invoke(_eventAsset, new[] { payload });
    }

    /// <summary>
    /// 使用指定的参数触发事件。
    /// </summary>
    public void Raise(object value)
    {
        if (_eventAsset == null)
        {
            Debug.LogWarning($"{name}: 未配置事件资源，无法触发。", this);
            return;
        }

        if (_raiseMethod == null)
        {
            Debug.LogWarning($"{name}: 无法找到 Raise 方法，事件类型可能不正确。", this);
            return;
        }

        if (value == null || !_payloadType.IsInstanceOfType(value))
        {
            Debug.LogWarning($"{name}: 参数类型不匹配。期望类型: {_payloadType.Name}, 实际类型: {value?.GetType().Name}", this);
            return;
        }

        _raiseMethod.Invoke(_eventAsset, new[] { value });
    }

    // 便捷方法，用于从Inspector或代码中调用
    public void RaiseInt(int value) => Raise(value);
    public void RaiseFloat(float value) => Raise(value);
    public void RaiseBool(bool value) => Raise(value);
    public void RaiseString(string value) => Raise(value);
    public void RaiseVector3(Vector3 value) => Raise(value);
    public void RaiseObject(UnityEngine.Object value) => Raise(value);
}

/// <summary>
/// 事件参数数据容器，存储不同类型的参数值。
/// </summary>
[System.Serializable]
public class EventPayloadData
{
    [SerializeField] private int _intValue;
    [SerializeField] private float _floatValue;
    [SerializeField] private bool _boolValue;
    [SerializeField] private string _stringValue;
    [SerializeField] private Vector3 _vector3Value;
    [SerializeField] private UnityEngine.Object _objectValue;

    /// <summary>
    /// 根据类型获取对应的参数值。
    /// </summary>
    public object GetPayloadForType(Type type)
    {
        if (type == typeof(int))
            return _intValue;
        if (type == typeof(float))
            return _floatValue;
        if (type == typeof(bool))
            return _boolValue;
        if (type == typeof(string))
            return _stringValue;
        if (type == typeof(Vector3))
            return _vector3Value;
        if (type == typeof(UnityEngine.Object))
            return _objectValue;
        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            return _objectValue;

        // 对于自定义类型，尝试返回null或创建默认实例
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }

    /// <summary>
    /// 设置指定类型的参数值。
    /// </summary>
    public void SetPayloadForType(Type type, object value)
    {
        if (type == typeof(int) && value is int intVal)
            _intValue = intVal;
        else if (type == typeof(float) && value is float floatVal)
            _floatValue = floatVal;
        else if (type == typeof(bool) && value is bool boolVal)
            _boolValue = boolVal;
        else if (type == typeof(string) && value is string stringVal)
            _stringValue = stringVal;
        else if (type == typeof(Vector3) && value is Vector3 vector3Val)
            _vector3Value = vector3Val;
        else if (typeof(UnityEngine.Object).IsAssignableFrom(type) && value is UnityEngine.Object objectVal)
            _objectValue = objectVal;
    }

    // 公共属性用于访问
    public int IntValue { get => _intValue; set => _intValue = value; }
    public float FloatValue { get => _floatValue; set => _floatValue = value; }
    public bool BoolValue { get => _boolValue; set => _boolValue = value; }
    public string StringValue { get => _stringValue; set => _stringValue = value; }
    public Vector3 Vector3Value { get => _vector3Value; set => _vector3Value = value; }
    public UnityEngine.Object ObjectValue { get => _objectValue; set => _objectValue = value; }
}

