using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Captures and propagates call-site information for UITween requests without changing existing APIs.
/// </summary>
public static class UITweenCallContext
{
    private static readonly AsyncLocal<ContextInfo?> _current = new();

    /// <summary>
    /// A snapshot of the call context used by the monitor.
    /// </summary>
    public readonly struct ContextInfo
    {
        public readonly string SourceType;
        public readonly string SourceName;
        public readonly string Details;
        public readonly string Method;
        public readonly string StackTrace;
        public readonly UnityEngine.Object SourceObject;
        public readonly int Depth;

        public ContextInfo(string sourceType, string sourceName, string details, string method, string stackTrace, UnityEngine.Object sourceObject, int depth)
        {
            SourceType = sourceType;
            SourceName = sourceName;
            Details = details;
            Method = method;
            StackTrace = stackTrace;
            SourceObject = sourceObject;
            Depth = depth;
        }

        public ContextInfo WithDetails(string details, bool append = false)
        {
            string combined = details;
            if (append && !string.IsNullOrEmpty(Details))
            {
                if (string.IsNullOrEmpty(details)) combined = Details;
                else combined = Details + " → " + details;
            }
            return new ContextInfo(SourceType, SourceName, combined, Method, StackTrace, SourceObject, Depth);
        }

        public bool IsValid => !string.IsNullOrEmpty(SourceType) || !string.IsNullOrEmpty(SourceName) || SourceObject != null;

        public string ComposeSummary()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(SourceType)) sb.Append(SourceType);
            if (!string.IsNullOrEmpty(SourceName))
            {
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append(SourceName);
            }
            if (!string.IsNullOrEmpty(Details))
            {
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append(Details);
            }
            return sb.Length > 0 ? sb.ToString() : "(unknown)";
        }
    }

    /// <summary>
    /// Disposable scope helper used to temporarily override the context.
    /// </summary>
    public readonly struct Scope : IDisposable
    {
        private readonly ContextInfo? _previous;

        public Scope(ContextInfo info)
        {
            _previous = _current.Value;
            _current.Value = info;
        }

        public void Dispose()
        {
            _current.Value = _previous;
        }
    }

    /// <summary>
    /// Begins a temporary scope for the provided source.
    /// </summary>
    public static Scope BeginScope(UnityEngine.Object sourceObject, string sourceType, string sourceName, string details = null)
    {
        var depth = (_current.Value?.Depth ?? -1) + 1;
        var info = new ContextInfo(sourceType, sourceName, details, CaptureMethodName(), CaptureStackTrace(), sourceObject, depth);
        return new Scope(info);
    }

    /// <summary>
    /// Returns the current scope, or captures the stack trace for the direct caller if no scope was provided.
    /// </summary>
    public static ContextInfo CaptureOrDefault(UnityEngine.Object fallbackObject, string sourceTypeFallback, string sourceNameFallback)
    {
        var ctx = _current.Value;
        if (ctx.HasValue && ctx.Value.IsValid)
        {
            return ctx.Value;
        }

        var method = CaptureMethodName(skipBuiltin: true);
        var trace = CaptureStackTrace();
        var type = string.IsNullOrEmpty(sourceTypeFallback) ? "Code" : sourceTypeFallback;
        string name = sourceNameFallback;
        if (string.IsNullOrEmpty(name))
        {
            if (fallbackObject != null) name = fallbackObject.name;
            else name = method ?? "Unknown";
        }

        return new ContextInfo(type, name, null, method, trace, fallbackObject, 0);
    }

    private static string CaptureMethodName(bool skipBuiltin = false)
    {
        try
        {
            var trace = new StackTrace(2, false);
            for (int i = 0; i < trace.FrameCount; i++)
            {
                var frame = trace.GetFrame(i);
                var method = frame?.GetMethod();
                if (method == null) continue;
                var declaringType = method.DeclaringType;
                if (declaringType == null) continue;

                if (skipBuiltin && (declaringType == typeof(UITweenPlayer) || declaringType == typeof(UITweenTrack) || declaringType == typeof(UITweenStateMachine) || declaringType == typeof(UITweenCallContext)))
                {
                    continue;
                }

                return declaringType.FullName + "." + method.Name;
            }
        }
        catch
        {
            // ignored
        }
        return null;
    }

    private static string CaptureStackTrace()
    {
        try
        {
            return Environment.StackTrace;
        }
        catch
        {
            return string.Empty;
        }
    }
}
