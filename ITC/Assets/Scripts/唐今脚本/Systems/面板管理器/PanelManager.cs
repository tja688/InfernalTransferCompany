using UnityEngine;

/// <summary>
/// 全局面板状态机 (Singleton)。
/// 负责管理当前激活的面板状态，并通过事件驱动面板切换。
/// </summary>
public class PanelManager : MonoBehaviour, IGameEventListener<string>
{
    /// <summary>
    /// 静态单例实例
    /// </summary>
    public static PanelManager Instance { get; private set; }

    [Header("配置")]
    [Tooltip("必须的面板库 ScriptableObject")]
    [SerializeField]
    private GamePanelLibrarySO _panelLibrary;

    [Tooltip("游戏启动时默认处于的面板状态")]
    [SerializeField]
    private string _defaultPanel = "None"; // 对应库中的 "None"

    [Header("事件系统")]
    [Tooltip("外部请求切换面板时监听的事件（需要 String 类型参数）")]
    [SerializeField]
    private StringGameEvent _requestPanelEvent;

    [Tooltip("当面板成功切换后广播的事件（携带新旧面板名称）")]
    [SerializeField]
    private PanelChangedGameEvent _panelChangedEvent;

    private string _currentPanel;
    private string _previousPanel;

    /// <summary>
    /// [公开接口] 获取当前激活的面板名称
    /// </summary>
    public string CurrentPanel => _currentPanel;

    /// <summary>
    /// [公开接口] 获取上一个面板的名称
    /// </summary>
    public string PreviousPanel => _previousPanel;

    // "饿加载" 单例模式
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("场景中存在多个 PanelManager 实例，将销毁多余的。");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 确保全局存在
        DontDestroyOnLoad(gameObject);

        // 验证配置
        if (_panelLibrary == null)
        {
            Debug.LogError("PanelManager 未配置 GamePanelLibrarySO！面板系统将无法工作。", this);
            return;
        }
        
        // 初始化默认状态
        _currentPanel = _defaultPanel;
        _previousPanel = "None";
    }

    void Start()
    {
        // 在第一帧广播初始状态，以便其他脚本在 Start() 中可以获取到
        BroadcastPanelChanged(_previousPanel, _currentPanel);
    }

    // ----------------------------------------------------------------
    // 事件监听
    // ----------------------------------------------------------------

    void OnEnable()
    {
        RegisterForEvents();
    }

    void OnDisable()
    {
        UnregisterFromEvents();
    }

    // ----------------------------------------------------------------
    // 核心逻辑
    // ----------------------------------------------------------------

    /// <summary>
    /// [公开接口] 尝试切换到指定的面板
    /// </summary>
    /// <param name="panelName">目标面板的名称</param>
    public void ChangePanel(string panelName)
    {
        // 1. 检查是否是同一个面板
        if (string.Equals(_currentPanel, panelName))
        {
            Debug.Log($"PanelManager: 试图切换到已激活的面板 '{panelName}'，已忽略。");
            return;
        }

        // 2. 验证面板是否存在于库中
        if (!_panelLibrary.panelNames.Contains(panelName))
        {
            Debug.LogError($"PanelManager: 切换失败！面板 '{panelName}' 不存在于 GamePanelLibrarySO 中。");
            return;
        }

        // 3. 更新状态
        _previousPanel = _currentPanel;
        _currentPanel = panelName;

        Debug.Log($"PanelManager: 面板状态切换: 从 '{_previousPanel}' -> 到 '{_currentPanel}'");

        // 4. 广播“切换成功”事件
        BroadcastPanelChanged(_previousPanel, _currentPanel);
    }

    /// <summary>
    /// IGameEventListener 接口实现：响应外部请求事件。
    /// </summary>
    public void OnEventRaised(string value)
    {
        ChangePanel(value);
    }

    private void RegisterForEvents()
    {
        if (_requestPanelEvent != null)
        {
            _requestPanelEvent.RegisterListener(this);
        }
        else
        {
            Debug.LogWarning("PanelManager: 未配置请求面板切换事件，将无法响应外部切换请求。", this);
        }
    }

    private void UnregisterFromEvents()
    {
        if (_requestPanelEvent != null)
        {
            _requestPanelEvent.UnregisterListener(this);
        }
    }

    private void BroadcastPanelChanged(string previousPanel, string newPanel)
    {
        if (_panelChangedEvent == null)
        {
            return;
        }

        var payload = new PanelChangedPayload(newPanel, previousPanel);
        _panelChangedEvent.Raise(payload);
    }
}