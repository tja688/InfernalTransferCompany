using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

/// 使用：PlayEntrance(target, action[, wait])
///  target: 目标对象名或关键字(speaker/listener/player/this等)
///  action: show | hide | toggle (也接受 open/close)
///  wait  : true | false (默认false；true则等 EntranceMotion.IsAnimating==false 再继续)
public class SequencerCommandPlayEntrance : SequencerCommand
{
    public void Start()
    {
        // 读取参数
        string p0 = GetParameter(0);
        string p1 = GetParameter(1);
        string p2 = GetParameter(2);

        // 兼容把动作写在第一个参数的情况：PlayEntrance(show, true)
        bool p0IsAction = IsAction(p0);
        Transform target = p0IsAction ? GetSubject(1) : GetSubject(0);
        string actionStr = (p0IsAction ? p0 : p1);

        if (target == null)
        {
            Debug.LogError("[PlayEntrance] 未找到目标对象。参数0应为对象名/关键字（如 speaker / listener / player）。");
            Stop();
            return;
        }

        var motion = target.GetComponent<EntranceMotion>();
        if (motion == null)
        {
            Debug.LogError($"[PlayEntrance] 在目标 {target.name} 上未找到 EntranceMotion 组件。");
            Stop();
            return;
        }

        // 解析动作
        Action action = ParseAction(actionStr);
        if (action == Action.Unknown)
        {
            Debug.LogWarning("[PlayEntrance] 第二个参数应为 show/hide/toggle（或 open/close）。默认用 show。");
            action = Action.Show;
        }

        bool wait = ParseBool(p0IsAction ? p1 : p2, false);

        // 执行动作
        switch (action)
        {
            case Action.Show:   motion.Show();   break;
            case Action.Hide:   motion.Hide();   break;
            case Action.Toggle: motion.Toggle(); break;
        }

        if (wait)
        {
            // 等到动画完成再继续对话
            StartCoroutine(WaitForMotion(motion));
        }
        else
        {
            // 立即结束该 sequence 命令
            Stop();
        }
    }

    System.Collections.IEnumerator WaitForMotion(EntranceMotion motion)
    {
        // 等待“正在动画”结束
        while (motion != null && motion.IsAnimating)
            yield return null;
        Stop();
    }

    enum Action { Show, Hide, Toggle, Unknown }

    static bool IsAction(string s)
    {
        var a = s != null ? s.Trim().ToLowerInvariant() : "";
        return a == "show" || a == "hide" || a == "toggle" || a == "open" || a == "close";
    }

    static Action ParseAction(string s)
    {
        var a = s != null ? s.Trim().ToLowerInvariant() : "";
        switch (a)
        {
            case "show":
            case "open":
                return Action.Show;
            case "hide":
            case "close":
                return Action.Hide;
            case "toggle":
                return Action.Toggle;
            default:
                return Action.Unknown;
        }
    }

    static bool ParseBool(string s, bool defaultValue)
    {
        if (string.IsNullOrEmpty(s)) return defaultValue;
        var v = s.Trim().ToLowerInvariant();
        if (v == "true" || v == "1" || v == "yes" || v == "y") return true;
        if (v == "false" || v == "0" || v == "no" || v == "n") return false;
        return defaultValue;
    }
}
