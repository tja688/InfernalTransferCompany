using System;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 面向「场景内」MMF_Player 对象的动效管理器。
/// - 通过 ID 映射到具体的 MMF_Player（可以是场景中的对象，也可以是预制体）
/// - 由舞台管理器等系统按 ID 调用，不再要求把动效做成独立的 ScriptableObject 资产
/// </summary>
[DisallowMultipleComponent]
public class StageSceneEffectStore : MonoBehaviour
{
    [Serializable]
    public class PlayerEntry
    {
        [Tooltip("用于在剧情 / 代码中调用的动效 ID")]
        public string PlayerID;

        [Tooltip("对应的 MMF_Player，可以是场景中的对象，也可以是项目中的预制体")]
        public MMF_Player PlayerPrefab;
    }

    [Tooltip("场景内可用的动效清单，通过 ID 进行映射和调用")]
    public List<PlayerEntry> Players = new List<PlayerEntry>();

    private Dictionary<string, MMF_Player> _playerMap;

    /// <summary>
    /// 构建 ID -> MMF_Player 的查找表。
    /// 建议在 Awake / Start 中调用，StageManager 已在 Awake 中调用。
    /// </summary>
    public void Initialize()
    {
        if (_playerMap == null)
        {
            _playerMap = new Dictionary<string, MMF_Player>();
        }
        else
        {
            _playerMap.Clear();
        }

        foreach (var entry in Players)
        {
            if (!string.IsNullOrEmpty(entry.PlayerID) && entry.PlayerPrefab != null)
            {
                if (!_playerMap.ContainsKey(entry.PlayerID))
                {
                    _playerMap.Add(entry.PlayerID, entry.PlayerPrefab);
                }
                else
                {
                    Debug.LogWarning($"[StageSceneEffectStore] Duplicate PlayerID found: {entry.PlayerID}");
                }
            }
        }
    }

    /// <summary>
    /// 通过 ID 获取一个 MMF_Player 模板。
    /// StageManager 会使用这个模板进行 Instantiate，并在运行时注入目标对象。
    /// </summary>
    public MMF_Player GetPlayerPrefab(string playerID)
    {
        if (_playerMap == null || _playerMap.Count == 0)
        {
            Initialize();
        }

        if (_playerMap.TryGetValue(playerID, out var player))
        {
            return player;
        }

        // 兜底：如果字典还没同步（例如在编辑器下动态修改了列表），就直接遍历列表
        foreach (var entry in Players)
        {
            if (entry.PlayerID == playerID) return entry.PlayerPrefab;
        }

        return null;
    }
}

/// <summary>
/// 兼容旧代码 / 旧资产用的壳类。
/// 之后可以在工程里把引用全部替换为 StageSceneEffectStore，再安全删除本类。
/// </summary>
[Obsolete("StageFeelPlayerStore 已重命名为 StageSceneEffectStore，请在引用处替换为新类型。")]
public class StageFeelPlayerStore : StageSceneEffectStore
{
}

