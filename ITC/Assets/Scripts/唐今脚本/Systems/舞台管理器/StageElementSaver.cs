using UnityEngine;
using PixelCrushers;

/// <summary>
/// 抽象基类：用于保存 StageElement 自定义子状态或内部 FSM。
/// 子类只需实现 Capture/Restore 方法并返回任意字符串（JSON、枚举名等），即可在存档体系中独立保存逻辑状态。
/// </summary>
[DisallowMultipleComponent]
public abstract class StageElementSaver : Saver
{
    [Tooltip("可选：显式指定要保存的 StageElement。不填则自动获取同节点上的组件。")]
    public StageElement Target;

    protected virtual void Awake()
    {
        EnsureTargetBinding();
    }

    public override string RecordData()
    {
        var stageElement = GetBoundElement();
        if (stageElement == null || string.IsNullOrEmpty(stageElement.StageElementID))
        {
            return string.Empty;
        }

        var data = new StageElementSaverData
        {
            ElementID = stageElement.StageElementID,
            Payload = CaptureInternalState()
        };

        return SaveSystem.Serialize(data);
    }

    public override void ApplyData(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return;
        }

        var data = SaveSystem.Deserialize<StageElementSaverData>(s);
        if (data == null)
        {
            return;
        }

        var resolved = ResolveTargetElement(data.ElementID);
        if (resolved == null)
        {
            return;
        }

        Target = resolved;
        RestoreInternalState(data.Payload);
    }

    protected virtual StageElement ResolveTargetElement(string elementId)
    {
        var bound = GetBoundElement();
        if (bound != null && bound.StageElementID == elementId)
        {
            return bound;
        }

        if (StageManager.Instance != null && !string.IsNullOrEmpty(elementId))
        {
            var element = StageManager.Instance.GetElement(elementId);
            if (element != null)
            {
                return element;
            }
        }

        return GetBoundElement();
    }

    protected StageElement GetBoundElement()
    {
        EnsureTargetBinding();
        return Target;
    }

    private void EnsureTargetBinding()
    {
        if (Target == null)
        {
            Target = GetComponent<StageElement>();
        }
    }

    /// <summary>
    /// 返回一个可序列化的字符串，用于描述当前子状态/自定义数据。
    /// </summary>
    protected abstract string CaptureInternalState();

    /// <summary>
    /// 根据存档内容恢复内部状态。
    /// </summary>
    protected abstract void RestoreInternalState(string payload);
}

[System.Serializable]
public class StageElementSaverData
{
    public string ElementID;
    public string Payload;
}

