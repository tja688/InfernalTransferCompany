using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 挂在【将被激活/切换】的对象本身上也可以运行（即便该对象一开始是失活的）。
/// 脚本会在运行时自动创建一个全局管理器，扫描场景里所有 HotkeySelfActivator（包含失活）并监听按键。
/// 默认功能：按下设定按键 → 激活本对象；也可选择“切换显隐（Toggle）”或指定修饰键。
/// </summary>
[DisallowMultipleComponent]
public class HotkeySelfActivator : MonoBehaviour {
    [Header("快捷键")]
    public KeyCode key = KeyCode.T;
    public bool requireCtrl  = false;
    public bool requireAlt   = false;
    public bool requireShift = false;

    [Header("行为")]
    [Tooltip("为 true 时按键切换显隐；为 false 时只做激活（设为 Active=true）。")]
    public bool toggleMode = false;
    [Tooltip("只在对象当前为失活时才响应（勾上可避免重复触发）。对 Toggle 模式无效。")]
    public bool onlyWhenInactive = true;

    [Header("可选：不操作自身而是操作这个对象")]
    public GameObject overrideTarget; // 留空则操作本 gameObject

    // —— 管理器 —— //
    static Manager _manager;

    void OnEnable() {
        // 目标可能是失活，这个 OnEnable 只有在“对象被激活时”才会被调用；
        // 因此不要依赖它注册。我们在 Manager 内部每次刷新时用 includeInactive 找所有组件。
        EnsureManagerExists();
    }

    void OnValidate() {
#if UNITY_EDITOR
        // 在编辑器修改参数时也确保管理器存在（方便进入 Play 后立即生效）
        if (!Application.isPlaying) return;
        EnsureManagerExists();
        _manager?.MarkDirty(); // 让管理器下次刷新
#endif
    }

    void EnsureManagerExists() {
        if (_manager != null) return;
        var go = new GameObject("[HotkeySelfActivator.Manager]");
        go.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(go);
        _manager = go.AddComponent<Manager>();
    }

    // ====== 全局管理器 ======
    private class Manager : MonoBehaviour {
        readonly List<HotkeySelfActivator> list = new List<HotkeySelfActivator>();
        float nextRefreshTime = 0f;
        bool dirty = true;

        void Awake() {
            SceneManager.sceneLoaded += (_, __) => { dirty = true; };
            SceneManager.activeSceneChanged += (_, __) => { dirty = true; };
            Refresh(); // 初始扫描一次
        }

        public void MarkDirty() => dirty = true;

        void Refresh() {
            list.Clear();
            // Unity 2020+：FindObjectsOfType(includeInactive:true) 能找到失活对象上的组件
            list.AddRange(FindObjectsOfType<HotkeySelfActivator>(true));
            dirty = false;
            nextRefreshTime = Time.unscaledTime + 1.0f; // 1 秒后允许下一次定时刷新
        }

        bool ModifiersOk(HotkeySelfActivator a) {
            if (a.requireCtrl  && !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) return false;
            if (a.requireAlt   && !(Input.GetKey(KeyCode.LeftAlt)     || Input.GetKey(KeyCode.RightAlt)))     return false;
            if (a.requireShift && !(Input.GetKey(KeyCode.LeftShift)   || Input.GetKey(KeyCode.RightShift)))   return false;
            return true;
        }

        void Update() {
            // 轻量刷新：场景切换/显式标记 或 每秒最多一次
            if (dirty || Time.unscaledTime >= nextRefreshTime) Refresh();

            // 遍历所有目标，监听各自的按键
            for (int i = 0; i < list.Count; i++) {
                var a = list[i];
                if (a == null) { dirty = true; continue; }

                if (Input.GetKeyDown(a.key) && ModifiersOk(a)) {
                    var target = a.overrideTarget ? a.overrideTarget : a.gameObject;

                    if (a.toggleMode) {
                        target.SetActive(!target.activeSelf);
                    } else {
                        if (!a.onlyWhenInactive || !target.activeSelf) {
                            target.SetActive(true);
                        }
                    }
                }
            }
        }
    }
}
