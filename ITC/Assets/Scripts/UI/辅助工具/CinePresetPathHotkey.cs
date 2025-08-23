using UnityEngine;
using PrimeTween; // 需已安装 PrimeTween
#if UNITY_EDITOR
using UnityEditor; // 仅用于在编辑器下更好看（可选）
#endif

[ExecuteAlways]
public class Camera2DPathStepperPrimeTween : MonoBehaviour {
    public enum TargetMode { AbsoluteWorld, OffsetFromOriginal }

    [Header("相机（正交2D；留空=Camera.main）")]
    public Camera cam;

    [Header("四个点位（A,B,C,D）")]
    public Transform pointA, pointB, pointC, pointD;

    [Header("点位解释方式")]
    public TargetMode targetMode = TargetMode.AbsoluteWorld;
    [Tooltip("是否应用点位的Z轴角度（只改Z旋转）")]
    public bool affectRotationZ = false;

    [Header("逐步控制")]
    public KeyCode nextKey = KeyCode.N;
    [Tooltip("每一步（A/B/C/D）的过渡时长")]
    public float stepDuration = 0.25f;
    public AnimationCurve stepEase = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("最后一步：拉回原始相机配置")]
    public float returnDuration = 2.5f;
    public AnimationCurve returnEase = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("过程选项")]
    [Tooltip("运镜过程中是否屏蔽再次触发")]
    public bool blockWhileMoving = true;

    [Header("原始视野保护（仅正交）")]
    [Tooltip("保证每一步的可视矩形完全在“原始视野”内；并在保持比例下尽可能贴边")]
    public bool keepInsideHomeView = true;
    [Tooltip("对原始视野边缘留的安全边距（相对原始半高的比例）")]
    [Range(0f, 0.2f)] public float safeMarginRatio = 0.02f;

    [Header("Gizmos 可视化")]
    public bool drawGizmos = true;
    public Color homeRectColor = new Color(0f, 1f, 0f, 0.6f);
    public Color targetRectColor = new Color(1f, 0.92f, 0.16f, 0.7f);
    public float gizmoZ = 0f; // 在场景里画线的Z平面（仅显示用）

    // --- 内部状态 ---
    Vector3 homePos;
    float   homeRotZ;
    float   homeSize; // 正交相机的 OrthographicSize（半高）
    bool    isOrtho;

    // 在“相机本地2D平面（Z旋转）”下的原始视野尺寸
    Vector2 homeCenterXY;
    float   homeHalfW, homeHalfH, marginW, marginH;

    int stepIndex = -1; // -1=原始；0..3 代表已执行到 A/B/C/D
    bool moving = false;
    Tween activeTween;

    void OnEnable(){
        EnsureCam();
        CacheHome();
    }

    void Reset(){ EnsureCam(); CacheHome(); }

    void EnsureCam(){
        if (!cam) cam = Camera.main;
    }

    void CacheHome(){
        if (!cam) return;
        isOrtho = cam.orthographic;
        homePos  = cam.transform.position;
        homeRotZ = cam.transform.eulerAngles.z;
        homeSize = isOrtho ? cam.orthographicSize : cam.fieldOfView;

        if (isOrtho){
            homeCenterXY = new Vector2(homePos.x, homePos.y);
            homeHalfH = homeSize;
            homeHalfW = homeSize * cam.aspect;
            marginH = homeHalfH * Mathf.Clamp01(safeMarginRatio);
            marginW = homeHalfW * Mathf.Clamp01(safeMarginRatio);
        }
    }

    void Update(){
        if (!Application.isPlaying) { // 编辑器下同步变更
            EnsureCam();
            CacheHome();
        }

        if (Application.isPlaying && Input.GetKeyDown(nextKey)){
            Step();
        }
    }

    public void Step(){
        if (moving && blockWhileMoving) return;

        if (stepIndex < 3){
            var t = GetPoint(stepIndex + 1);
            if (!t) { Debug.LogWarning("[Camera2DPathStepperPT] 缺少点位"); return; }
            MoveToPoint(t, stepDuration, stepEase);
            stepIndex++;
        } else {
            ReturnHome(returnDuration, returnEase);
            stepIndex = -1;
        }
    }

    Transform GetPoint(int idx){
        switch (idx){
            case 0: return pointA;
            case 1: return pointB;
            case 2: return pointC;
            case 3: return pointD;
        }
        return null;
    }

    // —— PrimeTween 驱动 —— //
    void MoveToPoint(Transform t, float duration, AnimationCurve curve){
        if (!cam){ Debug.LogError("Camera missing"); return; }
        if (!isOrtho){
            Debug.LogWarning("[Camera2DPathStepperPT] 建议使用正交相机；透视将仅移动不自动缩放。");
        }

        // 起点
        var tr = cam.transform;
        Vector2 p0 = new Vector2(tr.position.x, tr.position.y);
        float r0 = tr.eulerAngles.z;
        float s0 = isOrtho ? cam.orthographicSize : cam.fieldOfView;

        // 计算“夹紧后”的目标中心与最大允许尺寸
        Vector2 targetCenter; float targetSize; float targetRotZ;
        ComputeClampedView(t, out targetCenter, out targetSize, out targetRotZ);

        // 停掉旧Tween
        if (activeTween.isAlive) activeTween.Stop();

        moving = true;
        activeTween = Tween.Custom(0f, 1f, duration, onValueChange: u=>{
            float k = curve.Evaluate(u);

            // 位置（只XY，Z固定）
            var p = Vector2.LerpUnclamped(p0, targetCenter, k);
            tr.position = new Vector3(p.x, p.y, homePos.z);

            // 旋转（仅Z，按需）
            if (affectRotationZ){
                float rz = Mathf.LerpAngle(r0, targetRotZ, k);
                tr.rotation = Quaternion.Euler(0,0,rz);
            }

            // 尺寸（正交下插值到“允许的最大尺寸”，保证不越界）
            if (isOrtho){
                cam.orthographicSize = Mathf.LerpUnclamped(s0, targetSize, k);
            }
        }).OnComplete(()=>{
            // 对齐终点
            tr.position = new Vector3(targetCenter.x, targetCenter.y, homePos.z);
            if (affectRotationZ) tr.rotation = Quaternion.Euler(0,0,targetRotZ);
            if (isOrtho) cam.orthographicSize = targetSize;
            moving = false;
        });
    }

    void ReturnHome(float duration, AnimationCurve curve){
        if (!cam){ Debug.LogError("Camera missing"); return; }

        var tr = cam.transform;
        Vector2 p0 = new Vector2(tr.position.x, tr.position.y);
        float r0 = tr.eulerAngles.z;
        float s0 = isOrtho ? cam.orthographicSize : cam.fieldOfView;

        if (activeTween.isAlive) activeTween.Stop();

        moving = true;
        activeTween = Tween.Custom(0f, 1f, duration, onValueChange: u=>{
            float k = curve.Evaluate(u);

            var p = Vector2.LerpUnclamped(p0, homeCenterXY, k);
            tr.position = new Vector3(p.x, p.y, homePos.z);

            float rz = Mathf.LerpAngle(r0, homeRotZ, k);
            tr.rotation = Quaternion.Euler(0,0,rz);

            if (isOrtho){
                cam.orthographicSize = Mathf.LerpUnclamped(s0, homeSize, k);
            } else {
                cam.fieldOfView = Mathf.LerpUnclamped(s0, homeSize, k);
            }
        }).OnComplete(()=>{
            tr.position = new Vector3(homeCenterXY.x, homeCenterXY.y, homePos.z);
            tr.rotation = Quaternion.Euler(0,0,homeRotZ);
            if (isOrtho) cam.orthographicSize = homeSize; else cam.fieldOfView = homeSize;
            moving = false;
        });
    }

    /// <summary>
    /// 计算：把目标中心夹到“原始视野矩形（留边距）”内，并在保持比例下取“最大允许尺寸”，
    /// 使得以该中心为视图的矩形完全包含在原始视野内（长/短边尽可能贴边）。
    /// 如果是透视相机，将不改变尺寸，仅返回中心（同样夹紧）。
    /// </summary>
    void ComputeClampedView(Transform t, out Vector2 centerOut, out float sizeOut, out float rotZOut){
        var tr = cam.transform;
        // 目标中心（世界），只取XY；Offset模式：t的XY当“相对原始中心”的偏移量
        Vector2 desired = targetMode == TargetMode.AbsoluteWorld
            ? new Vector2(t.position.x, t.position.y)
            : homeCenterXY + (Vector2)t.position;

        // 相机Z旋转（本地坐标系）
        float theta = homeRotZ * Mathf.Deg2Rad;
        float cos = Mathf.Cos(theta), sin = Mathf.Sin(theta);

        // 把点变换到“相机本地平面”坐标：local = R^-1 * (world - homeCenter)
        Vector2 d = desired - homeCenterXY;
        Vector2 localDesired = new Vector2( cos*d.x + sin*d.y, -sin*d.x + cos*d.y );

        // 1) 夹中心到原始视野（留边距）
        float lx = Mathf.Clamp(localDesired.x, -homeHalfW + marginW, +homeHalfW - marginW);
        float ly = Mathf.Clamp(localDesired.y, -homeHalfH + marginH, +homeHalfH - marginH);

        // 2) 计算“最大允许半高(=size)”：在保持比例下，矩形不越界——
        //    半宽 = size * aspect；需满足：
        //    size*aspect <= homeHalfW - |lx| - marginW
        //    size        <= homeHalfH - |ly| - marginH
        float maxHalfWAllowed = Mathf.Max(0f, homeHalfW - Mathf.Abs(lx) - marginW);
        float maxHalfHAllowed = Mathf.Max(0f, homeHalfH - Mathf.Abs(ly) - marginH);

        float sAllowed = isOrtho
            ? Mathf.Min(maxHalfWAllowed / Mathf.Max(1e-6f, cam.aspect), maxHalfHAllowed, homeHalfH)
            : (isOrtho ? 0f : homeSize); // 透视下，我们不改FOV，保持原值

        // 输出中心：把夹紧后的 local 变回世界坐标：world = homeCenter + R * local
        Vector2 localClamped = new Vector2(lx, ly);
        Vector2 worldClamped = new Vector2(
            cos*localClamped.x - sin*localClamped.y,
            sin*localClamped.x + cos*localClamped.y
        ) + homeCenterXY;

        centerOut = worldClamped;
        sizeOut   = isOrtho ? Mathf.Max(0.01f, Mathf.Min(sAllowed, homeHalfH)) : homeSize;
        rotZOut   = affectRotationZ
            ? (targetMode == TargetMode.AbsoluteWorld
                ? t.eulerAngles.z
                : Mathf.DeltaAngle(0f, t.eulerAngles.z) + homeRotZ)
            : cam.transform.eulerAngles.z;
    }

    // =============== Gizmos 可视化 ===============
    void OnDrawGizmosSelected(){
        if (!drawGizmos || !cam) return;

        // 以当前缓存为准（编辑器下会在 Update 里刷）
        float theta = homeRotZ * Mathf.Deg2Rad;
        float cos = Mathf.Cos(theta), sin = Mathf.Sin(theta);

        // 画原始视野矩形（绿色）
        if (isOrtho){
            Gizmos.color = homeRectColor;
            DrawRect(homeCenterXY, homeHalfW, homeHalfH, homeRotZ);
        }

        // 画各点位的“夹紧后矩形”（黄色）
        Gizmos.color = targetRectColor;
        DrawTargetRect(pointA);
        DrawTargetRect(pointB);
        DrawTargetRect(pointC);
        DrawTargetRect(pointD);
    }

    void DrawTargetRect(Transform t){
        if (!t || !isOrtho) return;
        Vector2 center; float size; float rZ;
        ComputeClampedView(t, out center, out size, out rZ);
        float halfW = size * cam.aspect;
        float halfH = size;

        DrawRect(center, halfW, halfH, homeRotZ); // 与原始相机坐标系对齐（不随点位旋转）
        // 中心十字
        var z = gizmoZ;
        Gizmos.DrawLine(new Vector3(center.x-0.15f, center.y, z), new Vector3(center.x+0.15f, center.y, z));
        Gizmos.DrawLine(new Vector3(center.x, center.y-0.15f, z), new Vector3(center.x, center.y+0.15f, z));
    }

    void DrawRect(Vector2 center, float halfW, float halfH, float angleZ){
        float rad = angleZ * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad), s = Mathf.Sin(rad);

        Vector2[] corners = new Vector2[4]{
            new Vector2(-halfW, -halfH),
            new Vector2(+halfW, -halfH),
            new Vector2(+halfW, +halfH),
            new Vector2(-halfW, +halfH),
        };
        Vector3[] w = new Vector3[4];
        for (int i=0;i<4;i++){
            var v = corners[i];
            var r = new Vector2(c*v.x - s*v.y, s*v.x + c*v.y) + center;
            w[i] = new Vector3(r.x, r.y, gizmoZ);
        }
        Gizmos.DrawLine(w[0], w[1]);
        Gizmos.DrawLine(w[1], w[2]);
        Gizmos.DrawLine(w[2], w[3]);
        Gizmos.DrawLine(w[3], w[0]);
    }
}
