using System.Diagnostics;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime helper that can be attached next to <see cref="UITweenPlayer"/> to observe
/// the actual playback context of any preset. Enable the <see cref="debugEnabled"/>
/// switch to print a verbose report whenever the player starts / completes / aborts
/// a tween.
/// </summary>
[DisallowMultipleComponent]
public class UITweenDebugTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] UITweenPlayer player;

    [Header("Debugging")]
    [Tooltip("Turn on to emit detailed debug logs for every playback event.")]
    [SerializeField] bool debugEnabled = true;

    [TextArea(4, 12)]
    [Tooltip("Last generated debug report (also logged to the console).")]
    [SerializeField] string lastReport;

    RectTransform _rt;
    CanvasGroup _cg;
    Graphic _gfx;

    PlaybackDebugSnapshot _pendingSnapshot;
    float _playStartTime;
    float _expectedFinishTime;
    bool _overdueLogged;
    bool _awaitingCompletion;

    void Reset()
    {
        player = GetComponent<UITweenPlayer>();
    }

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null)
            _gfx = GetComponent<Graphic>();

        if (player == null)
            player = GetComponent<UITweenPlayer>();

        if (player != null)
        {
            player.SequencePrepared += HandleSequencePrepared;
            player.onPlay.AddListener(HandlePlayStarted);
            player.onComplete.AddListener(HandlePlayCompleted);
        }
        else
        {
            Debug.LogWarning("[UITweenDebugTester] No UITweenPlayer found on the same object.", this);
        }
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.SequencePrepared -= HandleSequencePrepared;
            player.onPlay.RemoveListener(HandlePlayStarted);
            player.onComplete.RemoveListener(HandlePlayCompleted);
        }
    }

    /// <summary>
    /// Public API to toggle debug output in runtime / inspector.
    /// </summary>
    public void SetDebugEnabled(bool enabled)
    {
        debugEnabled = enabled;
    }

    void HandleSequencePrepared(UITweenPreset preset, bool reversed, Sequence sequence)
    {
        if (preset == null)
        {
            _pendingSnapshot = default;
            return;
        }

        var snapshot = new PlaybackDebugSnapshot
        {
            preset = preset,
            reversed = reversed,
            sequence = sequence,
            duration = Mathf.Max(0.0001f, preset.duration),
            delay = preset.delay,
            loops = preset.loops,
            loopType = preset.loopType,
            unscaledTime = preset.unscaledTime,
            useCustomCurve = preset.useCustomCurve,
            customCurve = preset.customCurve,
            ease = preset.easeType
        };

        if (_rt != null)
        {
            snapshot.startAnchoredPosition = _rt.anchoredPosition;
            snapshot.startSize = _rt.sizeDelta;
            snapshot.startEulerZ = _rt.eulerAngles.z;
            snapshot.startPivot = _rt.pivot;
        }

        if (_cg != null)
        {
            snapshot.startAlpha = _cg.alpha;
        }
        else if (_gfx != null)
        {
            snapshot.startAlpha = _gfx.color.a;
            snapshot.startColor = _gfx.color;
        }

        if (player != null && player.TryGetBaseline(preset, out var basePos, out var baseSize, out var baseEuler, out var baseAlpha, out var baseColor, out var basePivot))
        {
            snapshot.hasBaseline = true;
            snapshot.baselinePosition = basePos;
            snapshot.baselineSize = baseSize;
            snapshot.baselineEulerZ = baseEuler;
            snapshot.baselineAlpha = baseAlpha;
            snapshot.baselineColor = baseColor;
            snapshot.baselinePivot = basePivot;
        }
        else
        {
            snapshot.baselinePosition = snapshot.startAnchoredPosition;
            snapshot.baselineSize = snapshot.startSize;
            snapshot.baselineEulerZ = snapshot.startEulerZ;
            snapshot.baselineAlpha = snapshot.startAlpha;
            snapshot.baselineColor = snapshot.startColor;
            snapshot.baselinePivot = snapshot.startPivot;
        }

        ComputeTargets(ref snapshot);

        if (sequence != null)
        {
            float delay = TweenExtensions.Delay(sequence);
            float duration = (float)TweenExtensions.Duration(sequence, includeLoops: true);
            snapshot.expectedDuration = delay + duration;
        }
        else
        {
            snapshot.expectedDuration = ComputeExpectedDurationFallback(snapshot);
        }

        _pendingSnapshot = snapshot;
        _awaitingCompletion = true;

        if (sequence != null)
        {
            sequence.OnKill(() => HandleSequenceKilled(sequence));
        }
    }

    void HandlePlayStarted()
    {
        if (!debugEnabled || _pendingSnapshot.preset == null)
            return;

        _playStartTime = _pendingSnapshot.unscaledTime ? Time.unscaledTime : Time.time;
        _overdueLogged = false;
        if (!float.IsInfinity(_pendingSnapshot.expectedDuration))
        {
            _expectedFinishTime = _playStartTime + _pendingSnapshot.expectedDuration;
        }
        else
        {
            _expectedFinishTime = float.PositiveInfinity;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[UITweenDebugTester] ▶ Play '{_pendingSnapshot.preset.presetName}' {( _pendingSnapshot.reversed ? "(reversed)" : string.Empty)}");
        sb.AppendLine($"  Duration: {_pendingSnapshot.duration:0.###}s  Delay: {_pendingSnapshot.delay:0.###}s  Loops: {_pendingSnapshot.loops} ({_pendingSnapshot.loopType})  Update: {( _pendingSnapshot.unscaledTime ? "Unscaled" : "Scaled")}");
        sb.AppendLine($"  Ease: {(_pendingSnapshot.useCustomCurve ? "CustomCurve" : _pendingSnapshot.ease.ToString())}");
        if (_pendingSnapshot.useCustomCurve && _pendingSnapshot.customCurve != null)
        {
            sb.AppendLine($"    Curve Keys: {FormatCurve(_pendingSnapshot.customCurve)}");
        }

        sb.AppendLine("  Start State:");
        sb.AppendLine($"    AnchoredPos: {_pendingSnapshot.startAnchoredPosition}");
        sb.AppendLine($"    SizeDelta  : {_pendingSnapshot.startSize}");
        sb.AppendLine($"    EulerZ     : {_pendingSnapshot.startEulerZ:0.###}");
        sb.AppendLine($"    Pivot      : {_pendingSnapshot.startPivot}");
        if (_pendingSnapshot.startAlpha.HasValue)
            sb.AppendLine($"    Alpha      : {_pendingSnapshot.startAlpha.Value:0.###}");
        if (_pendingSnapshot.startColor.HasValue)
            sb.AppendLine($"    Color      : {_pendingSnapshot.startColor.Value}");

        if (_pendingSnapshot.hasBaseline)
        {
            sb.AppendLine("  Cached Baseline:");
            sb.AppendLine($"    AnchoredPos: {_pendingSnapshot.baselinePosition}");
            sb.AppendLine($"    SizeDelta  : {_pendingSnapshot.baselineSize}");
            sb.AppendLine($"    EulerZ     : {_pendingSnapshot.baselineEulerZ:0.###}");
            sb.AppendLine($"    Pivot      : {_pendingSnapshot.baselinePivot}");
            if (_pendingSnapshot.baselineAlpha.HasValue)
                sb.AppendLine($"    Alpha      : {_pendingSnapshot.baselineAlpha.Value:0.###}");
            if (_pendingSnapshot.baselineColor.HasValue)
                sb.AppendLine($"    Color      : {_pendingSnapshot.baselineColor.Value}");
        }

        sb.AppendLine("  Target State:");
        sb.AppendLine($"    AnchoredPos: {_pendingSnapshot.targetAnchoredPosition}");
        sb.AppendLine($"    SizeDelta  : {_pendingSnapshot.targetSize}");
        sb.AppendLine($"    EulerZ     : {_pendingSnapshot.targetEulerZ:0.###}");
        if (_pendingSnapshot.targetPivot.HasValue)
            sb.AppendLine($"    Pivot      : {_pendingSnapshot.targetPivot.Value}");
        if (_pendingSnapshot.targetAlpha.HasValue)
            sb.AppendLine($"    Alpha      : {_pendingSnapshot.targetAlpha.Value:0.###}");
        if (_pendingSnapshot.targetColor.HasValue)
            sb.AppendLine($"    Color      : {_pendingSnapshot.targetColor.Value}");

        string originSummary;
        var callStack = CaptureInvocationTrace(out originSummary);
        if (!string.IsNullOrEmpty(originSummary))
            sb.AppendLine($"  Triggered By: {originSummary}");
        if (!string.IsNullOrEmpty(callStack))
        {
            sb.AppendLine("  Call Stack:");
            sb.Append(callStack);
        }

        if (_pendingSnapshot.usesBezier)
        {
            sb.AppendLine("  Path (Quadratic Bezier):");
            sb.AppendLine($"    Start : {_pendingSnapshot.startAnchoredPosition}");
            sb.AppendLine($"    Pass  : {_pendingSnapshot.bezierPassPoint}");
            sb.AppendLine($"    Ctrl  : {_pendingSnapshot.bezierControlPoint}");
            sb.AppendLine($"    End   : {_pendingSnapshot.targetAnchoredPosition}");
        }

        lastReport = sb.ToString();
        Debug.Log(lastReport, this);
    }

    void HandlePlayCompleted()
    {
        if (!debugEnabled || _pendingSnapshot.preset == null)
            return;

        _awaitingCompletion = false;
        float finishedAt = _pendingSnapshot.unscaledTime ? Time.unscaledTime : Time.time;
        float elapsed = finishedAt - _playStartTime;
        var snapshot = _pendingSnapshot;

        var sb = new StringBuilder();
        sb.AppendLine($"[UITweenDebugTester] ✔ Complete '{snapshot.preset.presetName}' in {elapsed:0.###}s (configured {snapshot.duration:0.###}s).");

        if (!float.IsInfinity(snapshot.expectedDuration))
        {
            float expected = snapshot.expectedDuration;
            float delta = Mathf.Abs(elapsed - expected);
            sb.AppendLine($"  Expected total duration: {expected:0.###}s (Δ {delta:0.###}s)");
        }

        var currentState = CaptureCurrentState();
        AppendStateComparison(ref sb, snapshot, currentState);

        Debug.Log(sb.ToString(), this);
    }

    void HandleSequenceKilled(Sequence sequence)
    {
        if (!debugEnabled || !_awaitingCompletion || sequence != _pendingSnapshot.sequence)
            return;

        _awaitingCompletion = false;
        Debug.LogWarning($"[UITweenDebugTester] ✖ Tween '{_pendingSnapshot.preset.presetName}' was interrupted before completion.", this);
    }

    void Update()
    {
        if (!debugEnabled || !_awaitingCompletion || _pendingSnapshot.preset == null)
            return;

        if (float.IsInfinity(_expectedFinishTime))
            return;

        float now = _pendingSnapshot.unscaledTime ? Time.unscaledTime : Time.time;
        const float tolerance = 0.1f;
        if (!_overdueLogged && now > _expectedFinishTime + tolerance)
        {
            _overdueLogged = true;
            bool sequencePlaying = _pendingSnapshot.sequence != null && TweenExtensions.IsPlaying(_pendingSnapshot.sequence);
            string status = sequencePlaying ? "Sequence still reports playing" : "Sequence is inactive";
            Debug.LogWarning($"[UITweenDebugTester] ⚠ Tween '{_pendingSnapshot.preset.presetName}' exceeded expected duration ({_pendingSnapshot.expectedDuration:0.###}s). {status}.", this);
        }
    }

    void ComputeTargets(ref PlaybackDebugSnapshot snapshot)
    {
        if (snapshot.preset == null || _rt == null)
            return;

        var preset = snapshot.preset;

        if (preset.animatePosition)
        {
            if (!preset.useRelativeMode && preset.useBezierPath)
            {
                snapshot.usesBezier = true;
                snapshot.bezierPassPoint = preset.passThroughPointC;
                float tStar = Mathf.Clamp(preset.passTStar, 0.05f, 0.95f);
                if (!snapshot.reversed)
                {
                    snapshot.targetAnchoredPosition = preset.targetAnchoredPosition;
                    snapshot.bezierControlPoint = SolveQuadraticControlPoint(snapshot.startAnchoredPosition, preset.targetAnchoredPosition, preset.passThroughPointC, tStar);
                }
                else
                {
                    snapshot.targetAnchoredPosition = snapshot.baselinePosition;
                    snapshot.bezierControlPoint = SolveQuadraticControlPoint(snapshot.startAnchoredPosition, snapshot.baselinePosition, preset.passThroughPointC, 1f - tStar);
                }
            }
            else
            {
                var basePos = snapshot.baselinePosition;
                if (preset.useRelativeMode)
                {
                    snapshot.targetAnchoredPosition = snapshot.reversed ? basePos : basePos + preset.targetAnchoredPosition;
                }
                else
                {
                    snapshot.targetAnchoredPosition = snapshot.reversed ? basePos : preset.targetAnchoredPosition;
                }
            }
        }
        else
        {
            snapshot.targetAnchoredPosition = snapshot.startAnchoredPosition;
        }

        if (preset.animateSize)
        {
            var baseSize = snapshot.baselineSize;
            snapshot.targetSize = preset.useRelativeMode
                ? (snapshot.reversed ? baseSize : baseSize + preset.targetSizeDelta)
                : (snapshot.reversed ? baseSize : preset.targetSizeDelta);
        }
        else
        {
            snapshot.targetSize = snapshot.startSize;
        }

        if (preset.animateRotationZ)
        {
            var baseEuler = snapshot.baselineEulerZ;
            snapshot.targetEulerZ = preset.useRelativeMode
                ? (snapshot.reversed ? baseEuler : baseEuler + preset.targetEulerZ)
                : (snapshot.reversed ? baseEuler : preset.targetEulerZ);
        }
        else
        {
            snapshot.targetEulerZ = snapshot.startEulerZ;
        }

        if (preset.animatePivot && !preset.useRelativeMode)
        {
            snapshot.targetPivot = snapshot.reversed ? snapshot.baselinePivot : preset.targetPivot;
        }

        if (preset.animateAlpha)
        {
            float startAlpha = snapshot.baselineAlpha ?? snapshot.startAlpha ?? 1f;
            float targetAlpha = preset.useRelativeMode
                ? (snapshot.reversed ? startAlpha : startAlpha + preset.targetAlpha)
                : (snapshot.reversed ? startAlpha : preset.targetAlpha);
            snapshot.targetAlpha = targetAlpha;
        }

        if (preset.animateColor)
        {
            Color startColor = snapshot.baselineColor ?? snapshot.startColor ?? Color.white;
            Color targetColor = snapshot.reversed ? startColor : preset.targetColor;
            snapshot.targetColor = targetColor;
        }
    }

    static Vector2 SolveQuadraticControlPoint(Vector2 start, Vector2 end, Vector2 pass, float tStar)
    {
        float u = 1f - tStar;
        float denom = 2f * u * tStar;
        if (denom < 1e-6f)
            return 0.5f * (start + end);
        return (pass - (u * u) * start - (tStar * tStar) * end) / denom;
    }

    static string FormatCurve(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0)
            return "<empty>";

        var sb = new StringBuilder();
        for (int i = 0; i < curve.length; i++)
        {
            var key = curve.keys[i];
            sb.Append($"({key.time:0.###},{key.value:0.###})");
            if (i < curve.length - 1)
                sb.Append(", ");
        }
        return sb.ToString();
    }

    string CaptureInvocationTrace(out string originSummary)
    {
        originSummary = string.Empty;
        var trace = new StackTrace(1, true);
        var sb = new StringBuilder();
        bool originAssigned = false;
        for (int i = 0; i < trace.FrameCount; i++)
        {
            var frame = trace.GetFrame(i);
            var method = frame.GetMethod();
            if (method == null)
                continue;

            var declaringType = method.DeclaringType;
            string typeName = declaringType != null ? declaringType.FullName : "<global>";
            if (typeName != null && (typeName.Contains(nameof(UITweenDebugTester)) || typeName.StartsWith("DG.Tweening") || typeName.StartsWith("System.")))
                continue;

            string formatted = FormatFrame(typeName, method.Name, frame);
            if (!originAssigned)
            {
                originAssigned = true;
                originSummary = formatted.Trim();
            }
            sb.Append("    ");
            sb.AppendLine(formatted);
        }

        return sb.ToString();
    }

    static string FormatFrame(string typeName, string methodName, StackFrame frame)
    {
        var sb = new StringBuilder();
        sb.Append(typeName);
        sb.Append('.');
        sb.Append(methodName);
        sb.Append(" (at ");
        string fileName = frame.GetFileName();
        if (!string.IsNullOrEmpty(fileName))
        {
            sb.Append(fileName);
            sb.Append(':');
            sb.Append(frame.GetFileLineNumber());
        }
        else
        {
            sb.Append("<unknown>");
        }
        sb.Append(')');
        return sb.ToString();
    }

    PlaybackStateSnapshot CaptureCurrentState()
    {
        var state = new PlaybackStateSnapshot();

        if (_rt != null)
        {
            state.position = _rt.anchoredPosition;
            state.size = _rt.sizeDelta;
            state.eulerZ = _rt.eulerAngles.z;
            state.pivot = _rt.pivot;
        }

        if (_cg != null)
        {
            state.alpha = _cg.alpha;
        }
        else if (_gfx != null)
        {
            state.alpha = _gfx.color.a;
            state.color = _gfx.color;
        }

        return state;
    }

    void AppendStateComparison(ref StringBuilder sb, in PlaybackDebugSnapshot expectedSnapshot, in PlaybackStateSnapshot actual)
    {
        sb.AppendLine("  Final State:");
        sb.AppendLine($"    AnchoredPos: {actual.position}");
        sb.AppendLine($"    SizeDelta  : {actual.size}");
        sb.AppendLine($"    EulerZ     : {actual.eulerZ:0.###}");
        sb.AppendLine($"    Pivot      : {actual.pivot}");
        if (actual.alpha.HasValue)
            sb.AppendLine($"    Alpha      : {actual.alpha.Value:0.###}");
        if (actual.color.HasValue)
            sb.AppendLine($"    Color      : {actual.color.Value}");

        sb.AppendLine("  Comparison vs Target:");
        AppendComparisonLine(ref sb, "AnchoredPos", actual.position, expectedSnapshot.targetAnchoredPosition, 0.5f);
        AppendComparisonLine(ref sb, "SizeDelta", actual.size, expectedSnapshot.targetSize, 0.5f);
        AppendComparisonLine(ref sb, "EulerZ", actual.eulerZ, expectedSnapshot.targetEulerZ, 0.5f);

        if (expectedSnapshot.targetPivot.HasValue)
            AppendComparisonLine(ref sb, "Pivot", actual.pivot, expectedSnapshot.targetPivot.Value, 0.001f);

        if (expectedSnapshot.targetAlpha.HasValue && actual.alpha.HasValue)
            AppendComparisonLine(ref sb, "Alpha", actual.alpha.Value, expectedSnapshot.targetAlpha.Value, 0.01f);

        if (expectedSnapshot.targetColor.HasValue && actual.color.HasValue)
        {
            var expectedColor = expectedSnapshot.targetColor.Value;
            var actualColor = actual.color.Value;
            float delta = Mathf.Max(Mathf.Abs(actualColor.r - expectedColor.r), Mathf.Abs(actualColor.g - expectedColor.g), Mathf.Abs(actualColor.b - expectedColor.b), Mathf.Abs(actualColor.a - expectedColor.a));
            sb.AppendLine($"    Color Δmax: {delta:0.###} (target {expectedColor})");
        }
    }

    static void AppendComparisonLine(ref StringBuilder sb, string label, Vector2 actual, Vector2 expected, float tolerance)
    {
        float delta = Vector2.Distance(actual, expected);
        sb.AppendLine($"    {label} Δ: {delta:0.###} (target {expected})");
        if (delta > tolerance)
            sb.AppendLine($"      ⚠ {label} difference exceeds tolerance {tolerance:0.###}");
    }

    static void AppendComparisonLine(ref StringBuilder sb, string label, float actual, float expected, float tolerance)
    {
        float delta = Mathf.Abs(actual - expected);
        sb.AppendLine($"    {label} Δ: {delta:0.###} (target {expected:0.###})");
        if (delta > tolerance)
            sb.AppendLine($"      ⚠ {label} difference exceeds tolerance {tolerance:0.###}");
    }

    static float ComputeExpectedDurationFallback(in PlaybackDebugSnapshot snapshot)
    {
        if (snapshot.loops < 0)
            return float.PositiveInfinity;

        float cycles = Mathf.Max(1, snapshot.loops + 1);
        float total = snapshot.delay + snapshot.duration * cycles;
        return total;
    }

    struct PlaybackDebugSnapshot
    {
        public UITweenPreset preset;
        public bool reversed;
        public Sequence sequence;
        public bool hasBaseline;

        public Vector2 startAnchoredPosition;
        public Vector2 startSize;
        public float startEulerZ;
        public Vector2 startPivot;
        public float? startAlpha;
        public Color? startColor;

        public Vector2 baselinePosition;
        public Vector2 baselineSize;
        public float baselineEulerZ;
        public float? baselineAlpha;
        public Color? baselineColor;
        public Vector2 baselinePivot;

        public Vector2 targetAnchoredPosition;
        public Vector2 targetSize;
        public float targetEulerZ;
        public Vector2? targetPivot;
        public float? targetAlpha;
        public Color? targetColor;

        public bool usesBezier;
        public Vector2 bezierControlPoint;
        public Vector2 bezierPassPoint;

        public float duration;
        public float delay;
        public int loops;
        public LoopType loopType;
        public bool unscaledTime;
        public bool useCustomCurve;
        public AnimationCurve customCurve;
        public Ease ease;
        public float expectedDuration;
    }

    struct PlaybackStateSnapshot
    {
        public Vector2 position;
        public Vector2 size;
        public float eulerZ;
        public Vector2 pivot;
        public float? alpha;
        public Color? color;
    }
}
