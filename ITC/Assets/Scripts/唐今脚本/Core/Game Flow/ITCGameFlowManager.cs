using System;
using System.Collections.Generic;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 负责管理游戏的宏观流程，并与 Dialogue System 建立桥接。
/// C# 只负责阶段切换 & 对话入口，所有微观剧情仍由 DS 自行驱动。
/// </summary>
public class ITCGameFlowManager : MonoBehaviour
{
    public enum GameMacroState
    {
        MainMenu,
        PreWork,
        Signing,
        PostWork,
        SimManagement
    }

    [Serializable]
    public class StateConversationMapping
    {
        [Tooltip("要映射的宏观状态")]
        public GameMacroState state = GameMacroState.PreWork;

        [Tooltip("进入该状态时自动启动的 DS 会话名字（为空代表纯 Gameplay 阶段）")]
        public string conversationName;
    }

    [Serializable]
    public class GameMacroStateEvent : UnityEvent<GameMacroState> { }

    private const string DefaultLuaVariableName = "GameMacroState";

    public static ITCGameFlowManager Instance { get; private set; }

    [Header("Lifecycle")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private GameMacroState defaultInitialState = GameMacroState.PreWork;

    [Header("Dialogue System Bridge")]
    [SerializeField] private string macroStateLuaVariable = DefaultLuaVariableName;
    [SerializeField] private bool stopActiveConversationOnStateChange = true;
    [SerializeField] private bool logTransitions = false;

    [Header("State ↔ Conversation 映射")]
    [SerializeField] private List<StateConversationMapping> stateConversationTable = new();

    [Header("事件回调（可选）")]
    [SerializeField] private GameMacroStateEvent onStateEntered = new();
    [SerializeField] private GameMacroStateEvent onStateExited = new();

    public GameMacroState CurrentState => _currentState;

    private readonly Dictionary<GameMacroState, string> _stateToConversation = new();
    private GameMacroState _currentState;
    private bool _hasBootstrappedState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ITCGameFlowManager] 场景中存在多个实例，将销毁后创建的对象。", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        BuildLookup();
    }

    private void OnEnable()
    {
        SaveSystem.saveDataApplied += HandleSaveDataApplied;
    }

    private void OnDisable()
    {
        SaveSystem.saveDataApplied -= HandleSaveDataApplied;
    }

    private void Start()
    {
        if (!initializeOnStart) return;

        if (TryReadStateFromLua(out var savedState))
        {
            ExecuteStateChange(savedState, !DialogueManager.isConversationActive);
        }
        else
        {
            ChangeState(defaultInitialState);
        }
    }

    private void OnValidate()
    {
        BuildLookup();
    }

    /// <summary>
    /// 对外唯一合法接口：宏观状态切换。
    /// </summary>
    public void ChangeState(GameMacroState newState)
    {
        ExecuteStateChange(newState, shouldStartConversation: true);
    }

    /// <summary>
    /// 提供给 Dialogue System sequence 调用，参数为字符串。
    /// </summary>
    public void RequestStateChange(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            Debug.LogError("[ITCGameFlowManager] RequestStateChange 收到空字符串。");
            return;
        }

        if (!Enum.TryParse(stateName, true, out GameMacroState parsedState))
        {
            Debug.LogError($"[ITCGameFlowManager] 无法解析状态: {stateName}");
            return;
        }

        ChangeState(parsedState);
    }

    /// <summary>
    /// 用于 UI/Button 等系统在纯 gameplay 阶段重新进入 DS。
    /// </summary>
    public bool TryStartConversationForState(GameMacroState state)
    {
        var conversationName = GetConversationName(state);
        if (string.IsNullOrWhiteSpace(conversationName))
        {
            return false;
        }

        if (stopActiveConversationOnStateChange && DialogueManager.isConversationActive)
        {
            DialogueManager.StopConversation();
        }

        DialogueManager.StartConversation(conversationName);
        return true;
    }

    private void ExecuteStateChange(GameMacroState newState, bool shouldStartConversation)
    {
        if (_hasBootstrappedState && _currentState == newState && shouldStartConversation)
        {
            // 同状态重新播放对话，允许用于“重进阶段”。
            StartConversationIfNeeded(newState);
            return;
        }

        GameMacroState previousState = _currentState;
        bool isStateActuallyChanging = !_hasBootstrappedState || _currentState != newState;

        if (isStateActuallyChanging && _hasBootstrappedState)
        {
            onStateExited?.Invoke(previousState);
        }

        _currentState = newState;
        _hasBootstrappedState = true;

        WriteStateToLua(newState);

        if (logTransitions)
        {
            Debug.Log($"[ITCGameFlowManager] 状态切换 {previousState} -> {newState} | convo: {GetConversationName(newState) ?? "<None>"}");
        }

        if (isStateActuallyChanging)
        {
            onStateEntered?.Invoke(newState);
        }

        if (shouldStartConversation)
        {
            StartConversationIfNeeded(newState);
        }
    }

    private void StartConversationIfNeeded(GameMacroState state)
    {
        var conversationName = GetConversationName(state);
        if (string.IsNullOrWhiteSpace(conversationName))
        {
            return;
        }

        if (!DialogueManager.hasInstance)
        {
            Debug.LogWarning("[ITCGameFlowManager] 尝试启动对话，但场景中没有 DialogueManager。");
            return;
        }

        if (stopActiveConversationOnStateChange && DialogueManager.isConversationActive)
        {
            DialogueManager.StopConversation();
        }

        DialogueManager.StartConversation(conversationName);
    }

    private string GetConversationName(GameMacroState state)
    {
        return _stateToConversation.TryGetValue(state, out var convo) ? convo : null;
    }

    private void BuildLookup()
    {
        _stateToConversation.Clear();

        if (stateConversationTable == null) return;

        foreach (var mapping in stateConversationTable)
        {
            if (mapping == null) continue;
            _stateToConversation[mapping.state] = string.IsNullOrWhiteSpace(mapping.conversationName)
                ? string.Empty
                : mapping.conversationName.Trim();
        }
    }

    private void HandleSaveDataApplied()
    {
        if (TryReadStateFromLua(out var restoredState))
        {
            var shouldStartConversation = !DialogueManager.isConversationActive;
            ExecuteStateChange(restoredState, shouldStartConversation);
        }
    }

    private void WriteStateToLua(GameMacroState state)
    {
        if (string.IsNullOrWhiteSpace(macroStateLuaVariable))
        {
            return;
        }

        DialogueLua.SetVariable(macroStateLuaVariable, state.ToString());
    }

    private bool TryReadStateFromLua(out GameMacroState state)
    {
        state = defaultInitialState;

        if (string.IsNullOrWhiteSpace(macroStateLuaVariable))
        {
            return false;
        }

        var value = DialogueLua.GetVariable(macroStateLuaVariable).asString;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Enum.TryParse(value, true, out state);
    }
}

