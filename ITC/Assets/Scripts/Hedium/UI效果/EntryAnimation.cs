using PrimeTween;
using Spine.Unity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MoreMountains;
using MoreMountains.Feedbacks;
using System;
[RequireComponent(typeof(Image))]
public class EntryAnimation : MonoBehaviour
{
    public MMF_Player Enter; 
    public MMF_Player Exit;
    public Action OnEntryComplete;
    public Action OnExitComplete;

    private void Awake()
    {


    }

    private void Start()
    {
        
    }

    /// <summary>
    /// 播放入场动画（针对 UI Image 的 RectTransform）：先把元素放到 entryStartPosition，再缓动到初始位置 initialLocalPosition。
    /// 返回生成的 Tween 以便外部控制（比如链式调用或停止）。
    /// </summary>
    public void PlayEntryAnimation()
    {
        Enter?.PlayFeedbacks();

        Enter?.Events.OnComplete.AddListener(() =>
        {
            OnEntryComplete?.Invoke();
        });
    }

    /// <summary>
    /// 播放出场动画：从当前位置缓动到 exitEndPosition。
    /// </summary>
    public void PlayExitAnimation()
    {
        Exit?.PlayFeedbacks();
        Exit?.Events.OnComplete.AddListener(() =>
        {
            OnExitComplete?.Invoke();
        });
    }
}