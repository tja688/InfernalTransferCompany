using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 通用移动测试器 - 支持2D、3D和UI对象的移动控制
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    /// <summary>
    /// 对象类型枚举
    /// </summary>
    public enum ObjectType
    {
        Auto,   // 自动检测
        Object2D,   // 2D对象（使用Rigidbody2D）
        Object3D,   // 3D对象（使用Rigidbody）
        UI      // UI对象（使用RectTransform）
    }

    [Header("对象类型设置")]
    [Tooltip("选择对象类型，或选择Auto自动检测")]
    public ObjectType objectType = ObjectType.Auto;

    [Header("移动速度")]
    [Tooltip("控制对象的移动速度")]
    public float moveSpeed = 5f;

    [Header("UI移动设置（仅UI对象有效）")]
    [Tooltip("UI对象是否使用世界坐标移动（false则使用本地坐标）")]
    public bool useWorldSpace = false;

    // 2D物理组件
    private Rigidbody2D rb2D;
    // 3D物理组件
    private Rigidbody rb3D;
    // UI组件
    private RectTransform rectTransform;
    // 检测到的对象类型
    private ObjectType detectedType;

    void Start()
    {
        // 自动检测对象类型
        DetectObjectType();
        
        // 根据类型初始化相应的组件
        InitializeComponents();
    }

    /// <summary>
    /// 自动检测对象类型
    /// </summary>
    void DetectObjectType()
    {
        if (objectType != ObjectType.Auto)
        {
            detectedType = objectType;
            return;
        }

        // 优先检测UI对象
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            detectedType = ObjectType.UI;
            return;
        }

        // 检测2D对象
        rb2D = GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            detectedType = ObjectType.Object2D;
            return;
        }

        // 检测3D对象
        rb3D = GetComponent<Rigidbody>();
        if (rb3D != null)
        {
            detectedType = ObjectType.Object3D;
            return;
        }

        // 如果都没有，默认使用Transform移动（无物理）
        detectedType = ObjectType.Object3D;
        Debug.LogWarning("在对象 " + gameObject.name + " 上没有找到 Rigidbody2D、Rigidbody 或 RectTransform 组件！将使用Transform移动（无物理效果）。");
    }

    /// <summary>
    /// 初始化相应的组件
    /// </summary>
    void InitializeComponents()
    {
        switch (detectedType)
        {
            case ObjectType.Object2D:
                if (rb2D == null)
                    rb2D = GetComponent<Rigidbody2D>();
                if (rb2D == null)
                    Debug.LogError("在对象 " + gameObject.name + " 上未找到 Rigidbody2D 组件！");
                break;

            case ObjectType.Object3D:
                if (rb3D == null)
                    rb3D = GetComponent<Rigidbody>();
                // 3D对象如果没有Rigidbody，仍然可以使用Transform移动
                break;

            case ObjectType.UI:
                if (rectTransform == null)
                    rectTransform = GetComponent<RectTransform>();
                if (rectTransform == null)
                    Debug.LogError("在对象 " + gameObject.name + " 上未找到 RectTransform 组件！");
                break;
        }
    }

    void Update()
    {
        // UI对象在Update中移动，因为UI更新在Update阶段
        if (detectedType == ObjectType.UI)
        {
            MoveUI();
        }
    }

    void FixedUpdate()
    {
        // 物理对象在FixedUpdate中移动
        switch (detectedType)
        {
            case ObjectType.Object2D:
                Move2D();
                break;

            case ObjectType.Object3D:
                Move3D();
                break;
        }
    }

    /// <summary>
    /// 2D对象移动（使用Rigidbody2D）
    /// </summary>
    void Move2D()
    {
        if (rb2D == null) return;

        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");

        Vector2 movement = new Vector2(moveHorizontal, moveVertical);
        rb2D.velocity = movement.normalized * moveSpeed;
    }

    /// <summary>
    /// 3D对象移动（使用Rigidbody或Transform）
    /// </summary>
    void Move3D()
    {
        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0f, moveVertical).normalized * moveSpeed;

        if (rb3D != null)
        {
            // 使用物理移动（推荐，支持碰撞）
            rb3D.velocity = new Vector3(movement.x, rb3D.velocity.y, movement.z);
        }
        else
        {
            // 使用Transform移动（无物理效果，可能穿透碰撞体）
            transform.Translate(movement * Time.fixedDeltaTime, Space.World);
        }
    }

    /// <summary>
    /// UI对象移动（使用RectTransform）
    /// </summary>
    void MoveUI()
    {
        if (rectTransform == null) return;

        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");

        Vector2 movement = new Vector2(moveHorizontal, moveVertical).normalized * moveSpeed * Time.deltaTime;

        if (useWorldSpace)
        {
            // 世界坐标移动
            rectTransform.position += new Vector3(movement.x, movement.y, 0f);
        }
        else
        {
            // 本地坐标移动（相对于父对象）
            rectTransform.anchoredPosition += movement;
        }
    }
}