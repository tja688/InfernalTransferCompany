using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 契约游戏配置 - ScriptableObject资源文件
/// 可在Inspector中调整游戏参数，支持多种难度配置
/// </summary>
[CreateAssetMenu(fileName = "HeContractGameConfig", menuName = "Hedium/Contract Game Config")]
public class HeContractGameConfig : ScriptableObject
{
    [Header("=== 基础设定 ===")]
    [Tooltip("顾客初始满意度")]
    [Range(1, 5)]
    public int initialSatisfaction = 3;
    
    [Tooltip("最大失败次数")]
    [Range(1, 5)]
    public int maxFailCount = 3;
    
    [Tooltip("最小满意度")]
    [Range(0, 3)]
    public int minSatisfaction = 1;

    [Header("=== 符文输入系统 ===")]
    [Tooltip("符文输入时间限制(秒)")]
    [Range(5f, 30f)]
    public float runeInputTimeLimit = 10f;

    [Tooltip("符文输入数量最大(个)")]
    [Range(5, 30)]
    public int runeInputCountMaxLimit = 5;
    [Tooltip("符文输入数量最小(个)")]
    [Range(5, 30)]
    public int runeInputCountMinLimit = 3;
    [Tooltip("符文显示时间限制(秒)")]
    [Range(5f, 30f)]
    public float runeShowTimeLimit = 4f;
    [Tooltip("符文游戏轮次(次)")]
    [Range(5, 30)]
    public int runeGameTuneCount = 3;
    [Tooltip("最大符文错误次数")]
    [Range(1, 5)]
    public int maxRuneErrors = 3;
    
    [Tooltip("二次错误导致满意度下降的阈值")]
    [Range(1, 3)]
    public int runeErrorSatisfactionThreshold = 2;

    [Header("=== 符文核对系统 ===")]
    [Tooltip("符文核对时间限制(秒)")]
    [Range(3f, 10f)]
    public float runeVerificationTime = 5f;
    
    [Tooltip("需要找出的扭曲符文数量")]
    [Range(1, 5)]
    public int runeVerificationCount = 3;
    
    [Tooltip("符文核对触发几率")]
    [Range(0f, 1f)]
    public float runeVerificationTriggerChance = 0.3f;

    [Header("=== 盖章系统 ===")]
    [Tooltip("印章蓄力时间(秒)")]
    [Range(1f, 5f)]
    public float stampChargeTime = 3f;
  
    [Tooltip("最大盖章尝试次数")]
    [Range(1, 5)]
    public int maxStampAttempts = 3;
    
    [Tooltip("盖章精度容差 (越小越难)")]
    [Range(0.05f, 0.3f)]
    public float stampAccuracyTolerance = 0.1f;
    
    [Tooltip("完美盖章的最佳时机 (蓄力时间的百分比)")]
    [Range(0.6f, 0.9f)]
    public float stampOptimalTiming = 0.8f;

    [Header("=== 灵魂收取系统 ===")]
    [Tooltip("灵魂收取精度容差 (半成 = 0.05)")]
    [Range(0.02f, 0.1f)]
    public float soulHarvestAccuracy = 0.05f;
    
    [Tooltip("分灵刀抖动幅度")]
    [Range(0.01f, 0.05f)]
    public float soulCutterShake = 0.02f;
    
    [Tooltip("分灵刀移动速度")]
    [Range(0.1f, 1f)]
    public float soulCutterMoveSpeed = 0.5f;

    [Header("=== 特殊事件系统 ===")]
    [Tooltip("特殊事件触发几率")]
    [Range(0f, 1f)]
    public float eventTriggerChance = 0.3f;
    
    [Tooltip("事件响应时间奖励阈值")]
    [Range(0.5f, 3f)]
    public float eventQuickResponseTime = 2f;

    [Header("=== 符文配置 ===")]
    [Tooltip("符文类型配置")]
    public List<RuneConfig> runeConfigs = new List<RuneConfig>();

    [Header("=== 契约类型配置 ===")]
    [Tooltip("契约类型和对应符文序列")]
    public List<HeContractTypeConfig> HeContractTypeConfigs = new List<HeContractTypeConfig>();

    int day = 1;

  
    public void CalculateCameConfigMaker(int day)
    {
        runeInputCountMaxLimit =5 ;
        runeInputCountMinLimit = 5;
        runeInputTimeLimit = 10f;
        runeShowTimeLimit = 4f;
        runeGameTuneCount = 3;

    }
    [System.Serializable]
    public class RuneConfig
    {
        public RuneType type;
        public string displayName;
        public Color normalColor = Color.white;
        public Color corruptedColor = Color.red;
        public Sprite normalSprite;
        public Sprite corruptedSprite;
        public string description;
        [Tooltip("用于输入的按键")]
        public KeyCode inputKey;
    }

    [System.Serializable]
    public class HeContractTypeConfig
    {
        public HeContractType HeContractType;
        public string displayName;
        public Color themeColor;
        public List<RuneType> requiredRuneSequence;
        public string description;
        [Tooltip("该契约类型的灵魂收取百分比范围")]
        [Range(0.1f, 0.8f)]
        public float minSoulPercentage = 0.2f;
        [Range(0.1f, 0.8f)]
        public float maxSoulPercentage = 0.6f;
    }


   
    /// <summary>
    /// 获取契约类型的符文序列
    /// </summary>
    public List<RuneType> GetRuneSequenceForContract(HeContractType HeContractType)
    {
        var config = HeContractTypeConfigs.Find(c => c.HeContractType == HeContractType);
        if (config != null && config.requiredRuneSequence.Count > 0)
        {
            return new List<RuneType>(config.requiredRuneSequence);
        }
        
        // 返回默认序列
        return GetDefaultRuneSequence(HeContractType);
    }

    /// <summary>
    /// 获取默认符文序列 (如果配置为空)
    /// </summary>
    private List<RuneType> GetDefaultRuneSequence(HeContractType HeContractType)
    {
        switch (HeContractType)
        {
            case HeContractType.Money:
                return new List<RuneType> { RuneType.Earth, RuneType.Fire, RuneType.Light };
            case HeContractType.Fame:
                return new List<RuneType> { RuneType.Light, RuneType.Air, RuneType.Fire };
            case HeContractType.Skill:
                return new List<RuneType> { RuneType.Water, RuneType.Earth, RuneType.Air };
            case HeContractType.Event:
                return new List<RuneType> { RuneType.Dark, RuneType.Fire, RuneType.Water };
            default:
                return new List<RuneType> { RuneType.Fire, RuneType.Water, RuneType.Earth };
        }
    }

    /// <summary>
    /// 获取契约类型的随机灵魂百分比
    /// </summary>
    public float GetRandomSoulPercentage(HeContractType HeContractType)
    {
        var config = HeContractTypeConfigs.Find(c => c.HeContractType == HeContractType);
        if (config != null)
        {
            return Random.Range(config.minSoulPercentage, config.maxSoulPercentage);
        }
        
        // 默认范围
        return Random.Range(0.2f, 0.6f);
    }

    /// <summary>
    /// 重置为默认值
    /// </summary>
    [ContextMenu("Reset to Default")]
    public void ResetToDefault()
    {
     
        
        // 设置默认符文配置
        if (runeConfigs.Count == 0)
        {
            runeConfigs = new List<RuneConfig>
            {
                new RuneConfig { type = RuneType.Fire, displayName = "火", inputKey = KeyCode.W, normalColor = Color.red },
                new RuneConfig { type = RuneType.Water, displayName = "水", inputKey = KeyCode.A, normalColor = Color.blue },
                new RuneConfig { type = RuneType.Earth, displayName = "土", inputKey = KeyCode.S, normalColor = new Color(0.6f, 0.4f, 0.2f) },
                new RuneConfig { type = RuneType.Air, displayName = "风", inputKey = KeyCode.D, normalColor = Color.cyan },
                new RuneConfig { type = RuneType.Light, displayName = "光", inputKey = KeyCode.Q, normalColor = Color.yellow },
                new RuneConfig { type = RuneType.Dark, displayName = "暗", inputKey = KeyCode.E, normalColor = new Color(0.3f, 0.3f, 0.3f) }
            };
        }
        
        // 设置默认契约类型配置
        if (HeContractTypeConfigs.Count == 0)
        {
            HeContractTypeConfigs = new List<HeContractTypeConfig>
            {
                new HeContractTypeConfig 
                { 
                    HeContractType = HeContractType.Money, 
                    displayName = "金钱契约", 
                    themeColor = Color.yellow,
                    requiredRuneSequence = new List<RuneType> { RuneType.Earth, RuneType.Fire, RuneType.Light },
                    minSoulPercentage = 0.2f,
                    maxSoulPercentage = 0.4f
                },
                new HeContractTypeConfig 
                { 
                    HeContractType = HeContractType.Fame, 
                    displayName = "名利契约", 
                    themeColor = Color.magenta,
                    requiredRuneSequence = new List<RuneType> { RuneType.Light, RuneType.Air, RuneType.Fire },
                    minSoulPercentage = 0.3f,
                    maxSoulPercentage = 0.5f
                },
                new HeContractTypeConfig 
                { 
                    HeContractType = HeContractType.Skill, 
                    displayName = "技能契约", 
                    themeColor = Color.cyan,
                    requiredRuneSequence = new List<RuneType> { RuneType.Water, RuneType.Earth, RuneType.Air },
                    minSoulPercentage = 0.2f,
                    maxSoulPercentage = 0.4f
                },
                new HeContractTypeConfig 
                { 
                    HeContractType = HeContractType.Event, 
                    displayName = "事件契约", 
                    themeColor = Color.red,
                    requiredRuneSequence = new List<RuneType> { RuneType.Dark, RuneType.Fire, RuneType.Water },
                    minSoulPercentage = 0.4f,
                    maxSoulPercentage = 0.6f
                }
            };
        }
        
        Debug.Log("配置已重置为默认值");
    }
}

/// <summary>
/// 简化的Button特性 (如果项目中没有其他Button实现)
/// </summary>
public class ButtonAttribute : PropertyAttribute
{
    public string MethodName { get; }
    
    public ButtonAttribute(string methodName = "")
    {
        MethodName = methodName;
    }
}