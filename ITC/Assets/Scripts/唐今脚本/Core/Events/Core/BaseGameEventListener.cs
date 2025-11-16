using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 泛型事件监听器基类。通过继承并指定具体泛型类型，即可创建可挂载的监听组件。
/// </summary>
/// <typeparam name="T">事件参数类型。</typeparam>
public abstract class BaseGameEventListener<T> : MonoBehaviour, IGameEventListener<T>
{
    [SerializeField]
    private BaseGameEvent<T> _event;

    [SerializeField]
    private UnityEvent<T> _response = new UnityEvent<T>();

    /// <summary>
    /// 当前监听的事件资源。
    /// </summary>
    public BaseGameEvent<T> Event
    {
        get => _event;
        set => SetEvent(value);
    }

    /// <summary>
    /// 事件响应，允许在 Inspector 中配置。
    /// </summary>
    public UnityEvent<T> Response => _response;

    protected virtual void OnEnable()
    {
        Register();
    }

    protected virtual void OnDisable()
    {
        Unregister();
    }

    /// <summary>
    /// 事件触发回调：默认调用 Inspector 中配置的 UnityEvent。
    /// </summary>
    public virtual void OnEventRaised(T value)
    {
        _response?.Invoke(value);
    }

    private void Register()
    {
        if (_event != null)
        {
            _event.RegisterListener(this);
        }
    }

    private void Unregister()
    {
        if (_event != null)
        {
            _event.UnregisterListener(this);
        }
    }

    private void SetEvent(BaseGameEvent<T> newEvent)
    {
        if (_event == newEvent)
        {
            return;
        }

        if (isActiveAndEnabled && _event != null)
        {
            _event.UnregisterListener(this);
        }

        _event = newEvent;

        if (isActiveAndEnabled && _event != null)
        {
            _event.RegisterListener(this);
        }
    }
}

