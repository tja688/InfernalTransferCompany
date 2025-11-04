using UnityEngine;

public class DialogueFocusStateTester : MonoBehaviour
{
    private DialogueStateManager _stateManager;
    private IInteractableUI _lastLoggedFocus;

    private void Awake()
    {
        _stateManager = DialogueStateManager.Instance;
        if (_stateManager == null)
        {
            Debug.LogWarning("[DialogueFocusStateTester] DialogueStateManager.Instance is not available in Awake. Will retry on Start.", this);
        }
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        if (_stateManager == null)
        {
            _stateManager = DialogueStateManager.Instance;
            Subscribe();
        }

        LogCurrentFocus();
    }

    private void OnDisable()
    {
        if (_stateManager != null)
        {
            _stateManager.OnFocusChanged -= HandleFocusChanged;
        }
    }

    private void Update()
    {
        if (_stateManager == null)
        {
            _stateManager = DialogueStateManager.Instance;
            if (_stateManager != null)
            {
                Subscribe();
                LogCurrentFocus();
            }
            return;
        }

        if (_lastLoggedFocus != _stateManager.CurrentFocus)
        {
            LogCurrentFocus();
        }
    }

    private void Subscribe()
    {
        if (_stateManager != null)
        {
            _stateManager.OnFocusChanged -= HandleFocusChanged;
            _stateManager.OnFocusChanged += HandleFocusChanged;
        }
    }

    private void HandleFocusChanged(IInteractableUI newFocus)
    {
        LogCurrentFocus();
    }

    private void LogCurrentFocus()
    {
        if (_stateManager == null) return;

        _lastLoggedFocus = _stateManager.CurrentFocus;
        var focusName = (_lastLoggedFocus as MonoBehaviour)?.name ?? "null";
        Debug.Log($"[DialogueFocusStateTester] Current Focus: {focusName}", this);
    }
}
