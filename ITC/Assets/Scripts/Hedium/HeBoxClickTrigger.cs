using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 可点击的 BoxCollider2D 物体回调脚本范式
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class HeBoxClickTrigger : MonoBehaviour
{
    [Header("点击回调事件")]
    public UnityEvent onClicked;

    // 鼠标点击检测（左键点击）
    private void OnMouseDown()
    {
        // 调用回调事件
        onClicked?.Invoke();
    }
}
