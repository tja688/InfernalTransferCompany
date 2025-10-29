
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 定义了面板状态的变更信息
/// </summary>
public readonly struct PanelStateChange
{
    public readonly string PreviousPanel;
    public readonly string CurrentPanel;

    public PanelStateChange(string previousPanel, string currentPanel)
    {
        PreviousPanel = previousPanel;
        CurrentPanel = currentPanel;
    }
}

/// <summary>
/// 定义了从一个面板到另一个面板的过渡动画
/// </summary>
[System.Serializable]
public class GamePanelTransition
{
    [Tooltip("源面板")]
    public string fromPanel;
    [Tooltip("目标面板")]
    public string toPanel;
    [Tooltip("当此过渡发生时播放的动画轨道")]
    public UITweenTrack transitionTrack;
    [Tooltip("要播放的轨道名称")]
    public string trackNameToPlay;
}

/// <summary>
/// 管理全局游戏面板状态的单例状态机。
/// </summary>
public class GamePanelStateMachine : MonoBehaviour
{
    #region Singleton and Initialization

    private static GamePanelStateMachine _instance;
    public static GamePanelStateMachine Instance
    {
        get
        {
            if (_instance == null)
            {
                // 场景中查找
                _instance = FindObjectOfType<GamePanelStateMachine>();

                // 如果没有，则自动创建
                if (_instance == null)
                {
                    var go = new GameObject("[GamePanelStateMachine]");
                    _instance = go.AddComponent<GamePanelStateMachine>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // 情况 1: Instance 属性先于 Awake 执行，并且找到了 this
        if (_instance == this)
        {
            // 我们已经被 Instance 属性“指定”为单例
            // 确保我们被正确设置并初始化
            DontDestroyOnLoad(gameObject);
            _currentPanel = startingPanel; // <--- 关键：补上初始化
            return;
        }

        // 情况 2: Awake 先于 Instance 属性执行
        if (_instance == null)
        {
            // 我们是第一个，将自己设为单例
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _currentPanel = startingPanel; // 初始化
        }
        // 情况 3: 场景中已存在一个单例 ( _instance != null 且 _instance != this )
        else if (_instance != this)
        {
            // 这是重复的实例，检查是否应该替换现有的
            bool existingIsBlank = _instance.startingPanel == "None";
            bool thisIsConfigured = this.startingPanel != "None";

            if (existingIsBlank && thisIsConfigured)
            {
                // 用这个“已配置”的实例替换现有的“空白”实例
                Destroy(_instance.gameObject);
                _instance = this;
                DontDestroyOnLoad(gameObject);
                _currentPanel = startingPanel; // 初始化
            }
            else
            {
                // 否则（现有的已配置，或当前的是空白的），销毁这个重复的
                Destroy(gameObject);
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateInstance()
    {
        // 确保在场景加载后实例存在
        var _ = Instance;
    }

    #endregion

    [Header("配置")]
    [Tooltip("关联的游戏面板库，用于获取所有面板名称")]
    [SerializeField] private GamePanelLibrarySO panelLibrary;

    [Tooltip("状态机的初始面板状态")]
    [SerializeField] private string startingPanel = "None";

    [Tooltip("开启后，将在控制台打印所有状态转换和轨道触发的日志")]
    [SerializeField] private bool debugMode = false;

    [Tooltip("定义面板之间的所有过渡动画")]
    [SerializeField] private List<GamePanelTransition> transitions = new List<GamePanelTransition>();

    private string _currentPanel;
    public string CurrentPanel => _currentPanel;

    private readonly List<Action<PanelStateChange>> _subscribers = new List<Action<PanelStateChange>>();

    /// <summary>
    /// 订阅面板状态变更通知。
    /// </summary>
    /// <param name="callback">状态变更时调用的回调函数</param>
    /// <returns>一个 IDisposable 对象，用于取消订阅</returns>
    public IDisposable Subscribe(Action<PanelStateChange> callback)
    {
        if (callback == null) return null;

        _subscribers.Add(callback);
        
        // 为新订阅者立即回放当前状态
        callback.Invoke(new PanelStateChange(_currentPanel, _currentPanel));

        return new Unsubscriber(() => _subscribers.Remove(callback));
    }

    /// <summary>
    /// (由 GamePanelChanger 调用) 请求切换到新的面板状态。
    /// </summary>
    internal void RequestStateChange(string newPanel)
    {
        if (string.IsNullOrEmpty(newPanel) || _currentPanel == newPanel)
        {
            return;
        }

        string previousPanel = _currentPanel;
        _currentPanel = newPanel;

        if (debugMode)
        {
            Debug.Log($"[GamePanelStateMachine] State changed: <color=yellow>{previousPanel}</color> -> <color=cyan>{_currentPanel}</color>", this);
        }

        // 触发过渡动画
        PlayTransition(previousPanel, _currentPanel);

        // 通知所有订阅者
        var change = new PanelStateChange(previousPanel, _currentPanel);
        for (int i = _subscribers.Count - 1; i >= 0; i--)
        {
            _subscribers[i]?.Invoke(change);
        }
    }

    private void PlayTransition(string from, string to)
    {
        var transition = transitions.Find(t => t.fromPanel == from && t.toPanel == to);
        if (transition?.transitionTrack != null && !string.IsNullOrEmpty(transition.trackNameToPlay))
        {
            if (debugMode)
            {
                Debug.Log($"[GamePanelStateMachine] Playing transition track: <color=lime>'{transition.trackNameToPlay}'</color> on {transition.transitionTrack.name}", transition.transitionTrack);
            }
            transition.transitionTrack.PlayTrackByName(transition.trackNameToPlay);
        }
    }

    // 辅助类，用于实现 IDisposable 退订
    private class Unsubscriber : IDisposable
    {
        private readonly Action _unsubscribeAction;

        public Unsubscriber(Action unsubscribeAction)
        {
            _unsubscribeAction = unsubscribeAction;
        }

        public void Dispose()
        {
            _unsubscribeAction?.Invoke();
        }
    }
    
    // 在 Inspector 中公开面板库，以便编辑器脚本可以访问
    public GamePanelLibrarySO PanelLibrary => panelLibrary;
}
