using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class GearHoverRotate : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("关联按钮")]
    public Button targetButton;          // 鼠标悬停的按钮

    [Header("旋转参数")]
    public float rotateSpeed = 180f;     // 每秒旋转角度（度/秒）
    public float accelTime = 0.5f;       // 加速到目标速度所需时间
    public float decelTime = 0.6f;       // 停止减速所需时间

    private RectTransform _rt;
    private Tween spinTween;
    private float currentSpeed = 0f;
    private bool spinning = false;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (targetButton != null)
        {
            // 手动挂接按钮事件（避免需要 EventTrigger）
            var trigger = targetButton.gameObject.AddComponent<UIBehaviourProxy>();
            trigger.onEnter += OnPointerEnter;
            trigger.onExit  += OnPointerExit;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartSpin();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopSpin();
    }

    void StartSpin()
    {
        if (spinning) return;
        spinning = true;
        spinTween?.Kill();

        // 从当前速度缓慢加速到目标速度
        DOTween.To(() => currentSpeed, v => currentSpeed = v, rotateSpeed, accelTime)
               .SetEase(Ease.OutCubic)
               .OnUpdate(() =>
               {
                   _rt.Rotate(0, 0, -currentSpeed * Time.deltaTime);
               })
               .OnComplete(() =>
               {
                   // 持续旋转（匀速阶段）
                   spinTween = DOTween.To(() => 0f, _ => 
                       _rt.Rotate(0, 0, -rotateSpeed * Time.deltaTime), 0f, 999f)
                       .SetEase(Ease.Linear)
                       .SetLoops(-1);
               });
    }

    void StopSpin()
    {
        if (!spinning) return;
        spinning = false;
        spinTween?.Kill();

        // 减速停止
        DOTween.To(() => currentSpeed, v => currentSpeed = v, 0f, decelTime)
               .SetEase(Ease.OutCubic)
               .OnUpdate(() =>
               {
                   _rt.Rotate(0, 0, -currentSpeed * Time.deltaTime);
               });
    }

    void OnDisable()
    {
        spinTween?.Kill();
    }
}