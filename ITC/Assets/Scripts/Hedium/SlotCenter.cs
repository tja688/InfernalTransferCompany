
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using Unity.VisualScripting;
    using UnityEngine;
    using UnityEngine.Events;
    using System;

public static class HeEventNames
{
    public const string DeliverDocumentEvent = "DeliverDocumentEvent";
    public const string EndDragEvent = "EndDragEvent";
    public const string DocumentErrorChosen = "DocumentErrorChosen";//DocumentError
    public const string EnableChooseRuneEvent = "EnableChooseRuneEvent";
    public const string ArrowFadeOutDelete = "ArrowFadeOutDelete";
    public const string OnChargingSth = "OnChargingSth";//
    public const string ChosenStampType = "ChosenStampType";//StampType
}

public class SlotCenter : MonoBehaviour
{
    // 保留原有的 pending 集合（等待补注册时记录）
    private HashSet<string> slot_table_reverse = new();

    // 存储事件委托（可以是不同 Action<T> 类型的委托）
    private Dictionary<string, Delegate> slot_table = new();

    public static SlotCenter Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void add_listener(string name, Action ev)
    {
        if (!slot_table.ContainsKey(name))
        {
            slot_table[name] = ev;
            if (slot_table_reverse.Contains(name))
            {
                Debug.Log($"补注册{name}事件");
                slot_table_reverse.Remove(name);
            }
        }
        else
        {
            slot_table[name] = Delegate.Combine(slot_table[name], ev);
        }
    }

    // 泛型注册：推荐使用此方法
    public void add_listener<T>(string name, Action<T> ev)
    {
        if (!slot_table.ContainsKey(name))
        {
            Debug.Log($"注册{name}事件");
            slot_table[name] = ev;
            if (slot_table_reverse.Contains(name))
            {
                Debug.Log($"补注册{name}事件");
                slot_table_reverse.Remove(name);
            }
        }
        else
        {
            slot_table[name] = Delegate.Combine(slot_table[name], ev);
        }
    }

    // 泛型移除
    public void remove_listener<T>(string name, Action<T> ev)
    {
        if (slot_table.TryGetValue(name, out var d))
        {
            var newd = Delegate.Remove(d, ev);
            if (newd == null)
                slot_table.Remove(name);
            else
                slot_table[name] = newd;
        }
    }

    public void remove_listener(string name, Action ev)
    {
        if (slot_table.TryGetValue(name, out var d))
        {
            var newd = Delegate.Remove(d, ev);
            if (newd == null)
                slot_table.Remove(name);
            else
                slot_table[name] = newd;
        }
    }

    // 完全注销某个事件名对应的所有监听器
    public void unregister_listener(string name)
    {
        slot_table.Remove(name);
    }

 

    // 泛型触发：推荐使用此方法
    public bool trigger_event<T>(string name, T param = default)
    {
        if (slot_table.TryGetValue(name, out var d))
        {
            Debug.Log($"{name}事件触发");

            // 如果整体委托本身就是 Action<T>
            if (d is Action<T> directAction)
            {
                try
                {
                    directAction.Invoke(param);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"事件{name}的处理器抛出异常: {ex.Message}");
                }
                return true;
            }

            // 如果是多播委托，逐个处理
            if (d is MulticastDelegate md)
            {
                foreach (var sub in md.GetInvocationList())
                {
                    try
                    {
                        if (sub is Action<T> typedSub)
                        {
                            typedSub.Invoke(param);
                        }
                        else
                        {
                            // 尝试将子委托的方法/目标绑定为 Action<T>
                            try
                            {
                                var action = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), sub.Target, sub.Method);
                                action.Invoke(param);
                            }
                            catch (Exception exCreate)
                            {
                                Debug.LogWarning($"事件{name}的处理器签名不匹配或调用失败: {exCreate.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"事件{name}的处理器抛出异常: {ex.Message}");
                    }
                }
                return true;
            }

            // 其他单一委托，尝试转换并调用
            try
            {
                var action = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), d.Target, d.Method);
                action.Invoke(param);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"事件{name}的处理器签名不匹配或调用失败: {ex.Message}");
            }

            return true;
        }
        else
        {
            slot_table_reverse.Add(name);
            Debug.Log($"无对应{name}事件");
            return false;
        }
    }

    // 无参触发：推荐使用此方法（不使用 DynamicInvoke）
    public bool trigger_event(string name)
    {
        if (slot_table.TryGetValue(name, out var d))
        {
            Debug.Log($"{name}事件触发");

            // 如果整体委托本身就是 Action
            if (d is Action directAction)
            {
                try
                {
                    directAction.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"事件{name}的处理器抛出异常: {ex.Message}");
                }
                return true;
            }

            // 如果是多播委托，逐个处理
            if (d is MulticastDelegate md)
            {
                foreach (var sub in md.GetInvocationList())
                {
                    try
                    {
                        if (sub is Action typedSub)
                        {
                            typedSub.Invoke();
                        }
                        else
                        {
                            // 尝试将子委托的方法/目标绑定为 Action
                            try
                            {
                                var action = (Action)Delegate.CreateDelegate(typeof(Action), sub.Target, sub.Method);
                                action.Invoke();
                            }
                            catch (Exception exCreate)
                            {
                                Debug.LogWarning($"事件{name}的处理器签名不匹配或调用失败: {exCreate.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"事件{name}的处理器抛出异常: {ex.Message}");
                    }
                }
                return true;
            }

            // 其他单一委托，尝试转换并调用
            try
            {
                var action = (Action)Delegate.CreateDelegate(typeof(Action), d.Target, d.Method);
                action.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"事件{name}的处理器签名不匹配或调用失败: {ex.Message}");
            }

            return true;                
        }
        else
        {
            slot_table_reverse.Add(name);
            Debug.Log($"无对应{name}事件");
            return false;
        }
    }
}
