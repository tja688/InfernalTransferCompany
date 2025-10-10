using UnityEngine;
using DG.Tweening;

public class UITweenController : MonoBehaviour
{
    [Header("Start State (Read-Only)")]
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 startScale;

    [Header("Target State")]
    public Vector3 targetPosition;
    public Vector3 targetScale;

    [Header("Animation Settings")]
    public float duration = 1f;
    public Ease easeType = Ease.OutQuad;

    // 这个方法由自定义编辑器脚本调用
    public void RecordStates()
    {
        // 先记录当前状态为目标状态
        targetPosition = transform.position;
        targetScale = transform.localScale;
        // 然后把初始状态也存一下，方便复位
        // (这里的逻辑可以更复杂，比如在Awake里存初始值)
    }
    
    // 这个方法也由编辑器脚本调用
    public void RevertToStart()
    {
        transform.position = startPosition;
        transform.localScale = startScale;
    }

    void Awake()
    {
        // 在游戏开始时记录初始状态
        startPosition = transform.position;
        startScale = transform.localScale;
    }

    public void DoAnimation()
    {
        transform.DOMove(targetPosition, duration).SetEase(easeType);
        transform.DOScale(targetScale, duration).SetEase(easeType);
    }
}