using UnityEngine;

/// <summary>
/// 将PlayerMovementController的移动信息转换为Animator参数
/// 用于驱动角色动画（X, Y方向参数和Speed速度参数）
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerMovementToAnimator : MonoBehaviour
{
    [Header("Animator参数设置")]
    [SerializeField]
    [Tooltip("Animator中X方向参数名称")]
    private string xParam = "X";

    [SerializeField]
    [Tooltip("Animator中Y方向参数名称")]
    private string yParam = "Y";

    [SerializeField]
    [Tooltip("Animator中速度参数名称")]
    private string speedParam = "Speed";

    [Header("调优设置")]
    [SerializeField]
    [Tooltip("最小移动速度阈值，小于此值认为静止（进入Idle）")]
    private float minMoveSpeed = 0.05f;

    [SerializeField]
    [Tooltip("参数平滑时间（秒）")]
    private float dampTime = 0.08f;

    [SerializeField]
    [Tooltip("是否钳制到四象限（±1, ±1）")]
    private bool snapToQuadrants = true;

    [Header("引用设置")]
    [SerializeField]
    [Tooltip("PlayerMovementController组件引用，如果为空则自动获取")]
    private PlayerMovementController movementController;

    private Animator anim;
    
    // 记住最后一次有效的朝向（归一化），用于静止时保持朝向
    private Vector2 lastFacing = new Vector2(0, 1); // 默认朝上

    void Awake()
    {
        anim = GetComponent<Animator>();
        
        // 如果没有手动指定，尝试自动获取PlayerMovementController
        if (movementController == null)
        {
            movementController = GetComponent<PlayerMovementController>();
            if (movementController == null)
            {
                Debug.LogWarning($"未找到PlayerMovementController组件，请手动指定或添加到同一GameObject上。", this);
            }
        }
    }

    void Update()
    {
        if (movementController == null || anim == null)
            return;

        // 获取移动方向和速度信息
        Vector2 moveDirection = movementController.MoveDirection;
        Vector2 moveInput = movementController.MoveInput;
        bool isMoving = movementController.IsMoving;
        
        // 使用输入值的模长作为速度指示（0-1之间，表示输入强度）
        // 这比使用moveDirection更准确，因为moveDirection可能经过轴向偏移变换
        float speed = moveInput.magnitude;
        
        // 如果正在移动且速度超过阈值，更新朝向和Animator参数
        if (isMoving && speed >= minMoveSpeed && moveDirection.sqrMagnitude > 1e-6f)
        {
            Vector2 normalizedDir = moveDirection.normalized;

            // 如果启用四象限钳制
            if (snapToQuadrants)
            {
                normalizedDir = new Vector2(
                    Mathf.Sign(normalizedDir.x) * Mathf.Clamp01(Mathf.Abs(normalizedDir.x)),
                    Mathf.Sign(normalizedDir.y) * Mathf.Clamp01(Mathf.Abs(normalizedDir.y))
                );
            }

            // 记住最新有效朝向
            lastFacing = normalizedDir;
            
            // 设置Animator参数（使用平滑过渡）
            SetParamsSmooth(normalizedDir.x, normalizedDir.y, speed);
        }
        else
        {
            // 静止：Speed=0，但X/Y保持lastFacing，用于驱动Idle混合树的朝向
            SetParamsSmooth(lastFacing.x, lastFacing.y, 0f);
        }
    }

    /// <summary>
    /// 平滑设置Animator参数
    /// </summary>
    void SetParamsSmooth(float x, float y, float speed)
    {
        anim.SetFloat(xParam, x, dampTime, Time.deltaTime);
        anim.SetFloat(yParam, y, dampTime, Time.deltaTime);
        
        if (!string.IsNullOrEmpty(speedParam))
        {
            anim.SetFloat(speedParam, speed, 0.05f, Time.deltaTime);
        }
    }

    /// <summary>
    /// 设置默认朝向（用于初始化或重置）
    /// </summary>
    public void SetDefaultFacing(Vector2 facing)
    {
        if (facing.sqrMagnitude > 1e-6f)
        {
            lastFacing = facing.normalized;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        minMoveSpeed = Mathf.Max(0f, minMoveSpeed);
        dampTime = Mathf.Max(0f, dampTime);
    }
#endif
}

