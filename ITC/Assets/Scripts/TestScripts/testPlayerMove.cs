using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 控制2D对象的上下左右移动
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("移动速度")]
    [Tooltip("控制角色的移动速度")]
    public float moveSpeed = 5f; // 你可以在Unity编辑器中随时调整这个数值

    private Rigidbody2D rb; // 用于物理移动

    // Start is called before the first frame update
    void Start()
    {
        // 获取附加到同一个游戏对象上的Rigidbody2D组件
        // Rigidbody2D是实现物理移动所必需的
        rb = GetComponent<Rigidbody2D>();

        // 检查是否成功获取了Rigidbody2D组件
        if (rb == null)
        {
            Debug.LogError("在对象 " + gameObject.name + " 上没有找到 Rigidbody2D 组件！请添加一个。");
        }
    }

    // Update is called once per frame
    // 我们在Update中检测输入，但在FixedUpdate中应用物理效果
    void Update()
    {
        // 这个脚本非常基础，我们也可以直接在Update中处理移动逻辑
        // 对于更复杂的物理交互，建议在FixedUpdate中处理
    }

    // FixedUpdate is called at a fixed interval and is used for physics-based calculations
    void FixedUpdate()
    {
        // 如果没有Rigidbody2D，则不执行任何操作
        if (rb == null) return;
        
        // --- 方法一：使用 transform.Translate (简单，但可能穿透碰撞体) ---
        // 这是最直接的移动方式，它会直接改变对象的位置。
        // 这种方法不依赖于物理引擎，因此在某些情况下可能会导致角色穿透墙壁。
        /*
        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            moveY = 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveY = -1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveX = -1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveX = 1f;
        }

        Vector3 moveDirection = new Vector3(moveX, moveY).normalized;
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
        */


        // --- 方法二：使用 Rigidbody2D.velocity (推荐，物理效果更佳) ---
        // 这是推荐的2D移动方式，因为它利用了Unity的物理引擎。
        // 这种方式可以更好地处理碰撞。
        float moveHorizontal = Input.GetAxisRaw("Horizontal"); // 获取A、D键或左右方向键的输入 (-1, 0, 1)
        float moveVertical = Input.GetAxisRaw("Vertical");     // 获取W、S键或上下方向键的输入 (-1, 0, 1)

        // 创建一个移动向量
        Vector2 movement = new Vector2(moveHorizontal, moveVertical);

        // 为了防止斜向移动速度过快（例如同时按住W和D），我们需要将向量归一化
        // .normalized 会将向量的长度变为1，但保持其方向
        // 然后乘以速度和时间，得到最终的位移
        rb.velocity = movement.normalized * moveSpeed;
    }
}