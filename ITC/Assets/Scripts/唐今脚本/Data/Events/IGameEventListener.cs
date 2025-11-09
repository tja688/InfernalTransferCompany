using System;

/// <summary>
/// 泛型事件监听接口，用于解耦事件广播方与监听方。
/// </summary>
/// <typeparam name="T">事件携带的数据类型。</typeparam>
public interface IGameEventListener<T>
{
    /// <summary>
    /// 当事件被触发时由事件系统调用。
    /// </summary>
    /// <param name="value">事件参数。</param>
    void OnEventRaised(T value);
}

