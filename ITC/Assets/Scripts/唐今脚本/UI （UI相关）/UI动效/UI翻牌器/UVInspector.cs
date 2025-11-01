using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ITC.UIFX
{
    /// <summary>
    /// 高性能UI网格贴图映射控制器：通过UV区域裁剪实现多贴图在单一网格中的分区采样显示。
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class UVInspector : MonoBehaviour
    {
        [Header("网格主体设置")]
        [Tooltip("包含所有基础单元格的父节点，通常挂有GridLayoutGroup。若为空则默认使用当前对象的RectTransform。")]
        [SerializeField] private RectTransform contentRoot;

        [Tooltip("是否包含非激活的子节点作为网格单元。")]
        [SerializeField] private bool includeInactiveCells;

        [Tooltip("若未手动指定，则自动在自身或ContentRoot上获取GridLayoutGroup。")]
        [SerializeField] private GridLayoutGroup gridLayout;

        [Header("贴图资源池")]
        [Tooltip("可被采样的源贴图列表，支持在运行时动态增删。")]
        [SerializeField] private List<Texture> sourceTextures = new List<Texture>();

        [Tooltip("计算UV时是否将第0行视为贴图顶部。多数UI需求建议保持开启。")]
        [SerializeField] private bool invertVerticalSampling = true;

        [Header("生命周期控制")]
        [Tooltip("开启后，OnEnable阶段将自动刷新子节点缓存并应用贴图。")]
        [SerializeField] private bool autoRefreshOnEnable = true;

        [Tooltip("开启后，在编辑器参数变化时自动刷新。运行时会忽略。")]
        [SerializeField] private bool autoRefreshOnValidate = true;

        [Header("测试/调试入口")]
        [Tooltip("用于整网格预览的贴图序号。")]
        [SerializeField] private int previewTextureIndex;

        [Tooltip("用于随机预览的随机种子。设置后可获得可复现的随机组合。")] 
        [SerializeField] private int randomPreviewSeed = Environment.TickCount;

        [Tooltip("调试时输出警告信息开关。")]
        [SerializeField] private bool logWarnings = true;

        private readonly List<RawImage> _cells = new List<RawImage>(64);
        private int[] _cellAssignments = Array.Empty<int>();
        private Rect _defaultUvRect = new Rect(0f, 0f, 1f, 1f);
        private int _columns = 1;
        private int _rows = 1;
        private System.Random _random;

        public IReadOnlyList<Texture> SourceTextures => sourceTextures;
        public IReadOnlyList<RawImage> Cells => _cells;
        public int ColumnCount => _columns;
        public int RowCount => _rows;

        private void Reset()
        {
            contentRoot = transform as RectTransform;
            gridLayout = contentRoot ? contentRoot.GetComponent<GridLayoutGroup>() : null;
            includeInactiveCells = false;
            autoRefreshOnEnable = true;
            autoRefreshOnValidate = true;
        }

        private void Awake()
        {
            EnsureRandomInstance();
        }

        private void OnEnable()
        {
            if (autoRefreshOnEnable)
            {
                RefreshGrid(true);
            }
        }

        private void OnValidate()
        {
            if (!autoRefreshOnValidate || Application.isPlaying)
            {
                return;
            }

            EnsureRandomInstance();
            RefreshGrid(true);
        }

        /// <summary>
        /// 刷新网格单元缓存，并可选地立即应用当前贴图分配。
        /// </summary>
        /// <param name="applyAssignments">是否立即应用当前的贴图分配状态。</param>
        public void RefreshGrid(bool applyAssignments)
        {
            if (contentRoot == null)
            {
                contentRoot = transform as RectTransform;
            }

            if (gridLayout == null && contentRoot != null)
            {
                gridLayout = contentRoot.GetComponent<GridLayoutGroup>();
            }

            CacheCells();
            ResolveGridMetrics(_cells.Count);
            ResizeAssignmentCache();

            if (applyAssignments)
            {
                ApplyAssignments();
            }
        }

        /// <summary>
        /// 动态设置指定单元的贴图序号。
        /// </summary>
        /// <param name="cellIndex">单元格索引。</param>
        /// <param name="textureIndex">贴图序号（-1表示清空）。</param>
        /// <param name="applyImmediately">是否立刻刷新视觉显示。</param>
        public void SetCellAssignment(int cellIndex, int textureIndex, bool applyImmediately = true)
        {
            if (_cells.Count == 0)
            {
                if (logWarnings)
                {
                    Debug.LogWarning("UV分区映射器：当前没有缓存的单元格，请先刷新网格。", this);
                }
                return;
            }

            if (cellIndex < 0 || cellIndex >= _cells.Count)
            {
                if (logWarnings)
                {
                    Debug.LogWarning($"UV分区映射器：单元格索引 {cellIndex} 超出范围。", this);
                }
                return;
            }

            EnsureAssignmentCacheSize();
            _cellAssignments[cellIndex] = textureIndex;

            if (applyImmediately)
            {
                ApplyAssignmentToCell(cellIndex, textureIndex);
            }
        }

        /// <summary>
        /// 使用外部提供的完整映射表批量更新所有单元。
        /// </summary>
        public void SetAssignments(IReadOnlyList<int> textureIndices, bool applyImmediately = true)
        {
            if (textureIndices == null)
            {
                throw new ArgumentNullException(nameof(textureIndices));
            }

            EnsureAssignmentCacheSize();

            int minCount = Mathf.Min(_cellAssignments.Length, textureIndices.Count);
            for (int i = 0; i < minCount; i++)
            {
                _cellAssignments[i] = textureIndices[i];
            }

            for (int i = minCount; i < _cellAssignments.Length; i++)
            {
                _cellAssignments[i] = -1;
            }

            if (applyImmediately)
            {
                ApplyAssignments();
            }
        }

        /// <summary>
        /// 将全部单元格切换为指定序号的贴图，并按网格位置自动计算UV。
        /// </summary>
        public void PreviewAllWithTexture(int textureIndex)
        {
            EnsureAssignmentCacheSize();
            for (int i = 0; i < _cellAssignments.Length; i++)
            {
                _cellAssignments[i] = textureIndex;
            }
            ApplyAssignments();
        }

        /// <summary>
        /// 随机分配贴图库中的贴图到全部单元，支持复现的随机种子。
        /// </summary>
        public void RandomizePreview(bool reseed = false)
        {
            if (sourceTextures == null || sourceTextures.Count == 0)
            {
                if (logWarnings)
                {
                    Debug.LogWarning("UV分区映射器：资源池为空，无法执行随机预览。", this);
                }
                return;
            }

            if (reseed)
            {
                randomPreviewSeed = Environment.TickCount;
                _random = new System.Random(randomPreviewSeed);
            }
            else
            {
                EnsureRandomInstance();
            }

            EnsureAssignmentCacheSize();
            for (int i = 0; i < _cellAssignments.Length; i++)
            {
                _cellAssignments[i] = _random.Next(0, sourceTextures.Count);
            }
            ApplyAssignments();
        }

        /// <summary>
        /// 清空所有单元格的贴图显示。
        /// </summary>
        public void ClearAll()
        {
            EnsureAssignmentCacheSize();
            for (int i = 0; i < _cellAssignments.Length; i++)
            {
                _cellAssignments[i] = -1;
            }
            ApplyAssignments();
        }

#if UNITY_EDITOR
        [ContextMenu("预览：整网格使用预览序号贴图")]
        private void ContextPreviewUniform()
        {
            PreviewAllWithTexture(previewTextureIndex);
        }

        [ContextMenu("预览：随机分配贴图库（重采样）")]
        private void ContextPreviewRandom()
        {
            RandomizePreview(true);
        }
#endif

        private void CacheCells()
        {
            _cells.Clear();

            if (contentRoot == null)
            {
                return;
            }

            for (int i = 0; i < contentRoot.childCount; i++)
            {
                Transform child = contentRoot.GetChild(i);
                if (!includeInactiveCells && !child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (child.TryGetComponent(out RawImage rawImage))
                {
                    _cells.Add(rawImage);
                }
            }
        }

        private void ResolveGridMetrics(int cellCount)
        {
            if (cellCount <= 0)
            {
                _columns = 0;
                _rows = 0;
                return;
            }

            if (gridLayout != null)
            {
                switch (gridLayout.constraint)
                {
                    case GridLayoutGroup.Constraint.FixedColumnCount:
                        _columns = Mathf.Max(1, gridLayout.constraintCount);
                        _rows = Mathf.Max(1, Mathf.CeilToInt(cellCount / (float)_columns));
                        return;

                    case GridLayoutGroup.Constraint.FixedRowCount:
                        _rows = Mathf.Max(1, gridLayout.constraintCount);
                        _columns = Mathf.Max(1, Mathf.CeilToInt(cellCount / (float)_rows));
                        return;

                    default:
                        EstimateMetricsFromLayout(cellCount);
                        return;
                }
            }

            EstimateMetricsByCount(cellCount);
        }

        private void EstimateMetricsFromLayout(int cellCount)
        {
            if (contentRoot == null)
            {
                EstimateMetricsByCount(cellCount);
                return;
            }

            Vector2 cellSize = gridLayout.cellSize;
            Vector2 spacing = gridLayout.spacing;
            float totalWidth = Mathf.Max(0f, contentRoot.rect.width + spacing.x);
            float stride = cellSize.x + spacing.x;

            if (stride <= 0.0001f)
            {
                EstimateMetricsByCount(cellCount);
                return;
            }

            int estimatedColumns = Mathf.Clamp(Mathf.FloorToInt(totalWidth / stride), 1, Mathf.Max(1, cellCount));
            _columns = Mathf.Max(1, estimatedColumns);
            _rows = Mathf.Max(1, Mathf.CeilToInt(cellCount / (float)_columns));
        }

        private void EstimateMetricsByCount(int cellCount)
        {
            int columns = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(cellCount)));
            _columns = Mathf.Clamp(columns, 1, cellCount);
            _rows = Mathf.Max(1, Mathf.CeilToInt(cellCount / (float)_columns));
        }

        private void ResizeAssignmentCache()
        {
            if (_cellAssignments.Length == _cells.Count)
            {
                return;
            }

            int[] newArray = new int[_cells.Count];
            for (int i = 0; i < newArray.Length; i++)
            {
                newArray[i] = i < _cellAssignments.Length ? _cellAssignments[i] : -1;
            }

            _cellAssignments = newArray;
        }

        private void EnsureAssignmentCacheSize()
        {
            if (_cellAssignments.Length != _cells.Count)
            {
                ResizeAssignmentCache();
            }
        }

        private void ApplyAssignments()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                int textureIndex = i < _cellAssignments.Length ? _cellAssignments[i] : -1;
                ApplyAssignmentToCell(i, textureIndex);
            }
        }

        private void ApplyAssignmentToCell(int cellIndex, int textureIndex)
        {
            if (cellIndex < 0 || cellIndex >= _cells.Count)
            {
                return;
            }

            RawImage target = _cells[cellIndex];

            if (textureIndex >= 0 && textureIndex < sourceTextures.Count)
            {
                Texture texture = sourceTextures[textureIndex];
                target.texture = texture;
                target.enabled = texture != null;
                target.uvRect = texture != null ? CalculateUvRect(cellIndex) : _defaultUvRect;
            }
            else
            {
                target.texture = null;
                target.uvRect = _defaultUvRect;
                target.enabled = false;
            }
        }

        private Rect CalculateUvRect(int cellIndex)
        {
            if (_columns <= 0 || _rows <= 0)
            {
                return _defaultUvRect;
            }

            int column = Mathf.Clamp(cellIndex % _columns, 0, _columns - 1);
            int row = Mathf.Clamp(cellIndex / _columns, 0, _rows - 1);

            float width = 1f / _columns;
            float height = 1f / _rows;
            float x = column * width;
            float y = invertVerticalSampling
                ? 1f - ((row + 1) * height)
                : row * height;

            return new Rect(x, y, width, height);
        }

        private void EnsureRandomInstance()
        {
            if (_random == null)
            {
                _random = new System.Random(randomPreviewSeed);
            }
        }
    }
}

