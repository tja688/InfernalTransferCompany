using UnityEngine;

/// <summary>
/// Toggles the mouse cursor visibility when the N key is pressed.
/// </summary>
public class MouseCursorToggle : MonoBehaviour
{
    private bool isCursorVisible = true;

    private void Start()
    {
        ApplyCursorState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            isCursorVisible = !isCursorVisible;
            ApplyCursorState();
        }
    }

    private void ApplyCursorState()
    {
        Cursor.visible = isCursorVisible;
        Cursor.lockState = isCursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
