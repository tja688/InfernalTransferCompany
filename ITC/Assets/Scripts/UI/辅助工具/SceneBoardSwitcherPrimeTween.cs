using System.Collections.Generic;
using UnityEngine;
using PrimeTween; // 已安装并引用

public class SceneBoardSwitcherPrimeTween : MonoBehaviour {
    public enum Style { CrossFade, PushLeft, PushRight, ZoomCross }

    [Header("场景原画根节点（按顺序）")]
    public List<GameObject> boards = new List<GameObject>();

    [Header("切换风格与参数")]
    public Style style = Style.CrossFade;
    [Tooltip("切换总时长（秒）")]
    public float duration = 0.6f;
    [Tooltip("自定义节奏（会作用在 0..1 的进度上）")]
    public AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Push 的位移（世界单位）")]
    public float pushDistance = 3f;
    [Tooltip("出场时的轻微放大比例")]
    public float outScale = 1.06f;
    [Tooltip("入场起始缩放")]
    public float inStartScale = 0.96f;

    [Header("热键（可关）")]
    public KeyCode nextKey = KeyCode.Space;
    public bool enableNumberKeysDirectJump = true; // 1..9 直跳（1→第0个）

    [Header("自动模式")]
    [Tooltip("自动模式：按此键开始/停止")]
    public KeyCode autoToggleKey = KeyCode.A;
    [Tooltip("两次切换的固定间隔（Start→Start，含动画时长）")]
    public float autoInterval = 2.0f;
    [Tooltip("开启自动模式后的首次延迟")]
    public float autoFirstDelay = 0.0f;
    [Tooltip("手动切换（Next/Jump）时是否自动退出自动模式")]
    public bool stopAutoOnManual = true;

    // ---- 缓存 ----
    class Cache {
        public GameObject go;
        public Transform tr;
        public Vector3 basePos;
        public Vector3 baseScale;
        public SpriteRenderer[] srs;
        public float[] baseAlpha;
    }
    readonly List<Cache> caches = new();
    int current = 0;
    bool isSwitching = false;
    Tween activeTween; // 切换动画
    // 自动模式
    bool autoRunning = false;
    float autoNextTick = 0f;

    void Awake() {
        if (boards == null || boards.Count == 0) {
            Debug.LogError("[SceneBoardSwitcherPrimeTween] 请在 boards 填入至少一个对象。");
            enabled = false; return;
        }
        caches.Clear();
        foreach (var go in boards) {
            if (!go) { caches.Add(null); continue; }
            var c = new Cache {
                go = go,
                tr = go.transform,
                basePos = go.transform.localPosition,
                baseScale = go.transform.localScale,
                srs = go.GetComponentsInChildren<SpriteRenderer>(true)
            };
            c.baseAlpha = new float[c.srs.Length];
            for (int i = 0; i < c.srs.Length; i++) c.baseAlpha[i] = c.srs[i].color.a;
            caches.Add(c);
        }

        // 初始化：只激活第一个
        for (int i = 0; i < caches.Count; i++) {
            var c = caches[i];
            if (c == null) continue;
            ResetTransform(c);
            SetGroupAlpha(c, 1f);
            c.go.SetActive(i == 0);
        }
        current = 0;
    }

    void Update() {
        // 手动下一张
        if (nextKey != KeyCode.None && Input.GetKeyDown(nextKey)) {
            if (stopAutoOnManual) StopAuto();
            Next();
        }

        // 数字键直跳
        if (enableNumberKeysDirectJump) {
            for (int i = 0; i < 9; i++) {
                var key = KeyCode.Alpha1 + i;
                if (Input.GetKeyDown(key)) {
                    if (stopAutoOnManual) StopAuto();
                    int idx = i; // 1→0号、2→1号...
                    if (idx < boards.Count) JumpTo(idx);
                }
            }
        }

        // 自动模式开/关
        if (autoToggleKey != KeyCode.None && Input.GetKeyDown(autoToggleKey))
            ToggleAuto();

        // 自动驱动：按固定间隔触发 Next（Start→Start）
        if (autoRunning && !isSwitching && Time.time >= autoNextTick) {
            if (current >= boards.Count - 1) { // 不循环：到末尾自动停
                StopAuto();
            } else {
                Next(); // Next 内部会设置 isSwitching
                autoNextTick = Time.time + autoInterval; // 下一次开拍时间
            }
        }
    }

    // ---------- 外部控制 ----------
    public void Next() {
        if (isSwitching) return;
        if (current >= boards.Count - 1) return; // 不循环
        SwitchTo(current + 1);
    }

    public void JumpTo(int index) {
        if (isSwitching) return;
        if (index < 0 || index >= boards.Count || index == current) return;
        SwitchTo(index);
    }

    public void ToggleAuto() {
        if (autoRunning) StopAuto();
        else StartAuto();
    }
    public void StartAuto() {
        if (autoRunning) return;
        autoRunning = true;
        autoNextTick = Time.time + Mathf.Max(0f, autoFirstDelay);
    }
    public void StopAuto() {
        autoRunning = false;
    }

    [ContextMenu("Test Next")]
    void CtxNext() => Next();

    // ---------- 切换核心 ----------
    void SwitchTo(int to) {
        var A = caches[current];
        var B = caches[to];
        if (A == null || B == null) return;

        if (activeTween.isAlive) activeTween.Stop();

        // 入场前把 B 激活并设初态
        B.go.SetActive(true);
        ResetTransform(A);
        ResetTransform(B);

        // 根据风格设置初/末状态
        Vector3 aPosStart = A.basePos, aPosEnd = A.basePos;
        Vector3 bPosStart = B.basePos, bPosEnd = B.basePos;
        Vector3 aScaleStart = A.baseScale, aScaleEnd = A.baseScale;
        Vector3 bScaleStart = B.baseScale, bScaleEnd = B.baseScale;
        float aAlphaStart = 1f, aAlphaEnd = 0f;
        float bAlphaStart = 0f, bAlphaEnd = 1f;

        switch (style) {
            case Style.CrossFade:
                aScaleEnd = A.baseScale * outScale;
                bScaleStart = B.baseScale * inStartScale;
                break;
            case Style.PushLeft:
                aPosEnd = A.basePos + new Vector3(-pushDistance, 0, 0);
                bPosStart = B.basePos + new Vector3(+pushDistance, 0, 0);
                break;
            case Style.PushRight:
                aPosEnd = A.basePos + new Vector3(+pushDistance, 0, 0);
                bPosStart = B.basePos + new Vector3(-pushDistance, 0, 0);
                break;
            case Style.ZoomCross:
                aScaleEnd = A.baseScale * outScale;
                bScaleStart = B.baseScale * inStartScale;
                break;
        }

        // 先把 B 放到起点状态（位置/缩放/透明度）
        B.tr.localPosition = bPosStart;
        B.tr.localScale = bScaleStart;
        SetGroupAlpha(B, bAlphaStart);

        isSwitching = true;
        activeTween = Tween.Custom(0f, 1f, duration,
            onValueChange: (u) => {
                float k = Mathf.Clamp01(easing.Evaluate(u));
                // A 出场
                A.tr.localPosition = Vector3.LerpUnclamped(aPosStart, aPosEnd, k);
                A.tr.localScale    = Vector3.LerpUnclamped(aScaleStart, aScaleEnd, k);
                SetGroupAlpha(A, Mathf.LerpUnclamped(aAlphaStart, aAlphaEnd, k));
                // B 入场
                B.tr.localPosition = Vector3.LerpUnclamped(bPosStart, bPosEnd, k);
                B.tr.localScale    = Vector3.LerpUnclamped(bScaleStart, bScaleEnd, k);
                SetGroupAlpha(B, Mathf.LerpUnclamped(bAlphaStart, bAlphaEnd, k));
            }
        ).OnComplete(() => {
            // 收尾：A 还原并隐藏；B 归位
            ResetTransform(A); SetGroupAlpha(A, 1f); A.go.SetActive(false);
            ResetTransform(B); SetGroupAlpha(B, 1f); B.go.SetActive(true);
            isSwitching = false;
            current = to;
        });
    }

    // ---------- 工具 ----------
    void ResetTransform(Cache c) {
        c.tr.localPosition = c.basePos;
        c.tr.localScale = c.baseScale;
    }
    void SetGroupAlpha(Cache c, float a01) {
        var a = Mathf.Clamp01(a01);
        for (int i = 0; i < c.srs.Length; i++) {
            var sr = c.srs[i];
            var col = sr.color;
            col.a = c.baseAlpha[i] * a;
            sr.color = col;
        }
    }
}
