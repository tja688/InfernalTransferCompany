# CUIGraphicOmniDirectional 快速开始指南

## 5分钟快速上手

### 步骤1：创建UI对象

1. 在Hierarchy中右键 → `UI` → `Image`
2. 调整Image的大小，例如 200x200

### 步骤2：添加组件

1. 选中刚创建的Image对象
2. 在Inspector中点击 `Add Component`
3. 搜索 "Omni" 或导航到 `UI/Effects/Extensions/Curly UI Graphic Omni-Directional`
4. 点击添加

### 步骤3：配置组件

组件添加后，你会看到：

- **UI Graphic** 字段会自动填充为Image组件
- 四个子对象自动创建：
  - BottomRefCurve（底部曲线）
  - LeftRefCurve（左侧曲线）
  - TopRefCurve（顶部曲线）
  - RightRefCurve（右侧曲线）

### 步骤4：开始形变

**方法A：使用快速预设**

在Inspector底部，你会看到几个预设按钮：

1. 点击 **"横向挤压"** - 查看左右挤压效果
2. 点击 **"膨胀效果"** - 查看气球效果
3. 点击 **"重置曲线到矩形边界"** - 恢复原状

**方法B：Scene视图手动调整**

1. 确保选中了UI对象
2. 在Scene视图中，你会看到彩色的控制点：
   - 🔴 红色 - 底部边
   - 🟢 绿色 - 左侧边
   - 🔵 蓝色 - 顶部边
   - 🟡 黄色 - 右侧边

3. 直接拖动控制点来调整形变

**方法C：精确数值控制**

1. 在Inspector中展开 **"曲线控制点位置比例"**
2. 展开任意一条曲线（如"底部曲线"）
3. 直接输入数值来精确控制

## 常见使用场景

### 场景1：按钮按下效果

**需求：** 按钮被按下时产生挤压效果

**实现步骤：**

1. 创建Button
2. 添加 `CUIGraphicOmniDirectional` 和 `CUIGraphicOmniAnimator`
3. 在Button的OnClick事件中：

```csharp
using UnityEngine;
using UnityEngine.UI.Extensions;

public class ButtonController : MonoBehaviour
{
    private CUIGraphicOmniAnimator animator;
    
    void Start()
    {
        animator = GetComponent<CUIGraphicOmniAnimator>();
    }
    
    public void OnButtonClick()
    {
        animator.AnimateVerticalSquash(0.2f);
    }
}
```

### 场景2：加载动画

**需求：** 加载图标产生脉冲效果

**实现步骤：**

1. 创建Image（加载图标）
2. 添加 `CUIGraphicOmniDirectional` 和 `CUIGraphicOmniAnimator`
3. 启动脉冲动画：

```csharp
using UnityEngine;
using UnityEngine.UI.Extensions;

public class LoadingIcon : MonoBehaviour
{
    private CUIGraphicOmniAnimator animator;
    
    void Start()
    {
        animator = GetComponent<CUIGraphicOmniAnimator>();
        animator.StartPulseAnimation(1.5f); // 1.5Hz频率
    }
    
    void OnDisable()
    {
        animator.StopCurrentAnimation();
    }
}
```

### 场景3：小球运动动画

**需求：** 实现动画12原则中的挤压与拉伸

**实现步骤：**

1. 创建Image（小球精灵）
2. 添加 `CUIGraphicOmniDirectional` 和 `CUIGraphicOmniAnimator`
3. 控制运动形变：

```csharp
using UnityEngine;
using UnityEngine.UI.Extensions;
using System.Collections;

public class BallMotion : MonoBehaviour
{
    private CUIGraphicOmniAnimator animator;
    private RectTransform rectTransform;
    
    public float dashSpeed = 500f;
    public float dashDuration = 0.3f;
    
    void Start()
    {
        animator = GetComponent<CUIGraphicOmniAnimator>();
        rectTransform = GetComponent<RectTransform>();
    }
    
    public void Dash()
    {
        StartCoroutine(DashCoroutine());
    }
    
    IEnumerator DashCoroutine()
    {
        // 冲刺阶段：横向拉伸
        animator.AnimateHorizontalStretch(dashDuration);
        
        // 同时移动
        Vector3 startPos = rectTransform.anchoredPosition;
        Vector3 endPos = startPos + Vector3.right * dashSpeed;
        
        float elapsed = 0;
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dashDuration;
            rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        // 刹车阶段：纵向挤压
        animator.AnimateVerticalSquash(0.2f);
        
        yield return new WaitForSeconds(0.2f);
        
        // 恢复
        animator.AnimateToOriginal(0.15f);
    }
}
```

### 场景4：悬停反馈

**需求：** 鼠标悬停时UI微微膨胀

**实现步骤：**

1. 创建Button或Image
2. 添加 `CUIGraphicOmniDirectional` 和 `CUIGraphicOmniAnimator`
3. 添加EventTrigger或实现接口：

```csharp
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.EventSystems;

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CUIGraphicOmniAnimator animator;
    
    void Start()
    {
        animator = GetComponent<CUIGraphicOmniAnimator>();
        animator.horizontalStrength = 0.1f; // 温和的效果
        animator.verticalStrength = 0.1f;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        animator.AnimateBulge(0.2f);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        animator.AnimateToOriginal(0.2f);
    }
}
```

## 参数调优指南

### Resolution（分辨率）

影响形变的平滑度：

| 值 | 效果 | 适用场景 |
|----|------|---------|
| 1-3 | 低质量，可能有锯齿 | 性能敏感的移动游戏 |
| 5-10 | 中等质量 | ⭐ 推荐日常使用 |
| 10-20 | 高质量 | 大图标、特殊效果 |
| 20+ | 极高质量 | 仅用于截图/展示 |

### Handle Size（手柄大小）

控制Scene视图中控制点的显示大小：

| 值 | 效果 |
|----|------|
| 0.5 | 小巧，适合密集控制点 |
| 1.0 | ⭐ 默认大小 |
| 2.0+ | 大手柄，便于选择 |

### 动画参数（CUIGraphicOmniAnimator）

```csharp
public class AnimationTuning : MonoBehaviour
{
    private CUIGraphicOmniAnimator animator;
    
    void Start()
    {
        animator = GetComponent<CUIGraphicOmniAnimator>();
        
        // 调整动画时长
        animator.defaultDuration = 0.3f; // 300ms
        
        // 调整形变强度
        animator.horizontalStrength = 0.2f; // 20%宽度
        animator.verticalStrength = 0.2f;   // 20%高度
        
        // 自定义缓动曲线
        animator.easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }
}
```

## 故障排除

### 问题1：看不到控制点

**可能原因：**
- Scene视图没有选中对象
- 控制点在画面外

**解决方法：**
1. 在Hierarchy中选中UI对象
2. 在Scene视图中按 `F` 键聚焦
3. 调整 `Handle Size` 参数

### 问题2：形变有锯齿

**解决方法：**
1. 提高 `Resolution` 参数（建议8-12）
2. 检查图片分辨率是否足够

### 问题3：性能问题

**解决方法：**
1. 降低 `Resolution`（建议5-8）
2. 禁用 `Is Lock With Ratio`（如果UI大小不变）
3. 减少同时形变的UI数量
4. 使用对象池

### 问题4：形变不生效

**检查清单：**
- ✅ `Is Curved` 已勾选
- ✅ `UI Graphic` 已正确引用
- ✅ 四个RefCurve子对象存在
- ✅ 控制点已经移动（不在原始位置）

### 问题5：动画没有播放

**检查清单：**
- ✅ `CUIGraphicOmniAnimator` 组件已添加
- ✅ `cuiGraphic` 引用已设置（通常自动）
- ✅ 调用了正确的动画方法
- ✅ `defaultDuration` 不为0

## 进阶技巧

### 技巧1：组合多个形变

```csharp
// 先保存原始点
animator.SaveOriginalPoints();

// 应用第一个形变
animator.AnimateHorizontalStretch(0.2f);

yield return new WaitForSeconds(0.2f);

// 保存当前状态作为新的原始点
animator.SaveOriginalPoints();

// 应用第二个形变
animator.AnimateVerticalSquash(0.2f);
```

### 技巧2：自定义形变曲线

```csharp
CUIGraphicOmniDirectional cui = GetComponent<CUIGraphicOmniDirectional>();

// 创建S形曲线
for (int p = 0; p < 4; p++)
{
    float t = p / 3f;
    float offset = Mathf.Sin(t * Mathf.PI) * 0.2f;
    
    Vector3 point = cui.GetControlPoint(2, p); // 顶部曲线
    point.y += cui.RectTrans.rect.height * offset;
    cui.SetControlPoint(2, p, point);
}
```

### 技巧3：同步多个UI

```csharp
public class SyncedDeformation : MonoBehaviour
{
    public CUIGraphicOmniDirectional master;
    public CUIGraphicOmniDirectional[] slaves;
    
    void Update()
    {
        foreach (var slave in slaves)
        {
            slave.CopyCurvesFrom(master);
        }
    }
}
```

### 技巧4：保存和加载预设

```csharp
[System.Serializable]
public class DeformationPreset
{
    public Vector3[] bottomPoints = new Vector3[4];
    public Vector3[] leftPoints = new Vector3[4];
    public Vector3[] topPoints = new Vector3[4];
    public Vector3[] rightPoints = new Vector3[4];
    
    public void SaveFrom(CUIGraphicOmniDirectional cui)
    {
        for (int p = 0; p < 4; p++)
        {
            bottomPoints[p] = cui.GetControlPoint(0, p);
            leftPoints[p] = cui.GetControlPoint(1, p);
            topPoints[p] = cui.GetControlPoint(2, p);
            rightPoints[p] = cui.GetControlPoint(3, p);
        }
    }
    
    public void ApplyTo(CUIGraphicOmniDirectional cui)
    {
        for (int p = 0; p < 4; p++)
        {
            cui.SetControlPoint(0, p, bottomPoints[p]);
            cui.SetControlPoint(1, p, leftPoints[p]);
            cui.SetControlPoint(2, p, topPoints[p]);
            cui.SetControlPoint(3, p, rightPoints[p]);
        }
    }
}
```

## 最佳实践

### ✅ 推荐做法

1. **从预设开始** - 先使用内置预设了解效果
2. **逐步调整** - 小幅度移动控制点，观察效果
3. **保持对称** - 对于常规UI，保持左右或上下对称
4. **适度形变** - 过度形变会影响可读性
5. **性能优先** - 移动平台控制Resolution在8以下

### ❌ 避免做法

1. **过度细分** - Resolution > 20 通常没必要
2. **忽略性能** - 大量UI同时形变会卡顿
3. **极端控制点** - 控制点移动过远会导致扭曲
4. **频繁刷新** - 每帧修改控制点会影响性能

## 学习资源

### 推荐阅读

1. [完整文档](CUIGraphicOmniDirectional_README.md)
2. [技术说明](CUIGraphicOmniDirectional_TechnicalNotes.md)
3. [动画12原则](https://en.wikipedia.org/wiki/Twelve_basic_principles_of_animation)

### 示例项目

查看 `Assets/Examples/CUIGraphicOmniDirectional/` 目录（如果有）获取更多示例场景。

---

**开始创作吧！** 🎨

如有问题，请参考完整文档或技术说明。

