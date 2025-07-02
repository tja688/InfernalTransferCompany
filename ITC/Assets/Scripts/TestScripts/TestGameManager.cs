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
    
}
