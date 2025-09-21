using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands; // Dialogue System 命名空间

/// <summary>
/// 自定义 Sequence 命令: ChangeScene("目标场景名")
/// 在对话指令里写 ChangeScene(MyScene) 就会调用这里
/// </summary>
public class SequencerCommandChangeScene : SequencerCommand
{
    public void Start()
    {
        // 参数0: 目标场景名字
        string sceneName = GetParameter(0);

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SequencerCommandChangeScene] 未提供场景名！");
            Stop();
            return;
        }

        if (TransitionManager2D.Instance != null)
        {
            TransitionManager2D.Instance.StartTransition(sceneName);
        }
        else
        {
            Debug.LogError("[SequencerCommandChangeScene] 找不到 TransitionManager2D 实例！");
        }

        Stop(); // 命令立即结束，转场异步执行
    }
}