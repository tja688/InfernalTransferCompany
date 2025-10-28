using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 定義了遊戲中可用的所有 UI 面板狀態。
/// </summary>
[CreateAssetMenu(menuName = "UITween/UIPanelStateConfiguration", fileName = "UIPanelStateConfiguration")]
public class UIPanelStateConfiguration : ScriptableObject
{
    [Serializable]
    public class PanelState
    {
        [Tooltip("面板狀態名稱，需全局唯一。")]
        public string stateName;
    }

    [Tooltip("當前遊戲中可用的所有面板狀態。")]
    public List<PanelState> panelStates = new();

    /// <summary>
    /// 取得所有狀態名稱。
    /// </summary>
    public IEnumerable<string> GetStateNames()
    {
        foreach (var state in panelStates)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.stateName))
            {
                continue;
            }

            yield return state.stateName.Trim();
        }
    }

    public bool Contains(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        foreach (var existing in panelStates)
        {
            if (existing == null)
            {
                continue;
            }

            if (string.Equals(existing.stateName, stateName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
