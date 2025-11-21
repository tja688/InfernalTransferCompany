using System;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

[CreateAssetMenu(fileName = "StageFeelPlayerStore", menuName = "ITC/Stage/StageFeelPlayerStore")]
public class StageFeelPlayerStore : ScriptableObject
{
    [Serializable]
    public class PlayerEntry
    {
        public string PlayerID;
        public MMF_Player PlayerPrefab;
    }

    public List<PlayerEntry> Players = new List<PlayerEntry>();

    private Dictionary<string, MMF_Player> _playerMap;

    public void Initialize()
    {
        _playerMap = new Dictionary<string, MMF_Player>();
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
                    Debug.LogWarning($"[StageFeelPlayerStore] Duplicate PlayerID found: {entry.PlayerID}");
                }
            }
        }
    }

    public MMF_Player GetPlayerPrefab(string playerID)
    {
        if (_playerMap == null) Initialize();

        if (_playerMap.TryGetValue(playerID, out var player))
        {
            return player;
        }

        // Fallback check in case Initialize wasn't called or list changed at runtime (editor)
        foreach (var entry in Players)
        {
            if (entry.PlayerID == playerID) return entry.PlayerPrefab;
        }

        return null;
    }
}
