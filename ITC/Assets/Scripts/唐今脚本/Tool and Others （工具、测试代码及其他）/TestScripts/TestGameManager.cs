using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class TestGameManager : MonoBehaviour
{

    public void StartDay3()
    {
        DialogueLua.SetVariable("Day", 3);
        DialogueLua.SetVariable("isTalkToOthersInDay3", false);
    }
    

    public void ExitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 编辑器模式退出
#else
    Application.Quit(); // 打包后退出游戏
#endif
    }

    
}
