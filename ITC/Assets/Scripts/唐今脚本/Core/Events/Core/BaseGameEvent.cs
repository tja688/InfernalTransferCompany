using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 泛型 ScriptableObject 事件基类，负责管理监听器注册与事件广播。
/// </summary>
/// <typeparam name="T">事件所携带的参数类型。</typeparam>
public abstract class BaseGameEvent<T> : ScriptableObject
{
    private readonly List<IGameEventListener<T>> _listeners = new List<IGameEventListener<T>>();

    /// <summary>
    /// 广播事件，向所有已注册监听器发送数据。
    /// </summary>
    /// <param name="value">事件参数。</param>
    public void Raise(T value)
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            IGameEventListener<T> listener = _listeners[i];
            if (listener == null)
            {
                _listeners.RemoveAt(i);
                continue;
            }

            listener.OnEventRaised(value);
        }
    }

    /// <summary>
    /// 注册监听器。
    /// </summary>
    public void RegisterListener(IGameEventListener<T> listener)
    {
        if (listener == null || _listeners.Contains(listener))
        {
            return;
        }

        _listeners.Add(listener);
    }

    /// <summary>
    /// 注销监听器。
    /// </summary>
    public void UnregisterListener(IGameEventListener<T> listener)
    {
        if (listener == null)
        {
            return;
        }

        _listeners.Remove(listener);
    }
}

