using UnityEngine;
using System.Collections.Generic;

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

    private void Start()
    {
        if (string.IsNullOrEmpty(StageElementID))
        {
            Debug.LogError($"[StageElement] StageElementID is empty on object {gameObject.name}. Please assign a unique ID.", gameObject);
            return;
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.RegisterElement(this);
        }
        else
        {
            Debug.LogWarning("[StageElement] StageManager instance not found.");
        }
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.UnregisterElement(this);
        }
    }

    /// <summary>
    /// Sets the state of the element. Should only be called by StageManager.
    /// </summary>
    /// <param name="newState"></param>
    public void SetState(ElementState newState)
    {
        _currentState = newState;

        // Find all IStageState components on this object and its children
        var stageStates = GetComponentsInChildren<IStageState>(true);
        foreach (var state in stageStates)
        {
            if (newState == ElementState.OnStage)
            {
                state.ToOnStage();
            }
            else
            {
                state.ToOutsideStage();
            }
        }
    }
}
