# CUI Graphic Omni-Directional 全向控制UI形变组件

## 概述

`CUIGraphicOmniDirectional` 是基于 Unity UI Extensions 的 CurlyUI 系统开发的增强版全向控制UI形变组件。与原版只能选择横向或纵向形变不同，本组件支持对**四条边同时进行独立的曲线控制**，实现更加灵活和自然的UI动画效果。

## 主要特性

### ✨ 核心功能

1. **四边独立控制** - 对上、下、左、右四条边分别使用贝塞尔曲线进行控制
2. **12个控制点** - 每条边4个控制点（角点共享），支持精细的曲线调整
3. **实时预览** - Scene视图中可视化编辑，所见即所得
4. **优化的句柄显示** - 自动调整控制点大小，解决了原版在Camera模式下控制点过远的问题
5. **快速预设** - 内置多种常用形变效果预设
6. **外部API** - 完整的公共接口，方便其他脚本调用

### 🎯 适用场景

- **动画12原则** - 实现挤压与拉伸（Squash and Stretch）原则
  - 小球冲刺时的横向拉伸
  - 刹车时的纵向挤压
  - 弹跳物体的形变
  
- **UI交互效果**
  - 按钮按下的形变反馈
  - 悬停时的微妙膨胀
  - 加载动画的波浪效果
  
- **特殊视觉效果**
  - 镜头畸变模拟
  - 透视效果
  - 艺术化的UI设计

## 使用方法

### 基础设置

1. **添加组件**
   ```
   选择UI对象 → Add Component → UI/Effects/Extensions/Curly UI Graphic Omni-Directional
   ```

2. **配置必要参数**
   - `UI Graphic`: 拖入需要形变的UI组件（Image、Text、RawImage等）
   - `Is Curved`: 启用/禁用形变效果
   - `Resolution`: 形变质量（5-10为推荐值）
   - `Handle Size`: Scene视图中控制点的显示大小

3. **调整曲线**
   - 在Scene视图中直接拖动控制点
   - 或在Inspector的"曲线控制点位置比例"面板中精确输入数值

### 控制点说明

组件使用4条贝塞尔曲线，每条曲线有4个控制点：

- **底部曲线 (Bottom)** - 红色 - 从左到右
  - P1: 左下角
  - P2: 左下控制点
  - P3: 右下控制点
  - P4: 右下角

- **左侧曲线 (Left)** - 绿色 - 从下到上
  - P1: 左下角（与底部P1共享）
  - P2: 左下控制点
  - P3: 左上控制点
  - P4: 左上角

- **顶部曲线 (Top)** - 蓝色 - 从左到右
  - P1: 左上角（与左侧P4共享）
  - P2: 左上控制点
  - P3: 右上控制点
  - P4: 右上角

- **右侧曲线 (Right)** - 黄色 - 从下到上
  - P1: 右下角（与底部P4共享）
  - P2: 右下控制点
  - P3: 右上控制点
  - P4: 右上角（与顶部P4共享）

### 快速预设效果

Inspector面板提供了4个常用预设：

1. **横向挤压** - 左右两边向内弯曲，模拟水平方向的挤压
2. **纵向挤压** - 上下两边向内弯曲，模拟垂直方向的挤压
3. **膨胀效果** - 四边向外膨胀，创建气球般的效果
4. **波浪效果** - 上下边缘产生波浪，适合水面或旗帜效果

### Inspector操作按钮

- **重置曲线到矩形边界** - 将所有控制点恢复到初始位置
- **从参考组件复制曲线** - 从另一个CUIGraphicOmniDirectional组件复制曲线设置

## 编程接口 (API)

### 公共属性

```csharp
// 启用/禁用形变
bool IsCurved { get; set; }

// 自动调整
bool IsLockWithRatio { get; set; }

// 形变分辨率
float Resolution { get; set; }

// 句柄大小
float HandleSize { get; set; }

// UI组件引用
Graphic UIGraphic { get; set; }

// 参考组件
CUIGraphicOmniDirectional RefCUIGraphic { get; set; }
```

### 公共方法

#### 设置和获取控制点

```csharp
// 设置指定边的控制点
// curveIndex: 0=底部, 1=左侧, 2=顶部, 3=右侧
// pointIndex: 0-3
// position: 局部空间坐标
void SetControlPoint(int curveIndex, int pointIndex, Vector3 position)

// 获取指定边的控制点
Vector3 GetControlPoint(int curveIndex, int pointIndex)
```

#### 曲线操作

```csharp
// 重置所有曲线到矩形边界
void ResetToRect()

// 从另一个组件复制曲线设置
void CopyCurvesFrom(CUIGraphicOmniDirectional source)

// 获取形变后的位置（用于高级用途）
Vector3 GetQuadInterpolatedPoint(float xRatio, float yRatio)
```

### 使用示例

#### 示例1: 动态形变动画

```csharp
using UnityEngine;
using UnityEngine.UI.Extensions;

public class BallSquashStretch : MonoBehaviour
{
    public CUIGraphicOmniDirectional cuiGraphic;
    public float squashAmount = 0.3f;
    public float stretchAmount = 0.2f;
    
    // 冲刺时横向拉伸
    public void OnDash()
    {
        StartCoroutine(HorizontalStretch());
    }
    
    IEnumerator HorizontalStretch()
    {
        float duration = 0.2f;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 左右边向外，上下边向内
            float horizontalOffset = Mathf.Lerp(0, stretchAmount, t);
            float verticalOffset = Mathf.Lerp(0, -squashAmount, t);
            
            // 调整左边
            Vector3 leftP1 = cuiGraphic.GetControlPoint(1, 1);
            leftP1.x = -horizontalOffset * cuiGraphic.RectTrans.rect.width;
            cuiGraphic.SetControlPoint(1, 1, leftP1);
            
            // 调整右边
            Vector3 rightP1 = cuiGraphic.GetControlPoint(3, 1);
            rightP1.x = cuiGraphic.RectTrans.rect.width + horizontalOffset * cuiGraphic.RectTrans.rect.width;
            cuiGraphic.SetControlPoint(3, 1, rightP1);
            
            // 调整顶部和底部...
            
            yield return null;
        }
    }
    
    // 刹车时纵向挤压
    public void OnBrake()
    {
        StartCoroutine(VerticalSquash());
    }
    
    IEnumerator VerticalSquash()
    {
        // 类似的实现...
        yield return null;
    }
}
```

#### 示例2: 按钮按下效果

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class SquashyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private CUIGraphicOmniDirectional cuiGraphic;
    private Vector3[,] originalPoints;
    
    void Start()
    {
        cuiGraphic = GetComponent<CUIGraphicOmniDirectional>();
        
        // 保存原始控制点
        originalPoints = new Vector3[4, 4];
        for (int c = 0; c < 4; c++)
        {
            for (int p = 0; p < 4; p++)
            {
                originalPoints[c, p] = cuiGraphic.GetControlPoint(c, p);
            }
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        // 按下时挤压
        float squash = 0.15f;
        
        for (int c = 0; c < 4; c++)
        {
            for (int p = 1; p < 3; p++) // 只调整中间控制点
            {
                Vector3 point = originalPoints[c, p];
                
                // 向中心挤压
                if (c == 0) point.y += cuiGraphic.RectTrans.rect.height * squash; // 底部向上
                if (c == 1) point.x += cuiGraphic.RectTrans.rect.width * squash;   // 左边向右
                if (c == 2) point.y -= cuiGraphic.RectTrans.rect.height * squash; // 顶部向下
                if (c == 3) point.x -= cuiGraphic.RectTrans.rect.width * squash;   // 右边向左
                
                cuiGraphic.SetControlPoint(c, p, point);
            }
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        // 释放时恢复
        for (int c = 0; c < 4; c++)
        {
            for (int p = 0; p < 4; p++)
            {
                cuiGraphic.SetControlPoint(c, p, originalPoints[c, p]);
            }
        }
    }
}
```

#### 示例3: 呼吸动画

```csharp
using UnityEngine;
using UnityEngine.UI.Extensions;

public class BreathingEffect : MonoBehaviour
{
    public CUIGraphicOmniDirectional cuiGraphic;
    public float breathSpeed = 1f;
    public float breathAmount = 0.1f;
    
    private Vector3[,] originalPoints;
    
    void Start()
    {
        // 保存原始点
        originalPoints = new Vector3[4, 4];
        for (int c = 0; c < 4; c++)
        {
            for (int p = 0; p < 4; p++)
            {
                originalPoints[c, p] = cuiGraphic.GetControlPoint(c, p);
            }
        }
    }
    
    void Update()
    {
        float breath = Mathf.Sin(Time.time * breathSpeed) * breathAmount;
        
        for (int c = 0; c < 4; c++)
        {
            for (int p = 1; p < 3; p++) // 中间控制点
            {
                Vector3 point = originalPoints[c, p];
                
                // 向外膨胀
                if (c == 0) point.y -= cuiGraphic.RectTrans.rect.height * breath;
                if (c == 1) point.x -= cuiGraphic.RectTrans.rect.width * breath;
                if (c == 2) point.y += cuiGraphic.RectTrans.rect.height * breath;
                if (c == 3) point.x += cuiGraphic.RectTrans.rect.width * breath;
                
                cuiGraphic.SetControlPoint(c, p, point);
            }
        }
    }
}
```

## 性能优化建议

1. **Resolution设置**
   - 简单形变: 3-5
   - 一般形变: 5-10
   - 复杂形变: 10-20
   - 不建议超过30

2. **运行时修改**
   - 频繁修改控制点时，考虑批量更新后一次性调用Refresh()
   - 静态UI可以在Start后禁用IsLockWithRatio

3. **移动平台**
   - 降低Resolution
   - 减少同时形变的UI数量
   - 考虑使用对象池

## 技术细节

### 形变算法

组件使用**改进的双线性插值算法**：

1. 对于UI上的每个顶点，计算其在原始矩形中的相对位置(xRatio, yRatio)
2. 在四条贝塞尔曲线上采样对应位置的点
3. 使用加权平均混合水平和垂直方向的插值结果
4. 得到最终的变形位置

这种方法比原版的单方向插值更加灵活，能够实现真正的四边独立控制。

### 控制点坐标系

- 所有控制点使用**局部空间坐标**
- 原点在RectTransform的pivot位置
- 使用比例坐标存储，支持动态调整大小

### Scene视图优化

编辑器使用 `HandleUtility.GetHandleSize()` 来计算控制点句柄的屏幕相对大小，确保：
- 在任何缩放级别下都清晰可见
- 不会因为距离太远而难以选择
- 自适应不同的Canvas Render Mode（Overlay / Camera / World Space）

## 常见问题

### Q: 控制点看不见或太小？
A: 调整Inspector中的 `Handle Size` 参数，或在Scene视图中放大画面。

### Q: 形变看起来有锯齿？
A: 提高 `Resolution` 参数的值。

### Q: 运行时修改控制点没有效果？
A: 确保调用了 `SetControlPoint()` 方法，该方法会自动触发刷新。

### Q: 如何实现相对形变（不受图片滚动影响）？
A: 当前版本的形变是基于顶点的，会跟随UV坐标。要实现独立于内容的形变，需要将形变组件放在父对象上，内容放在子对象中。

### Q: 可以用在UGUI的所有组件上吗？
A: 可以，支持所有继承自 `Graphic` 的组件，包括 Image、Text、RawImage 等。

### Q: 性能如何？
A: 主要开销在于网格细分。在移动设备上建议控制 Resolution 在 10 以下，单个UI形变的性能影响很小。

## 版本历史

### v1.0 (当前版本)
- ✅ 四边独立控制
- ✅ 12个控制点系统
- ✅ Scene视图可视化编辑
- ✅ 优化的句柄显示
- ✅ 快速预设效果
- ✅ 完整的编程API

### 未来计划
- 🔲 动画曲线编辑器集成
- 🔲 更多预设效果
- 🔲 形变动画录制系统
- 🔲 性能优化（GPU加速）

## 致谢

本组件基于 [CurlyUI by Titinious](https://github.com/Titinious/CurlyUI) 开发，在原有基础上进行了大幅改进和扩展。

## 许可证

遵循 Unity UI Extensions 的许可证。

---

**享受创作吧！如有问题或建议，欢迎反馈。** 🎨✨

