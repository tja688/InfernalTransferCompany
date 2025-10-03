// ResponseNavBridge.cs —— 菜单 ↔ 外部按钮的显式导航桥接
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ResponseNavBridge : MonoBehaviour
{
    [Header("External entries")]
    public Selectable topExitTarget;    // 例如 BtnQuickSave（从“最上继续上”要到哪）
    public Selectable bottomExitTarget; // 例如 BtnSettings（从“最下继续下”要到哪）
    public bool bidirectional = true;   // 外部按钮能否再回到菜单

    public void OnResponsesBuilt(Selectable[] responseSelectables)
    {
        if (responseSelectables == null || responseSelectables.Length == 0) return;

        var list = responseSelectables
            .Where(s => s && s.IsActive() && s.interactable)
            .OrderBy(s => s.transform.position.y) // 屏幕Y：通常y大的在上
            .ToArray();

        var bottom = list.First();
        var top    = list.Last();

        SetUpDown(top,    null,          topExitTarget);
        SetUpDown(bottom, bottomExitTarget, null);

        if (bidirectional)
        {
            // 让外部按钮也能回到菜单（例如回到顶部/底部任一）
            if (topExitTarget)    SetUpDown(topExitTarget,    top,    bottom);
            if (bottomExitTarget) SetUpDown(bottomExitTarget, top,    bottom);
        }
    }

    void SetUpDown(Selectable s, Selectable up, Selectable down)
    {
        if (!s) return;
        var nav = s.navigation;
        nav.mode = Navigation.Mode.Explicit;
        if (up)   nav.selectOnUp   = up;
        if (down) nav.selectOnDown = down;
        s.navigation = nav;
    }
}
