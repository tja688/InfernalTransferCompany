using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 契约类型枚举
/// </summary>
public enum HeContractType
{
    Money,      // 金钱
    Fame,       // 名利  
    Skill,      // 特技
    Event       // 事件
}

/// <summary>
/// 突发事件类型
/// </summary>
public enum ContractEventType
{
    Phone,      // 电话
    Gun,        // 拔枪
    Epilepsy,   // 癫痫
    Transform,  // 变形
    Dialogue    // 对话
}

/// <summary>
/// 符文类型
/// </summary>
public enum RuneType
{
    Fire,       // 火
    Water,      // 水
    Earth,      // 土
    Air,        // 风
    Light,      // 光
    Dark        // 暗
}

/// <summary>
/// 游戏配置
/// </summary>
[System.Serializable]
public class GameConfig
{
    [Header("基础设定")]
    public int initialSatisfaction = 3;     
    public int maxFailCount = 3;            
    public int minSatisfaction = 1;         
    
    [Header("符文输入")]
    public float runeInputTimeLimit = 10f;   
    public int maxRuneErrors = 3;            
    
    [Header("符文核对")]
    public float runeVerificationTime = 5f;  
    public int runeVerificationCount = 3;    
    public float runeVerificationTriggerChance = 0.3f; 
    
    [Header("盖章系统")]
    public float stampChargeTime = 3f;       
    public int maxStampAttempts = 3;         
    public float stampAccuracyTolerance = 0.1f; 
    
    [Header("灵魂收取")]
    public float soulHarvestAccuracy = 0.05f; 
    public float soulCutterShake = 0.02f;     
}   

/// <summary>
/// 契约文书数据
/// </summary>
[System.Serializable]
public class ContractDocument
{
    public string customerName;
    public string occupation;
    public Sprite customerPhoto;            
    public DateTime appointmentDate;        
    public string contractDescription;      
    public float soulPercentage;           
    public HeContractType HeContractType;
    
    [Header("文书状态")]
    public bool isSealed = true;            
    public bool isGenuine = true;           
    public bool hasITCWatermark = true;     
    public bool isInkGenuine = true;        
    public bool isContentMatched = true;    
    public bool isDateCorrect = true;       
    public bool isIdentityMatched = true;   
}

/// <summary>
/// 顾客数据
/// </summary>
[System.Serializable]
public class Customer
{
    public string name;
    public string occupation;
    public Sprite photo;
    public string spokenRequest;            
    public bool isDisguised = false;        
    public bool isClocardalMember = false;  
}

/// <summary>
/// 突发事件数据
/// </summary>
[System.Serializable]
public class EventData
{
    public string description;
    public ContractEventType type;
    public float duration = 5f;            
    public Action<HeContractContext> OnResolve;
    public Action<HeContractContext> OnFail;
    public bool requiresInput = true;       
}

/// <summary>
/// 符文数据
/// </summary>
[System.Serializable]
public class RuneData
{
    public RuneType type;
    public Sprite normalSprite;
    public Sprite corruptedSprite;
    public string description;
}

/// <summary>
/// 契约上下文 - 保存整个签约流程的状态
/// </summary>
public class HeContractContext
{
    public Customer customer;               
    public ContractDocument document;       
    public int satisfaction = 3;            
    public int failCount = 0;               
    public bool isTerminated = false;       
    
    [Header("流程状态")]
    public bool documentVerified = false;   
    public bool runesCompleted = false;     
    public bool eventHandled = false;       
    public bool stampApplied = false;       
    public bool soulHarvested = false;      
    
    [Header("错误计数")]
    public int runeErrors = 0;              
    public int stampAttempts = 0;  
    public bool isFaild = false;

    public void AddFailure()
    {
        failCount++;
        Debug.Log($"签约失败次数增加: {failCount}");
    }
    
    public void DecreaseSatisfaction(int amount = 1)
    {
        satisfaction = Mathf.Max(0, satisfaction - amount);
        Debug.Log($"顾客满意度降低: {satisfaction}");
    }
    
    public void IncreaseSatisfaction(int amount = 1)
    {
        satisfaction = Mathf.Min(5, satisfaction + amount);
        Debug.Log($"顾客满意度提升: {satisfaction}");
    }
}