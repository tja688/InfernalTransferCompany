using UnityEngine;
using PixelCrushers.DialogueSystem;

public class DSGlobalMessageBridge : MonoBehaviour {
    
    // 全局事件：任何地方都可订阅
    public static event System.Action<Transform> OnConvStart;
    public static event System.Action<Subtitle> OnConvLine;
    public static event System.Action<Transform> OnConvEnd;

    // 这些方法名必须与 DS 的 Unity Messages 匹配
    // DS 会在对话参与者以及 Dialogue Manager 上调用它们
// 在文件顶部或类内增加一个开关
    static bool DEBUG_LOG = false;

// 修改三个 Unity Message 为带日志版本：
    void OnConversationStart(Transform actor) {
        if (DEBUG_LOG) Debug.Log($"[Bridge] OnConversationStart from={actor?.name} frame={Time.frameCount}", this);
        OnConvStart?.Invoke(actor);
    }

    void OnConversationLine(Subtitle subtitle) {
        if (DEBUG_LOG) {
            var convId = subtitle?.dialogueEntry?.conversationID ?? -1;
            var entryId = subtitle?.dialogueEntry?.id ?? -1;
            Debug.Log($"[Bridge] OnConversationLine conv={convId} entry={entryId} speaker={subtitle?.speakerInfo?.Name} text=\"{subtitle?.formattedText?.text}\" frame={Time.frameCount}", this);
        }
        OnConvLine?.Invoke(subtitle);
    }

    void OnConversationEnd(Transform actor) {
        if (DEBUG_LOG) Debug.Log($"[Bridge] OnConversationEnd from={actor?.name} frame={Time.frameCount}", this);
        OnConvEnd?.Invoke(actor);
    }


    // 便捷：在运行时确保桥接器存在于 Dialogue Manager 上
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureBridge() {
        var dm = DialogueManager.instance;
        if (dm == null) return;
        var go = dm.gameObject;
        if (!go.GetComponent<DSGlobalMessageBridge>()) go.AddComponent<DSGlobalMessageBridge>();
    }
}