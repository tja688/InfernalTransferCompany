using PrimeTween;
using Spine.Unity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Image))]
public class EntryAnimation : MonoBehaviour
{
    [Header("入场动画设置")]
    [Tooltip("入场动画的起始位置（localPosition，相对于父 RectTransform）")]
    [SerializeField] private Vector3 entryStartPosition;

    [Tooltip("入场动画持续时间（秒）")]
    [SerializeField] private float entryDuration = 0.6f;

    [Tooltip("入场动画缓动曲线")]
    [SerializeField] private Ease entryEase = Ease.OutQuad;

    [Header("出场动画设置")]
    [Tooltip("出场动画的结束位置（localPosition，相对于父 RectTransform）")]
    [SerializeField] private Vector3 exitEndPosition;

    [Tooltip("出场动画持续时间（秒）")]
    [SerializeField] private float exitDuration = 0.6f;

    [Tooltip("出场动画缓动曲线")]
    [SerializeField] private Ease exitEase = Ease.InQuad;

    private Image targetImage;
    private RectTransform rectTransform;
    // 记录元素在场景中作为“正常位置”的局部位置（入场动画的目标）
    private Vector3 initialLocalPosition;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        rectTransform = targetImage.rectTransform;
        // 记录初始位置作为入场目标位置
        initialLocalPosition = rectTransform.localPosition;
    }

    private void Start()
    {
        
    }

    /// <summary>
    /// 播放入场动画（针对 UI Image 的 RectTransform）：先把元素放到 entryStartPosition，再缓动到初始位置 initialLocalPosition。
    /// 返回生成的 Tween 以便外部控制（比如链式调用或停止）。
    /// </summary>
    public Tween PlayEntryAnimation()
    {
        // 停止该 RectTransform 上所有正在播放的 Tween（避免冲突）
        Tween.StopAll(rectTransform);

        // 先立即设置到入场起点
        rectTransform.localPosition = entryStartPosition;
        Debug.Log($"localP{exitEndPosition}" );
        // 缓动到记录的初始位置（界面上的正常位置）
       return Tween.LocalPosition(
            target: rectTransform,
            endValue: exitEndPosition,
            duration: entryDuration,
            ease: entryEase
        );
    }

    /// <summary>
    /// 播放出场动画：从当前位置缓动到 exitEndPosition。
    /// </summary>
    public void PlayExitAnimation()
    {
        
        Tween.StopAll(rectTransform);
        rectTransform.localPosition = entryStartPosition;



    }
}