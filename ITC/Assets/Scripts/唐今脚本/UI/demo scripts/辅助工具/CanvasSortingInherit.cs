using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class CanvasSortingInherit : MonoBehaviour
{
    [Tooltip("保持 Override Sorting 开着（需要自定义 order），仅同步 Layer/Camera")]
    public bool keepOverrideSorting = true;

    [Tooltip("找不到父 Canvas 时，默认用哪个 Sorting Layer 名称")]
    public string fallbackLayerName = "UI";

    Canvas self;

    void OnEnable()
    {
        self = GetComponent<Canvas>();
        Apply();
    }

    void OnTransformParentChanged() => Apply();
    void OnValidate() { self = GetComponent<Canvas>(); Apply(); }

    void Apply()
    {
        if (!self) return;

        // 你这些覆盖 Canvas 通常需要 overrideSorting=true 才能单独控制 order
        self.overrideSorting = keepOverrideSorting;

        // 同步父 Canvas 的 sortingLayer / camera
        var parent = GetComponentInParent<Canvas>();
        if (parent && parent != self)
        {
            self.sortingLayerID = parent.sortingLayerID;
            self.sortingLayerName = parent.sortingLayerName;   // 方便在 Inspector 里也看到 UI
            self.worldCamera = parent.worldCamera;             // 屏摄/URP Overlay 时很关键
            // NOTE: 不改 self.sortingOrder，让你自己在 Inspector 填的 order 生效
        }
        else
        {
            // 没有父 Canvas（比如单独打开预制体）就使用回退 Layer
            self.sortingLayerName = fallbackLayerName;         // 若无名为 UI 的层，请在项目里先建
            // worldCamera 留空也可以（Overlay 模式），或按需指定
        }
    }
}