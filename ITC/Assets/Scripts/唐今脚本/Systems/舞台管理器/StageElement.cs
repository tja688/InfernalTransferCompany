using UnityEngine;

public static class StageElementSubStates
{
    public const string Outside = "Outside";
    public const string Idle = "Idle";
}

public class StageElement : MonoBehaviour
{
    [Tooltip("Unique identifier for this stage element.")]
    public string StageElementID;

    public enum ElementState
    {
        OutsideStage,
        OnStage
    }

    [SerializeField]
    private ElementState _currentState = ElementState.OutsideStage;
    public ElementState CurrentState => _currentState;

    [SerializeField]
    private string _currentSubState = StageElementSubStates.Outside;
    public string CurrentSubState => string.IsNullOrEmpty(_currentSubState) ? StageElementSubStates.Outside : _currentSubState;

    protected virtual string DefaultOnStageSubState => StageElementSubStates.Idle;

    public Transform StageTransform => _cachedTransform != null ? _cachedTransform : (_cachedTransform = transform);

    private Transform _cachedTransform;

    private void Awake()
    {
        _cachedTransform = transform;
        ValidateIdentifier();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ValidateIdentifier();
    }
#endif

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.HandleElementDestroyed(this);
        }
    }

    /// <summary>
    /// 由 StageManager 调用，表示舞台元素开始进入舞台。
    /// </summary>
    public virtual void OnStageEnter()
    {
        ApplyState(ElementState.OnStage);
        SetSubState(DefaultOnStageSubState);
        HandleStageEnter();
    }

    /// <summary>
    /// 由 StageManager 调用，表示舞台元素离开舞台。
    /// </summary>
    public virtual void OnStageExit()
    {
        HandleStageExit();
        SetSubState(StageElementSubStates.Outside);
        ApplyState(ElementState.OutsideStage);
    }

    public virtual string CaptureSubStateSnapshot()
    {
        return CurrentSubState;
    }

    public virtual void RestoreSubStateSnapshot(string snapshot)
    {
        _currentSubState = string.IsNullOrEmpty(snapshot) ? StageElementSubStates.Outside : snapshot;
    }

    internal void ApplyState(ElementState newState)
    {
        if (_currentState == newState)
        {
            return;
        }

        _currentState = newState;
        NotifyStageStateReceivers(newState);
    }

    protected void SetSubState(string newState)
    {
        if (string.IsNullOrEmpty(newState))
        {
            newState = StageElementSubStates.Outside;
        }

        if (_currentSubState == newState)
        {
            return;
        }

        _currentSubState = newState;
        OnSubStateChanged(newState);
    }

    protected virtual void HandleStageEnter() { }

    protected virtual void HandleStageExit() { }

    protected virtual void OnSubStateChanged(string newState) { }

    private void NotifyStageStateReceivers(ElementState state)
    {
        var stageStates = GetComponentsInChildren<IStageState>(true);
        foreach (var stageState in stageStates)
        {
            if (stageState == null) continue;

            if (state == ElementState.OnStage)
            {
                stageState.ToOnStage();
            }
            else
            {
                stageState.ToOutsideStage();
            }
        }
    }

    private void ValidateIdentifier()
    {
        if (!Application.isPlaying)
        {
            // 在编辑器调整时允许为空，避免刷屏
            return;
        }

        if (string.IsNullOrEmpty(StageElementID))
        {
            Debug.LogWarning($"[StageElement] StageElementID is empty on object {gameObject.name}. Please assign a unique ID.", gameObject);
        }
    }
}
