#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Utility behaviour for editor-only testing that exits play mode when the Escape key is pressed.
/// Attach this component to any GameObject in the scene while running in the Unity editor.
/// </summary>
public sealed class EditorPlayModeExitTester : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
