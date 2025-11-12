
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using Unity.VisualScripting;
    using UnityEngine;
    using UnityEngine.Events;
    using System;
public enum HeEventNamesOption
{
    DeliverDocumentEvent,
    EndDragEvent,
    DocumentErrorChosen,
    EnableChooseRuneEvent,
    ArrowFadeOutDelete,
    OnChargingSth,
    ChosenStampType,
    LetStopTypeWriter,
    LetStartTypeWriter,
    OnIsReadyTypeWriter,
    OnTypeWriterEndType,
}
public static class HeEventNames
{
    public const string DeliverDocumentEvent = "DeliverDocumentEvent";
    public const string EndDragEvent = "EndDragEvent";
    public const string DocumentErrorChosen = "DocumentErrorChosen";//DocumentError
    public const string EnableChooseRuneEvent = "EnableChooseRuneEvent";
    public const string ArrowFadeOutDelete = "ArrowFadeOutDelete";
    public const string OnChargingSth = "OnChargingSth";//
    public const string ChosenStampType = "ChosenStampType";//StampType
    public const string LetStopTypeWriter = "LetStopTypeWriter";//
    public const string LetStartTypeWriter = "LetStartTypeWriter";//
    public const string OnIsReadyTypeWriter = "OnIsReadyTypeWriter";
    public const string OnTypeWriterEndType = "OnTypeWriterEndType";//
}

public class SlotCenter : MonoBehaviour
{

    [Header("选择 HeEventNames 常量触发")]
    public HeEventNamesOption selectedEvent = HeEventNamesOption.DeliverDocumentEvent;

    [Header("输入自定义字符串触发")]
    public string customEventName;



    private HashSet<string> slot_table_reverse = new();

    private Dictionary<string, Delegate> slot_table = new();

    public static SlotCenter Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Inspector 按钮触发选择的常量事件
    [ContextMenu("Trigger Selected Const Event")]
    public void TriggerSelectedConstEvent()
    {
        string eventName = selectedEvent.ToString();
        Debug.Log($"[Inspector Debug] 触发 HeEventNames 常量事件: {eventName}");
        SlotCenter.Instance?.trigger_event(eventName);
    }

    // Inspector 按钮触发自定义字符串事件
    [ContextMenu("Trigger Custom String Event")]
    public void TriggerCustomStringEvent()
    {
        if (!string.IsNullOrEmpty(customEventName))
        {
            Debug.Log($"[Inspector Debug] 触发自定义事件: {customEventName}");
            SlotCenter.Instance?.trigger_event(customEventName);
        }
        else
        {
            Debug.LogWarning("CustomEventName 为空！");
        }
    }
    public void add_listener(string name, Action ev)
    {
        Debug.Log($"添加listener:{name}");
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
        Debug.Log($"添加listener:{name}");
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



        Debug.Log($"注销特定监听者:{name}");
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

        Debug.Log($"注销特定监听者:{name}");
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
        Debug.Log($"注销全部监听者:{name}");
        slot_table.Remove(name);
    }

 

    // 泛型触发：推荐使用此方法
    public bool trigger_event<T>(string name, T param = default)
    {
        if (slot_table.TryGetValue(name, out var d))
        {
            Debug.Log($"{name}事件触发,参数类型为:{param.GetType().Name}");

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
