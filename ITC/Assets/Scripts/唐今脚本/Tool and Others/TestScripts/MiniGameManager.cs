using PixelCrushers.DialogueSystem;
using UnityEngine;

public class MiniGameManager : MonoBehaviour 
{
    public void OnMiniGameWin()
    {
        // 处理你的游戏逻辑...
        Debug.Log("小游戏胜利！");

        // 核心代码：告诉 DS，"MiniGameDone" 这个信号来了
        // DS 收到后，会立刻结束那个节点的 WaitForMessage，自动跳到下一句
        Sequencer.Message("MiniGameDone");
    }
}