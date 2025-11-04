using UnityEngine;
using Pathfinding;

public class ClickToMove2D : MonoBehaviour {
    public Transform target;

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            var wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            wp.z = 0f;
            var nn = AstarPath.active.GetNearest(wp, NNConstraint.Default);
            target.position = nn.position; // 合法可行走点
        }
    }
}