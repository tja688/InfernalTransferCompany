using UnityEngine;
using Pathfinding; // A* Pathfinding Project

[RequireComponent(typeof(Animator))]
public class AIDirectionToAnimator : MonoBehaviour
{
    [Header("Animator params")]
    public string xParam = "X";
    public string yParam = "Y";
    public string speedParam = "Speed";

    [Header("Tuning")]
    public float minMoveSpeed = 0.05f;   // 小于此速度认为静止（进入 Idle）
    public float dampTime = 0.08f;       // 参数平滑
    public bool snapToQuadrants = true;  // 是否钳到四象限(±1,±1)

    [Header("Refs")]
    public AIPath ai;                    // 可在Inspector手动拖，也可 GetComponent

    Animator anim;
    Vector3 lastPos;

    // 关键：记住“最后一次有效的朝向”（归一化）
    Vector2 lastFacing = new Vector2(0, 1); // 默认朝上，可按需要改

    void Awake() {
        anim = GetComponent<Animator>();
        if (!ai) ai = GetComponent<AIPath>();
        lastPos = transform.position;
    }

    void Update() {
        // 用位移估算实际速度（稳）
        Vector3 cur = transform.position;
        float dt = Mathf.Max(Time.deltaTime, 1e-5f);
        Vector3 worldVel = (cur - lastPos) / dt;
        lastPos = cur;
        float speed = worldVel.magnitude;

        // 计算“期望方向”
        Vector2 dir;
        if (ai && ai.hasPath && !ai.reachedDestination) {
            Vector3 toTarget = ai.steeringTarget - cur;
            dir = new Vector2(toTarget.x, toTarget.y); // 若用XZ平面改为(x,z)
        } else {
            dir = new Vector2(worldVel.x, worldVel.y);
        }

        // 运动中：更新朝向缓存与Animator
        if (speed >= minMoveSpeed && dir.sqrMagnitude > 1e-6f) {
            Vector2 n = dir.normalized;

            if (snapToQuadrants) {
                n = new Vector2(Mathf.Sign(n.x) * Mathf.Clamp01(Mathf.Abs(n.x)),
                                Mathf.Sign(n.y) * Mathf.Clamp01(Mathf.Abs(n.y)));
                // 进一步二值化也可： if (Mathf.Abs(n.x) < 0.33f) n.x = 0f; 同理 y
            }

            lastFacing = n; // 记住最新有效朝向
            SetParamsSmooth(n.x, n.y, speed);
            return;
        }

        // 静止：Speed=0，但X/Y保持 lastFacing，驱动你的 Idle 混合树朝向
        SetParamsSmooth(lastFacing.x, lastFacing.y, 0f);
    }

    void SetParamsSmooth(float x, float y, float s) {
        anim.SetFloat(xParam, x, dampTime, Time.deltaTime);
        anim.SetFloat(yParam, y, dampTime, Time.deltaTime);
        if (!string.IsNullOrEmpty(speedParam))
            anim.SetFloat(speedParam, s, 0.05f, Time.deltaTime);
    }
}
