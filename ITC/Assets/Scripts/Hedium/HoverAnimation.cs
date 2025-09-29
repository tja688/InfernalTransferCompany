using PrimeTween;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoverAnimation : MonoBehaviour
{
    [SpineAnimation] public string hoverAnimation;
    [SpineAnimation] public string idleAnimation;
    private SkeletonAnimation skeletonAnimation;
    public Transform targetImage;
    
    private bool isHovering = false;
    private Tween scaleTween;
    private Tween rotationTween;
    

    
    // Start is called before the first frame update
    void Start()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
   
    }

    // Update is called once per frame
    void OnMouseEnter()
    {
        if (isHovering) return; // 防止重复触发
        
        isHovering = true;
        
       
        scaleTween.Stop();
        rotationTween.Stop();

        //skeletonAnimation.state.SetAnimation(0, hoverAnimation, true);

        Vector3 aim = (Vector3.one * 1.1f);
        aim.z = 1;
        scaleTween = Tween.Scale(targetImage, aim, 0.5f, Ease.OutQuad);
    
        rotationTween = Tween.LocalRotation(targetImage, new Vector3(0f, 0f, 10f), 0.5f, Ease.OutQuad);
    }

    void OnMouseExit()
    {
        if (!isHovering) return; // 防止重复触发
        
        isHovering = false;
        
    
        scaleTween.Stop();
        rotationTween.Stop();
        
        // skeletonAnimation.state.SetAnimation(0, idleAnimation, true);
        
     
        scaleTween = Tween.Scale(targetImage, Vector3.one, 0.5f, Ease.OutQuad);
        rotationTween = Tween.LocalRotation(targetImage, Vector3.zero, 0.5f, Ease.OutQuad);
       
    }
}
