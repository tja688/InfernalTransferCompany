using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Central runtime monitor that records the lifecycle of UITween animations.
/// Thread-safe ring buffer with active mapping for live updates.
/// </summary>
public sealed class UITweenMonitor : ScriptableObject
{
    public const int Capacity = 512;
    public const long InvalidId = 0;

    public static UITweenMonitor Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = CreateInstance<UITweenMonitor>();
                _instance.hideFlags = HideFlags.HideAndDontSave;
            }
            return _instance;
        }
    }

    public event Action Changed;

    private static UITweenMonitor _instance;
    private readonly UITweenMonitorEntry[] _buffer = new UITweenMonitorEntry[Capacity];
    private readonly Dictionary<long, int> _active = new();
    private int _nextIndex;
    private int _count;
    private long _nextRequestId = InvalidId;
    private readonly object _lock = new();

    public enum EntryStatus
    {
        Pending,
        Playing,
        Completed,
        Interrupted
    }

    [Serializable]
    public struct UITweenMonitorEntry
    {
        public long requestId;
        public EntryStatus status;
        public double createdAt;
        public double startedAt;
        public double endedAt;
        public int startFrame;
        public int endFrame;
        public string initiatorType;
        public string initiatorName;
        public string initiatorDetails;
        public string initiatorMethod;
        public string initiatorStack;
        public UnityEngine.Object initiatorObject;
        public string responderPath;
        public string responderName;
        public UnityEngine.Object responderObject;
        public string presetName;
        public bool reversed;
        public float presetDuration;
        public string interruptionReason;
        public int tweenHash;

        public bool IsActive => status == EntryStatus.Pending || status == EntryStatus.Playing;
    }

    public long Register(UITweenPlayer player, UITweenPreset preset, bool reversed, Tween tween, UITweenCallContext.ContextInfo context)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        var entry = new UITweenMonitorEntry
        {
            requestId = Interlocked.Increment(ref _nextRequestId),
            status = EntryStatus.Pending,
            createdAt = Time.realtimeSinceStartup,
            startedAt = 0d,
            endedAt = 0d,
            startFrame = -1,
            endFrame = -1,
            initiatorType = context.SourceType ?? string.Empty,
            initiatorName = context.SourceName ?? string.Empty,
            initiatorDetails = context.Details ?? string.Empty,
            initiatorMethod = context.Method ?? string.Empty,
            initiatorStack = context.StackTrace ?? string.Empty,
            initiatorObject = context.SourceObject,
            responderObject = player,
            responderName = player.name,
            responderPath = BuildPath(player.transform),
            presetName = preset != null ? preset.presetName : "<null>",
            reversed = reversed,
            presetDuration = preset != null ? preset.duration : 0f,
            interruptionReason = string.Empty,
            tweenHash = tween != null ? tween.GetHashCode() : 0
        };

        lock (_lock)
        {
            int index = _nextIndex;
            _nextIndex = (_nextIndex + 1) % Capacity;
            if (_count < Capacity) _count++;

            ref var slot = ref _buffer[index];
            if (slot.requestId != InvalidId)
            {
                _active.Remove(slot.requestId);
            }

            _buffer[index] = entry;
            _active[entry.requestId] = index;
        }

        RaiseChanged();
        return entry.requestId;
    }

    public void MarkStarted(long requestId)
    {
        bool changed = false;
        lock (_lock)
        {
            if (_active.TryGetValue(requestId, out var index))
            {
                var entry = _buffer[index];
                entry.status = EntryStatus.Playing;
                entry.startedAt = Time.realtimeSinceStartup;
                entry.startFrame = Time.frameCount;
                _buffer[index] = entry;
                changed = true;
            }
        }
        if (changed) RaiseChanged();
    }

    public void MarkCompleted(long requestId)
    {
        bool changed = false;
        lock (_lock)
        {
            if (_active.TryGetValue(requestId, out var index))
            {
                var entry = _buffer[index];
                entry.status = EntryStatus.Completed;
                entry.endedAt = Time.realtimeSinceStartup;
                entry.endFrame = Time.frameCount;
                _buffer[index] = entry;
                _active.Remove(requestId);
                changed = true;
            }
        }
        if (changed) RaiseChanged();
    }

    public void MarkInterrupted(long requestId, string reason)
    {
        bool changed = false;
        lock (_lock)
        {
            if (_active.TryGetValue(requestId, out var index))
            {
                var entry = _buffer[index];
                entry.status = EntryStatus.Interrupted;
                entry.endedAt = Time.realtimeSinceStartup;
                entry.endFrame = Time.frameCount;
                entry.interruptionReason = reason ?? string.Empty;
                _buffer[index] = entry;
                _active.Remove(requestId);
                changed = true;
            }
        }
        if (changed) RaiseChanged();
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _active.Clear();
            _count = 0;
            _nextIndex = 0;
            _nextRequestId = InvalidId;
        }
        RaiseChanged();
    }

    public void GetEntries(List<UITweenMonitorEntry> destination)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        lock (_lock)
        {
            destination.Clear();
            if (_count == 0) return;

            int index = (_nextIndex - _count + Capacity) % Capacity;
            for (int i = 0; i < _count; i++)
            {
                int bufferIndex = (index + i) % Capacity;
                var entry = _buffer[bufferIndex];
                if (entry.requestId != InvalidId)
                {
                    destination.Add(entry);
                }
            }
        }
    }

    public bool TryGetEntry(long requestId, out UITweenMonitorEntry entry)
    {
        lock (_lock)
        {
            if (_active.TryGetValue(requestId, out var index))
            {
                entry = _buffer[index];
                return true;
            }
        }
        entry = default;
        return false;
    }

    public static string BuildPath(Transform transform)
    {
        if (transform == null) return string.Empty;
        lock (_pathLock)
        {
            _sharedPathBuilder.Clear();
            var stack = _sharedTransformStack;
            stack.Clear();

            var current = transform;
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            bool first = true;
            while (stack.Count > 0)
            {
                if (!first) _sharedPathBuilder.Append('/');
                _sharedPathBuilder.Append(stack.Pop());
                first = false;
            }

            string path = _sharedPathBuilder.ToString();
            _sharedPathBuilder.Clear();
            return path;
        }
    }

    private static readonly Stack<string> _sharedTransformStack = new();
    private static readonly System.Text.StringBuilder _sharedPathBuilder = new System.Text.StringBuilder(256);
    private static readonly object _pathLock = new();

    private void RaiseChanged()
    {
        Changed?.Invoke();
    }
}
