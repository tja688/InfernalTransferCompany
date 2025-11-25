using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 基础2D人物移动控制器：使用Unity新输入系统监听WASD移动，
/// 支持45度斜向地图的轴向偏移设置。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField]
    [Tooltip("移动速度（单位/秒）")]
    private float moveSpeed = 5f;

    [SerializeField]
    [Tooltip("是否使用物理移动（Rigidbody2D）")]
    private bool usePhysicsMovement = true;

    [Header("45度斜向地图设置")]
    [SerializeField]
    [Tooltip("启用轴向偏移，用于支持45度斜向地图")]
    private bool enableAxisOffset = false;

    [SerializeField]
    [Tooltip("轴向偏移角度（度），通常为45度")]
    [Range(0f, 90f)]
    private float axisOffsetAngle = 45f;

    [Header("输入设置")]
    [SerializeField]
    [Tooltip("输入动作资源（Input Action Asset），如果为空则使用默认输入")]
    private InputActionAsset inputActionAsset;

    [SerializeField]
    [Tooltip("移动输入动作名称，默认为 'Move'")]
    private string moveActionName = "Move";

    private InputAction moveAction;
    private Rigidbody2D rb2d;
    private Vector2 moveInput;
    private Vector2 moveDirection;

    /// <summary>当前移动输入值（归一化）</summary>
    public Vector2 MoveInput => moveInput;

    /// <summary>当前移动方向（应用轴向偏移后）</summary>
    public Vector2 MoveDirection => moveDirection;

    /// <summary>是否正在移动</summary>
    public bool IsMoving => moveInput.magnitude > 0.01f;

    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        InitializeInput();
    }

    void OnEnable()
    {
        if (moveAction != null)
            moveAction.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null)
            moveAction.Disable();
    }

    void Update()
    {
        ReadInput();
        CalculateMoveDirection();
    }

    void FixedUpdate()
    {
        if (usePhysicsMovement)
        {
            ApplyPhysicsMovement();
        }
    }

    void LateUpdate()
    {
        if (!usePhysicsMovement)
        {
            ApplyDirectMovement();
        }
    }

    void InitializeInput()
    {
        // 如果指定了输入动作资源，从中获取移动动作
        if (inputActionAsset != null)
        {
            moveAction = inputActionAsset.FindAction(moveActionName);
            if (moveAction == null)
            {
                Debug.LogWarning($"在输入动作资源中未找到名为 '{moveActionName}' 的动作，将使用默认输入。", this);
                CreateDefaultInputAction();
            }
        }
        else
        {
            CreateDefaultInputAction();
        }

        if (moveAction != null)
        {
            moveAction.Enable();
        }
    }

    void CreateDefaultInputAction()
    {
        // 创建默认的移动输入动作
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
    }

    void ReadInput()
    {
        if (moveAction == null)
            return;

        moveInput = moveAction.ReadValue<Vector2>();
    }

    void CalculateMoveDirection()
    {
        if (enableAxisOffset && axisOffsetAngle != 0f)
        {
            // 应用轴向偏移：将输入向量旋转指定角度
            float angleRad = axisOffsetAngle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            // 旋转矩阵：将输入向量旋转角度
            moveDirection = new Vector2(
                moveInput.x * cos - moveInput.y * sin,
                moveInput.x * sin + moveInput.y * cos
            );
        }
        else
        {
            moveDirection = moveInput;
        }
    }

    void ApplyPhysicsMovement()
    {
        if (rb2d == null)
            return;

        Vector2 velocity = moveDirection * moveSpeed;
        rb2d.velocity = velocity;
    }

    void ApplyDirectMovement()
    {
        Vector2 movement = moveDirection * moveSpeed * Time.deltaTime;
        transform.position += new Vector3(movement.x, movement.y, 0f);
    }

    /// <summary>
    /// 设置移动速度
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>
    /// 设置轴向偏移角度
    /// </summary>
    public void SetAxisOffsetAngle(float angle)
    {
        axisOffsetAngle = Mathf.Clamp(angle, 0f, 90f);
    }

    /// <summary>
    /// 启用/禁用轴向偏移
    /// </summary>
    public void SetAxisOffsetEnabled(bool enabled)
    {
        enableAxisOffset = enabled;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        axisOffsetAngle = Mathf.Clamp(axisOffsetAngle, 0f, 90f);
    }
#endif
}


