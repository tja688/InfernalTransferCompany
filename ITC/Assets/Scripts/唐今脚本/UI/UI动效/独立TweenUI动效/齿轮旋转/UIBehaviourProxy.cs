using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 简单的 UI 事件转发器，用于在 Button 上捕获鼠标进入/退出事件。
/// </summary>
public class UIBehaviourProxy : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public System.Action<PointerEventData> onEnter;
    public System.Action<PointerEventData> onExit;

    public void OnPointerEnter(PointerEventData eventData) => onEnter?.Invoke(eventData);
    public void OnPointerExit(PointerEventData eventData) => onExit?.Invoke(eventData);
}