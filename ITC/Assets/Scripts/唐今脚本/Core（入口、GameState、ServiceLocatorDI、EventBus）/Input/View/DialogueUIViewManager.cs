// DialogueUIViewManager.cs (Corrected Version)
using UnityEngine;
using PixelCrushers.DialogueSystem; // 引用DS命名空间

/// <summary>
/// 监听Dialogue System的事件，并根据事件来Push/Pop对应的FocusScope。
/// 这个脚本应该挂载在Dialogue System UI预制件的根对象上。
/// </summary>
public class DialogueUIViewManager : MonoBehaviour
{
    [Tooltip("将你的回复选项面板（挂载了FocusScope的那个）拖到这里")]
    public FocusScope responseMenuScope;

    // 【新增】: 缓存场景中的DialogueSystemEvents组件实例
    private DialogueSystemEvents _dialogueSystemEvents;

    private void Awake()
    {
        // 在唤醒时，查找并缓存DialogueSystemEvents的实例
        _dialogueSystemEvents = FindObjectOfType<DialogueSystemEvents>();
        if (_dialogueSystemEvents == null)
        {
            Debug.LogError("场景中找不到 DialogueSystemEvents 组件！请确保它已添加到Dialogue Manager对象上。", this);
        }
    }

    private void OnEnable()
    {
        if (_dialogueSystemEvents == null) return;

        // 【修正】: 通过获取到的实例，使用AddListener来订阅UnityEvent事件
        _dialogueSystemEvents.conversationEvents.onConversationResponseMenu.AddListener(OnResponseMenu);
        _dialogueSystemEvents.conversationEvents.onConversationLine.AddListener(OnConversationLine);
        _dialogueSystemEvents.conversationEvents.onConversationEnd.AddListener(OnConversationEnd);
    }

    private void OnDisable()
    {
        if (_dialogueSystemEvents == null) return;

        // 【修正】: 相应地，使用RemoveListener来取消订阅
        _dialogueSystemEvents.conversationEvents.onConversationResponseMenu.RemoveListener(OnResponseMenu);
        _dialogueSystemEvents.conversationEvents.onConversationLine.RemoveListener(OnConversationLine);
        _dialogueSystemEvents.conversationEvents.onConversationEnd.RemoveListener(OnConversationEnd);
    }

    // 当回复菜单出现时
    private void OnResponseMenu(Response[] responses)
    {
        if (responseMenuScope != null)
        {
            DialogueStateManager.Instance.PushScope(responseMenuScope);
        }
    }

    // 当显示下一句台词时（意味着玩家已经做出了选择）
    private void OnConversationLine(Subtitle subtitle)
    {
        if (responseMenuScope != null)
        {
            DialogueStateManager.Instance.PopScope();
        }
    }

    // 当对话结束时
    private void OnConversationEnd(Transform actor)
    {
        if (responseMenuScope != null)
        {
            DialogueStateManager.Instance.PopScope();
        }
    }
}