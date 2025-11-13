# CUIGraphicOmniDirectional 技术说明文档

## 关键技术改进

### 1. 控制点显示优化（解决Camera模式问题）

#### 问题分析

原版CUIGraphic在使用Camera渲染模式的Canvas时，存在以下问题：

1. **控制点距离过远** - 控制点手柄在Scene视图中离UI元素过远，需要大幅缩放才能看到
2. **手柄大小固定** - 使用固定大小的手柄，在不同缩放级别下体验不一致
3. **难以选择** - 当UI较小时，手柄可能完全看不见

#### 原因

原版代码使用了简单的 `Handles.DoPositionHandle()` 方法，没有考虑到：
- Canvas的不同渲染模式（Overlay vs Camera vs World Space）
- Camera的距离和缩放
- Scene视图的缩放级别

#### 解决方案

在 `CUIGraphicOmniDirectionalEditor.cs` 的 `OnSceneGUI()` 方法中，我们实现了自适应的手柄大小计算：

```csharp
// 优化的句柄大小 - 基于屏幕距离
float handleSizeValue = HandleUtility.GetHandleSize(worldPoint) * 0.1f * script.HandleSize;

// 使用FreeMoveHandle替代DoPositionHandle
Vector3 newPt = Handles.FreeMoveHandle(
    worldPoint,
    handleRotation,
    handleSizeValue,
    Vector3.zero,
    Handles.DotHandleCap
);
```

**关键点：**

1. **HandleUtility.GetHandleSize()** - Unity内置方法，返回在给定世界位置处，在屏幕上显示1单位大小所需的世界空间大小
2. **动态缩放系数** - 0.1f 是基础缩放，`script.HandleSize` 允许用户在Inspector中调整
3. **FreeMoveHandle** - 比 DoPositionHandle 更灵活，可以自定义大小和形状

#### 效果

- ✅ 在Overlay模式下，手柄大小合适
- ✅ 在Camera模式下，手柄自动适应相机距离
- ✅ 在World Space模式下，手柄跟随世界空间缩放
- ✅ 用户可通过 `HandleSize` 参数微调

### 2. 四边独立控制算法

#### 原版限制

原版CUIGraphic使用两条贝塞尔曲线（上下或左右），形变计算公式：

```csharp
Vector3 pos = refCurves[0].GetPoint(horRatio) * (1 - verRatio) + 
              refCurves[1].GetPoint(horRatio) * verRatio;
```

这种方法只能在一个方向上使用贝塞尔曲线，另一个方向是线性插值。

#### 新算法

我们使用**四边独立贝塞尔曲线**，通过改进的双线性插值：

```csharp
public Vector3 GetQuadInterpolatedPoint(float _xRatio, float _yRatio)
{
    // 从四条边获取边界点
    Vector3 bottomPoint = refCurves[bottomCurveIdx].GetPoint(_xRatio);
    Vector3 topPoint = refCurves[topCurveIdx].GetPoint(_xRatio);
    Vector3 leftPoint = refCurves[leftCurveIdx].GetPoint(_yRatio);
    Vector3 rightPoint = refCurves[rightCurveIdx].GetPoint(_yRatio);

    // 双向插值
    Vector3 horizontalInterp = Vector3.Lerp(bottomPoint, topPoint, _yRatio);
    Vector3 verticalInterp = Vector3.Lerp(leftPoint, rightPoint, _xRatio);

    // 混合结果
    Vector3 result = (horizontalInterp + verticalInterp) * 0.5f;

    return result;
}
```

**算法优势：**

1. **真正的四边独立控制** - 每条边都可以有自己的曲线形状
2. **平滑过渡** - 通过加权平均混合水平和垂直插值
3. **角点精确对齐** - 四条曲线的端点共享，保证角点位置一致

#### 数学原理

对于矩形内的任意点 P(x, y)，其变形后的位置计算如下：

1. 计算比例坐标：
   - `xRatio = (x - rect.xMin) / rect.width`
   - `yRatio = (y - rect.yMin) / rect.height`

2. 在四条边上采样：
   - `P_bottom = BottomCurve(xRatio)` - 底边在x位置的点
   - `P_top = TopCurve(xRatio)` - 顶边在x位置的点
   - `P_left = LeftCurve(yRatio)` - 左边在y位置的点
   - `P_right = RightCurve(yRatio)` - 右边在y位置的点

3. 双向插值：
   - `P_h = Lerp(P_bottom, P_top, yRatio)` - 水平方向插值
   - `P_v = Lerp(P_left, P_right, xRatio)` - 垂直方向插值

4. 混合结果：
   - `P_final = (P_h + P_v) / 2`

这种方法确保了边界点精确落在对应的贝塞尔曲线上，内部点平滑过渡。

### 3. 控制点数据结构

#### 12个控制点布局

```
角点共享模式：

Left[3] = Top[0] -------- Top[1] -------- Top[2] -------- Top[3] = Right[3]
    |                                                              |
    |                                                              |
Left[2]                                                        Right[2]
    |                                                              |
    |                   UI Content Area                            |
Left[1]                                                        Right[1]
    |                                                              |
    |                                                              |
Bottom[0] = Left[0] --- Bottom[1] ---- Bottom[2] ---- Bottom[3] = Right[0]
```

**实际控制点数量：**
- 4条曲线 × 4个控制点 = 16个点
- 4个角点共享 = -4个点
- **总计：12个独立控制点**

#### 数据存储

```csharp
// 每条曲线独立存储4个控制点
protected CUIBezierCurve[] refCurves; // 长度4

// 比例坐标存储（用于动态调整大小）
protected Vector3_Array2D[] refCurvesControlRatioPoints; // 长度4
```

**为什么使用比例坐标？**

当RectTransform的大小改变时，我们希望形变保持相对形状。比例坐标的计算：

```csharp
ratioPoint.x = (localPoint.x + width * pivot.x) / width;
ratioPoint.y = (localPoint.y + height * pivot.y) / height;
```

恢复时：

```csharp
localPoint.x = ratioPoint.x * width - width * pivot.x;
localPoint.y = ratioPoint.y * height - height * pivot.y;
```

### 4. 网格细分（Tessellation）

#### 为什么需要细分？

贝塞尔曲线是连续的，但UI网格是离散的。如果不细分，形变会出现锯齿。

#### 细分策略

```csharp
float quadSize = 100.0f / resolution;

int heightQuadEdgeNum = Mathf.Max(1, 
    Mathf.CeilToInt((v_topLeft.position - v_bottomLeft.position).magnitude / quadSize));
int widthQuadEdgeNum = Mathf.Max(1, 
    Mathf.CeilToInt((v_topRight.position - v_topLeft.position).magnitude / quadSize));
```

**自适应细分：**
- 根据原始四边形的大小决定细分数量
- `resolution` 参数控制细分密度
- 小的UI元素自动使用较少的细分，节省性能

#### 性能考虑

| Resolution | 细分级别 | 适用场景 | 性能影响 |
|-----------|---------|---------|---------|
| 1-3 | 低 | 简单形变，小UI | 很小 |
| 5-10 | 中 | 一般形变 | 较小 |
| 10-20 | 高 | 复杂曲线 | 中等 |
| 20+ | 极高 | 艺术化效果 | 较大 |

**优化建议：**
- 移动平台：Resolution ≤ 8
- PC平台：Resolution ≤ 15
- 静态UI：初始化后可禁用 `isLockWithRatio`

### 5. 相对形变模式探讨

#### 用户需求

> "当图片滚动时让图片自己滚自己的，而对这个图片的形变不受到图片自身滚动的影响"

#### 技术分析

**当前实现：**
- 形变直接作用于顶点位置
- UV坐标（纹理坐标）也会跟随顶点一起变形
- 因此图片内容会随着形变而扭曲

**要实现相对形变需要：**

1. **分离形变和内容**
   ```
   方案A：使用两个Canvas
   - 外层Canvas：应用形变（CUIGraphicOmniDirectional）
   - 内层Canvas：图片内容（可以滚动）
   
   方案B：Shader实现
   - 顶点着色器：应用形变
   - 片元着色器：使用未变形的UV采样
   ```

2. **Shader方案示例**（伪代码）
   ```hlsl
   v2f vert(appdata v)
   {
       v2f o;
       
       // 保存原始UV
       o.originalUV = v.texcoord;
       
       // 应用形变到顶点位置
       float2 ratio = v.texcoord; // 使用UV作为比例
       float3 deformedPos = GetDeformedPosition(v.vertex, ratio);
       o.vertex = UnityObjectToClipPos(deformedPos);
       
       // 调整后的UV（考虑滚动）
       o.uv = v.texcoord + _ScrollOffset;
       
       return o;
   }
   
   fixed4 frag(v2f i) : SV_Target
   {
       // 使用未变形的UV采样
       return tex2D(_MainTex, i.uv);
   }
   ```

#### 当前解决方案

**推荐的实践方法：**

```
GameObject (形变容器)
├─ CUIGraphicOmniDirectional
├─ Image (透明或单色)
└─ Child GameObject (内容)
   ├─ Image (实际图片)
   └─ Mask (可选)
```

这样形变只影响外层容器，内部内容可以自由滚动。

**代码示例：**

```csharp
// 外层容器设置
var container = new GameObject("DeformContainer");
var rectTransform = container.AddComponent<RectTransform>();
var image = container.AddComponent<Image>();
image.color = Color.white; // 可以是透明的
var cuiGraphic = container.AddComponent<CUIGraphicOmniDirectional>();
cuiGraphic.UIGraphic = image;

// 内层内容
var content = new GameObject("Content");
content.transform.SetParent(container.transform);
var contentImage = content.AddComponent<Image>();
contentImage.sprite = yourSprite;
// 内容可以滚动，不受形变影响
```

### 6. 编辑器可视化

#### 颜色编码

我们为四条边使用了不同的颜色，便于识别：

```csharp
Color[] curveColors = { 
    new Color(1f, 0.3f, 0.3f), // 红色 - 底部
    new Color(0.3f, 1f, 0.3f), // 绿色 - 左侧
    new Color(0.3f, 0.3f, 1f), // 蓝色 - 顶部
    new Color(1f, 1f, 0.3f)    // 黄色 - 右侧
};
```

#### 可视化元素

1. **控制点手柄** - 圆点，可拖动
2. **控制线** - 半透明线，连接控制点
3. **贝塞尔曲线** - 实线，显示实际曲线形状
4. **边界连接线** - 白色半透明，显示四边形轮廓
5. **标签** - 显示控制点名称（如"Bottom-P1"）

#### Scene视图交互

```csharp
// 自定义标签样式
GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
labelStyle.normal.textColor = curveColors[c];
labelStyle.fontStyle = FontStyle.Bold;
Handles.Label(worldPoint + Vector3.up * handleSizeValue * 2, 
    string.Format("{0}-P{1}", curveLabels[c], p + 1), labelStyle);
```

### 7. 性能优化技术

#### 延迟刷新

```csharp
public void Refresh()
{
    Invoke(nameof(RefreshDelayed), 0.01f);
}
```

避免频繁刷新，批量更新后统一刷新一次。

#### 条件刷新

```csharp
protected override void OnRectTransformDimensionsChange()
{
    if (isLockWithRatio)  // 只在需要时才更新
    {
        UpdateCurveControlPointPositions();
    }
}
```

#### 对象复用

```csharp
protected List<UIVertex> reuse_quads = new List<UIVertex>();
```

重用List避免频繁的内存分配。

### 8. 外部API设计

#### 设计原则

1. **直观易用** - 方法名清晰表达功能
2. **参数验证** - 检查索引范围，防止越界
3. **自动刷新** - 修改控制点后自动触发刷新
4. **支持撤销** - 编辑器中集成Undo系统

#### 关键API

```csharp
// 基础控制
void SetControlPoint(int curveIndex, int pointIndex, Vector3 position)
Vector3 GetControlPoint(int curveIndex, int pointIndex)

// 批量操作
void ResetToRect()
void CopyCurvesFrom(CUIGraphicOmniDirectional source)

// 高级功能
Vector3 GetQuadInterpolatedPoint(float xRatio, float yRatio)
```

#### 使用场景

**动画驱动：**
```csharp
// 外部脚本可以轻松控制形变
cuiGraphic.SetControlPoint(0, 1, newPosition);
```

**状态同步：**
```csharp
// 多个UI元素保持相同形变
foreach (var ui in uiElements)
{
    ui.CopyCurvesFrom(masterUI);
}
```

## 未来改进方向

### 短期计划

1. **GPU加速** - 使用Compute Shader进行网格细分和变形计算
2. **动画曲线集成** - 直接使用AnimationCurve定义形变
3. **预设库** - 内置更多常用形变效果

### 长期规划

1. **时间轴支持** - 集成Unity Timeline
2. **物理模拟** - 基于物理的形变（如弹性、阻尼）
3. **AI辅助** - 自动生成符合动画原则的形变曲线

## 参考资料

- [Unity UI Extensions Project](https://bitbucket.org/UnityUIExtensions/unity-ui-extensions)
- [CurlyUI Original Repository](https://github.com/Titinious/CurlyUI)
- [Bezier Curve Mathematics](https://en.wikipedia.org/wiki/B%C3%A9zier_curve)
- [Animation Principles - Squash and Stretch](https://en.wikipedia.org/wiki/Squash_and_stretch)

---

**文档版本：** 1.0  
**最后更新：** 2025年11月  
**维护者：** Custom Implementation Team

