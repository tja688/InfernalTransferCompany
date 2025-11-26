using MoreMountains.Feedbacks;
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
    public static float radius = 30f;
    public static float startAngle = 135f;
    public static float endAngle = 45f;


    public static float burrDegree = 15f;

    public static float scaleFactor = 0.1f;

    // 每一轮间隔时间
    public float TuneIntervalTime = 1f;



    public int index = 0;
    private bool enableInput = false;
    private List<int> requiredRunes;
    private List<int> inputRunes;

    //不需要传递复杂成功判定，暂时不使用
    private HeSuccessLayer successLayer = HeSuccessLayer.Normal;



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
 
    public void ClearStates()
    {
        inputRunes?.Clear();
        requiredRunes?.Clear();
        successLayer=HeSuccessLayer.Normal;
        index = 0;
        foreach (var item in spawnedItems)
        {
            if (item != null)
                Destroy(item);
        }
        spawnedItems.Clear();
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
    private void EnterAnimationFlow(GameObject obj)
    {
        var handle= obj.GetComponent<MMFPlayerHandleArrow>();


        handle.Play();
    }


    private  void ArrangeInArc_A(List<int> list)
    {
        // 清除现有元素

        spawnedItems.Clear();

        var itemCount = list.Count;
        if (ArrowGroupGamePrefab == null || itemCount <= 0)
            return;

        float totalAngle = endAngle - startAngle;
        float angleStep = itemCount > 1 ? totalAngle / (itemCount - 1) : 0;
        Vector2 centerPos = Vector2.zero;

        for (int i = 0; i < itemCount; i++)
        {
            // 计算对称缩放
            float mid = (float)(itemCount-1) / 2;
            float dis = Math.Abs(i - mid);
            dis*= scaleFactor;
            float targetScale = 1f - dis;



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
                itemRect.localEulerAngles = new Vector3(0, 0, arrowKeyMap[list[i]]+UnityEngine.Random.Range(-burrDegree, burrDegree));
            }
            var handle = item.GetComponent<MMFPlayerHandleArrow>();

            handle.SetEnterPosition( pos,Vector3.zero, targetScale);
            item.SetActive(true);
            spawnedItems.Add(item);

        }
    }
    /// <summary>
    /// 
    /// </summary>
    private void SpawnRuneArrows()
    {

        ArrangeInArc_A(requiredRunes);
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
                ToSuccess();

                if (inputRunes.Count == requiredRunes.Count)
                {
                    OnceGameEnd(HeSuccessLayer.Normal);
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

        var curIndex = inputRunes.Count-1;

    
        for (int i = curIndex; i < spawnedItems.Count; i++)
        {
            var obj = spawnedItems[i];

            if (i == curIndex)
            {
                var handle = obj.GetComponent<MMFPlayerHandleArrow>();
                handle.FaildFade();
            }
            else
            {
                var handle = obj.GetComponent<MMFPlayerHandleArrow>();
                handle.SuccessFade();
            }
        }
        OnceGameEnd(HeSuccessLayer.Fail);


    }
    private void OnceGameEnd(HeSuccessLayer type)
    {
        DisableKeyInput();

        StartCoroutine(HeCoroutineUtil.Run(() => {
            return Inner();
            System.Collections.IEnumerator Inner()
            {
                yield return new WaitForSeconds(TuneIntervalTime);
                handle.GameScheduling(type);
            }
        }));


    }
    private void ToSuccess()
    {
        var curIndex = inputRunes.Count - 1;

        var obj = spawnedItems[curIndex];

        var handle = obj.GetComponent<MMFPlayerHandleArrow>();
        //Debug.Log($"第{curIndex}个key触发退出效果 ");

        handle.SuccessFade();
        SlotCenter.Instance.trigger_event(HeEventNames.LetContinueTypeWriter);

    }

    private bool OneTuneRhythmGame()
    {

        SpawnRuneArrows();
        
        Debug.Log($"debug:{spawnedItems.Count} " );
        foreach (var item in spawnedItems)
        {
            EnterAnimationFlow(item);
        }



        SlotCenter.Instance.add_listener("OnSpawnRuneArrowsEnd", EnableKeyInput,true);



        return true;

    }

    /// <summary>
    /// 初始化一场游戏并且开始
    /// </summary>
    /// <param name="MaxArrowCount"></param>
    /// <param name="MinArrowCount"></param>
    /// <returns></returns>
    public void playOneTuneRhythmGame(int maxArrowCount, int minArrowCount)
    {


        ClearStates();
        requiredRunes ??= new List<int>();
        inputRunes ??= new List<int>();
        var random = UnityEngine.Random.Range(minArrowCount, maxArrowCount+1);
        Debug.Log($"本轮符文箭头数量: {random}，最小箭头数量:{minArrowCount},最大:{maxArrowCount}");
        GenerateRequiredRunes(random);


        OneTuneRhythmGame();



    }

    private void GenerateRequiredRunes(int count)
    {

        
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
    public int MaxFailCount = 3;

    public int TuneCount { get; set; }
    public int MaxArrowCount { get; set; }
    public int MinArrowCount { get; set; }
    public int TuneCountCurrent { get; set; }


    private int FailCountCurrent;
HeSuccessLayer SuccessLayer = HeSuccessLayer.Normal;
    public Rhythmgame rhythGame;
    public RhythmgameHandle(int tuneCount, int minArrowCount, int maxArrowCount)
    {
        if (maxArrowCount < minArrowCount)
        {
            Debug.LogError("最大箭头数量小于最小箭头数量，参数错误");
        }
        Debug.Log($"RhythmgameHandle创建 参数:tuneCount:{tuneCount},maxArrowCount:{maxArrowCount},minArrowCount{minArrowCount}");
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
        if (rhythGame == null)
        {
            Debug.LogError("句柄未绑定游戏实例");
        }
        else
            onReadyForBreakLine();
    }
    /// <summary>
    /// 第一轮
    /// </summary>
    private void NextTune()
    {


     
        if(TuneCountCurrent > TuneCount)
        {
            Debug.LogError("当前轮数大于总轮数");
        }
        else
        if (rhythGame == null)
        {
            Debug.LogError("句柄未绑定游戏实例");
        }
        else
        {
            Debug.Log($"第 {TuneCountCurrent + 1} 轮游戏开始");
            SlotCenter.Instance.add_listener(HeEventNames.OnReadyForBreakLine, onReadyForBreakLine, true);
        }

    }
    private void onReadyForBreakLine()
    {

        rhythGame.playOneTuneRhythmGame(MaxArrowCount, MinArrowCount);

    }

    public void GameScheduling(HeSuccessLayer type)
    {
        switch (type)
        {
         
            case HeSuccessLayer.Fail:
                // 处理 Success
                FailCountCurrent++;
                Debug.Log($"当前失败次数: {FailCountCurrent} / {MaxFailCount}");
                if (FailCountCurrent==MaxFailCount)
               { 
                    
                    
                    SlotCenter.Instance.trigger_event<HeSuccessLayer>(HeEventNames.OnRythmGameEnd, HeSuccessLayer.Fail); 
                    SlotCenter.Instance.trigger_event(HeEventNames.LetStopTypeWriter);


                }
                      else
                {
                    SlotCenter.Instance.trigger_event(HeEventNames.LetLineBreakTypeWriter);
                    NextTune();

                }

                break;
    
            case HeSuccessLayer.Normal:
                TuneCountCurrent++;
                if(TuneCountCurrent == TuneCount)
                {
                    SlotCenter.Instance.trigger_event<HeSuccessLayer>(HeEventNames.OnRythmGameEnd, HeSuccessLayer.Success);
                    SlotCenter.Instance.trigger_event(HeEventNames.LetStopTypeWriter);





                }
                else
                {
                    SlotCenter.Instance.trigger_event(HeEventNames.LetLineBreakTypeWriter);
                    NextTune();



                }


                break;
            default:
                // 处理未知类型
                Debug.LogError("奇怪的游戏结果枚举: " + type);
                break;
        }
    }


}