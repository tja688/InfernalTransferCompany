/// Credit - Custom implementation based on CurlyUI by Titinious
/// Enhanced editor for omnidirectional control

using UnityEditor;

namespace UnityEngine.UI.Extensions
{
    [CustomEditor(typeof(CUIGraphicOmniDirectional), true)]
    public class CUIGraphicOmniDirectionalEditor : Editor
    {
        protected static bool isCurveGpFold = false;
        protected static bool[] curveFoldouts = new bool[4] { false, false, false, false };

        protected Vector3[] reuse_Vector3s = new Vector3[4];

        public override void OnInspectorGUI()
        {
            CUIGraphicOmniDirectional script = (CUIGraphicOmniDirectional)this.target;

            // 帮助信息
            EditorGUILayout.HelpBox("全向形变UI (Omni-Directional CUI) 支持对四条边进行独立的曲线控制，实现更灵活的UI形变效果。", MessageType.Info);

            if (script.UIGraphic == null)
            {
                EditorGUILayout.HelpBox("必须设置 UI Graphic 组件（例如 Image、Text、RawImage）。", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("调整四条边的贝塞尔曲线控制点来变形UI。提高分辨率可以改善曲线质量。", MessageType.Info);
            }

            DrawDefaultInspector();

            EditorGUILayout.Space();

            // 曲线位置比例编辑器
            isCurveGpFold = EditorGUILayout.Foldout(isCurveGpFold, "曲线控制点位置比例");
            if (isCurveGpFold)
            {
                EditorGUI.indentLevel++;

                string[] curveLabels = { "底部曲线 (Bottom)", "左侧曲线 (Left)", "顶部曲线 (Top)", "右侧曲线 (Right)" };
                Color[] curveColors = { 
                    new Color(1f, 0.3f, 0.3f), // 红色 - 底部
                    new Color(0.3f, 1f, 0.3f), // 绿色 - 左侧
                    new Color(0.3f, 0.3f, 1f), // 蓝色 - 顶部
                    new Color(1f, 1f, 0.3f)    // 黄色 - 右侧
                };

                for (int c = 0; c < 4; c++)
                {
                    GUI.color = curveColors[c];
                    curveFoldouts[c] = EditorGUILayout.Foldout(curveFoldouts[c], curveLabels[c]);
                    GUI.color = Color.white;

                    if (curveFoldouts[c])
                    {
                        EditorGUI.indentLevel++;
                        Vector3[] controlPoints = script.RefCurvesControlRatioPoints[c].array;

                        EditorGUI.BeginChangeCheck();
                        for (int p = 0; p < controlPoints.Length; p++)
                        {
                            reuse_Vector3s[p] = EditorGUILayout.Vector3Field(string.Format("控制点 {0}", p + 1), controlPoints[p]);
                        }

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(script, "Change Ratio Points");
                            EditorUtility.SetDirty(script);

                            System.Array.Copy(reuse_Vector3s, script.RefCurvesControlRatioPoints[c].array, controlPoints.Length);
                            script.UpdateCurveControlPointPositions();
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // 操作按钮
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            if (GUILayout.Button("重置曲线到矩形边界"))
            {
                script.ResetToRect();
                EditorUtility.SetDirty(script);
            }

            EditorGUILayout.Space();

            // 参考CUI组件功能
            EditorGUI.BeginDisabledGroup(script.RefCUIGraphic == null);

            if (GUILayout.Button("从参考组件复制曲线"))
            {
                Undo.RecordObject(script, "Copy from Reference");
                for (int c = 0; c < 4; c++)
                {
                    Undo.RecordObject(script.RefCurves[c], "Copy from Reference");
                }
                EditorUtility.SetDirty(script);

                script.CopyCurvesFrom(script.RefCUIGraphic);
            }

            EditorGUILayout.HelpBox("设置 Ref CUI Graphic 后，可以从另一个组件复制曲线设置。", MessageType.Info);

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            // 预设效果
            EditorGUILayout.LabelField("快速预设效果", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("横向挤压"))
            {
                ApplyHorizontalSquash(script);
            }
            if (GUILayout.Button("纵向挤压"))
            {
                ApplyVerticalSquash(script);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("膨胀效果"))
            {
                ApplyBulge(script);
            }
            if (GUILayout.Button("波浪效果"))
            {
                ApplyWave(script);
            }
            EditorGUILayout.EndHorizontal();
        }

        protected virtual void OnSceneGUI()
        {
            CUIGraphicOmniDirectional script = (CUIGraphicOmniDirectional)this.target;

            script.ReportSet();

            Color[] curveColors = { 
                new Color(1f, 0.3f, 0.3f), // 红色 - 底部
                new Color(0.3f, 1f, 0.3f), // 绿色 - 左侧
                new Color(0.3f, 0.3f, 1f), // 蓝色 - 顶部
                new Color(1f, 1f, 0.3f)    // 黄色 - 右侧
            };

            string[] curveLabels = { "Bottom", "Left", "Top", "Right" };

            for (int c = 0; c < script.RefCurves.Length; c++)
            {
                CUIBezierCurve curve = script.RefCurves[c];

                if (curve.ControlPoints == null)
                    continue;

                Vector3[] controlPoints = curve.ControlPoints;
                Transform handleTransform = curve.transform;
                Quaternion handleRotation = curve.transform.rotation;

                // 绘制控制点
                Handles.color = curveColors[c];
                
                for (int p = 0; p < CUIBezierCurve.CubicBezierCurvePtNum; p++)
                {
                    EditorGUI.BeginChangeCheck();
                    
                    Vector3 worldPoint = handleTransform.TransformPoint(controlPoints[p]);
                    
                    // 优化的句柄大小 - 基于屏幕距离
                    float handleSizeValue = HandleUtility.GetHandleSize(worldPoint) * 0.1f * script.HandleSize;
                    
                    // 绘制标签
                    GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                    labelStyle.normal.textColor = curveColors[c];
                    labelStyle.fontStyle = FontStyle.Bold;
                    Handles.Label(worldPoint + Vector3.up * handleSizeValue * 2, 
                        string.Format("{0}-P{1}", curveLabels[c], p + 1), labelStyle);
                    
                    // 绘制位置句柄
                    Vector3 newPt = Handles.FreeMoveHandle(
                        worldPoint,
                        handleRotation,
                        handleSizeValue,
                        Vector3.zero,
                        Handles.DotHandleCap
                    );

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(curve, "Move Point");
                        Undo.RecordObject(script, "Move Point");
                        EditorUtility.SetDirty(curve);
                        controlPoints[p] = handleTransform.InverseTransformPoint(newPt);
                    }
                }

                // 绘制控制线
                Handles.color = new Color(curveColors[c].r, curveColors[c].g, curveColors[c].b, 0.5f);
                Handles.DrawLine(handleTransform.TransformPoint(controlPoints[0]), handleTransform.TransformPoint(controlPoints[1]));
                Handles.DrawLine(handleTransform.TransformPoint(controlPoints[1]), handleTransform.TransformPoint(controlPoints[2]));
                Handles.DrawLine(handleTransform.TransformPoint(controlPoints[2]), handleTransform.TransformPoint(controlPoints[3]));

                // 绘制贝塞尔曲线
                int sampleSize = 20;
                Handles.color = curveColors[c];
                for (int s = 0; s < sampleSize; s++)
                {
                    Vector3 p1 = handleTransform.TransformPoint(curve.GetPoint((float)s / sampleSize));
                    Vector3 p2 = handleTransform.TransformPoint(curve.GetPoint((float)(s + 1) / sampleSize));
                    Handles.DrawLine(p1, p2);
                }

                curve.EDITOR_ControlPoints = controlPoints;
            }

            // 绘制四个角的连接线
            if (script.RefCurves != null && script.RefCurves.Length == 4)
            {
                Handles.color = new Color(1f, 1f, 1f, 0.3f);
                
                // 左下角到右下角 (bottom[0] to bottom[3])
                Handles.DrawLine(
                    script.RefCurves[0].transform.TransformPoint(script.RefCurves[0].ControlPoints[0]),
                    script.RefCurves[0].transform.TransformPoint(script.RefCurves[0].ControlPoints[3])
                );
                
                // 左下角到左上角 (left[0] to left[3])
                Handles.DrawLine(
                    script.RefCurves[1].transform.TransformPoint(script.RefCurves[1].ControlPoints[0]),
                    script.RefCurves[1].transform.TransformPoint(script.RefCurves[1].ControlPoints[3])
                );
                
                // 左上角到右上角 (top[0] to top[3])
                Handles.DrawLine(
                    script.RefCurves[2].transform.TransformPoint(script.RefCurves[2].ControlPoints[0]),
                    script.RefCurves[2].transform.TransformPoint(script.RefCurves[2].ControlPoints[3])
                );
                
                // 右下角到右上角 (right[0] to right[3])
                Handles.DrawLine(
                    script.RefCurves[3].transform.TransformPoint(script.RefCurves[3].ControlPoints[0]),
                    script.RefCurves[3].transform.TransformPoint(script.RefCurves[3].ControlPoints[3])
                );
            }

            script.Refresh();
        }

        #region Preset Effects

        private void ApplyHorizontalSquash(CUIGraphicOmniDirectional script)
        {
            Undo.RecordObject(script, "Apply Horizontal Squash");
            for (int c = 0; c < 4; c++)
            {
                Undo.RecordObject(script.RefCurves[c], "Apply Horizontal Squash");
            }

            script.ResetToRect();

            // 调整左右两边向内弯曲
            float squashAmount = 0.2f;
            
            // 左边曲线中间点向右
            Vector3 leftP1 = script.GetControlPoint(1, 1);
            leftP1.x += script.RectTrans.rect.width * squashAmount;
            script.SetControlPoint(1, 1, leftP1);
            
            Vector3 leftP2 = script.GetControlPoint(1, 2);
            leftP2.x += script.RectTrans.rect.width * squashAmount;
            script.SetControlPoint(1, 2, leftP2);
            
            // 右边曲线中间点向左
            Vector3 rightP1 = script.GetControlPoint(3, 1);
            rightP1.x -= script.RectTrans.rect.width * squashAmount;
            script.SetControlPoint(3, 1, rightP1);
            
            Vector3 rightP2 = script.GetControlPoint(3, 2);
            rightP2.x -= script.RectTrans.rect.width * squashAmount;
            script.SetControlPoint(3, 2, rightP2);

            EditorUtility.SetDirty(script);
        }

        private void ApplyVerticalSquash(CUIGraphicOmniDirectional script)
        {
            Undo.RecordObject(script, "Apply Vertical Squash");
            for (int c = 0; c < 4; c++)
            {
                Undo.RecordObject(script.RefCurves[c], "Apply Vertical Squash");
            }

            script.ResetToRect();

            // 调整上下两边向内弯曲
            float squashAmount = 0.2f;
            
            // 底部曲线中间点向上
            Vector3 bottomP1 = script.GetControlPoint(0, 1);
            bottomP1.y += script.RectTrans.rect.height * squashAmount;
            script.SetControlPoint(0, 1, bottomP1);
            
            Vector3 bottomP2 = script.GetControlPoint(0, 2);
            bottomP2.y += script.RectTrans.rect.height * squashAmount;
            script.SetControlPoint(0, 2, bottomP2);
            
            // 顶部曲线中间点向下
            Vector3 topP1 = script.GetControlPoint(2, 1);
            topP1.y -= script.RectTrans.rect.height * squashAmount;
            script.SetControlPoint(2, 1, topP1);
            
            Vector3 topP2 = script.GetControlPoint(2, 2);
            topP2.y -= script.RectTrans.rect.height * squashAmount;
            script.SetControlPoint(2, 2, topP2);

            EditorUtility.SetDirty(script);
        }

        private void ApplyBulge(CUIGraphicOmniDirectional script)
        {
            Undo.RecordObject(script, "Apply Bulge");
            for (int c = 0; c < 4; c++)
            {
                Undo.RecordObject(script.RefCurves[c], "Apply Bulge");
            }

            script.ResetToRect();

            // 所有边向外膨胀
            float bulgeAmount = 0.15f;
            
            // 底部向下
            Vector3 bottomP1 = script.GetControlPoint(0, 1);
            bottomP1.y -= script.RectTrans.rect.height * bulgeAmount;
            script.SetControlPoint(0, 1, bottomP1);
            
            Vector3 bottomP2 = script.GetControlPoint(0, 2);
            bottomP2.y -= script.RectTrans.rect.height * bulgeAmount;
            script.SetControlPoint(0, 2, bottomP2);
            
            // 顶部向上
            Vector3 topP1 = script.GetControlPoint(2, 1);
            topP1.y += script.RectTrans.rect.height * bulgeAmount;
            script.SetControlPoint(2, 1, topP1);
            
            Vector3 topP2 = script.GetControlPoint(2, 2);
            topP2.y += script.RectTrans.rect.height * bulgeAmount;
            script.SetControlPoint(2, 2, topP2);
            
            // 左边向左
            Vector3 leftP1 = script.GetControlPoint(1, 1);
            leftP1.x -= script.RectTrans.rect.width * bulgeAmount;
            script.SetControlPoint(1, 1, leftP1);
            
            Vector3 leftP2 = script.GetControlPoint(1, 2);
            leftP2.x -= script.RectTrans.rect.width * bulgeAmount;
            script.SetControlPoint(1, 2, leftP2);
            
            // 右边向右
            Vector3 rightP1 = script.GetControlPoint(3, 1);
            rightP1.x += script.RectTrans.rect.width * bulgeAmount;
            script.SetControlPoint(3, 1, rightP1);
            
            Vector3 rightP2 = script.GetControlPoint(3, 2);
            rightP2.x += script.RectTrans.rect.width * bulgeAmount;
            script.SetControlPoint(3, 2, rightP2);

            EditorUtility.SetDirty(script);
        }

        private void ApplyWave(CUIGraphicOmniDirectional script)
        {
            Undo.RecordObject(script, "Apply Wave");
            for (int c = 0; c < 4; c++)
            {
                Undo.RecordObject(script.RefCurves[c], "Apply Wave");
            }

            script.ResetToRect();

            // 创建波浪效果
            float waveAmount = 0.1f;
            
            // 顶部和底部波浪
            Vector3 topP1 = script.GetControlPoint(2, 1);
            topP1.y += script.RectTrans.rect.height * waveAmount;
            script.SetControlPoint(2, 1, topP1);
            
            Vector3 topP2 = script.GetControlPoint(2, 2);
            topP2.y -= script.RectTrans.rect.height * waveAmount;
            script.SetControlPoint(2, 2, topP2);
            
            Vector3 bottomP1 = script.GetControlPoint(0, 1);
            bottomP1.y -= script.RectTrans.rect.height * waveAmount;
            script.SetControlPoint(0, 1, bottomP1);
            
            Vector3 bottomP2 = script.GetControlPoint(0, 2);
            bottomP2.y += script.RectTrans.rect.height * waveAmount;
            script.SetControlPoint(0, 2, bottomP2);

            EditorUtility.SetDirty(script);
        }

        #endregion
    }
}

