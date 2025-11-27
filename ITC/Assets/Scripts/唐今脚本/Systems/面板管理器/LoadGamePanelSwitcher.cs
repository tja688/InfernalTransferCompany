using PixelCrushers;
using UnityEngine;

/// <summary>
/// 读档面板切换自动触发器
/// 监听 Dialogue System 的读档事件，当发生时通知面板管理器切换到指定面板。
/// </summary>
public class LoadGamePanelSwitcher : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("要触发的面板切换事件 (StringGameEvent)")]
    [SerializeField]
    private StringGameEvent _panelSwitchEvent;

    [Tooltip("读档后要切换到的目标面板名称")]
    [SerializeField]
    private string _targetPanelName;

    private void OnEnable()
    {
        // 注册 Dialogue System 的读档结束事件
        SaveSystem.loadEnded += OnLoadEnded;
    }

    private void OnDisable()
    {
        // 注销事件
        SaveSystem.loadEnded -= OnLoadEnded;
    }

    private void OnLoadEnded()
    {
        if (_panelSwitchEvent != null)
        {
            Debug.Log($"[LoadGamePanelSwitcher] 读档结束，正在请求切换面板至: {_targetPanelName}");
            _panelSwitchEvent.Raise(_targetPanelName);
        }
        else
        {
            Debug.LogWarning("[LoadGamePanelSwitcher] 未配置 Panel Switch Event，无法触发面板切换。");
        }
    }
}
