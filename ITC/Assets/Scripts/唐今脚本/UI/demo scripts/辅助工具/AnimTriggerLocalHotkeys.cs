using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 绑定在“单个动画对象”上的通用 Trigger 热键器：
/// - 自动获取本对象（或子节点）的 Animator；
/// - 在 Inspector 中配置任意多条（Trigger名 ↔ 快捷键）；
/// - 按键即在本 Animator 上 SetTrigger(同名)；
/// - 默认提供 trigger ↔ Space；支持可选修饰键/长按连发。
/// </summary>
[DisallowMultipleComponent]
public class AnimTriggerLocalHotkeys : MonoBehaviour
{
    [System.Serializable]
    public class TriggerBinding {
        [Tooltip("Animator 中 Trigger 参数的名字（区分大小写）")]
        public string triggerName = "Start";

        [Tooltip("触发的按键")]
        public KeyCode key = KeyCode.Space;

        [Header("可选修饰键（需全部满足）")]
        public bool requireCtrl = false;
        public bool requireAlt  = false;
        public bool requireShift= false;

        [Header("长按设置")]
        [Tooltip("长按时是否以固定间隔反复触发")]
        public bool repeatWhileHeld = false;
        [Tooltip("repeatWhileHeld 为 true 时的触发间隔（秒）")]
        public float repeatInterval = 0.12f;

        // 运行时节流
        [System.NonSerialized] public float nextRepeatTime = 0f;
    }

    [Header("目标 Animator（留空则自动寻找）")]
    public Animator targetAnimator;

    [Tooltip("若未手动指定 Animator，则在子层级中查找（true）或仅在本节点查找（false）")]
    public bool findInChildrenIfNull = true;

    [Header("快捷键绑定")]
    public List<TriggerBinding> bindings = new List<TriggerBinding> {
        new TriggerBinding { triggerName = "trigger", key = KeyCode.Space }
    };

    [Header("日志")]
    [Tooltip("触发成功时在 Console 打印")]
    public bool logOnTrigger = false;
    [Tooltip("当 Animator 不存在该 Trigger 参数时是否警告")]
    public bool warnIfParamMissing = true;

    HashSet<string> _triggerParams; // 缓存本 Animator 的 Trigger 参数名集合

    void Awake() {
        EnsureAnimator();
        BuildTriggerCache();
    }

    void EnsureAnimator() {
        if (targetAnimator == null) {
            targetAnimator = findInChildrenIfNull
                ? GetComponentInChildren<Animator>(true)
                : GetComponent<Animator>();
        }
        if (targetAnimator == null) {
            Debug.LogWarning($"[AnimTriggerLocalHotkeys] {name} 未找到 Animator。");
        }
    }

    void BuildTriggerCache() {
        _triggerParams = new HashSet<string>();
        if (targetAnimator == null) return;
        try {
            foreach (var p in targetAnimator.parameters) {
                if (p.type == AnimatorControllerParameterType.Trigger) {
                    _triggerParams.Add(p.name);
                }
            }
        } catch { /* 某些运行时替换控制器的情况可能抛异常，忽略 */ }
    }

    void Update() {
        // 运行时若换了 Animator（或一开始没找到），尝试补救
        if (targetAnimator == null) {
            EnsureAnimator();
            if (targetAnimator != null) BuildTriggerCache();
        }

        foreach (var b in bindings) {
            if (b == null || string.IsNullOrEmpty(b.triggerName)) continue;

            bool modifiersOK =
                (!b.requireCtrl  || Input.GetKey(KeyCode.LeftControl)  || Input.GetKey(KeyCode.RightControl)) &&
                (!b.requireAlt   || Input.GetKey(KeyCode.LeftAlt)      || Input.GetKey(KeyCode.RightAlt)) &&
                (!b.requireShift || Input.GetKey(KeyCode.LeftShift)    || Input.GetKey(KeyCode.RightShift));

            if (!modifiersOK) continue;

            if (!b.repeatWhileHeld) {
                if (Input.GetKeyDown(b.key)) Trigger(b.triggerName);
            } else {
                if (Input.GetKey(b.key)) {
                    if (Time.unscaledTime >= b.nextRepeatTime) {
                        b.nextRepeatTime = Time.unscaledTime + Mathf.Max(0.01f, b.repeatInterval);
                        Trigger(b.triggerName);
                    }
                }
                if (Input.GetKeyUp(b.key)) b.nextRepeatTime = 0f;
            }
        }
    }

    /// <summary>在本对象的 Animator 上触发指定 Trigger 名（若存在）</summary>
    public void Trigger(string triggerName) {
        if (targetAnimator == null || string.IsNullOrEmpty(triggerName)) return;

        // 如果控制器换过，尝试实时刷新一次参数表
        if (_triggerParams == null || _triggerParams.Count == 0) BuildTriggerCache();

        bool exists = _triggerParams?.Contains(triggerName) ?? false;
        if (!exists) {
            // 再做一次“保守尝试”（某些运行时替换导致缓存不准）
            exists = HasTriggerParam(targetAnimator, triggerName);
            if (exists) _triggerParams.Add(triggerName);
        }

        if (exists) {
            targetAnimator.ResetTrigger(triggerName); // 清理残留
            targetAnimator.SetTrigger(triggerName);
            if (logOnTrigger) Debug.Log($"[AnimTriggerLocalHotkeys] {name} -> SetTrigger('{triggerName}')");
        } else if (warnIfParamMissing) {
            Debug.LogWarning($"[AnimTriggerLocalHotkeys] {name} 的 Animator 不存在 Trigger 参数 '{triggerName}'。");
        }
    }

    bool HasTriggerParam(Animator a, string name) {
        try {
            foreach (var p in a.parameters) {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == name) return true;
            }
        } catch {}
        return false;
    }

    // 便于 Timeline Signal / UI Button 直接调用
    public void Trigger_default() => Trigger("trigger");
    public void Trigger_custom(string triggerName) => Trigger(triggerName);

    // 若运行时切换了 Animator Controller，可手动调用刷新
    public void RescanParameters() => BuildTriggerCache();

    // 右键菜单测试
    [ContextMenu("Test Trigger (default)")]
    void CtxTest() => Trigger("trigger");
}
