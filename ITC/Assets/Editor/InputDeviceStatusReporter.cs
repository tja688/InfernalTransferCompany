#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[InitializeOnLoad]
public static class InputDeviceStatusReporter
{
    static InputDeviceStatusReporter()
    {
        SceneView.duringSceneGui += OnSceneGui;
    }

    private static void OnSceneGui(SceneView sceneView)
    {
        var currentEvent = Event.current;
        if (currentEvent == null)
        {
            return;
        }

        if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.M)
        {
            LogCurrentDeviceStatus();
            currentEvent.Use();
        }
    }

    private static void LogCurrentDeviceStatus()
    {
        var builder = new StringBuilder();
        builder.AppendLine("=== Input Device Status ===");

        builder.AppendLine("Connected devices:");
        if (InputSystem.devices.Count == 0)
        {
            builder.AppendLine("  (none)");
        }
        else
        {
            foreach (var device in InputSystem.devices)
            {
                builder.AppendLine($"  - {GetDeviceDisplayName(device)} (layout: {device.layout})");
            }
        }

        builder.AppendLine("Disconnected devices:");
        var disconnectedDevices = InputSystem.disconnectedDevices;
        if (disconnectedDevices.Count == 0)
        {
            builder.AppendLine("  (none)");
        }
        else
        {
            foreach (var device in disconnectedDevices)
            {
                builder.AppendLine($"  - {GetDeviceDisplayName(device)} (layout: {device.layout})");
            }
        }

        Debug.Log(builder.ToString());
    }

    private static string GetDeviceDisplayName(InputDevice device)
    {
        if (!string.IsNullOrEmpty(device.displayName))
        {
            return device.displayName;
        }

        if (!string.IsNullOrEmpty(device.name))
        {
            return device.name;
        }

        return device.layout;
    }
}
#endif
