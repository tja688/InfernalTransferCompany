using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D 立绘“纵深旋转”切换（独立管理器版）
/// - 不改 sprite/材质/颜色，仅控制 Transform 旋转与 SpriteRenderer 显隐
/// - 列表中的每个对象都是独立个体（场景中的 GameObject + SpriteRenderer）
/// - 初始：index 0 为默认展示，朝向 0°；其余全部隐藏并设置为 90°（竖线）
/// - 切换：当前 0→90 隐藏；下一个 90→0 显示；不循环
/// </summary>
public class PortraitSpinSwitcher2D : MonoBehaviour
{
    [Header("场景中的立绘对象（第一个视为默认展示）")]
    public List<SpriteRenderer> portraits = new List<SpriteRenderer>();

    [Header("参数")]
    [Tooltip("单侧旋转时长（秒）：出场 0→90 与 入场 90→0 各用这段时间")]
    public float halfDuration = 0.12f;
    [Tooltip("节奏曲线（0→1 对应 0°→90° 或 90°→0°）")]
    public AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("在90°附近的轻微挤压错觉")]
    [Range(0f, 0.4f)] public float squashAmount = 0.08f;
    [Tooltip("切换过程中是否屏蔽再次触发")]
    public bool blockWhileSwitching = true;

    [Header("热键")]
    public KeyCode nextKey = KeyCode.Space;
    public bool enableNumberKeysDirectJump = true; // 1..9 直跳到对应索引（1=0号）

    int _current = 0;
    bool _isSwitching = false;

    void Awake()
    {
        if (portraits == null || portraits.Count == 0)
        {
            Debug.LogError("[PortraitSpinSwitcher2D] 请在 portraits 列表里添加至少一个 SpriteRenderer。");
            enabled = false; return;
        }

        // 初始化：第一个正面显示，其余隐藏且转为90°
        for (int i = 0; i < portraits.Count; i++)
        {
            var sr = portraits[i];
            if (!sr) continue;

            var t = sr.transform;
            var e = t.localEulerAngles;

            if (i == 0)
            {
                e.y = 0f;
                t.localEulerAngles = e;
                sr.enabled = true; // 显示
            }
            else
            {
                e.y = 90f;
                t.localEulerAngles = e;
                sr.enabled = false; // 隐藏（保留激活以便后续旋转）
            }
        }
        _current = 0;
    }

    void Update()
    {
        if (nextKey != KeyCode.None && Input.GetKeyDown(nextKey))
            Next();

        if (enableNumberKeysDirectJump)
        {
            for (int i = 0; i < 9; i++)
            {
                var key = KeyCode.Alpha1 + i;
                if (Input.GetKeyDown(key))
                {
                    int idx = i; // 1键→0号，2键→1号...
                    if (idx < portraits.Count)
                        JumpTo(idx);
                }
            }
        }
    }

    /// <summary>切到下一个（不循环）</summary>
    public void Next()
    {
        if (blockWhileSwitching && _isSwitching) return;
        if (_current >= portraits.Count - 1) return; // 到最后一个了

        int next = _current + 1;
        StartCoroutine(CoSwitch(_current, next));
        _current = next;
    }

    /// <summary>直接跳到指定索引（不循环）</summary>
    public void JumpTo(int index)
    {
        if (blockWhileSwitching && _isSwitching) return;
        if (index < 0 || index >= portraits.Count) return;
        if (index == _current) return;

        StartCoroutine(CoSwitch(_current, index));
        _current = index;
    }

    IEnumerator CoSwitch(int fromIdx, int toIdx)
    {
        _isSwitching = true;

        var fromSR = SafeGet(fromIdx);
        var toSR   = SafeGet(toIdx);
        if (!fromSR || !toSR)
        {
            _isSwitching = false; yield break;
        }

        Transform fromT = fromSR.transform;
        Transform toT   = toSR.transform;

        // 出场/入场初始状态对齐
        SetY(fromT, 0f);
        SetY(toT, 90f);
        toSR.enabled = true; // 先让下一个可见（但还是90°侧面，看不到）

        Vector3 fromBase = fromT.localScale;
        Vector3 toBase   = toT.localScale;

        // --- 出场：0→90 ---
        float t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / halfDuration);
            float k = easing.Evaluate(u);
            float yOut = Mathf.Lerp(0f, 90f, k);
            SetY(fromT, yOut);

            if (squashAmount > 0f)
            {
                float cos = Mathf.Abs(Mathf.Cos(yOut * Mathf.Deg2Rad)); // 1→0
                float squash = 1f + (1f - cos) * squashAmount;
                fromT.localScale = new Vector3(fromBase.x, fromBase.y * squash, fromBase.z);
            }
            yield return null;
        }
        // 出场完成：隐藏上一个，复位scale
        fromSR.enabled = false;
        fromT.localScale = fromBase;

        // --- 入场：90→0 ---
        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / halfDuration);
            float k = easing.Evaluate(u);
            float yIn = Mathf.Lerp(90f, 0f, k);
            SetY(toT, yIn);

            if (squashAmount > 0f)
            {
                float cos = Mathf.Abs(Mathf.Cos(yIn * Mathf.Deg2Rad)); // 0→1
                float squash = 1f + (1f - cos) * squashAmount;
                toT.localScale = new Vector3(toBase.x, toBase.y * squash, toBase.z);
            }
            yield return null;
        }
        // 入场完成：复位scale、角度对齐
        SetY(toT, 0f);
        toT.localScale = toBase;

        _isSwitching = false;
    }

    // --- helpers ---
    SpriteRenderer SafeGet(int idx)
    {
        if (idx < 0 || idx >= portraits.Count) return null;
        return portraits[idx];
    }
    void SetY(Transform t, float y)
    {
        var e = t.localEulerAngles; e.y = y; t.localEulerAngles = e;
    }
}
