using Spine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;

using UnityEngine.UI;
using static MoreMountains.Tools.ShaderController;
public enum SuccessType { 
    BigSuccess,
    MediaSuccess,
    SmallSuccess,
    Faild,
    BigFailed,
}

public class RingChangeColor : MonoBehaviour, IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("拖拽匹配设置")]
    public string targetType; // 拖拽物类型匹配标识
    private bool isMatchedDraggingOver = false; // 是否有匹配的拖拽物在上方

    [Header("颜色阶段配置")]
    public Color initialColor = Color.gray; // 初始颜色
    public Color targetGreen = Color.green; // 目标绿色（有效区域基准色）
    public Color endBlue = Color.blue; // 超时结束颜色
    public Color successColor = Color.yellow; // 成功反馈色
    public Color failColor = Color.red; // 失败反馈色

    [Header("时间参数（可通过接口修改）")]
    [Tooltip("从初始色到绿色的时长（秒）")]
    public float phase1Duration = 2f;
    [Tooltip("从绿色到蓝色的时长（秒）")]
    public float phase2Duration = 1f;
    [Tooltip("接近绿色的阈值（0-1，如0.9表示90%接近绿色时有效）")]
    public float greenThreshold = 0.9f;
    public float greenThresholdSuccessSmall = 0.8f;

    private Image ringImage; // 圆环图像
    public delegate void OnJudgeResult(bool isSuccess, SuccessType type);
    public OnJudgeResult onJudgeResult;

    public bool lockOnce = false;
    private bool isAnimating = false; // 动画是否运行中
    private float animationTime = 0f; // 当前动画时长
    private bool isTriggered = false; // 是否已触发结束判断
    void Start()
    {
        // 初始化圆环颜色
        if (ringImage == null)
        {
            ringImage=GetComponent<Image>();
            ringImage.color = initialColor;
            ringImage.material = new Material(Shader.Find("UI/Unlit/Transparent"));
        }
        
    }
  
    public struct debugTec {
        bool f;
        string str;
        public debugTec( string strValue)
        {
            f = true;
            str = strValue;
        }
        public void debug()
        {
            if (f == true)
            {
                Debug.Log(str);
                f = false;
            }
            return;
        }
            
            
            
            }

    void Update()
    {
        //dbg.debug();
        // 动画运行中且未触发判断时，更新颜色渐变
        if (isAnimating && !isTriggered)
        {
            animationTime += Time.deltaTime;
            float totalDuration = phase1Duration + phase2Duration;
            // 检查是否超时（蓝色阶段结束）
            if (animationTime >= totalDuration)
            {
                isAnimating = false;
                JudgeResult(false, "超时（蓝色阶段结束）",SuccessType .Faild);
                return;
            }

            // 阶段1：初始色 → 绿色（有效区域）
            if (animationTime <= phase1Duration)
            {
                //dbg3.debug();
                float t1 = animationTime / phase1Duration;
                ringImage.color = Color.Lerp(initialColor, targetGreen, t1);
            }
            // 阶段2：绿色 → 蓝色（超时区域）
            else
            {
                //dbg4.debug();
                float t2 = (animationTime - phase1Duration) / phase2Duration;
                ringImage.color = Color.Lerp(targetGreen, endBlue, t2);
            }
        }
    }

    /// <summary>
    /// 拖拽物进入时触发（启动动画）
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null&& lockOnce==true) return;

        DraggableUI draggable = eventData.pointerDrag.TryGetComponent<DraggableUI>(out var d) ? d : null;
        if (draggable == null) { 
            Debug.LogError("拖拽物无法获得缺少DraggableUI组件，无法启动动画");

            return;
        }

        // 类型匹配时启动动画
        if (draggable.targetType == targetType)
        {
         
            isMatchedDraggingOver = true;
            StartAnimation(); // 开启动画
        }
        else
        {
            Debug.LogError("拖拽物类型不匹配，无法启动动画");
        }
    }

    /// <summary>
    /// 拖拽物离开时触发（停止动画）
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isMatchedDraggingOver)
        {
            isMatchedDraggingOver = false;
            StopAnimation(); // 停止动画
        }
    }

    /// <summary>
    /// 拖拽结束时触发（判断时机）
    /// </summary>
    public void OnRemoteEndDrag()
    {
        if (isMatchedDraggingOver && isAnimating && !isTriggered)
        {
            isTriggered = true;
            isAnimating = false;

            float totalDuration = phase1Duration + phase2Duration;
            // 检查当前动画阶段，判断结果
            if (animationTime >= totalDuration)
            {
                JudgeResult(false, "超时（拖拽结束时已过蓝色阶段）",SuccessType.Faild);
            }
            else if (animationTime <= phase1Duration)
            {
                // 阶段1：判断是否接近绿色
                float t1 = animationTime / phase1Duration;
                if (t1 >= greenThreshold)
                {
                    JudgeResult(true, "时机正确（接近绿色区域）", SuccessType.BigSuccess);
                }
                else if (t1 >= greenThresholdSuccessSmall)
                {
                    JudgeResult(true, "时机较好（接近绿色区域）", SuccessType.SmallSuccess);
                }
                else
                {
                    JudgeResult(false, "时机过早（未接近绿色区域）", SuccessType.Faild);
                }
            }
            else
            {
                // 阶段2：已过绿色区域
                JudgeResult(false, "时机过晚（已进入蓝色区域）", SuccessType.Faild);
            }
        }
    }

    /// <summary>
    /// 启动颜色渐变动画
    /// </summary>
    private void StartAnimation()
    {
        isAnimating = true;
        animationTime = 0f;
        isTriggered = false;
        ringImage.color = initialColor; // 重置为初始色
        Debug.Log("动画启动：开始向绿色渐变");
        lockOnce = true;
    }

    /// <summary>
    /// 停止动画并重置状态
    /// </summary>
    private void StopAnimation()
    {
        lockOnce = false;
        isAnimating = false;
        isTriggered = false;
        ringImage.color = initialColor; // 重置颜色
        Debug.Log("动画停止：拖拽物离开");
    }

    /// <summary>
    /// 判定结果并触发回调
    /// </summary>
    private void JudgeResult(bool success, string reason,SuccessType type)
    {
        isTriggered = true;
        // 视觉反馈
        ringImage.color = success ? successColor : failColor;
        // 日志与回调
        Debug.Log($"判定结果：{(success ? "成功" : "失败")} - {reason}");
        onJudgeResult?.Invoke(success, type); // 通知外部
        StopAnimation();
    }

    /// <summary>
    /// 外部接口：设置时间参数
    /// </summary>
    /// <param name="phase1">到绿色的时长（秒）</param>
    /// <param name="phase2">绿色到蓝色的时长（秒）</param>
    /// <param name="threshold">接近绿色的阈值（0-1）</param>
    public void SetTimeParameters(float phase1, float phase2, float threshold)
    {
        phase1Duration = Mathf.Max(0.1f, phase1); // 避免0或负数
        phase2Duration = Mathf.Max(0.1f, phase2);
        greenThreshold = Mathf.Clamp01(threshold); // 限制在0-1
    }

    /// <summary>
    /// 重置功能（外部可调用）
    /// </summary>
    public void ResetRing()
    {
        StopAnimation();
        isMatchedDraggingOver = false;
    }

}