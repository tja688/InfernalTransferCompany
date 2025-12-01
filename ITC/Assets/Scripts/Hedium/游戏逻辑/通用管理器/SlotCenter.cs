
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
using System.Reflection;
    using Unity.VisualScripting;
    using UnityEngine;
    using UnityEngine.Events;
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
    LetLineBreakTypeWriter,
    OnReadyForBreakLine,
    OnMatchedDraggingOver,
    OnChargingGameEnd,

    TriggerDebugStage,
    TriggerRuneInputStage,
    TriggerStampStage,
    TriggerSoulHarvestStage,
    TriggerSpecialEventStage,
    TriggerDocumentVerifierStage,
}
public static class HeEventNames
{
    public const string DeliverDocumentEvent = "DeliverDocumentEvent";
    public const string EndDragEvent = "EndDragEvent";
    public const string DocumentErrorChosen = "DocumentErrorChosen";//enum DocumentError
    //public const string EnableChooseRuneEvent = "EnableChooseRuneEvent";
    public const string OnSpawnRuneArrowsEnd = "OnSpawnRuneArrowsEnd";
    public const string OnChargingGameEnd = "OnChargingGameEnd";//
    public const string ChosenStampType = "ChosenStampType";//enum StampType

    public const string OnRythmGameEnd = "OnRythmGameEnd";//enum HeSuccessLayer
    public const string NextTuneRhygame = "NextTuneRhygame";
    public const string OnReadyForBreakLine = "OnReadyForBreakLine";
    public const string OnMatchedDraggingOver = "OnMatchedDraggingOver";



    public const string LetLineBreakTypeWriter = "LetLineBreakTypeWriter";//
    public const string LetContinueTypeWriter = "LetContinueTypeWriter";//
    public const string LetStopTypeWriter = "LetStopTypeWriter";//
    public const string LetStartTypeWriter = "LetStartTypeWriter";//
    public const string OnIsReadyTypeWriter = "OnIsReadyTypeWriter";
    public const string OnTypeWriterEndType = "OnTypeWriterEndType";//


    public const string TriggerDebugStage = "TriggerDebugStage";    
    public const string TriggerRuneInputStage = "TriggerRuneInputStage";
    public const string TriggerStampStage = "TriggerStampStage";
    public const string TriggerSoulHarvestStage = "TriggerSoulHarvestStage";
    public const string TriggerSpecialEventStage = "TriggerSpecialEventStage";
    public const string TriggerDocumentVerifierStage = "TriggerDocumentVerifierStage";








}

public class SlotCenter : MonoBehaviour
{

    [Header("选择 HeEventNames 常量触发")]
    public HeEventNamesOption selectedEvent = HeEventNamesOption.DeliverDocumentEvent;

    [Header("输入自定义字符串触发")]
    public string customEventName;

    private class EventListenerInfo
    {
        public Delegate ListenerDelegate;
        public bool IsOnce;
        public String DelegateFuncName;
        public int ParamCount { get; private set; } // 参数数量（0=无参，1=1个参数，n=多个参数）
        public Type[] ParamTypes { get; private set; } // 参数类型数组（如 [typeof(int)]）
        public bool HasReturnValue { get; private set; } // 是否有返回值（Func<> 有返回值，Action<> 无）
        public EventListenerInfo(Delegate listenerDelegate, bool isOnce)
        {
            ListenerDelegate = listenerDelegate;
            IsOnce = isOnce;
            ResolveMethodInfo(listenerDelegate.Method);




        }
        private void ResolveMethodInfo(System.Reflection.MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                ParamCount = 0;
                ParamTypes = Type.EmptyTypes;
                HasReturnValue = false;
                Debug.LogWarning("MethodInfo 为空，无法解析委托签名");
                return;
            }
            var parameters = methodInfo.GetParameters();
            ParameterInfo[] paramInfos = methodInfo.GetParameters();
            ParamCount = paramInfos.Length;
            ParamTypes = new Type[ParamCount];
            for (int i = 0; i < ParamCount; i++)
            {
                ParamTypes[i] = paramInfos[i].ParameterType;
            }

            HasReturnValue = methodInfo.ReturnType != typeof(void);
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


    [ContextMenu("PrintAll Event")]
    public void PrintAllEvent()
    {
        string logOutput = "--- All Registered Events ---\n";

        if (eventTable.Count == 0)
        {
            logOutput += "No events registered.\n";
        }
        else
        {
            foreach (var entry in eventTable)
            {
                logOutput += $"\n[Event: {entry.Key}]\n";
                if (entry.Value.Count == 0)
                {
                    logOutput += "  (No listeners)\n";
                    continue;
                }

                foreach (var listenerInfo in entry.Value)
                {
                    string paramDesc = listenerInfo.ParamCount == 0 ? "无参" :
                        $"参数: {listenerInfo.ParamCount} ({string.Join(", ", Array.ConvertAll(listenerInfo.ParamTypes, t => t.Name))})";
                    logOutput += $"  -> Listener: {listenerInfo.DelegateFuncName}\n";
                    logOutput += $"     IsOnce: {listenerInfo.IsOnce}, {paramDesc}, HasReturn: {listenerInfo.HasReturnValue}\n";
                }
            }
        }

        Debug.Log(logOutput);
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

        string paramDesc = info.ParamCount == 0 ? "无参" : $"参数数量：{info.ParamCount}，类型：{string.Join(",", (IEnumerable<Type>) info.ParamTypes)}";
        string returnDesc = info.HasReturnValue ? "有返回值" : "无返回值";
        Debug.Log($"添加listener: {name}, IsOnce={isOnce} | {paramDesc} | {returnDesc}");
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
            if(info == null)
            {
                Debug.LogWarning($"事件{name}的信息为空");
                return false;
            }
            if(info.ListenerDelegate == null)
            {
                Debug.LogWarning($"事件{name}的委托为空");
                return false;
            }
       

                switch (info.ListenerDelegate)
                {
                    case Action a:
                        a.Invoke();
                        Debug.Log($"事件{name}的处理器成功调用{info.DelegateFuncName},是否为一次性事件{info.IsOnce}");
                        break;
                    case MulticastDelegate md:
                        Debug.Log("触发多播，似乎不符合预期");
                        md.DynamicInvoke();
                        break;
                    default:
                        Debug.LogWarning($"事件{name}调用失败，签名不支持");
                        break;
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
            if (info == null)
            {
                Debug.LogWarning($"事件{name}的信息为空");
                return false;
            }
            if (info.ListenerDelegate == null)
            {
                Debug.LogWarning($"事件{name}的委托为空");
                return false;
            }
       

                    switch (info.ListenerDelegate)
                    {
                    case Action a:
                            a.Invoke();
                            break;
                    case Action<T> a:
                            a.Invoke(param);
                            break;
                    default:
                            Debug.LogWarning($"事件{name}调用失败，签名不支持");
                            break;
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