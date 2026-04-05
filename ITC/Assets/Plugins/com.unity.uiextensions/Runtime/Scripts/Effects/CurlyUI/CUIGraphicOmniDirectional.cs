/// Credit - Custom implementation based on CurlyUI by Titinious
/// Enhanced for omnidirectional control with 4 independent bezier curves

using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.UI.Extensions
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Effects/Extensions/Curly UI Graphic Omni-Directional")]
    public class CUIGraphicOmniDirectional : BaseMeshEffect
    {
        #region Constants
        
        public readonly static int bottomCurveIdx = 0;
        public readonly static int leftCurveIdx = 1;
        public readonly static int topCurveIdx = 2;
        public readonly static int rightCurveIdx = 3;
        
        #endregion

        #region Description

        [Tooltip("启用/禁用形变效果。禁用时快速查看原始UI。")]
        [SerializeField]
        protected bool isCurved = true;
        public bool IsCurved
        {
            get { return isCurved; }
            set { isCurved = value; }
        }

        [Tooltip("启用后根据UI布局的动态变化自动调整曲线。")]
        [SerializeField]
        protected bool isLockWithRatio = true;
        public bool IsLockWithRatio
        {
            get { return isLockWithRatio; }
            set { isLockWithRatio = value; }
        }

        [Tooltip("提高分辨率以改善曲线图形的质量。值越高，形变越平滑，但性能开销越大。")]
        [SerializeField]
        [Range(0.01f, 30.0f)]
        protected float resolution = 5.0f;
        public float Resolution
        {
            get { return resolution; }
            set { resolution = Mathf.Max(0.01f, value); }
        }

        [Tooltip("控制点句柄的显示大小（仅在Scene视图中）。")]
        [SerializeField]
        [Range(0.1f, 5.0f)]
        protected float handleSize = 1.0f;
        public float HandleSize
        {
            get { return handleSize; }
            set { handleSize = Mathf.Max(0.1f, value); }
        }

        #endregion

        #region Links

        protected RectTransform rectTrans;
        public RectTransform RectTrans
        {
            get
            {
                if (rectTrans == null)
                    rectTrans = GetComponent<RectTransform>();
                return rectTrans;
            }
        }

        [Tooltip("需要形变的UI Graphic组件（Image、Text、RawImage等）。")]
        [SerializeField]
        protected Graphic uiGraphic;
        public Graphic UIGraphic
        {
            get { return uiGraphic; }
            set { uiGraphic = value; }
        }

        [Tooltip("参考的CUI组件，用于同步曲线设置。")]
        [SerializeField]
        protected CUIGraphicOmniDirectional refCUIGraphic;
        public CUIGraphicOmniDirectional RefCUIGraphic
        {
            get { return refCUIGraphic; }
            set { refCUIGraphic = value; }
        }

        [Tooltip("四条边的贝塞尔曲线：Bottom(下), Left(左), Top(上), Right(右)。")]
        [SerializeField]
        protected CUIBezierCurve[] refCurves;
        public CUIBezierCurve[] RefCurves
        {
            get { return refCurves; }
        }

        [HideInInspector]
        [SerializeField]
        protected Vector3_Array2D[] refCurvesControlRatioPoints;
        public Vector3_Array2D[] RefCurvesControlRatioPoints
        {
            get { return refCurvesControlRatioPoints; }
        }

#if UNITY_EDITOR
        public CUIBezierCurve[] EDITOR_RefCurves
        {
            set { refCurves = value; }
        }

        public Vector3_Array2D[] EDITOR_RefCurvesControlRatioPoints
        {
            set { refCurvesControlRatioPoints = value; }
        }
#endif

        #endregion

        #region Reuse
        
        protected List<UIVertex> reuse_quads = new List<UIVertex>();
        
        #endregion

        #region Helper Methods

        protected UIVertex uiVertexLerp(UIVertex _a, UIVertex _b, float _time)
        {
            UIVertex tmpUIVertex = new UIVertex();

            tmpUIVertex.position = Vector3.Lerp(_a.position, _b.position, _time);
            tmpUIVertex.normal = Vector3.Lerp(_a.normal, _b.normal, _time);
            tmpUIVertex.tangent = Vector3.Lerp(_a.tangent, _b.tangent, _time);
            tmpUIVertex.uv0 = Vector2.Lerp(_a.uv0, _b.uv0, _time);
            tmpUIVertex.uv1 = Vector2.Lerp(_a.uv1, _b.uv1, _time);
            tmpUIVertex.color = Color.Lerp(_a.color, _b.color, _time);

            return tmpUIVertex;
        }

        /// <summary>
        /// 双线性插值 - 改进版，支持四条边独立控制
        /// </summary>
        protected UIVertex uiVertexBerp(UIVertex v_bottomLeft, UIVertex v_topLeft, UIVertex v_topRight, UIVertex v_bottomRight, float _xTime, float _yTime)
        {
            UIVertex topX = uiVertexLerp(v_topLeft, v_topRight, _xTime);
            UIVertex bottomX = uiVertexLerp(v_bottomLeft, v_bottomRight, _xTime);
            return uiVertexLerp(bottomX, topX, _yTime);
        }

        protected void tessellateQuad(List<UIVertex> _quads, int _thisQuadIdx)
        {
            UIVertex v_bottomLeft = _quads[_thisQuadIdx];
            UIVertex v_topLeft = _quads[_thisQuadIdx + 1];
            UIVertex v_topRight = _quads[_thisQuadIdx + 2];
            UIVertex v_bottomRight = _quads[_thisQuadIdx + 3];

            float quadSize = 100.0f / resolution;

            int heightQuadEdgeNum = Mathf.Max(1, Mathf.CeilToInt((v_topLeft.position - v_bottomLeft.position).magnitude / quadSize));
            int widthQuadEdgeNum = Mathf.Max(1, Mathf.CeilToInt((v_topRight.position - v_topLeft.position).magnitude / quadSize));

            for (int x = 0; x < widthQuadEdgeNum; x++)
            {
                for (int y = 0; y < heightQuadEdgeNum; y++)
                {
                    _quads.Add(new UIVertex());
                    _quads.Add(new UIVertex());
                    _quads.Add(new UIVertex());
                    _quads.Add(new UIVertex());

                    float xRatio = (float)x / widthQuadEdgeNum;
                    float yRatio = (float)y / heightQuadEdgeNum;
                    float xPlusOneRatio = (float)(x + 1) / widthQuadEdgeNum;
                    float yPlusOneRatio = (float)(y + 1) / heightQuadEdgeNum;

                    _quads[_quads.Count - 4] = uiVertexBerp(v_bottomLeft, v_topLeft, v_topRight, v_bottomRight, xRatio, yRatio);
                    _quads[_quads.Count - 3] = uiVertexBerp(v_bottomLeft, v_topLeft, v_topRight, v_bottomRight, xRatio, yPlusOneRatio);
                    _quads[_quads.Count - 2] = uiVertexBerp(v_bottomLeft, v_topLeft, v_topRight, v_bottomRight, xPlusOneRatio, yPlusOneRatio);
                    _quads[_quads.Count - 1] = uiVertexBerp(v_bottomLeft, v_topLeft, v_topRight, v_bottomRight, xPlusOneRatio, yRatio);
                }
            }
        }

        protected void tessellateGraphic(List<UIVertex> _verts)
        {
            for (int v = 0; v < _verts.Count; v += 6)
            {
                reuse_quads.Add(_verts[v]); // bottom left
                reuse_quads.Add(_verts[v + 1]); // top left
                reuse_quads.Add(_verts[v + 2]); // top right
                reuse_quads.Add(_verts[v + 4]); // bottom right
            }

            int oriQuadNum = reuse_quads.Count / 4;
            for (int q = 0; q < oriQuadNum; q++)
            {
                tessellateQuad(reuse_quads, q * 4);
            }

            // 移除原始四边形
            reuse_quads.RemoveRange(0, oriQuadNum * 4);

            _verts.Clear();

            // 处理新四边形并转换为三角形
            for (int q = 0; q < reuse_quads.Count; q += 4)
            {
                _verts.Add(reuse_quads[q]);
                _verts.Add(reuse_quads[q + 1]);
                _verts.Add(reuse_quads[q + 2]);
                _verts.Add(reuse_quads[q + 2]);
                _verts.Add(reuse_quads[q + 3]);
                _verts.Add(reuse_quads[q]);
            }

            reuse_quads.Clear();
        }

        #endregion

        #region Events

        protected override void OnRectTransformDimensionsChange()
        {
            if (isLockWithRatio)
            {
                UpdateCurveControlPointPositions();
            }
        }

        public void Refresh()
        {
            Invoke(nameof(RefreshDelayed), 0.01f);
        }

        private void RefreshDelayed()
        {
            ReportSet();

            // 更新比例位置
            for (int c = 0; c < refCurves.Length; c++)
            {
                CUIBezierCurve curve = refCurves[c];

                if (curve.ControlPoints != null)
                {
                    Vector3[] controlPoints = curve.ControlPoints;

                    for (int p = 0; p < CUIBezierCurve.CubicBezierCurvePtNum; p++)
                    {
#if UNITY_EDITOR
                        Undo.RecordObject(this, "Move Point");
#endif
                        Vector3 ratioPoint = controlPoints[p];
                        ratioPoint.x = (ratioPoint.x + rectTrans.rect.width * rectTrans.pivot.x) / rectTrans.rect.width;
                        ratioPoint.y = (ratioPoint.y + rectTrans.rect.height * rectTrans.pivot.y) / rectTrans.rect.height;
                        refCurvesControlRatioPoints[c][p] = ratioPoint;
                    }
                }
            }

            // 刷新UI
            if (uiGraphic != null)
            {
                uiGraphic.enabled = false;
                uiGraphic.enabled = true;
            }
        }

        #endregion

        #region Flash-Phase

        protected override void Awake()
        {
            base.Awake();
            OnRectTransformDimensionsChange();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnRectTransformDimensionsChange();
        }

        #endregion

        #region Configurations

        /// <summary>
        /// 检查、准备并设置所需的一切
        /// </summary>
        public virtual void ReportSet()
        {
            if (rectTrans == null)
                rectTrans = GetComponent<RectTransform>();

            if (refCurves == null)
                refCurves = new CUIBezierCurve[4];

            bool isCurvesReady = true;

            for (int c = 0; c < 4; c++)
            {
                isCurvesReady = isCurvesReady && refCurves[c] != null;
            }

            isCurvesReady = isCurvesReady && refCurves.Length == 4;

            if (!isCurvesReady)
            {
                CUIBezierCurve[] curves = refCurves;

                string[] curveNames = { "BottomRefCurve", "LeftRefCurve", "TopRefCurve", "RightRefCurve" };

                for (int c = 0; c < 4; c++)
                {
                    if (refCurves[c] == null)
                    {
                        GameObject go = new GameObject();
                        go.transform.SetParent(transform);
                        go.transform.localPosition = Vector3.zero;
                        go.transform.localEulerAngles = Vector3.zero;
                        go.name = curveNames[c];

                        curves[c] = go.AddComponent<CUIBezierCurve>();
                    }
                    else
                    {
                        curves[c] = refCurves[c];
                    }
                    curves[c].ReportSet();
                }

                refCurves = curves;
            }

            if (refCurvesControlRatioPoints == null || refCurvesControlRatioPoints.Length != 4)
            {
                refCurvesControlRatioPoints = new Vector3_Array2D[refCurves.Length];

                for (int c = 0; c < refCurves.Length; c++)
                {
                    refCurvesControlRatioPoints[c].array = new Vector3[refCurves[c].ControlPoints.Length];
                }

                InitializeCurvesToRect();
                Refresh();
            }

            for (int c = 0; c < 4; c++)
            {
                refCurves[c].OnRefresh = Refresh;
            }
        }

        /// <summary>
        /// 初始化四条边的控制点到矩形边界
        /// </summary>
        public void InitializeCurvesToRect()
        {
            if (refCurves == null || refCurves.Length != 4)
                return;

            float width = rectTrans.rect.width;
            float height = rectTrans.rect.height;
            float pivotX = rectTrans.pivot.x;
            float pivotY = rectTrans.pivot.y;

            // Bottom curve (从左到右)
            for (int p = 0; p < CUIBezierCurve.CubicBezierCurvePtNum; p++)
            {
                Vector3 point = new Vector3();
                point.x = width * p / (CUIBezierCurve.CubicBezierCurvePtNum - 1) - width * pivotX;
                point.y = -height * pivotY;
                point.z = 0;
                refCurves[bottomCurveIdx].ControlPoints[p] = point;
            }

            // Top curve (从左到右)
            for (int p = 0; p < CUIBezierCurve.CubicBezierCurvePtNum; p++)
            {
                Vector3 point = new Vector3();
                point.x = width * p / (CUIBezierCurve.CubicBezierCurvePtNum - 1) - width * pivotX;
                point.y = height - height * pivotY;
                point.z = 0;
                refCurves[topCurveIdx].ControlPoints[p] = point;
            }

            // Left curve (从下到上)
            for (int p = 0; p < CUIBezierCurve.CubicBezierCurvePtNum; p++)
            {
                Vector3 point = new Vector3();
                point.x = -width * pivotX;
                point.y = height * p / (CUIBezierCurve.CubicBezierCurvePtNum - 1) - height * pivotY;
                point.z = 0;
                refCurves[leftCurveIdx].ControlPoints[p] = point;
            }

            // Right curve (从下到上)
            for (int p = 0; p < CUIBezierCurve.CubicBezierCurvePtNum; p++)
            {
                Vector3 point = new Vector3();
                point.x = width - width * pivotX;
                point.y = height * p / (CUIBezierCurve.CubicBezierCurvePtNum - 1) - height * pivotY;
                point.z = 0;
                refCurves[rightCurveIdx].ControlPoints[p] = point;
            }

            // 更新比例点
            for (int c = 0; c < refCurves.Length; c++)
            {
                for (int p = 0; p < CUIBezierCurve.CubicBezierCurvePtNum; p++)
                {
                    Vector3 ratioPoint = refCurves[c].ControlPoints[p];
                    ratioPoint.x = (ratioPoint.x + width * pivotX) / width;
                    ratioPoint.y = (ratioPoint.y + height * pivotY) / height;
                    refCurvesControlRatioPoints[c][p] = ratioPoint;
                }
            }
        }

        public void UpdateCurveControlPointPositions()
        {
            ReportSet();

            for (int c = 0; c < refCurves.Length; c++)
            {
                CUIBezierCurve curve = refCurves[c];

#if UNITY_EDITOR
                Undo.RecordObject(curve, "Move Rect");
#endif

                for (int p = 0; p < refCurves[c].ControlPoints.Length; p++)
                {
                    Vector3 newPt = refCurvesControlRatioPoints[c][p];
                    newPt.x = newPt.x * rectTrans.rect.width - rectTrans.rect.width * rectTrans.pivot.x;
                    newPt.y = newPt.y * rectTrans.rect.height - rectTrans.rect.height * rectTrans.pivot.y;
                    curve.ControlPoints[p] = newPt;
                }
            }
        }

        #endregion

        #region Mesh Modification

        public override void ModifyMesh(Mesh _mesh)
        {
            if (!IsActive())
                return;

            using (VertexHelper vh = new VertexHelper(_mesh))
            {
                ModifyMesh(vh);
                vh.FillMesh(_mesh);
            }
        }

        public override void ModifyMesh(VertexHelper _vh)
        {
            if (!IsActive())
                return;

            List<UIVertex> vertexList = new List<UIVertex>();
            _vh.GetUIVertexStream(vertexList);

            modifyVertices(vertexList);

            _vh.Clear();
            _vh.AddUIVertexTriangleStream(vertexList);
        }

        protected virtual void modifyVertices(List<UIVertex> _verts)
        {
            if (!IsActive())
                return;

            tessellateGraphic(_verts);

            if (!isCurved)
            {
                return;
            }

            for (int index = 0; index < _verts.Count; index++)
            {
                var uiVertex = _verts[index];

                // 计算顶点的水平和垂直比例位置 (0.0 - 1.0)
                float horRatio = (uiVertex.position.x + rectTrans.rect.width * rectTrans.pivot.x) / rectTrans.rect.width;
                float verRatio = (uiVertex.position.y + rectTrans.rect.height * rectTrans.pivot.y) / rectTrans.rect.height;

                // 使用四边形插值获取变形后的位置
                Vector3 pos = GetQuadInterpolatedPoint(horRatio, verRatio);

                uiVertex.position = pos;
                _verts[index] = uiVertex;
            }
        }

        #endregion

        #region Services - Public API

        /// <summary>
        /// 获取四边形插值空间中的点
        /// 这是核心算法：基于四条边的贝塞尔曲线进行双线性插值
        /// </summary>
        /// <param name="_xRatio">水平比例 (0-1)</param>
        /// <param name="_yRatio">垂直比例 (0-1)</param>
        /// <returns>变形后的位置</returns>
        public Vector3 GetQuadInterpolatedPoint(float _xRatio, float _yRatio)
        {
            if (refCurves == null || refCurves.Length != 4)
                return Vector3.zero;

            // 从四条边获取边界点
            Vector3 bottomPoint = refCurves[bottomCurveIdx].GetPoint(_xRatio);
            Vector3 topPoint = refCurves[topCurveIdx].GetPoint(_xRatio);
            Vector3 leftPoint = refCurves[leftCurveIdx].GetPoint(_yRatio);
            Vector3 rightPoint = refCurves[rightCurveIdx].GetPoint(_yRatio);

            // 双线性插值
            // 水平方向插值
            Vector3 horizontalInterp = Vector3.Lerp(bottomPoint, topPoint, _yRatio);
            // 垂直方向插值
            Vector3 verticalInterp = Vector3.Lerp(leftPoint, rightPoint, _xRatio);

            // 混合两个方向的插值
            // 使用加权平均来平滑结合
            Vector3 result = (horizontalInterp + verticalInterp) * 0.5f;

            return result;
        }

        /// <summary>
        /// 设置指定边的控制点
        /// </summary>
        /// <param name="curveIndex">曲线索引 (0=底部, 1=左侧, 2=顶部, 3=右侧)</param>
        /// <param name="pointIndex">控制点索引 (0-3)</param>
        /// <param name="position">新位置（局部空间）</param>
        public void SetControlPoint(int curveIndex, int pointIndex, Vector3 position)
        {
            if (refCurves == null || curveIndex < 0 || curveIndex >= 4)
                return;

            if (pointIndex < 0 || pointIndex >= CUIBezierCurve.CubicBezierCurvePtNum)
                return;

            refCurves[curveIndex].ControlPoints[pointIndex] = position;

            // 更新比例点
            Vector3 ratioPoint = position;
            ratioPoint.x = (ratioPoint.x + rectTrans.rect.width * rectTrans.pivot.x) / rectTrans.rect.width;
            ratioPoint.y = (ratioPoint.y + rectTrans.rect.height * rectTrans.pivot.y) / rectTrans.rect.height;
            refCurvesControlRatioPoints[curveIndex][pointIndex] = ratioPoint;

            Refresh();
        }

        /// <summary>
        /// 获取指定边的控制点
        /// </summary>
        /// <param name="curveIndex">曲线索引 (0=底部, 1=左侧, 2=顶部, 3=右侧)</param>
        /// <param name="pointIndex">控制点索引 (0-3)</param>
        /// <returns>控制点位置（局部空间）</returns>
        public Vector3 GetControlPoint(int curveIndex, int pointIndex)
        {
            if (refCurves == null || curveIndex < 0 || curveIndex >= 4)
                return Vector3.zero;

            if (pointIndex < 0 || pointIndex >= CUIBezierCurve.CubicBezierCurvePtNum)
                return Vector3.zero;

            return refCurves[curveIndex].ControlPoints[pointIndex];
        }

        /// <summary>
        /// 重置所有曲线到矩形边界
        /// </summary>
        public void ResetToRect()
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Reset to Rect");
            for (int c = 0; c < 4; c++)
            {
                Undo.RecordObject(refCurves[c], "Reset to Rect");
            }
#endif
            InitializeCurvesToRect();
            Refresh();
        }

        /// <summary>
        /// 从另一个CUI组件复制曲线设置
        /// </summary>
        public void CopyCurvesFrom(CUIGraphicOmniDirectional source)
        {
            if (source == null || source.RefCurves == null || source.RefCurves.Length != 4)
                return;

#if UNITY_EDITOR
            Undo.RecordObject(this, "Copy Curves");
            for (int c = 0; c < 4; c++)
            {
                Undo.RecordObject(refCurves[c], "Copy Curves");
            }
#endif

            for (int c = 0; c < 4; c++)
            {
                for (int p = 0; p < CUIBezierCurve.CubicBezierCurvePtNum; p++)
                {
                    refCurvesControlRatioPoints[c][p] = source.RefCurvesControlRatioPoints[c][p];
                }
            }

            UpdateCurveControlPointPositions();
            Refresh();
        }

        #endregion
    }
}

