using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 面板切换过滤器工具。
/// 监听面板切换事件，当匹配到配置的"当前面板 -> 目标面板"转换时，触发 UnityEvent。
/// </summary>
public class PanelTransitionFilter : MonoBehaviour, IGameEventListener<string>
{
    [Header("配置")]
    [Tooltip("面板名称库，用于在 Inspector 中提供下拉选择（自动从 PanelManager 获取）。")]
    [SerializeField]
    private GamePanelLibrarySO _panelLibrary;

    [Tooltip("当前面板名称（从下拉菜单选择）。")]
    [SerializeField]
    private string _currentPanel = "None";

    [Tooltip("目标变换面板名称（从下拉菜单选择）。")]
    [SerializeField]
    private string _targetPanel = "None";

    [Header("事件")]
    [Tooltip("监听的面板切换事件。")]
    [SerializeField]
    private StringGameEvent _panelChangedEvent;

    [Header("输出")]
    [Tooltip("当匹配成功时触发的事件。")]
    [SerializeField]
    private UnityEvent _onMatchSuccess;

    /// <summary>
    /// 内部维护的当前面板状态
    /// </summary>
    private string _internalCurrentPanel = string.Empty;

    void Awake()
    {
        AutoFindPanelLibrary();
        
        // 初始化内部当前面板状态
        if (PanelManager.Instance != null)
        {
            _internalCurrentPanel = PanelManager.Instance.CurrentPanel;
        }
        else
        {
            _internalCurrentPanel = "None";
        }
    }

    void OnEnable()
    {
        // 注册面板切换事件
        if (_panelChangedEvent != null)
        {
            _panelChangedEvent.RegisterListener(this);
        }
        else
        {
            Debug.LogWarning($"{name}: 未配置 PanelChangedEvent，将无法监听面板切换事件。", this);
        }

        // 自动查找面板库（运行时）
        if (_panelLibrary == null)
        {
            AutoFindPanelLibrary();
        }

        // 同步内部状态
        if (PanelManager.Instance != null)
        {
            _internalCurrentPanel = PanelManager.Instance.CurrentPanel;
        }
    }

    void OnDisable()
    {
        if (_panelChangedEvent != null)
        {
            _panelChangedEvent.UnregisterListener(this);
        }
    }

    /// <summary>
    /// IGameEventListener 接口实现：响应面板切换事件。
    /// </summary>
    public void OnEventRaised(string newPanelName)
    {
        HandlePanelChanged(newPanelName);
    }

    /// <summary>
    /// 处理面板切换事件：检查是否匹配配置的转换。
    /// </summary>
    private void HandlePanelChanged(string newPanelName)
    {
        // 更新内部维护的当前面板状态
        string previousPanel = _internalCurrentPanel;
        _internalCurrentPanel = newPanelName;

        // 检查是否匹配配置的转换
        // "None" 或空字符串被视为通配符，匹配任何面板
        bool matchesCurrentPanel = string.IsNullOrEmpty(_currentPanel) || 
                                   _currentPanel == "None" ||
                                   string.Equals(_currentPanel, previousPanel, System.StringComparison.Ordinal);
        bool matchesTargetPanel = string.IsNullOrEmpty(_targetPanel) || 
                                 _targetPanel == "None" ||
                                 string.Equals(_targetPanel, newPanelName, System.StringComparison.Ordinal);

        // 如果匹配成功，触发事件
        if (matchesCurrentPanel && matchesTargetPanel)
        {
            Debug.Log($"{name}: 面板切换匹配成功！从 '{previousPanel}' -> 到 '{newPanelName}'", this);
            _onMatchSuccess?.Invoke();
        }
    }

    /// <summary>
    /// 自动查找场景中的 PanelManager 并获取其面板库。
    /// </summary>
    private void AutoFindPanelLibrary()
    {
        if (_panelLibrary != null)
        {
            return; // 已手动配置，不自动查找
        }

        if (PanelManager.Instance != null)
        {
            // 通过反射获取 PanelManager 的 _panelLibrary 字段
            var panelManagerType = typeof(PanelManager);
            var fieldInfo = panelManagerType.GetField("_panelLibrary", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (fieldInfo != null)
            {
                var library = fieldInfo.GetValue(PanelManager.Instance) as GamePanelLibrarySO;
                if (library != null)
                {
                    _panelLibrary = library;
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                    #endif
                }
            }
        }
    }
}

