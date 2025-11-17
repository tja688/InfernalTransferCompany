
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
    //EnableChooseRuneEvent,
    OnSpawnRuneArrowsEnd,
    OnChargingSth,
    ChosenStampType,
    LetStopTypeWriter,
    LetStartTypeWriter,
    OnIsReadyTypeWriter,
    OnTypeWriterEndType,
    OnRythmGameEnd,
    NextTuneRhygame,
    LetContinueTypeWriter,
}
public static class HeEventNames
{
    public const string DeliverDocumentEvent = "DeliverDocumentEvent";
    public const string EndDragEvent = "EndDragEvent";
    public const string DocumentErrorChosen = "DocumentErrorChosen";//enum DocumentError
    //public const string EnableChooseRuneEvent = "EnableChooseRuneEvent";
    public const string OnSpawnRuneArrowsEnd = "OnSpawnRuneArrowsEnd";
    public const string OnChargingSth = "OnChargingSth";//
    public const string ChosenStampType = "ChosenStampType";//enum StampType

    public const string OnRythmGameEnd = "OnRythmGameEnd";//enum HeSuccessLayer
    public const string NextTuneRhygame = "NextTuneRhygame";









    public const string LetContinueTypeWriter = "LetContinueTypeWriter";//
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
    private class EventListenerInfo
    {
        public Delegate ListenerDelegate;
        public bool IsOnce;
        public EventListenerInfo(Delegate listenerDelegate, bool isOnce)
        {
            ListenerDelegate = listenerDelegate;
            IsOnce = isOnce;
        }
    }

    // 大表：事件名 -> 监听器列表
    private Dictionary<string, List<EventListenerInfo>> eventTable = new();
    // 反向索引：Delegate -> EventListenerInfo
    private Dictionary<Delegate, EventListenerInfo> reverseLookup = new();




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

    #region Inspector 调试按钮
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
    #endregion
    #region 添加监听器

    public void add_listener(string name, Action ev, bool isOnce = false)
    {
        AddListenerInternal(name, ev, isOnce);
    }

    public void add_listener<T>(string name, Action<T> ev, bool isOnce = false)
    {
        AddListenerInternal(name, ev, isOnce);
    }

    private void AddListenerInternal(string name, Delegate ev, bool isOnce)
    {
        var info = new EventListenerInfo(ev, isOnce);

        if (!eventTable.ContainsKey(name))
            eventTable[name] = new List<EventListenerInfo>();

        eventTable[name].Add(info);
        reverseLookup[ev] = info;

        Debug.Log($"添加listener: {name}, IsOnce={isOnce}");
    }




    #endregion

    #region 移除监听器
    // 泛型移除
    public void remove_listener(string name, Action ev)
    {
        RemoveListenerInternal(name, ev);
    }

    public void remove_listener<T>(string name, Action<T> ev)
    {
        RemoveListenerInternal(name, ev);
    }

    private void RemoveListenerInternal(string name, Delegate ev)
    {
        if (eventTable.TryGetValue(name, out var list))
        {
            list.RemoveAll(info => info.ListenerDelegate == ev);
            reverseLookup.Remove(ev);
            if (list.Count == 0)
                eventTable.Remove(name);
        }
    }

    public void unregister_listener(string name)
    {
        if (eventTable.TryGetValue(name, out var list))
        {
            foreach (var info in list)
            {
                reverseLookup.Remove(info.ListenerDelegate);
            }
            eventTable.Remove(name);
        }
    }

    #endregion

    #region 触发事件

    public bool trigger_event(string name)
    {
        if (!eventTable.TryGetValue(name, out var list) || list.Count == 0)
        {
            Debug.Log($"无对应{name}事件");
            return false;
        }

        Debug.Log($"{name}事件触发");

        // 复制列表，避免触发中修改列表导致循环异常
        var listCopy = new List<EventListenerInfo>(list);
        if(listCopy.Count == 0)
        {
            Debug.Log($"无对应{name}事件");
        }
        foreach (var info in listCopy)
        {
            try
            {
                switch (info.ListenerDelegate)
                {
                    case Action a:
                        a.Invoke();
                        break;
                    case MulticastDelegate md:
                        md.DynamicInvoke();
                        break;
                    default:
                        Debug.LogWarning($"事件{name}调用失败，签名不支持");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"事件{name}的处理器抛出异常: {ex.Message}");
            }

            // 如果是一次性监听器，触发后移除
            if (info.IsOnce)
            {
                list.Remove(info);
                reverseLookup.Remove(info.ListenerDelegate);
            }
        }

        // 列表为空则删除事件名
        if (list.Count == 0)
            eventTable.Remove(name);

        return true;
    }

    public bool trigger_event<T>(string name, T param)
    {
        if (!eventTable.TryGetValue(name, out var list) || list.Count == 0)
        {
            Debug.Log($"无对应{name}事件");
            return false;
        }

        Debug.Log($"{name}事件触发,参数类型为:{param.GetType().Name}");

        var listCopy = new List<EventListenerInfo>(list);
        if (listCopy.Count == 0)
        {
            Debug.Log($"无对应{name}事件");
        }
        foreach (var info in listCopy)
        {
            try
            {
                switch (info.ListenerDelegate)
                {
                    case Action<T> a:
                        a.Invoke(param);
                        break;
                    case MulticastDelegate md:
                        md.DynamicInvoke(param);
                        break;
                    default:
                        Debug.LogWarning($"事件{name}调用失败，签名不支持");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"事件{name}的处理器抛出异常: {ex.Message}");
            }

            if (info.IsOnce)
            {
                list.Remove(info);
                reverseLookup.Remove(info.ListenerDelegate);
            }
        }

        if (list.Count == 0)
            eventTable.Remove(name);

        return true;
    }

    #endregion
}