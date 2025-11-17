using System;
using System.Collections.Generic;
using UnityEngine;

public enum HeSuccessLayer {
    BigSuccess,
    Success,
    Normal,
    Fail,
    BigFail,
}


public class Rhythmgame : MonoBehaviour
{

   
    public  RhythmgameHandle handle  { get; private set; }
    public static float radius = 300f;
    public static float startAngle = 135f;
    public static float endAngle = 45f;










    public int index = 0;
    private bool enableInput = false;
    private List<int> requiredRunes;
    private List<int> inputRunes;







    private List<GameObject> spawnedItems = new List<GameObject>();







    private static Dictionary<int, float> arrowKeyMap = new Dictionary<int, float>()
    {
        {0,0f},   // 上
        {1,180f}, // 下
        {2,90f},  // 左
        {3,270f}  // 右
    };


    private static Dictionary<int, KeyCode> runeKeyMap = new Dictionary<int, KeyCode>()
    {
        {0, KeyCode.W}, // 上
        {1, KeyCode.S}, // 下
        {2, KeyCode.A}, // 左
        {3, KeyCode.D}  // 右
    };
    public void OnStartGame() {
        EnableKeyInput();


    }
    public void ClearStates()
    {
        inputRunes =null;
        requiredRunes = null;
        index = 0;
        return;
    }
    public void SetHandle(RhythmgameHandle handle1)
    {
        handle = handle1;
        handle1.rhythGame = this;
        return;
    }
    public void ClearHandle()
    {
        handle = null;
        return;
    }


    public GameObject ArrowGroupGamePrefab;
    private static Dictionary<int, String> runeStringMap = new Dictionary<int, String>()
    {
        {0, "上"}, // 上
        {1, "下"}, // 下
        {2, "左"}, // 左
        {3, "右"}  // 右
    };

    private void Start()
    {
        HeKeyInput.Instance.OnMoveAction += ProcessRuneInput;
        

    }
    private  void ArrangeInArc_A(List<int> list)
    {
        // 清除现有元素
        foreach (var item in spawnedItems)
        {
            if (item != null)
                Destroy(item);
        }
        spawnedItems.Clear();

        var itemCount = list.Count;
        if (ArrowGroupGamePrefab == null || itemCount <= 0)
            return;

        float totalAngle = endAngle - startAngle;
        float angleStep = itemCount > 1 ? totalAngle / (itemCount - 1) : 0;
        Vector2 centerPos = Vector2.zero;

        for (int i = 0; i < itemCount; i++)
        {
            // 计算位置角度
            float currentAngle = startAngle + (i * angleStep);
            float rad = currentAngle * Mathf.Deg2Rad;

            // 计算坐标
            float x = radius * Mathf.Cos(rad);
            float y = radius * Mathf.Sin(rad);
            Vector2 pos = centerPos + new Vector2(x, y);

            // 实例化UI元素
            GameObject item = Instantiate(ArrowGroupGamePrefab, transform);
            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                itemRect.anchoredPosition = pos;
                itemRect.localEulerAngles = new Vector3(0, 0, arrowKeyMap[list[i]]);
            }


            spawnedItems.Add(item);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    private void SpawnRuneArrows()
    {

        //ArrangeInArc_A(requiredRunes);
    }

    private void EnableKeyInput()
    {
        enableInput = true;
    }
    private void DisableKeyInput()
    {
        enableInput = false;
    }

    private void ProcessRuneInput(int directIndex)
    {
        if (!enableInput)
        {
            return;
        }
        var targetCount  =  requiredRunes.Count;

        var curIndex = inputRunes.Count;

        if (curIndex >= targetCount)
        {
            Debug.LogError("当前输入数量已达上限，Unreachable");
            return;
        }
        else
        {
            inputRunes.Add(directIndex);
            if (inputRunes[curIndex] == requiredRunes[curIndex])
            {
                //处理单次成功
                if(inputRunes.Count == requiredRunes.Count)
                {
                    ToSuccess();
                }
            }
            else
            {
                ToFaild();





            }
        }
    }
    private void ToFaild()
    {





    }

    private void ToSuccess()
    {



    }

    private bool OneTuneRhythmGame()
    {

        SpawnRuneArrows();

        SlotCenter.Instance.add_listener("OnSpawnRuneArrowsEnd", OnStartGame);



        return true;

    }
    public bool playOneTuneRhythmGame(int maxArrowCount, int minArrowCount)
    {



        ClearStates();
        var random = UnityEngine.Random.Range(minArrowCount, maxArrowCount+1);
        GenerateRequiredRunes(random);


        OneTuneRhythmGame();



        requiredRunes = null;
        return true;
    }

    private void GenerateRequiredRunes(int count)
    {

        requiredRunes = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            requiredRunes.Add(UnityEngine.Random.Range(0, 4));
        }

        var runeNames = requiredRunes.ConvertAll(r => runeStringMap[r]);
        Debug.Log("生成符文箭头序列: " + string.Join(", ", runeNames)+$"长度: {requiredRunes.Count}");
    }

}



public class RhythmgameHandle
{
    class Config
    {
        public int TuneCount { get; set; }
        public int MaxArrowCount { get; set; }
        public int MinArrowCount { get; set; }
        public Config(int tuneCount, int maxArrowCount, int minArrowCount)
        {

        }

    }
    Config config;
    public int TuneCount { get; set; }
    public int MaxArrowCount { get; set; }
    public int MinArrowCount { get; set; }
    public int TuneCountCurrent { get; set; }
    HeSuccessLayer SuccessLayer = HeSuccessLayer.Normal;
    public Rhythmgame rhythGame;
    public RhythmgameHandle(int tuneCount, int maxArrowCount, int minArrowCount)
    {
        SlotCenter.Instance.add_listener(HeEventNames.OnIsReadyTypeWriter, OnTypeWriterIsReady, true);



        SlotCenter.Instance.add_listener("NextTuneRhygame", OnTypeWriterIsReady);
        TuneCount = tuneCount;
        MaxArrowCount = maxArrowCount;
        MinArrowCount = minArrowCount;
        TuneCountCurrent = 0;


    }

    ~RhythmgameHandle()
    {
        if (SlotCenter.Instance != null)
        {
            SlotCenter.Instance.remove_listener(HeEventNames.OnIsReadyTypeWriter, OnTypeWriterIsReady);
            SlotCenter.Instance.remove_listener(HeEventNames.NextTuneRhygame, OnTypeWriterIsReady);
        }
        rhythGame?.ClearHandle();

    }
    private void OnTypeWriterIsReady()
    {


        if (TuneCountCurrent == TuneCount)
        {
            SlotCenter.Instance.trigger_event<HeSuccessLayer>(HeEventNames.OnRythmGameEnd, HeSuccessLayer.Success);


            return;
        }
        else if(TuneCountCurrent > TuneCount)
        {
            Debug.LogError("当前轮数大于总轮数");
        }
        var a=  rhythGame.playOneTuneRhythmGame(MaxArrowCount, MinArrowCount);
    }

}