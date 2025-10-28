using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 簡易的 UI 面板狀態機，負責在遊戲生命週期內維護當前面板狀態。
/// </summary>
public class UIPanelStateMachine : MonoBehaviour
{
    public const string DefaultResourcePath = "UI/DefaultUIPanelStateConfiguration";

    static UIPanelStateMachine _instance;

    [Tooltip("面板狀態配置資源。若為空，將嘗試從 Resources/" + DefaultResourcePath + " 加載。")]
    [SerializeField] private UIPanelStateConfiguration configuration;

    [Tooltip("狀態機初始化時的默認狀態。如果留空，將使用配置中的第一個狀態。")]
    [SerializeField] private string initialStateName;

    [Serializable]
    public class Transition
    {
        [Tooltip("目標狀態名稱。")]
        public string targetState;

        [Tooltip("可選的過渡動畫。若指定，將在轉換時播放。")]
        public UITweenTrack track;

        [Tooltip("播放的 Track 名稱（優先於索引）。")]
        public string trackName;

        [Tooltip("當 Track 名稱未指定時，使用的 Track 索引。")]
        public int trackIndex;

        public void Play()
        {
            if (track == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(trackName))
            {
                track.PlayTrackByName(trackName);
            }
            else
            {
                track.PlayTrack(trackIndex);
            }
        }
    }

    [Serializable]
    public class StateTransitions
    {
        [Tooltip("當前狀態名稱。")]
        public string stateName;

        [Tooltip("從當前狀態到其他狀態的過渡配置。")]
        public List<Transition> transitions = new();

        public Transition FindTransition(string target)
        {
            if (transitions == null)
            {
                return null;
            }

            foreach (var transition in transitions)
            {
                if (transition == null)
                {
                    continue;
                }

                if (string.Equals(transition.targetState, target, StringComparison.Ordinal))
                {
                    return transition;
                }
            }

            return null;
        }
    }

    [Tooltip("針對每個狀態的過渡動畫配置。")]
    [SerializeField] private List<StateTransitions> stateTransitions = new();

    readonly Dictionary<string, StateTransitions> _transitionLookup = new(StringComparer.Ordinal);

    public static UIPanelStateMachine Instance
    {
        get
        {
            EnsureInstance();
            return _instance;
        }
    }

    public event Action<string, string> StateTransitionRequested;
    public event Action<string, string> StateChanged;

    public string CurrentState { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        EnsureInstance();
    }

    static void EnsureInstance()
    {
        if (_instance != null)
        {
            return;
        }

        var existing = FindObjectOfType<UIPanelStateMachine>();
        if (existing != null)
        {
            _instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            _instance.InitializeIfNeeded();
            return;
        }

        var go = new GameObject("UIPanelStateMachine");
        _instance = go.AddComponent<UIPanelStateMachine>();
        DontDestroyOnLoad(go);
        _instance.InitializeIfNeeded();
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeIfNeeded();
    }

    void InitializeIfNeeded()
    {
        if (configuration == null)
        {
            configuration = Resources.Load<UIPanelStateConfiguration>(DefaultResourcePath);
        }

        BuildLookup();

        if (!string.IsNullOrEmpty(CurrentState))
        {
            return;
        }

        CurrentState = ResolveInitialState();
        if (!string.IsNullOrEmpty(CurrentState))
        {
            StateChanged?.Invoke(null, CurrentState);
        }
    }

    void BuildLookup()
    {
        _transitionLookup.Clear();
        if (configuration == null)
        {
            return;
        }

        var stateNames = new HashSet<string>(configuration.GetStateNames(), StringComparer.Ordinal);

        foreach (var transitions in stateTransitions)
        {
            if (transitions == null || string.IsNullOrEmpty(transitions.stateName))
            {
                continue;
            }

            if (!stateNames.Contains(transitions.stateName))
            {
                continue;
            }

            if (!_transitionLookup.ContainsKey(transitions.stateName))
            {
                _transitionLookup.Add(transitions.stateName, transitions);
            }
        }

        foreach (var stateName in stateNames)
        {
            if (!_transitionLookup.ContainsKey(stateName))
            {
                var transitions = new StateTransitions { stateName = stateName };
                _transitionLookup[stateName] = transitions;
                bool existsInList = false;
                foreach (var existing in stateTransitions)
                {
                    if (existing != null && string.Equals(existing.stateName, stateName, StringComparison.Ordinal))
                    {
                        existsInList = true;
                        break;
                    }
                }

                if (!existsInList)
                {
                    stateTransitions.Add(transitions);
                }
            }
        }
    }

    string ResolveInitialState()
    {
        if (configuration == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(initialStateName) && configuration.Contains(initialStateName))
        {
            return initialStateName;
        }

        foreach (var stateName in configuration.GetStateNames())
        {
            if (!string.IsNullOrEmpty(stateName))
            {
                return stateName;
            }
        }

        return null;
    }

    public bool RequestState(string targetState)
    {
        if (configuration == null || !configuration.Contains(targetState))
        {
            Debug.LogWarning($"UIPanelStateMachine: 嘗試切換到未定義的狀態 {targetState}。");
            return false;
        }

        string previousState = CurrentState;
        StateTransitionRequested?.Invoke(previousState, targetState);

        if (!string.Equals(previousState, targetState, StringComparison.Ordinal))
        {
            PlayTransition(previousState, targetState);
            CurrentState = targetState;
            StateChanged?.Invoke(previousState, targetState);
        }

        return true;
    }

    void PlayTransition(string fromState, string toState)
    {
        if (string.IsNullOrEmpty(fromState) || string.IsNullOrEmpty(toState))
        {
            return;
        }

        if (!_transitionLookup.TryGetValue(fromState, out var transitions) || transitions == null)
        {
            return;
        }

        var transition = transitions.FindTransition(toState);
        transition?.Play();
    }

    public IReadOnlyCollection<string> GetAllStates()
    {
        if (configuration == null)
        {
            return Array.Empty<string>();
        }

        return new List<string>(configuration.GetStateNames());
    }

    public void RegisterTransitionTrack(string fromState, string toState, UITweenTrack track, string trackName = null, int trackIndex = 0)
    {
        if (string.IsNullOrEmpty(fromState) || string.IsNullOrEmpty(toState))
        {
            return;
        }

        if (!_transitionLookup.TryGetValue(fromState, out var transitions) || transitions == null)
        {
            transitions = new StateTransitions { stateName = fromState };
            _transitionLookup[fromState] = transitions;
            bool existsInList = false;
            foreach (var existingTransitions in stateTransitions)
            {
                if (existingTransitions != null && string.Equals(existingTransitions.stateName, fromState, StringComparison.Ordinal))
                {
                    existsInList = true;
                    break;
                }
            }

            if (!existsInList)
            {
                stateTransitions.Add(transitions);
            }
        }

        var existing = transitions.FindTransition(toState);
        if (existing == null)
        {
            existing = new Transition { targetState = toState };
            transitions.transitions.Add(existing);
        }

        existing.track = track;
        existing.trackName = trackName;
        existing.trackIndex = trackIndex;
    }
}
