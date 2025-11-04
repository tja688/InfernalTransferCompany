using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSubmitDebugger : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        _button.onClick.AddListener(HandleButtonClicked);
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    private void HandleButtonClicked()
    {
        Debug.Log($"UI Button '{_button.name}' was pressed.", this);
    }
}