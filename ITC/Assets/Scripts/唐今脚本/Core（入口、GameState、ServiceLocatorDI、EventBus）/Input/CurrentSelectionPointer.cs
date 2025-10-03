using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PixelCrushers;

/// <summary>
/// Listens to the active EventSystem and repositions a pointer graphic so that it
/// always sits on top of the currently selected selectable (menu option, button, etc.).
/// Only the pointer's position is adjusted; scale, rotation and other properties are left untouched.
/// </summary>
public class CurrentSelectionPointer : MonoBehaviour
{
    [Header("Pointer Graphic")]
    [Tooltip("The rect transform that should be moved to the selected option's centre.")]
    [SerializeField] private RectTransform pointerGraphic;

    [Tooltip("Optional parent canvas used to resolve screen/world conversions.")]
    [SerializeField] private Canvas referenceCanvas;

    [Header("Positioning")]
    [Tooltip("An optional offset applied after the pointer has been aligned to the target centre.")]
    [SerializeField] private Vector3 worldOffset = Vector3.zero;

    [Tooltip("Hide the pointer when there is no selectable currently focused.")]
    [SerializeField] private bool hideWhenNoSelection = true;

    [Tooltip("Hide the pointer when the dialogue system switches to mouse mode (cursor control).")]
    [SerializeField] private bool hideWhenUsingCursor = true;

    private EventSystem _cachedEventSystem;
    private GameObject _currentSelection;
    private RectTransform _pointerParentRect;
    private bool _initialised;

    private void Awake()
    {
        if (pointerGraphic == null)
        {
            pointerGraphic = GetComponent<RectTransform>();
        }

        if (pointerGraphic != null)
        {
            _pointerParentRect = pointerGraphic.parent as RectTransform;
        }

        if (referenceCanvas == null && pointerGraphic != null)
        {
            referenceCanvas = pointerGraphic.GetComponentInParent<Canvas>();
        }
    }

    private void OnEnable()
    {
        RefreshEventSystem();
        ForceUpdatePointer();
        _initialised = true;
    }

    private void OnDisable()
    {
        _currentSelection = null;
        _initialised = false;
    }

    private void Update()
    {
        RefreshEventSystem();

        if (_cachedEventSystem == null)
        {
            return;
        }

        var selected = _cachedEventSystem.currentSelectedGameObject;
        if (selected != _currentSelection)
        {
            _currentSelection = selected;
            ForceUpdatePointer();
        }
        else if (_initialised && _currentSelection != null)
        {
            // The target may animate or move; keep tracking its position even if the selection didn't change.
            UpdatePointerPosition(_currentSelection);
        }
    }

    private void RefreshEventSystem()
    {
        if (_cachedEventSystem != null && _cachedEventSystem.isActiveAndEnabled)
        {
            return;
        }

        _cachedEventSystem = EventSystem.current;
    }

    private void ForceUpdatePointer()
    {
        if (pointerGraphic == null)
        {
            return;
        }

        if (hideWhenUsingCursor && InputDeviceManager.deviceUsesCursor)
        {
            SetPointerVisibility(false);
            return;
        }

        if (_currentSelection == null)
        {
            SetPointerVisibility(!hideWhenNoSelection);
            return;
        }

        SetPointerVisibility(true);
        UpdatePointerPosition(_currentSelection);
    }

    private void UpdatePointerPosition(GameObject target)
    {
        if (pointerGraphic == null || target == null)
        {
            return;
        }

        if (hideWhenUsingCursor && InputDeviceManager.deviceUsesCursor)
        {
            SetPointerVisibility(false);
            return;
        }

        var targetRect = target.GetComponent<RectTransform>();
        Vector3 worldCenter;

        if (targetRect != null)
        {
            worldCenter = targetRect.TransformPoint(targetRect.rect.center);
        }
        else
        {
            worldCenter = target.transform.position;
        }

        worldCenter += worldOffset;

        ApplyToRectTransform(worldCenter);
    }

    private void ApplyToRectTransform(Vector3 worldPosition)
    {
        if (pointerGraphic == null)
        {
            return;
        }

        if (referenceCanvas == null)
        {
            pointerGraphic.position = worldPosition;
            return;
        }

        var renderMode = referenceCanvas.renderMode;
        Camera camera = null;

        switch (renderMode)
        {
            case RenderMode.ScreenSpaceCamera:
                camera = referenceCanvas.worldCamera;
                break;
            case RenderMode.WorldSpace:
                camera = referenceCanvas.worldCamera;
                break;
        }

        if (_pointerParentRect != null)
        {
            var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_pointerParentRect, screenPoint, camera, out localPoint))
            {
                pointerGraphic.anchoredPosition = localPoint;
                return;
            }
        }

        // Fallback for cases where we cannot resolve a local point (e.g., no parent rect transform).
        pointerGraphic.position = worldPosition;
    }

    private void SetPointerVisibility(bool visible)
    {
        if (pointerGraphic == null)
        {
            return;
        }

        if (!hideWhenNoSelection && !visible)
        {
            return;
        }

        var graphic = pointerGraphic.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.enabled = visible;
        }
        else
        {
            pointerGraphic.gameObject.SetActive(visible);
        }
    }
}
