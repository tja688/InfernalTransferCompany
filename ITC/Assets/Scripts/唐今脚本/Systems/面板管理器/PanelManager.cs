using MoreMountains.Tools; // 用于 MMEventListener
using UnityEngine;

/// <summary>
/// 全局面板状态机 (Singleton)。
/// 负责管理当前激活的面板状态，并通过事件驱动面板切换。
/// </summary>
// 确保你继承了 MonoBehaviour 并且 实现了 MMEventListener<RequestPanelChangeEvent>
public class PanelManager : MonoBehaviour, MMEventListener<RequestPanelChangeEvent>
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
        MMEventManager.TriggerEvent(new PanelChangedEvent 
        { 
            NewPanelName = _currentPanel, 
            PreviousPanelName = _previousPanel 
        });
    }

    // ----------------------------------------------------------------
    // 事件监听
    // ----------------------------------------------------------------

    void OnEnable()
    {
        // 开始监听“请求切换”事件
        this.MMEventStartListening<RequestPanelChangeEvent>();
    }

    void OnDisable()
    {
        // 停止监听
        this.MMEventStopListening<RequestPanelChangeEvent>();
    }

    /// <summary>
    /// 收到切换请求时的处理函数
    /// </summary>
    public void OnMMEvent(RequestPanelChangeEvent e)
    {
        ChangePanel(e.TargetPanelName);
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
        MMEventManager.TriggerEvent(new PanelChangedEvent 
        { 
            NewPanelName = _currentPanel, 
            PreviousPanelName = _previousPanel 
        });
    }
}