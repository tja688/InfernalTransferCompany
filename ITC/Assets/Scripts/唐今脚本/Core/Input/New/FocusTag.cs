using System.Collections.Generic;
using PixelCrushers;
using UnityEngine;

// 挂在任何 UIPanel 上即可把它注册为某个 Key（动态实例的面板也适用）
[RequireComponent(typeof(UIPanel))]
public class FocusTag : MonoBehaviour
{
    public FocusKey key = FocusKey.Custom1;

    private UIPanel panel;
    private void Awake() => panel = GetComponent<UIPanel>();
    private void OnEnable() { if (panel != null) FocusHub.Instance?.Register(key, panel); }
    private void OnDisable(){ if (panel != null) FocusHub.Instance?.Unregister(panel); }
}