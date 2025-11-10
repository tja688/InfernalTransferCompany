using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic; // 引入列表和哈希集

// --- 为了在 Inspector 中显示带参数的事件，需要定义一个可序列化的类 ---
[System.Serializable]
public class UICollidableEvent : UnityEvent<UICollidable> { }