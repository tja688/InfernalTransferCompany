using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using PixelCrushers.DialogueSystem;

public class MenuPanelNavPatcher : MonoBehaviour
{
    public ResponseNavBridge bridge;
    public Selectable externalTopEntry;
    public Selectable externalBottomEntry;

    int _lastButtonCount = -1;

    void LateUpdate()
    {
        // 每帧检查当前响应按钮数量是否变化（生成/清理）
        var responses = GetComponentsInChildren<StandardUIResponseButton>(true);
        if (responses.Length != _lastButtonCount)
        {
            _lastButtonCount = responses.Length;
            StartCoroutine(EndOfFramePatch());
        }
    }

    IEnumerator EndOfFramePatch()
    {
        yield return null; // 等一帧，确保实例化/布局完成
        var selectables = GetComponentsInChildren<StandardUIResponseButton>(true)
            .Select(rb => rb.GetComponent<Selectable>())
            .Where(s => s != null).ToArray();

        if (bridge)
        {
            bridge.topExitTarget    = externalTopEntry;
            bridge.bottomExitTarget = externalBottomEntry;
            bridge.OnResponsesBuilt(selectables);
        }
    }
}