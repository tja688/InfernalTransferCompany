using System.Collections;
using UnityEngine;

/// <summary>
/// Periodically logs whether the cursor is hidden.
/// </summary>
public class MouseCursorVisibilityTester : MonoBehaviour
{
    [SerializeField]
    [Min(0.1f)]
    private float logIntervalSeconds = 1f;

    private Coroutine monitorRoutine;

    private void OnEnable()
    {
        if (monitorRoutine == null)
        {
            monitorRoutine = StartCoroutine(LogCursorVisibility());
        }
    }

    private void OnDisable()
    {
        if (monitorRoutine != null)
        {
            StopCoroutine(monitorRoutine);
            monitorRoutine = null;
        }
    }

    private IEnumerator LogCursorVisibility()
    {
        while (true)
        {
            Debug.Log($"[CursorVisibilityTester] Cursor hidden: {!Cursor.visible}");
            yield return new WaitForSeconds(Mathf.Max(0.1f, logIntervalSeconds));
        }
    }
}
