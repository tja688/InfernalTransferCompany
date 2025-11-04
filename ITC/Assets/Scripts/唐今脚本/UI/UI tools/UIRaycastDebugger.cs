using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // 必须引入这个命名空间来处理UI事件

/// <summary>
/// UI射线检测调试器
/// 当脚本激活时，会按设定的间隔时间，持续打印当前鼠标指针下的UI对象。
/// 这对于调试被“隐藏”或“透明”UI阻挡点击事件的问题非常有用。
/// </summary>
public class UIRaycastDebugger : MonoBehaviour
{
    [Tooltip("设置检查的时间间隔（秒），例如 0.5f")]
    public float checkInterval = 0.5f;

    // 用来存储当前鼠标事件的数据
    private PointerEventData pointerEventData;
    
    // 用来存储射线检测的结果列表
    private List<RaycastResult> raycastResults;

    // 协程的引用，方便在OnDisable时停止
    private Coroutine runningCoroutine;

    /// <summary>
    /// 当该组件被激活时调用
    /// </summary>
    void OnEnable()
    {
        // 确保场景中有 EventSystem
        if (EventSystem.current == null)
        {
            Debug.LogError("场景中缺少 EventSystem！无法进行UI射线检测。请在Hierarchy中右键 -> UI -> Event System 来添加一个。");
            this.enabled = false; // 禁用此脚本
            return;
        }

        // 初始化
        pointerEventData = new PointerEventData(EventSystem.current);
        raycastResults = new List<RaycastResult>();

        // 启动循环检测的协程
        // 确保不会重复启动
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }
        runningCoroutine = StartCoroutine(CheckRaycastLoop());
        
        Debug.Log("[UIRaycastDebugger] UI射线检测脚本已激活。");
    }

    /// <summary>
    /// 当该组件被禁用时调用
    /// </summary>
    void OnDisable()
    {
        // 停止协程，避免在对象禁用后继续运行
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }
        Debug.Log("[UIRaycastDebugger] UI射线检测脚本已禁用。");
    }

    /// <summary>
    /// 循环检测的协程
    /// </summary>
    private IEnumerator CheckRaycastLoop()
    {
        // 使用 while(true) 循环，因为协程的启停由 OnEnable/OnDisable 控制
        while (true)
        {
            // 执行检测逻辑
            PerformRaycastCheck();
            
            // 等待预设的间隔时间
            yield return new WaitForSeconds(checkInterval);
        }
    }

    /// <summary>
    /// 执行一次射线检测并打印结果
    /// </summary>
    private void PerformRaycastCheck()
    {
        // 1. 更新指针数据的位置为当前鼠标位置
        pointerEventData.position = Input.mousePosition;

        // 2. 清空上一次的检测结果
        raycastResults.Clear();

        // 3. 执行UI射线检测 (这会检测所有层级的UI)
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        // 4. 分析并打印结果
        if (raycastResults.Count > 0)
        {
            // raycastResults[0] 是最顶层（最先被击中）的对象
            GameObject topHitObject = raycastResults[0].gameObject;

            // 构建一个更详细的日志，帮助你分析
            System.Text.StringBuilder logMessage = new System.Text.StringBuilder();
            logMessage.AppendLine($"[UI调试] 鼠标当前击中的最顶层对象是: **{topHitObject.name}**");

            // 如果击中的不止一个（有重叠），把它们都列出来
            if (raycastResults.Count > 1)
            {
                logMessage.AppendLine("--- 完整的阻挡链 (从上到下) ---");
                for (int i = 0; i < raycastResults.Count; i++)
                {
                    logMessage.AppendLine($"  {i}: {raycastResults[i].gameObject.name}");
                }
            }

            // 打印日志。第二个参数(topHitObject)能让你在Console中点击该日志时，
            // 自动在Hierarchy面板中高亮这个对象，非常方便！
            Debug.Log(logMessage.ToString(), topHitObject);
        }
        else
        {
            // 鼠标当前没有悬停在任何UI元素上
            Debug.Log("[UI调试] 鼠标未悬停在任何UI对象上。");
        }
    }
}