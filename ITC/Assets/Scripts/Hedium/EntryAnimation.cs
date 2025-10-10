using PrimeTween;
using Spine.Unity;
using System.Collections.Generic;
using UnityEngine;

public class EntryAnimation : MonoBehaviour
{
    [Header("入场动画设置")]
    [Tooltip("入场动画的起始位置")]
    [SerializeField] private Vector3 entryStartPosition;

    [Tooltip("入场动画持续时间（秒）")]
    [SerializeField] private float entryDuration = 0.6f;

    [Tooltip("入场动画缓动曲线")]
    [SerializeField] private Ease entryEase = Ease.OutQuad;

    [Header("出场动画设置")]
    [Tooltip("出场动画的结束位置")]
    [SerializeField] private Vector3 exitEndPosition;

    [Tooltip("出场动画持续时间（秒）")]
    [SerializeField] private float exitDuration = 0.6f;

    [Tooltip("出场动画缓动曲线")]
    [SerializeField] private Ease exitEase = Ease.InQuad;

  

    private void Awake()
    {
     
    }
    private void Start()
    {
 

   
    }
    /// <summary>
    /// 播放入场动画：从指定起点移动到初始位置
    /// </summary>
    public Tween PlayEntryAnimation()
    {
        // 停止目标对象上所有正在播放的Tween动画（避免冲突）
        Tween.StopAll(transform);

   
      


        return Tween.Position(
            target: transform,
            endValue: exitEndPosition,
            duration: entryDuration,
            ease: entryEase
        );
    }

    /// <summary>
    /// 播放出场动画：从当前位置移动到指定终点
    /// </summary>
    public Tween PlayExitAnimation()
    {
       
        Tween.StopAll(transform);

    
        return Tween.Position(
            target: transform,
            endValue: entryStartPosition,
            duration: exitDuration,
            ease: exitEase
        );
    }
}