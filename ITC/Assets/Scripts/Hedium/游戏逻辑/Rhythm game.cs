using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;



public class Rhythmgame : MonoBehaviour
{

    public static float radius = 182f;
    public static float startAngle = 135f;
    public static float endAngle = 45f;
    public static float fadeDuration = 1f;
    public static float startDelay = 0f;
    private bool enableInput= false;
    public List<GameObject> spawnedItems = new List<GameObject>();
    private static Dictionary<int, float> arrowKeyMap = new Dictionary<int, float>()
    {
        {0,0f},   // 上
        {1,180f}, // 下
        {2,90f},  // 左
        {3,270f}  // 右
    };


    private List<int> requiredRunes;
    private List<int> inputRunes;
    private static Dictionary<int, KeyCode> runeKeyMap = new Dictionary<int, KeyCode>()
    {
        {0, KeyCode.W}, // 上
        {1, KeyCode.S}, // 下
        {2, KeyCode.A}, // 左
        {3, KeyCode.D}  // 右
    };

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


    }





    private bool OneTuneRhythmGame()
    {

        SpawnRuneArrows();

        SlotCenter.Instance.add_listener("OnSpawnRuneArrowsEnd", EnableKeyInput);



        return true;

    }
    public bool playOneTuneRhythmGame(int maxArrowCount, int minArrowCount)
    {
        var random = UnityEngine.Random.Range(maxArrowCount, minArrowCount);
        GenerateRequiredRunes(random);


        OneTuneRhythmGame();



        requiredRunes = null;
        return true;
    }

    private void GenerateRequiredRunes(int count)
    {
        requiredRunes = new List<int>();
        for (int i = 0; i < requiredRunes.Count; i++)
        {
            requiredRunes.Add(UnityEngine.Random.Range(0, count));
        }

        UnityEngine.Debug.Log($"需要输入符文序列: {string.Join(", ", requiredRunes)}");
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
    Rhythmgame handle;
    public RhythmgameHandle(int tuneCount, int maxArrowCount, int minArrowCount)
    {
        SlotCenter.Instance.add_listener(HeEventNames.OnIsReadyTypeWriter, OnTypeWriterIsReady, true);
        TuneCount = tuneCount;
        MaxArrowCount = maxArrowCount;
        MinArrowCount = minArrowCount;
    }


    private void OnTypeWriterIsReady()
    {
        for (int i = 0; i < TuneCount; i++)
        {
            if (!handle.playOneTuneRhythmGame(MaxArrowCount, MinArrowCount))
            {
                SlotCenter.Instance.trigger_event<bool>("OnEndRhythmgame", false);

            }

            SlotCenter.Instance.trigger_event<bool>("OnEndRhythmgame", true);

        }





    }

}