using UnityEngine;

/// <summary>
/// 泛型事件触发器基类，可通过 Inspector 配置事件资源与默认参数。
/// </summary>
/// <typeparam name="T">事件参数类型。</typeparam>
public abstract class BaseGameEventRaiser<T> : MonoBehaviour
{
    [Header("事件配置")]
    [SerializeField]
    private BaseGameEvent<T> _event;

    [Tooltip("调用 Raise() 时使用的默认参数")]
    [SerializeField]
    private T _payload;

    /// <summary>
    /// 事件资源。
    /// </summary>
    public BaseGameEvent<T> Event
    {
        get => _event;
        set => _event = value;
    }

    /// <summary>
    /// 默认参数。
    /// </summary>
    public T Payload
    {
        get => _payload;
        set => _payload = value;
    }

    /// <summary>
    /// 触发事件，使用默认参数。
    /// </summary>
    public void Raise()
    {
        RaiseInternal(_payload);
    }

    /// <summary>
    /// 触发事件，使用传入参数。
    /// </summary>
    public void Raise(T value)
    {
        RaiseInternal(value);
    }

    private void RaiseInternal(T value)
    {
        if (_event == null)
        {
            Debug.LogWarning($"{name}: 未配置事件资源，无法触发。", this);
            return;
        }

        _event.Raise(value);
    }
}

