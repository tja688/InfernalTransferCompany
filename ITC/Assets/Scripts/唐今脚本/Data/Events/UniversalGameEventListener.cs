using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 通用事件监听器，可以自动识别并监听任何类型的GameEvent。
/// 支持：int, float, bool, string, Vector3, Object, 以及自定义类型。
/// 使用内部代理监听器来处理事件注册和响应。
/// </summary>
public class UniversalGameEventListener : MonoBehaviour
{
    [Header("事件配置")]
    [SerializeField]
    [Tooltip("要监听的事件资源（ScriptableObject）")]
    private ScriptableObject _eventAsset;

    [Header("响应配置")]
    [SerializeField]
    private bool _useUnityEvent = true;

    // 不同类型的响应事件
    [SerializeField] private UnityEvent<int> _intResponse = new UnityEvent<int>();
    [SerializeField] private UnityEvent<float> _floatResponse = new UnityEvent<float>();
    [SerializeField] private UnityEvent<bool> _boolResponse = new UnityEvent<bool>();
    [SerializeField] private UnityEvent<string> _stringResponse = new UnityEvent<string>();
    [SerializeField] private UnityEvent<Vector3> _vector3Response = new UnityEvent<Vector3>();
    [SerializeField] private UnityEvent<UnityEngine.Object> _objectResponse = new UnityEvent<UnityEngine.Object>();

    // 自定义事件响应（通过反射调用）
    [SerializeField] private UnityEvent _customResponse = new UnityEvent();

    private object _proxyListener;
    private Type _eventType;
    private Type _payloadType;

    /// <summary>
    /// 当前关联的事件资源。
    /// </summary>
    public ScriptableObject EventAsset
    {
        get => _eventAsset;
        set
        {
            if (_eventAsset == value) return;
            UnregisterFromEvent();
            _eventAsset = value;
            AnalyzeEventType();
            RegisterToEvent();
        }
    }

    /// <summary>
    /// 检测到的事件参数类型。
    /// </summary>
    public Type DetectedPayloadType => _payloadType;

    private void OnEnable()
    {
        AnalyzeEventType();
        RegisterToEvent();
    }

    private void OnDisable()
    {
        UnregisterFromEvent();
    }

    /// <summary>
    /// 分析事件类型，确定其泛型参数。
    /// </summary>
    private void AnalyzeEventType()
    {
        _eventType = null;
        _payloadType = null;
        _proxyListener = null;

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

        // 创建代理监听器
        CreateProxyListener();
    }

    /// <summary>
    /// 创建代理监听器来处理事件响应。
    /// </summary>
    private void CreateProxyListener()
    {
        if (_payloadType == null) return;

        // 创建实现了 IGameEventListener<T> 的代理对象
        Type listenerInterfaceType = typeof(IGameEventListener<>).MakeGenericType(_payloadType);
        _proxyListener = Activator.CreateInstance(typeof(ProxyListener<>).MakeGenericType(_payloadType), this);
    }

    /// <summary>
    /// 注册到事件。
    /// </summary>
    private void RegisterToEvent()
    {
        if (_eventAsset == null || _proxyListener == null) return;

        MethodInfo registerMethod = _eventType?.GetMethod("RegisterListener");
        if (registerMethod != null)
        {
            registerMethod.Invoke(_eventAsset, new[] { _proxyListener });
        }
    }

    /// <summary>
    /// 从事件注销。
    /// </summary>
    private void UnregisterFromEvent()
    {
        if (_eventAsset == null || _proxyListener == null) return;

        MethodInfo unregisterMethod = _eventType?.GetMethod("UnregisterListener");
        if (unregisterMethod != null)
        {
            unregisterMethod.Invoke(_eventAsset, new[] { _proxyListener });
        }
    }

    /// <summary>
    /// 内部方法：处理事件响应。
    /// </summary>
    internal void OnEventRaisedInternal(object value)
    {
        if (!_useUnityEvent) return;

        if (value is int intVal)
            _intResponse?.Invoke(intVal);
        else if (value is float floatVal)
            _floatResponse?.Invoke(floatVal);
        else if (value is bool boolVal)
            _boolResponse?.Invoke(boolVal);
        else if (value is string stringVal)
            _stringResponse?.Invoke(stringVal);
        else if (value is Vector3 vector3Val)
            _vector3Response?.Invoke(vector3Val);
        else if (value is UnityEngine.Object objectVal)
            _objectResponse?.Invoke(objectVal);
        else
            _customResponse?.Invoke();
    }

    // 公共方法用于Inspector中的UnityEvent配置
    public UnityEvent<int> IntResponse => _intResponse;
    public UnityEvent<float> FloatResponse => _floatResponse;
    public UnityEvent<bool> BoolResponse => _boolResponse;
    public UnityEvent<string> StringResponse => _stringResponse;
    public UnityEvent<Vector3> Vector3Response => _vector3Response;
    public UnityEvent<UnityEngine.Object> ObjectResponse => _objectResponse;
    public UnityEvent CustomResponse => _customResponse;
}

/// <summary>
/// 代理监听器，用于将事件回调转发到 UniversalGameEventListener。
/// </summary>
internal class ProxyListener<T> : IGameEventListener<T>
{
    private readonly UniversalGameEventListener _target;

    public ProxyListener(UniversalGameEventListener target)
    {
        _target = target;
    }

    public void OnEventRaised(T value)
    {
        _target?.OnEventRaisedInternal(value);
    }
}

