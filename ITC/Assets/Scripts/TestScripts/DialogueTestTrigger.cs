// 引入官方文档指定的命名空间
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// 根据官方文档，使用标准API，在按下空格键时触发指定对话。
/// </summary>
public class DialogueTestTrigger : MonoBehaviour
{
    [Tooltip("要启动的对话的标题，该标题在Dialogue Editor中定义")]
    [ConversationPopup] // 使用官方文档中提到的 [ConversationPopup] 特性，可以在Inspector中获得一个方便的下拉菜单
    public string conversationTitle;

    void Update()
    {
        // 检测玩家是否按下了空格键
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 检查标题是否为空，避免不必要的错误
            if (!string.IsNullOrEmpty(conversationTitle))
            {
                // 使用官方文档中最常用的方法来启动对话
                DialogueManager.StartConversation(conversationTitle,this.transform);
            }
            else
            {
                Debug.LogWarning("对话标题未指定，无法开始对话！", this);
            }
        }
    }
}