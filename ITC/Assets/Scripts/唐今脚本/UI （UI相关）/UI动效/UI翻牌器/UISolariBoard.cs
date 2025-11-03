using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics; // 1. 在文件顶部添加这个 using
using UnityEngine.EventSystems; // <--- 在这里添加这一行

namespace ITC.UIFX
{
    public enum FlipStrategy
    {
        Simultaneous,
        PureRandom,
        PerlinNoise
    }

    /// <summary>
    /// Solari Board 风格的 UI 翻牌控制器，依赖 UVInspector 提供的网格与贴图库，实现分阶段的全局翻牌过渡。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UISolariBoard : MonoBehaviour
    {
        [Header("核心引用")]
        [SerializeField] private UVInspector uvInspector;
        [Tooltip("若未手动指定，将在Awake阶段尝试在自身或子物体中查找UVInspector组件。")]
        [SerializeField] private bool autoResolveInspector = true;
        [Tooltip("是否使用UnscaledDeltaTime驱动动画，避免受Time.timeScale影响。")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("默认翻牌配置")]
        [Min(0f)] [SerializeField] private float defaultStartupDuration = 0.5f;
        [Min(0f)] [SerializeField] private float defaultFlippingDuration = 1.0f;
        [Min(0f)] [SerializeField] private float defaultEndingDuration = 0.5f;
        [SerializeField] private FlipStrategy defaultStrategy = FlipStrategy.PureRandom;
        [Tooltip("翻转速度（度/秒），决定每个单元格的翻动节奏。")]
        [Min(1f)] [SerializeField] private float rotationSpeed = 720f;
        [Tooltip("用于上下文菜单测试的默认目标贴图索引。")]
        [SerializeField] private int defaultTargetTextureIndex;

        [Header("随机策略参数")]
        [Tooltip("是否使用固定随机种子，便于可重复的调试。")]
        [SerializeField] private bool deterministicRandom;
        [Tooltip("固定随机模式下使用的种子值。")]
        [SerializeField] private int randomSeed = 2025;

        [Header("柏林噪声策略参数")]
        [Tooltip("生成噪声时使用的基础种子。每次过渡会在此基础上引入扰动。")]
        [SerializeField] private int perlinSeed = 12345;
        [Tooltip("噪声采样尺度，值越大变化越平缓。")]
        [Min(0.0001f)] [SerializeField] private float perlinScale = 0.35f;
        [Tooltip("用于设定多个扩散起点，值越大越易产生多源扩散效果。")]
        [Range(1, 8)] [SerializeField] private int perlinAnchorCount = 2;
        [Tooltip("起点权重：0仅使用噪声，1则完全按起点距离排序。")]
        [Range(0f, 1f)] [SerializeField] private float anchorInfluence = 0.6f;

        [Header("音效设置")]
        [Tooltip("用于播放翻牌音效的AudioSource组件。")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("翻牌切换内容时播放的音效片段。")]
        [SerializeField] private AudioClip flipSoundClip;
        [Tooltip("最小音效播放间隔（秒），当翻牌数量很多时使用此间隔。")]
        [Min(0.01f)] [SerializeField] private float minSoundInterval = 0.05f;
        [Tooltip("最大音效播放间隔（秒），当翻牌数量很少时使用此间隔。")]
        [Min(0.01f)] [SerializeField] private float maxSoundInterval = 0.3f;
        [Tooltip("用于映射翻牌数量到播放间隔的参考单元格数量。当翻牌数量达到此值时，使用最小间隔。")]
        [Min(1)] [SerializeField] private int referenceCellCount = 20;

        private enum CellPhase
        {
            Idle,
            Waiting,
            Flipping,
            Ending,
            Finalizing,
            Completed
        }

        private sealed class CellRuntime
        {
            public int index;
            public RawImage rawImage;
            public RectTransform rectTransform;
            public Rect uvRect;
            public float startDelay;
            public float stopDelay;
            public float cumulativeAngle;
            public float nextSwapAngle;
            public float stopAngle;
            public bool finalTextureApplied;
            public CellPhase phase;
        }

        private readonly List<CellRuntime> _cells = new List<CellRuntime>(128);

        private bool _transitionActive;
        private float _elapsed;
        private float _startupDuration;
        private float _flippingDuration;
        private float _endingDuration;
        private float _endingPhaseStart;
        private Texture _targetTexture;
        private FlipStrategy _activeStrategy;
        private System.Random _random;
        private int _transitionSerial;
        private int _cachedColumns = 1;
        private int _cachedRows = 1;

        // 音效播放控制
        private float _soundTimer;
        private float _currentSoundInterval;

        public bool IsTransitionActive => _transitionActive;

        private void Awake()
        {
            if (uvInspector == null && autoResolveInspector)
            {
                uvInspector = GetComponent<UVInspector>() ?? GetComponentInChildren<UVInspector>();
            }
        }

        private void Update()
        {
            if (!_transitionActive)
            {
                return;
            }

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (deltaTime <= Mathf.Epsilon)
            {
                return;
            }

            _elapsed += deltaTime;

            bool anyRunning = false;
            int activeFlipCount = 0;
            for (int i = 0; i < _cells.Count; i++)
            {
                if (UpdateCell(_cells[i], deltaTime))
                {
                    anyRunning = true;
                }
                
                // 统计正在翻牌的单元格数量（Flipping、Ending、Finalizing状态）
                if (_cells[i].phase == CellPhase.Flipping || 
                    _cells[i].phase == CellPhase.Ending || 
                    _cells[i].phase == CellPhase.Finalizing)
                {
                    activeFlipCount++;
                }
            }

            // 根据当前翻牌数量动态调整音效播放间隔
            UpdateSoundPlayback(activeFlipCount, deltaTime);

            if (!anyRunning)
            {
                CompleteTransition();
            }
        }

        /// <summary>
        /// 以完整参数启动一次翻牌过渡。
        /// </summary>
        public void StartFlipTransition(Texture targetTexture, FlipStrategy strategyType, float startupDuration = 0.5f, float flippingDuration = 1.0f, float endingDuration = 0.5f)
        {
            if (!PrepareTransition(targetTexture, strategyType, startupDuration, flippingDuration, endingDuration))
            {
                return;
            }

            _transitionActive = true;
        }

        /// <summary>
        /// 使用贴图池中的索引，按默认配置触发翻牌（便于UnityEvent调用）。
        /// </summary>
        public void StartFlipTransition(int targetTextureIndex)
        {
            // 打印时间、目标索引和调用堆栈
            // UnityEngine.Debug.LogWarning($"UISolariBoard: StartFlipTransition({targetTextureIndex}) " +
            //                    $"在 {Time.time} 时被调用。\n" +
            //                    $"调用堆栈: \n{new StackTrace()}\n", this);

            Texture target = ResolveTextureByIndex(targetTextureIndex);
            StartFlipTransition(target, defaultStrategy, defaultStartupDuration, defaultFlippingDuration, defaultEndingDuration);
        }

        /// <summary>
        /// 取消当前过渡，所有单元立即归零角度。
        /// </summary>
        public void CancelTransition()
        {
            if (!_transitionActive)
            {
                return;
            }

            for (int i = 0; i < _cells.Count; i++)
            {
                ResetCellTransform(_cells[i]);
            }

            CompleteTransition();
        }

        /// <summary>
        /// 立即清空翻牌器内所有基础单元显示的纹理（索引0的默认清空状态）。
        /// </summary>
        public void ClearAllCells()
        {
            if (!EnsureInspector())
            {
                UnityEngine.Debug.LogWarning("UISolariBoard：未找到 UVInspector，无法清空单元格。", this);
                return;
            }

            if (!SynchronizeCells())
            {
                return;
            }

            for (int i = 0; i < _cells.Count; i++)
            {
                ApplyTextureToCell(_cells[i], null);
                ResetCellTransform(_cells[i]);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("调试：使用默认配置翻牌")]
        private void ContextStartDefault()
        {
            Texture target = ResolveTextureByIndex(defaultTargetTextureIndex);
            StartFlipTransition(target, defaultStrategy, defaultStartupDuration, defaultFlippingDuration, defaultEndingDuration);
        }

        [ContextMenu("调试：终止当前翻牌")]
        private void ContextCancel()
        {
            CancelTransition();
        }
#endif

        private bool PrepareTransition(Texture targetTexture, FlipStrategy strategyType, float startupDuration, float flippingDuration, float endingDuration)
        {
            if (!EnsureInspector())
            {
                UnityEngine.Debug.LogWarning("UISolariBoard：未找到 UVInspector，无法启动翻牌。", this);
                return false;
            }

            if (!SynchronizeCells())
            {
                UnityEngine.Debug.LogWarning("UISolariBoard：UVInspector 未发现可用的 RawImage 单元格。", this);
                return false;
            }

            AbortInternal();

            _targetTexture = targetTexture;
            _activeStrategy = strategyType;
            _startupDuration = Mathf.Max(0f, startupDuration);
            _flippingDuration = Mathf.Max(0f, flippingDuration);
            _endingDuration = Mathf.Max(0f, endingDuration);
            _endingPhaseStart = _startupDuration + _flippingDuration;
            _elapsed = 0f;

            InitialiseRandomEngines();
            ApplyDelays(false, _startupDuration);
            ApplyDelays(true, _endingDuration);

            for (int i = 0; i < _cells.Count; i++)
            {
                CellRuntime cell = _cells[i];
                cell.cumulativeAngle = 0f;
                cell.nextSwapAngle = 90f;
                cell.stopAngle = 0f;
                cell.finalTextureApplied = false;
                cell.phase = CellPhase.Waiting;
                cell.rectTransform.localEulerAngles = Vector3.zero;
            }

            // 重置音效播放计时器
            _soundTimer = 0f;
            _currentSoundInterval = minSoundInterval;

            return true;
        }

        private bool EnsureInspector()
        {
            if (uvInspector != null)
            {
                return true;
            }

            if (!autoResolveInspector)
            {
                return false;
            }

            uvInspector = GetComponent<UVInspector>() ?? GetComponentInChildren<UVInspector>();
            return uvInspector != null;
        }

        private bool SynchronizeCells()
        {
            uvInspector.RefreshGrid(false);

            IReadOnlyList<RawImage> sourceCells = uvInspector.Cells;
            if (sourceCells == null || sourceCells.Count == 0)
            {
                _cells.Clear();
                return false;
            }

            _cells.Clear();
            _cachedColumns = Mathf.Max(1, uvInspector.ColumnCount);
            _cachedRows = Mathf.Max(1, uvInspector.RowCount);

            for (int i = 0; i < sourceCells.Count; i++)
            {
                RawImage raw = sourceCells[i];
                if (raw == null)
                {
                    continue;
                }

                var runtime = new CellRuntime
                {
                    index = i,
                    rawImage = raw,
                    rectTransform = raw.rectTransform,
                    uvRect = uvInspector.GetUvRectForCell(i),
                    startDelay = 0f,
                    stopDelay = 0f,
                    cumulativeAngle = 0f,
                    nextSwapAngle = 90f,
                    stopAngle = 0f,
                    finalTextureApplied = false,
                    phase = CellPhase.Waiting
                };

                runtime.rectTransform.localEulerAngles = Vector3.zero;
                _cells.Add(runtime);
            }

            return _cells.Count > 0;
        }

        private void InitialiseRandomEngines()
        {
            int seed = deterministicRandom
                ? randomSeed + _transitionSerial
                : unchecked(Environment.TickCount + (_transitionSerial * 97));

            _random = new System.Random(seed);
            _transitionSerial++;
        }

        private void ApplyDelays(bool isEndingPhase, float duration)
        {
            float[] delays = BuildDelays(_activeStrategy, duration, isEndingPhase);
            for (int i = 0; i < _cells.Count && i < delays.Length; i++)
            {
                if (isEndingPhase)
                {
                    _cells[i].stopDelay = delays[i];
                }
                else
                {
                    _cells[i].startDelay = delays[i];
                }
            }
        }

        private float[] BuildDelays(FlipStrategy strategy, float duration, bool isEndingPhase)
        {
            int count = _cells.Count;
            float[] delays = new float[count];

            if (count == 0 || duration <= Mathf.Epsilon)
            {
                return delays;
            }

            switch (strategy)
            {
                case FlipStrategy.Simultaneous:
                    return delays;
                case FlipStrategy.PureRandom:
                    for (int i = 0; i < count; i++)
                    {
                        delays[i] = (float)(_random.NextDouble() * duration);
                    }
                    return delays;
                case FlipStrategy.PerlinNoise:
                    int phaseSeed = unchecked(perlinSeed ^ (_transitionSerial * 397) ^ (isEndingPhase ? 0x1F : 0x17));
                    return BuildPerlinDelays(duration, phaseSeed);
                default:
                    return delays;
            }
        }

        private float[] BuildPerlinDelays(float duration, int seed)
        {
            int count = _cells.Count;
            float[] delays = new float[count];
            if (count == 0)
            {
                return delays;
            }

            System.Random rng = new System.Random(seed);
            float offsetX = (float)rng.NextDouble() * 1000f;
            float offsetY = (float)rng.NextDouble() * 1000f;
            int anchors = Mathf.Clamp(perlinAnchorCount, 1, count);

            List<Vector2> anchorPositions = new List<Vector2>(anchors);
            for (int i = 0; i < anchors; i++)
            {
                anchorPositions.Add(new Vector2(rng.Next(0, _cachedColumns), rng.Next(0, _cachedRows)));
            }

            float diagonal = Mathf.Sqrt((_cachedColumns - 1) * (_cachedColumns - 1) + (_cachedRows - 1) * (_cachedRows - 1));
            if (diagonal <= Mathf.Epsilon)
            {
                diagonal = 1f;
            }

            float scale = Mathf.Max(0.0001f, perlinScale);
            float minValue = float.MaxValue;
            float maxValue = float.MinValue;
            float[] values = new float[count];

            for (int i = 0; i < count; i++)
            {
                int column = _cells[i].index % _cachedColumns;
                int row = _cells[i].index / _cachedColumns;

                float noise = Mathf.PerlinNoise((column + offsetX) * scale, (row + offsetY) * scale);

                float closestDistance = diagonal;
                for (int a = 0; a < anchorPositions.Count; a++)
                {
                    float distance = Vector2.Distance(new Vector2(column, row), anchorPositions[a]);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                    }
                }

                float normalizedDistance = Mathf.Clamp01(closestDistance / diagonal);
                float anchorBias = 1f - normalizedDistance; // 越靠近锚点值越大
                float blended = Mathf.Lerp(noise, 1f - anchorBias, anchorInfluence);

                values[i] = blended;
                if (blended < minValue) minValue = blended;
                if (blended > maxValue) maxValue = blended;
            }

            float range = maxValue - minValue;
            if (range <= 0.0001f)
            {
                for (int i = 0; i < count; i++)
                {
                    delays[i] = duration * 0.5f;
                }
                return delays;
            }

            for (int i = 0; i < count; i++)
            {
                float normalized = (values[i] - minValue) / range;
                delays[i] = Mathf.Lerp(0f, duration, normalized);
            }

            return delays;
        }

        private bool UpdateCell(CellRuntime cell, float deltaTime)
        {
            if (cell.phase == CellPhase.Completed || cell.rawImage == null || cell.rectTransform == null)
            {
                return false;
            }

            if (cell.phase == CellPhase.Waiting)
            {
                if (_elapsed >= cell.startDelay)
                {
                    cell.phase = CellPhase.Flipping;
                    cell.cumulativeAngle = 0f;
                    cell.nextSwapAngle = 90f;
                }
                else
                {
                    return true;
                }
            }

            if ((cell.phase == CellPhase.Flipping || cell.phase == CellPhase.Waiting) && _elapsed >= _endingPhaseStart + cell.stopDelay)
            {
                cell.phase = CellPhase.Ending;
            }

            float speed = Mathf.Max(1f, rotationSpeed);
            cell.cumulativeAngle += speed * deltaTime;

            bool continueLoop = true;
            while (continueLoop && cell.cumulativeAngle >= cell.nextSwapAngle)
            {
                if (cell.phase == CellPhase.Ending && !cell.finalTextureApplied)
                {
                    ApplyTextureToCell(cell, _targetTexture);
                    cell.finalTextureApplied = true;
                    cell.phase = CellPhase.Finalizing;
                    cell.stopAngle = Mathf.Ceil((cell.cumulativeAngle + 0.0001f) / 360f) * 360f;
                    cell.nextSwapAngle = float.PositiveInfinity;
                    continueLoop = false;
                }
                else if (cell.phase == CellPhase.Flipping || cell.phase == CellPhase.Ending)
                {
                    Texture randomTexture = GetRandomTexture();
                    ApplyTextureToCell(cell, randomTexture);
                    cell.nextSwapAngle += 180f;
                }
                else
                {
                    cell.nextSwapAngle += 180f;
                }
            }

            if (cell.phase == CellPhase.Finalizing)
            {
                if (cell.cumulativeAngle >= cell.stopAngle)
                {
                    cell.cumulativeAngle = cell.stopAngle;
                    ResetCellTransform(cell);
                    cell.phase = CellPhase.Completed;
                    return false;
                }
            }

            float visualAngle = Mathf.Repeat(cell.cumulativeAngle, 360f);
            cell.rectTransform.localEulerAngles = new Vector3(visualAngle, 0f, 0f);

            return cell.phase != CellPhase.Completed;
        }

        private void ApplyTextureToCell(CellRuntime cell, Texture texture)
        {
            if (cell.rawImage == null)
            {
                return;
            }

            cell.rawImage.texture = texture;
            if (texture != null)
            {
                cell.rawImage.uvRect = cell.uvRect;
                cell.rawImage.enabled = true;
            }
            else
            {
                cell.rawImage.enabled = false;
            }
        }

        /// <summary>
        /// 根据当前翻牌数量动态更新音效播放间隔并播放音效。
        /// </summary>
        private void UpdateSoundPlayback(int activeFlipCount, float deltaTime)
        {
            if (audioSource == null || activeFlipCount == 0)
            {
                _soundTimer = 0f;
                return;
            }

            // 根据翻牌数量计算播放间隔（函数映射）
            // 数量越多，间隔越短；数量越少，间隔越长
            // 使用线性插值：当数量为1时使用maxInterval，当数量>=referenceCellCount时使用minInterval
            float normalizedCount = Mathf.Clamp01(activeFlipCount / (float)Mathf.Max(1, referenceCellCount));
            _currentSoundInterval = Mathf.Lerp(maxSoundInterval, minSoundInterval, normalizedCount);
            _currentSoundInterval = Mathf.Max(minSoundInterval, _currentSoundInterval);

            // 累加计时器
            _soundTimer += deltaTime;

            // 达到间隔时播放音效
            if (_soundTimer >= _currentSoundInterval)
            {
                PlayFlipSound();
                _soundTimer = 0f; // 重置计时器
            }
        }

        /// <summary>
        /// 播放翻牌音效。
        /// </summary>
        private void PlayFlipSound()
        {
            if (audioSource == null)
            {
                return;
            }

            // 确定要播放的音频片段：优先使用flipSoundClip，否则尝试从AudioSource获取
            AudioClip clipToPlay = flipSoundClip;
            if (clipToPlay == null && audioSource.clip != null)
            {
                clipToPlay = audioSource.clip;
            }

            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }

        private Texture GetRandomTexture()
        {
            IReadOnlyList<Texture> textures = uvInspector?.SourceTextures;
            if (textures == null || textures.Count == 0)
            {
                return _targetTexture;
            }

            if (_random == null)
            {
                _random = new System.Random();
            }

            // 随机选择时不包括清空状态（索引0），只从实际贴图中选择
            int attempts = Mathf.Max(1, textures.Count);
            for (int i = 0; i < attempts; i++)
            {
                int index = _random.Next(textures.Count);
                Texture candidate = textures[index];
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return _targetTexture;
        }

        private Texture ResolveTextureByIndex(int textureIndex)
        {
            // 索引0表示清空状态（不显示任何图片）
            if (textureIndex == 0)
            {
                return null;
            }

            if (textureIndex < 0 || uvInspector == null)
            {
                return null;
            }

            IReadOnlyList<Texture> textures = uvInspector.SourceTextures;
            if (textures == null || textures.Count == 0)
            {
                return null;
            }

            // 索引1对应原来的索引0，索引2对应原来的索引1，以此类推
            int actualIndex = textureIndex - 1;
            if (actualIndex < 0 || actualIndex >= textures.Count)
            {
                return null;
            }

            return textures[actualIndex];
        }

        private void ResetCellTransform(CellRuntime cell)
        {
            if (cell.rectTransform != null)
            {
                cell.rectTransform.localEulerAngles = Vector3.zero;
            }
        }

        private void AbortInternal()
        {
            if (!_transitionActive)
            {
                return;
            }

            for (int i = 0; i < _cells.Count; i++)
            {
                ResetCellTransform(_cells[i]);
            }

            _transitionActive = false;
            _soundTimer = 0f;
        }

        private void CompleteTransition()
        {
            _transitionActive = false;
            _elapsed = 0f;
            _soundTimer = 0f;
        }

        /// <summary>
        /// 【推荐】启动翻牌至清空状态（索引0），并立即清除EventSystem的选择。
        /// 专用于解决关闭菜单按钮导致的“幽灵触发”问题。
        /// </summary>
        public void StartFlipToClearAndDeselect()
        {
            // 1. 像以前一样，调用清空（索引0）的翻牌
            // 这会触发翻牌器开始翻转到空图像
            StartFlipTransition(0); 
            
            // 2. (关键修复) 立刻取消 EventSystem 的当前选择
            // 确保没有按钮保持“选中”状态
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            else
            {
                // 增加一个安全检查，以防 EventSystem 丢失
                UnityEngine.Debug.LogWarning("UISolariBoard: EventSystem.current 为空，无法取消选择。", this);
            }
        }
    }
}

