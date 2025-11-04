using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class AlwaysTopCanvas : MonoBehaviour {
    public int extra = 1; // 永远比最大再高这么多
    Canvas c;
    void Awake() {
        c = GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay; // 继续用“覆盖”
        c.overrideSorting = true; // 根Canvas也可设为true
        Bump();
    }
    void OnEnable() => Bump();
    public void Bump() {
        int max = 0;
        foreach (var other in FindObjectsOfType<Canvas>(true)) {
            if (other == c) continue;
            if (other.sortingOrder > max) max = other.sortingOrder;
        }
        c.sortingOrder = max + extra;
    }
}