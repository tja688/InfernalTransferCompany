// StampingFlowController.cs
// 依赖：UniTask (Cysharp.Threading.Tasks) + UniRx + 你之前的 EntranceMotion.cs
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using UniRx;
using UniRx.Triggers;

[AddComponentMenu("Game/Stamping Flow Controller")]
public class StampingFlowController : MonoBehaviour {
    [Header("Hotkeys")]
    public KeyCode startKey = KeyCode.P;   // 开始事件流
    public KeyCode endKey   = KeyCode.S;   // 结束（盖章收尾）

    [Header("References")]
    [Tooltip("卷轴入/退场动效组件")]
    public EntranceMotion scrollMotion;
    [Tooltip("契约入/退场动效组件")]
    public EntranceMotion contractMotion;
    [Tooltip("检查器区域（玩家鼠标需移入此Collider2D并点击）")]
    public Collider2D inspectorArea;
    [Tooltip("印章预制体（世界物体，内部可放Sprite）")]
    public GameObject stampPrefab;
    [Tooltip("用于屏幕->世界坐标换算，留空用Camera.main")]
    public Camera worldCamera;

    [Header("Stamp Follow")]
    [Tooltip("惰性跟随时间（SmoothDamp的平滑时间，越小越跟手）")]
    [Range(0.01f, 0.5f)] public float followSmoothTime = 0.06f;
    [Tooltip("印章位置微调偏移")]
    public Vector3 stampOffset = Vector3.zero;
    [Tooltip("用UnscaledDeltaTime做跟随（暂停时也跟随）")]
    public bool followUseUnscaledTime = true;

    [Header("Flow Options")]
    [Tooltip("开始时禁用 inspectorArea，卷轴入场后再启用")]
    public bool autoControlInspectorEnable = true;
    [Tooltip("契约入场时卷轴是否同时出场")]
    public bool hideScrollWhenContractIn = true;

    bool isRunning;
    CancellationTokenSource flowCts;

    void Awake() {
        if (worldCamera == null) worldCamera = Camera.main;
        if (autoControlInspectorEnable && inspectorArea) inspectorArea.enabled = false;

        // Rx：按P启动
        this.UpdateAsObservable()
            .Where(_ => Input.GetKeyDown(startKey))
            .Where(_ => !isRunning)
            .Subscribe(_ => RunFlowAsync(this.GetCancellationTokenOnDestroy()).Forget())
            .AddTo(this);
    }

    async UniTaskVoid RunFlowAsync(CancellationToken externalCt) {
        if (scrollMotion == null || contractMotion == null || inspectorArea == null || stampPrefab == null) {
            Debug.LogError("[StampingFlow] 引用未设置完整。");
            return;
        }

        isRunning = true;
        flowCts?.Cancel();
        flowCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        var ct = flowCts.Token;

        try {
            // 1) 卷轴入场
            await ShowAsync(scrollMotion, ct);

            // 2) 激活检查器并等待“鼠标进入+点击”
            if (autoControlInspectorEnable) inspectorArea.enabled = true;
            await WaitClickInsideColliderAsync(inspectorArea, worldCamera, ct);
            if (autoControlInspectorEnable) inspectorArea.enabled = false;

            // 3) 契约入场 & （可选）卷轴出场
            var contractInTask = ShowAsync(contractMotion, ct);
            if (hideScrollWhenContractIn) {
                // 并行让卷轴退场
                _ = HideAsync(scrollMotion, ct);
            }
            await contractInTask;

            // 4) 契约入场后：隐藏鼠标 + 生成印章 & 惰性跟随
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;

            var stamp = Instantiate(stampPrefab);
            var stampTr = stamp.transform;
            // 初始放到鼠标位置
            stampTr.position = MouseWorldAtSameZ(worldCamera, stampTr.position) + stampOffset;

            using (var stampFollowCts = CancellationTokenSource.CreateLinkedTokenSource(ct)) {
                var followTask = FollowMouseLazyAsync(stampTr, stampOffset, stampFollowCts.Token);

                // 5) 等待按下结束键 S
                await UniTask.WaitUntil(() => Input.GetKeyDown(endKey), cancellationToken: ct);

                // 6) 收尾：停止跟随、销毁印章、显示鼠标、契约退场
                stampFollowCts.Cancel();
                SafeDestroy(stamp);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                await HideAsync(contractMotion, ct);
            }
        }
        catch (System.OperationCanceledException) {
            // 被取消时兜底复原
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        finally {
            if (autoControlInspectorEnable && inspectorArea) inspectorArea.enabled = false;
            isRunning = false;
        }
    }

    // ---------- helpers ----------

    async UniTask ShowAsync(EntranceMotion m, CancellationToken ct) {
        m.Show();
        await UniTask.WaitUntil(() => !m.IsAnimating && m.IsShown, cancellationToken: ct);
    }

    async UniTask HideAsync(EntranceMotion m, CancellationToken ct) {
        m.Hide();
        await UniTask.WaitUntil(() => !m.IsAnimating && !m.IsShown, cancellationToken: ct);
    }

    async UniTask WaitClickInsideColliderAsync(Collider2D area, Camera cam, CancellationToken ct) {
        // 等鼠标“进入区域”后再等待点击
        await UniTask.WaitUntil(() => OverArea(area, cam), cancellationToken: ct);
        await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0), cancellationToken: ct);
    }

    bool OverArea(Collider2D area, Camera cam) {
        var wp = MouseWorldAtPlane(cam, WorldToScreenZ(cam, area.bounds.center));
        return area.OverlapPoint((Vector2)wp);
    }

    float WorldToScreenZ(Camera cam, Vector3 worldPos) {
        return cam.WorldToScreenPoint(worldPos).z;
    }

    Vector3 MouseWorldAtSameZ(Camera cam, Vector3 referenceWorldPos) {
        float refZ = WorldToScreenZ(cam, referenceWorldPos);
        return MouseWorldAtPlane(cam, refZ);
    }

    Vector3 MouseWorldAtPlane(Camera cam, float planeScreenZ) {
        var sp = Input.mousePosition;
        sp.z = Mathf.Max(0.01f, planeScreenZ);
        return cam.ScreenToWorldPoint(sp);
    }

    async UniTask FollowMouseLazyAsync(Transform tr, Vector3 offset, CancellationToken ct) {
        Vector3 vel = Vector3.zero;
        while (!ct.IsCancellationRequested) {
            var target = MouseWorldAtSameZ(worldCamera, tr.position) + offset;
            float dt = followUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            tr.position = Vector3.SmoothDamp(tr.position, target, ref vel, followSmoothTime, Mathf.Infinity, dt);
            await UniTask.Yield(ct);
        }
    }

    void SafeDestroy(Object obj) {
        if (obj == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(obj);
        else Destroy(obj);
#else
        Destroy(obj);
#endif
    }

    void OnDestroy() {
        flowCts?.Cancel();
        flowCts?.Dispose();
    }
}
