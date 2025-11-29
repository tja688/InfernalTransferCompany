using UnityEngine;

/// <summary>
/// UI Canvas 持久化工具。
/// 确保挂载该脚本的对象不会随着场景变更而销毁。
/// 如果遇到重复对象，则摧毁新的那个，保留老的。
/// </summary>
[RequireComponent(typeof(Canvas))]
public class PersistentCanvas : MonoBehaviour
{
    private static PersistentCanvas _instance;

    /// <summary>
    /// 单例实例
    /// </summary>
    public static PersistentCanvas Instance => _instance;

    private Canvas _canvas;

    void Awake()
    {
        // 检查是否已存在实例
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"检测到重复的 PersistentCanvas 对象 '{gameObject.name}'，将销毁新对象并保留原有实例。", this);
            Destroy(gameObject);
            return;
        }

        // 设置为单例实例
        _instance = this;

        // 获取 Canvas 组件
        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError($"{name}: 未找到 Canvas 组件！", this);
            return;
        }

        // 确保对象不会随场景变更而销毁
        DontDestroyOnLoad(gameObject);

        Debug.Log($"PersistentCanvas '{gameObject.name}' 已设置为持久化对象。", this);
    }

    void OnDestroy()
    {
        // 如果当前实例被销毁，清除单例引用
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 获取关联的 Canvas 组件
    /// </summary>
    public Canvas Canvas => _canvas;

#if UNITY_EDITOR
    void OnValidate()
    {
        // 在编辑器中验证 Canvas 组件是否存在
        if (_canvas == null)
        {
            _canvas = GetComponent<Canvas>();
        }

        if (_canvas == null)
        {
            Debug.LogWarning($"{name}: PersistentCanvas 需要 Canvas 组件！", this);
        }
    }
#endif
}









