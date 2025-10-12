using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UITweenPlayer))]
public class UIStateMachine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("默认 Profile")]
    [SerializeField] private List<StateAnimationBinding> stateAnimations = new();
    [SerializeField] private UIState startingState = UIState.Normal;
    [SerializeField] private string defaultProfileId = "default";

    [Header("额外 Profile")]
    public List<UIStateMachineProfile> additionalProfiles = new();

    [Header("层级映射")]
    public List<UIStateMachineLayerProfile> layerProfiles = new();

    private UIState _currentState;
    private UITweenPlayer _tweenPlayer;
    private readonly Dictionary<string, ProfileRuntimeData> _profiles = new();
    private ProfileRuntimeData _activeProfile;
    private string _activeProfileId;

    private void Awake()
    {
        _tweenPlayer = GetComponent<UITweenPlayer>();
        BuildProfiles();
    }

    private void Start()
    {
        EnsureActiveProfile();
        if (_activeProfile != null)
        {
            _currentState = _activeProfile.startingState;
        }
    }

    private void BuildProfiles()
    {
        _profiles.Clear();

        if (string.IsNullOrEmpty(defaultProfileId))
        {
            defaultProfileId = "default";
        }

        var defaultRuntime = BuildRuntimeData(stateAnimations, startingState);
        _profiles[defaultProfileId] = defaultRuntime;

        foreach (var profile in additionalProfiles)
        {
            if (profile == null) continue;
            if (string.IsNullOrEmpty(profile.profileId)) continue;
            _profiles[profile.profileId] = BuildRuntimeData(profile.stateAnimations, profile.startingState);
        }

        if (!_profiles.TryGetValue(defaultProfileId, out _activeProfile))
        {
            foreach (var pair in _profiles)
            {
                _activeProfileId = pair.Key;
                _activeProfile = pair.Value;
                break;
            }
        }
        else
        {
            _activeProfileId = defaultProfileId;
        }
    }

    private static ProfileRuntimeData BuildRuntimeData(List<StateAnimationBinding> bindings, UIState startState)
    {
        var dict = new Dictionary<UIState, StateAnimationBinding>();
        if (bindings != null)
        {
            foreach (var binding in bindings)
            {
                if (binding == null) continue;
                dict[binding.state] = binding;
            }
        }

        return new ProfileRuntimeData
        {
            startingState = startState,
            bindings = dict
        };
    }

    private void EnsureActiveProfile()
    {
        if (_activeProfile == null)
        {
            BuildProfiles();
            if (!_profiles.TryGetValue(defaultProfileId, out _activeProfile))
            {
                _activeProfile = BuildRuntimeData(stateAnimations, startingState);
                _profiles[defaultProfileId] = _activeProfile;
                _activeProfileId = defaultProfileId;
            }
        }
    }

    private void TransitionTo(UIState newState)
    {
        EnsureActiveProfile();
        if (_currentState == newState) return;

        var bindings = _activeProfile.bindings;
        if (bindings == null) return;

        if (bindings.TryGetValue(_currentState, out var oldBinding) && oldBinding != null)
        {
            if (oldBinding.reverseOnExit && oldBinding.onEnterPreset != null)
            {
                _tweenPlayer.PlayReversed(oldBinding.onEnterPreset);
            }
            else if (oldBinding.onExitPreset != null)
            {
                _tweenPlayer.Play(oldBinding.onExitPreset);
            }
        }

        if (bindings.TryGetValue(newState, out var newBinding) && newBinding != null)
        {
            if (newBinding.onEnterPreset != null)
            {
                _tweenPlayer.Play(newBinding.onEnterPreset);
            }
        }

        _currentState = newState;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_currentState == UIState.Pressed || _currentState == UIState.Disabled) return;
        TransitionTo(UIState.Hover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_currentState == UIState.Pressed || _currentState == UIState.Disabled) return;
        TransitionTo(UIState.Normal);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_currentState == UIState.Disabled) return;
        TransitionTo(UIState.Pressed);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_currentState == UIState.Pressed)
        {
            if (eventData.pointerCurrentRaycast.gameObject == gameObject)
            {
                TransitionTo(UIState.Hover);
            }
            else
            {
                TransitionTo(UIState.Normal);
            }
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            if (_currentState != UIState.Selected) TransitionTo(UIState.Selected);
        }
        else
        {
            if (_currentState == UIState.Selected) TransitionTo(UIState.Normal);
        }
    }

    public bool ApplyProfile(string profileId, bool resetState = true)
    {
        EnsureActiveProfile();
        if (string.IsNullOrEmpty(profileId)) return false;
        if (!_profiles.TryGetValue(profileId, out var profile)) return false;
        _activeProfile = profile;
        _activeProfileId = profileId;
        if (resetState)
        {
            _currentState = profile.startingState;
        }
        return true;
    }

    public bool ApplyLevelProfile(UIHierarchyLevel level, bool resetState = true)
    {
        foreach (var mapping in layerProfiles)
        {
            if (mapping == null) continue;
            if (mapping.level == level && !string.IsNullOrEmpty(mapping.profileId))
            {
                return ApplyProfile(mapping.profileId, resetState);
            }
        }

        return false;
    }

    public void ResetToDefaultProfile(bool resetState = true)
    {
        ApplyProfile(defaultProfileId, resetState);
    }

    public void KillActiveTween()
    {
        if (_tweenPlayer != null)
        {
            _tweenPlayer.Kill(false);
        }
    }

    public string ActiveProfileId => _activeProfileId;

    private class ProfileRuntimeData
    {
        public UIState startingState;
        public Dictionary<UIState, StateAnimationBinding> bindings;
    }
}

[System.Serializable]
public class UIStateMachineProfile
{
    public string profileId = "profile";
    public UIState startingState = UIState.Normal;
    public List<StateAnimationBinding> stateAnimations = new();
}

[System.Serializable]
public class UIStateMachineLayerProfile
{
    public UIHierarchyLevel level = UIHierarchyLevel.GameUI;
    public string profileId;
}
