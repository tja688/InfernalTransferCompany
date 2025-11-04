
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存储游戏中所有面板名称的 ScriptableObject 库。
/// </summary>
[CreateAssetMenu(fileName = "GamePanelLibrary", menuName = "Game Panel/Panel Library", order = 1)]
public class GamePanelLibrarySO : ScriptableObject
{
    [Tooltip("在此处定义游戏中所有面板的唯一名称")]
    public List<string> panelNames = new List<string> { "None" };
}
