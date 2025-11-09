using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Game Events/Panel Changed Event", fileName = "PanelChangedEvent")]
public class PanelChangedGameEvent : BaseGameEvent<PanelChangedPayload> { }

/// <summary>
/// 自定义 UnityEvent 以在 Inspector 中友好展示面板切换数据。
/// </summary>
[System.Serializable]
public class PanelChangedUnityEvent : UnityEvent<PanelChangedPayload> { }

public class PanelChangedGameEventListener : BaseGameEventListener<PanelChangedPayload> { }

