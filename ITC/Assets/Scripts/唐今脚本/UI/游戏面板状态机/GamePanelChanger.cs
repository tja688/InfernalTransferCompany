
using UnityEngine;

/// <summary>
/// 负责向 GamePanelStateMachine 发送面板切换请求的唯一入口点。
/// </summary>
public class GamePanelChanger : MonoBehaviour
{
    [Header("调试")]
    [SerializeField]
    [Tooltip("如果勾选，当发出请求时会在控制台打印调试信息")]
    private bool logDebugInfo = false;

    /// <summary>
    /// (可通过 UnityEvent 调用) 请求切换到指定的面板。
    /// </summary>
    /// <param name="panelName">目标面板的名称</param>
    public void ChangeToPanel(string panelName)
    {
        if (logDebugInfo)
        {
            Debug.Log($"[GamePanelChanger] on object '{gameObject.name}' received a request to change panel to: {panelName}", this);
        }

        GamePanelStateMachine.Instance.RequestStateChange(panelName);
    }

    /// <summary>
    /// (可通过 UnityEvent 调用) 请求切换到指定的面板，用于适配没有字符串参数的 UnityEvent。
    /// </summary>
    public void ChangeToPanel(GamePanelLibrarySO library, int panelIndex)
    {
        if (library == null || panelIndex < 0 || panelIndex >= library.panelNames.Count)
        {
            if (logDebugInfo)
            {
                Debug.LogWarning($"[GamePanelChanger] on object '{gameObject.name}' received an invalid request with null library or out-of-bounds index: {panelIndex}", this);
            }
            return;
        }

        string panelName = library.panelNames[panelIndex];
        ChangeToPanel(panelName);
    }
}
