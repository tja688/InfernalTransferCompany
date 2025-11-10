using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic; // 引入列表和哈希集

/// <summary>
/// (检测方)
/// 挂载在发起检测的 UI 元素上（例如拖拽的物体）。
/// 它会检测与一个或多个 'UICollidable' 目标的碰撞。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIDetector : MonoBehaviour
{
    [Header("检测设置")]
    [Tooltip("Canvas 使用的渲染相机 (Screen Space - Camera 模式下必需)")]
    public Camera canvasCamera;

    [Tooltip("此检测器需要检测的目标列表")]
    public List<UICollidable> targetsToDetect = new List<UICollidable>();

    [Header("自动查找目标 (可选项)")]
    [Tooltip("如果设置了此项，脚本会在启动时自动查找其所有子物体中的 UICollidable 组件")]
    public Transform targetsParent;
    
    [Header("碰撞事件")]
    [Tooltip("当与某个目标开始碰撞时触发")]
    public UICollidableEvent OnTargetEnter;

    [Tooltip("当与某个目标保持碰撞时触发")]
    public UICollidableEvent OnTargetStay;

    [Tooltip("当与某个目标结束碰撞时触发")]
    public UICollidableEvent OnTargetExit;

    // 内部状态管理
    private RectTransform _detectorRect; // 检测方自己的 RectTransform
    private Vector3[] _corners = new Vector3[4]; // 缓存数组，避免每帧分配

    // 使用 HashSet 快速跟踪当前正在碰撞的目标
    private HashSet<UICollidable> _currentlyCollidingTargets = new HashSet<UICollidable>();

    void Awake()
    {
        _detectorRect = GetComponent<RectTransform>();

        if (canvasCamera == null)
        {
            Debug.LogError("UIDetector: 'Canvas Camera' 未设置！", this);
        }

        // 如果设置了父物体，自动填充目标列表
        if (targetsParent != null)
        {
            targetsToDetect.Clear(); // 清空在 Inspector 中手动添加的
            targetsToDetect.AddRange(targetsParent.GetComponentsInChildren<UICollidable>());
            Debug.Log($"在 {targetsParent.name} 下自动找到了 {targetsToDetect.Count} 个目标。");
        }
    }

    /// <summary>
    /// **核心：手动调用此方法来执行一次碰撞检测**
    /// </summary>
    public void CheckCollisions()
    {
        if (canvasCamera == null || _detectorRect == null || targetsToDetect.Count == 0)
        {
            return; // 必要条件不足
        }

        // 1. 获取检测方在屏幕上的 Rect
        Rect detectorScreenRect = GetScreenRect(_detectorRect, canvasCamera);

        // 2. 创建一个新的集合来跟踪 *这一帧* 碰撞的所
        HashSet<UICollidable> thisFrameCollisions = new HashSet<UICollidable>();

        // 3. 遍历所有目标
        foreach (UICollidable target in targetsToDetect)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                continue; // 跳过空目标或被禁用的目标
            }

            // 4. 获取目标在屏幕上的 Rect
            Rect targetScreenRect = GetScreenRect(target.rectTransform, canvasCamera);

            // 5. 检查重叠
            if (detectorScreenRect.Overlaps(targetScreenRect))
            {
                // 发生了重叠
                thisFrameCollisions.Add(target);

                if (_currentlyCollidingTargets.Contains(target))
                {
                    // 之前就在碰撞 -> 触发 Stay
                    OnTargetStay.Invoke(target);
                }
                else
                {
                    // 这一帧刚开始碰撞 -> 触发 Enter
                    Debug.Log($"[Enter] {name} 开始碰撞 {target.name}");
                    OnTargetEnter.Invoke(target);
                }
            }
        }

        // 6. 检查上一帧在碰撞，但这一帧已停止碰撞的目标 (Exit)
        // 遍历上一帧的碰撞列表
        foreach (UICollidable oldTarget in _currentlyCollidingTargets)
        {
            if (!thisFrameCollisions.Contains(oldTarget))
            {
                // 之前在碰撞，现在不在了 -> 触发 Exit
                Debug.Log($"[Exit] {name} 停止碰撞 {oldTarget.name}");
                OnTargetExit.Invoke(oldTarget);
            }
        }

        // 7. 更新状态：将这一帧的碰撞列表作为新的 "上一帧" 列表
        _currentlyCollidingTargets = thisFrameCollisions;
    }

    /// <summary>
    /// (辅助函数) 获取 RectTransform 在屏幕空间中的 Rect 边界
    /// </summary>
    private Rect GetScreenRect(RectTransform rt, Camera camera)
    {
        rt.GetWorldCorners(_corners); // 使用缓存的数组

        Vector2 screenPoint0 = camera.WorldToScreenPoint(_corners[0]);
        Vector2 screenPoint1 = camera.WorldToScreenPoint(_corners[1]);
        Vector2 screenPoint2 = camera.WorldToScreenPoint(_corners[2]);
        Vector2 screenPoint3 = camera.WorldToScreenPoint(_corners[3]);

        float minX = Mathf.Min(screenPoint0.x, screenPoint1.x, screenPoint2.x, screenPoint3.x);
        float maxX = Mathf.Max(screenPoint0.x, screenPoint1.x, screenPoint2.x, screenPoint3.x);
        float minY = Mathf.Min(screenPoint0.y, screenPoint1.y, screenPoint2.y, screenPoint3.y);
        float maxY = Mathf.Max(screenPoint0.y, screenPoint1.y, screenPoint2.y, screenPoint3.y);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}