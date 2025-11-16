using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

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

    [Tooltip("允许匹配的当前面板集合（留空视为通配）。")]
    [SerializeField]
    private List<string> _currentPanelSelections = new List<string>();

    [Tooltip("允许匹配的目标面板集合（留空视为通配）。")]
    [SerializeField]
    private List<string> _targetPanelSelections = new List<string>();

    [FormerlySerializedAs("_currentPanel")]
    [SerializeField, HideInInspector]
    private string _legacyCurrentPanel = "None";

    [FormerlySerializedAs("_targetPanel")]
    [SerializeField, HideInInspector]
    private string _legacyTargetPanel = "None";

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
        EnsureSelectionListsInitialized();
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
        EnsureSelectionListsInitialized();
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
        EnsureSelectionListsInitialized();
        // 更新内部维护的当前面板状态
        string previousPanel = _internalCurrentPanel;
        _internalCurrentPanel = newPanelName;

        // 检查是否匹配配置的转换
        bool matchesCurrentPanel = MatchesPanelSelection(_currentPanelSelections, _legacyCurrentPanel, previousPanel);
        bool matchesTargetPanel = MatchesPanelSelection(_targetPanelSelections, _legacyTargetPanel, newPanelName);

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

    /// <summary>
    /// 判定指定面板名称是否匹配选择集合。
    /// </summary>
    private static bool MatchesPanelSelection(List<string> selections, string legacySinglePanel, string panelToCheck)
    {
        if (IsWildcardSelection(selections, legacySinglePanel))
        {
            return true;
        }

        if (selections != null)
        {
            for (int i = 0; i < selections.Count; i++)
            {
                var candidate = selections[i];
                if (string.IsNullOrEmpty(candidate) || candidate == "None")
                {
                    continue;
                }

                if (string.Equals(candidate, panelToCheck, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        if (!string.IsNullOrEmpty(legacySinglePanel) && legacySinglePanel != "None")
        {
            return string.Equals(legacySinglePanel, panelToCheck, System.StringComparison.Ordinal);
        }

        return false;
    }

    /// <summary>
    /// 判断当前选择是否表示通配。
    /// </summary>
    private static bool IsWildcardSelection(List<string> selections, string legacySinglePanel)
    {
        if (selections != null && selections.Count > 0)
        {
            for (int i = 0; i < selections.Count; i++)
            {
                if (!string.IsNullOrEmpty(selections[i]) && selections[i] != "None")
                {
                    return false;
                }
            }
            return true;
        }

        return string.IsNullOrEmpty(legacySinglePanel) || legacySinglePanel == "None";
    }

    /// <summary>
    /// 确保序列化列表被初始化。
    /// </summary>
    private void EnsureSelectionListsInitialized()
    {
        if (_currentPanelSelections == null)
        {
            _currentPanelSelections = new List<string>();
        }

        if (_targetPanelSelections == null)
        {
            _targetPanelSelections = new List<string>();
        }
    }
}

