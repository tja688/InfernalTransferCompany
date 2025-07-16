using UnityEngine;
using PixelCrushers.DialogueSystem;

// 这个脚本可以挂载到任何对象上，比如玩家或者Dialogue Manager
public class DialogueDebugLogger : MonoBehaviour
{
    // 当对话系统准备显示一句台词时，这个方法会被自动调用
    public void OnConversationLine(Subtitle subtitle)
    {
        // 如果subtitle为空，直接返回，避免报错
        if (subtitle == null) return;

        Debug.Log("--- 对话日志 ---");
        Debug.Log("台词内容: " + subtitle.formattedText.text);

        // 检查说话人的信息是否正确
        if (subtitle.speakerInfo != null)
        {
            Debug.Log("数据库中指定的说话人: " + subtitle.speakerInfo.Name + " [ID=" + subtitle.speakerInfo.id + "]");

            // 【【【 最关键的诊断信息 】】】
            // 检查系统是否在场景中找到了这个说话人对应的GameObject
            if (subtitle.speakerInfo.transform != null)
            {
                Debug.Log("<color=green>成功: 系统在场景中找到了说话人对应的Transform，它的名字是: " + subtitle.speakerInfo.transform.name + "</color>");
            }
            else
            {
                Debug.Log("<color=red>失败: 系统没能在场景中找到这个说话人对应的Transform！请检查该角色是否在场景中，并且身上挂载了正确的Dialogue Actor组件。</color>");
            }
        }
        else
        {
            Debug.LogWarning("警告: 这句台词的说话人信息(speakerInfo)为空！");
        }
        Debug.Log("--- 日志结束 ---");
    }
}