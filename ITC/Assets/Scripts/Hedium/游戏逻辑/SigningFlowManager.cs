
using MoreMountains.Feedbacks;
using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;




#region DebugStage
public class DebugStage : IContractStage
{
    public bool IsCompleted => false;

    public bool HasFailed => false;

    public string StageName => "";

    private void Updata()
    {

    }

    private void Exit()
    {
    }

    
    public void Update()
    {
    }

    void IContractStage.Exit()
    {
        Exit();
    }

    public void Enter()
    {
        throw new NotImplementedException();
    }
}

#endregion


#region Utility Classes
public static class HeCoroutineUtil
{
    public static IEnumerator Run(System.Func<IEnumerator> coroutine)
    {
        return coroutine();
    }
}
[Serializable]
enum StampType
{
    Circular,
    Diamond,
    Triangular,
    Spherical,
    None
}
[SerializeField]
/// <summary>
/// 文书错误类型枚举
/// 用于标识在文书核验过程中可能出现的各种问题
/// </summary>
public enum DocumentError
{


    /// <summary>
    /// 
    /// 有错误
    /// 
    /// </summary>
    NoPass,

    /// <summary>
    /// 无错误 
    /// </summary>
    Pass,
    
    ///<summary,>伪实现，不判断类型只判断对错</summary>
    Stub,
    /// <summary>封蜡破损 - 文书的封蜡不完整</summary>
    BrokenSeal,

    /// <summary>伪造文书 - 文书本身系伪造</summary>
    ForgeryDocument,

    /// <summary>缺少水印 - 文书缺少ITC公司官方水印</summary>
    MissingWatermark,

    /// <summary>假冒墨水 - 使用了非官方墨水</summary>
    FakeInk,

    /// <summary>内容不符 - 顾客口述需求与契约内容不匹配</summary>
    ContentMismatch,

    /// <summary>日期错误 - 预约日期与当前日期不符</summary>
    IncorrectDate,

    /// <summary>身份不符 - 文书记录的身份与实际不符</summary>
    IdentityMismatch,

    /// <summary>伪装顾客 - 顾客使用假身份</summary>
    DisguisedCustomer,

    /// <summary>危险人物 - 克洛克达尔帮成员或其他危险人物</summary>
    DangerousCustomer
}
#endregion 

#region Contract Stages
#region DocumentVerifier Stages
/// <summary>
/// 文书核验系统
/// </summary>
public class DocumentVerifier : IContractStage
{
    private HeContractContext context;
    private HeContractUIManager uiManager;

    private bool completed = false;
    private bool failed = false;
    private List<DocumentError> detectedErrors = new List<DocumentError>();

    public bool IsCompleted => completed;
    public bool HasFailed => failed;
    public string StageName => "文书核验";
    public List<DocumentError> DetectedErrors => detectedErrors;




  
    public void Enter()
    {
        context = GameObject.FindFirstObjectByType<SigningFlowManager>()?.ctx;
        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        uiManager.RestoreTypeWriterGameObject();
        Debug.Log("=== 开始文书核验阶段 ===");
        
        // 显示文书核验UI
        //uiManager?.ShowDocumentVerification(ctx);
        initRes();
        // 执行核验逻辑
        PerformDocumentVerification();
        //Debug.Assert(detectedErrors.Count>0);
    
       
         Debug.Log($"检测到{detectedErrors.Count}个问题，等待玩家决策...");

    }

    public void Update()
    {



    }
    public void ToExit()
    {
        context.documentVerified = true;
        completed = true;
    }


    private void pneumaticAuxiliaryHook()
    {


        uiManager.OnOpenPneumaticChannelClick();
        uiManager.pneumaticChannelSkeleton.GetComponent<SkeletonHoverHighLight>().effectTurn.EnableAllEffect = false;
    }
    public void initRes()
    {
        uiManager.pneumaticChannelSkeleton.GetComponent<SkeletonHoverHighLight>().effectTurn.EnableAllEffect = true;
        Debug.Log("初始化气动辅助钩按钮事件");
        Button button = uiManager.pneumaticChannelSkeleton.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(
           pneumaticAuxiliaryHook
            );

        button.interactable = true;





        SlotCenter.Instance.add_listener<DocumentError>(HeEventNames.DocumentErrorChosen, DocumentJudgeProsses,true);
        var Hover = uiManager.pneumaticChannelSkeleton.GetComponent<SkeletonHoverHighLight>();

        //Hover.SetHighLight();
       
    }
    public void Exit()
    {

        Button button = uiManager.pneumaticChannelSkeleton.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.interactable = false;
        SlotCenter.Instance.remove_listener<DocumentError>(HeEventNames.DocumentErrorChosen, DocumentJudgeProsses);
        detectedErrors.Clear();
        var Hover = uiManager?.pneumaticChannelSkeleton.GetComponent<SkeletonHoverHighLight>();
        Hover.enableHighLightOnHover = false;
        Debug.Log("=== 文书核验阶段结束 ===");
        if (detectedErrors.Count > 0)
        {


            Debug.Log($"检测到的错误: {string.Join(", ", detectedErrors)}");
        }
    }

    private void PerformDocumentVerification()
    {
        var doc = context.document;
        var customer = context.customer;
        detectedErrors.Clear();
        
        // 检查封蜡
        if (!doc.isSealed)
        {
            detectedErrors.Add(DocumentError.BrokenSeal);
            Debug.Log($"文书错误: {GetErrorDescription(DocumentError.BrokenSeal)}");
        }
        
        // 检查文书真伪
        if (!doc.isGenuine)
        {
            detectedErrors.Add(DocumentError.ForgeryDocument);
            Debug.Log($"文书错误: {GetErrorDescription(DocumentError.ForgeryDocument)}");
        }
        
        if (!doc.hasITCWatermark)
        {
            detectedErrors.Add(DocumentError.MissingWatermark);
            Debug.Log($"文书错误: {GetErrorDescription(DocumentError.MissingWatermark)}");
        }
        
        if (!doc.isInkGenuine)
        {
            detectedErrors.Add(DocumentError.FakeInk);
            Debug.Log($"文书错误: {GetErrorDescription(DocumentError.FakeInk)}");
        }
        
        // 检查内容匹配
        if (!doc.isContentMatched)
        {
            detectedErrors.Add(DocumentError.ContentMismatch);
            Debug.Log($"文书错误: {GetErrorDescription(DocumentError.ContentMismatch)}");
        }
        
        // 检查日期
        if (!doc.isDateCorrect)
        {
            detectedErrors.Add(DocumentError.IncorrectDate);
            Debug.Log($"文书错误: {GetErrorDescription(DocumentError.IncorrectDate)}");
        }
        
        // 检查身份
        if (!doc.isIdentityMatched)
        {
            detectedErrors.Add(DocumentError.IdentityMismatch);
            Debug.Log($"文书错误: {GetErrorDescription(DocumentError.IdentityMismatch)}");
        }
        
        if (customer.isDisguised)
        {
            detectedErrors.Add(DocumentError.DisguisedCustomer);
            Debug.Log($"文书错误: {GetErrorDescription(DocumentError.DisguisedCustomer)}");
        }
        
        if (customer.isClocardalMember)
        {

            detectedErrors.Add(DocumentError.DangerousCustomer);
            Debug.Log($"文书错误: {GetErrorDescription(DocumentError.DangerousCustomer)}");
        }
        
        // 判断验证结果
        if (detectedErrors.Count > 0)
        {
            Debug.Log($"文书核验发现 {detectedErrors.Count} 个问题，需要拒绝签约");
            //Stub
            detectedErrors.Add(DocumentError.Stub);
        
        }
        else
        {
          
        }
    }
    
    /// <summary>
    /// 获取错误描述
    /// </summary>
    public static string GetErrorDescription(DocumentError error)
    {
        return error switch
        {
            DocumentError.BrokenSeal => "封蜡已破损",
            DocumentError.ForgeryDocument => "文书系伪造",
            DocumentError.MissingWatermark => "缺少ITC水印",
            DocumentError.FakeInk => "墨水系假冒",
            DocumentError.ContentMismatch => "口述内容与契约不符",
            DocumentError.IncorrectDate => "预约日期不正确",
            DocumentError.IdentityMismatch => "身份证明不符",
            DocumentError.DisguisedCustomer => "顾客身份造假",
            DocumentError.DangerousCustomer => "危险人物(克洛克达尔帮成员)",
            _ => "未知错误"
        };
    }
    private void JudgeFaild()
    {
      
       
      
    }
    private void JudgeSuccess()
    {

    }
    private void DocumentJudgeProsses(DocumentError error)
    {
        if (error == DocumentError.Stub)
        {
            Debug.Log("强制下一阶段");

       
            ToExit();
            JudgeSuccess();
        }
        else
        if (detectedErrors.Count > 0)
        {
            if (error == DocumentError.Pass)
            {
                Debug.Log("玩家选择文书无误，但仍有待处理错误，核验文书不通过");

                JudgeFaild();
            }
            else if (error==DocumentError.NoPass)
            {
                Debug.Log("存在问题，判断正确");
                JudgeSuccess();
            }
            else if (detectedErrors.Contains(error))
            {
                Debug.Log($"问题{error}已处理,核验文书不通过");
                JudgeSuccess();
            }
            else
            {
                Debug.Log($"玩家问题选择错误");
                JudgeFaild();
            }


        }
        else
        {
            if (error == DocumentError.Pass)
            {
                Debug.Log("玩家选择文书无误，通过到下一阶段");
                ToExit();
                JudgeSuccess();
            }
            else
            {
                Debug.Log($"玩家选择了错误的文书问题，文书并没有错误，核验文书不通过");
                JudgeFaild();
            }

        }
        


    }

}
#endregion
#region RuneInputManager Stages
/// <summary>
/// 符文输入管理器
/// </summary>
public class RuneInputManager : IContractStage
{
    private HeContractContext context;
    private HeContractUIManager uiManager;
    private HeContractGameConfig gameConfig;
    private bool completed = false;
    private bool failed = false;

    private float timeRemaining;


    public bool IsCompleted => completed;
    public bool HasFailed => failed;
    public string StageName => "符文输入";

    private bool enableTimer = false;
    
    private bool detailsFillCompleted = false;  
    private float runeShowDuration;

    private int ArrowMaxCount;
    private int ArrowMinCount;
    int deleteArrowCount = 0;
    bool enableProcessedKey = false;
    public void Enter()
    {
        detailsFillCompleted = false;

        context =  GameObject.FindFirstObjectByType<SigningFlowManager>()?.ctx; ;
        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        uiManager.RestoreTypeWriterGameObject();






        gameConfig = GameObject.FindFirstObjectByType<SigningFlowManager>()?.gameConfig;
        runeShowDuration = gameConfig.runeShowTimeLimit;
        ArrowMaxCount = gameConfig.runeInputCountMaxLimit;
        ArrowMinCount = gameConfig.runeInputCountMinLimit;
        var targetTuneCount = gameConfig.runeGameTuneCount;
        Debug.Log("=== 开始符文输入阶段 ===");
       
  
        timeRemaining = gameConfig?.runeInputTimeLimit ?? 10f;

        if (uiManager != null )
        {
           uiManager.ArrowGroupGameObject.GetComponent<Rhythmgame>()
                .SetHandle(  new RhythmgameHandle(targetTuneCount,ArrowMinCount,ArrowMaxCount));
           SlotCenter.Instance.trigger_event(HeEventNames.LetStartTypeWriter);
           SlotCenter.Instance.add_listener<HeSuccessLayer>(HeEventNames.OnRythmGameEnd,OnRythmGameEnd);
        }
        else
        {
            Debug.LogError("HeContractUIManager 未找到，无法初始化符文输入界面");
        }



    }


    public void Exit()
    {
   
    }
    public void Update()
    {
        return;

    }
    private  void ToEnd()
    {
        completed = true;


    }
    private void OnRythmGameEnd(HeSuccessLayer success)
    {
        switch (success)
        {
            case HeSuccessLayer.BigSuccess:
                Debug.Log("游戏大成功!");
                break;
            case HeSuccessLayer.Success:
                Debug.Log("游戏成功!");


                ToEnd();
                break;
            case HeSuccessLayer.Normal:
                Debug.Log("游戏普通完成!");




                break;
            case HeSuccessLayer.Fail:
                Debug.Log("游戏失败!");



                break;
            case HeSuccessLayer.BigFail:
                Debug.Log("游戏大失败!");
                break;
            default:
                Debug.Log("未知游戏结果!");
                break;
        }
        return;
    }
    private void ProcessGameResult()
    {

    }





    }
#endregion


#region SpecialEventSystem Stages
/// <summary>
/// 特殊事件系统
/// </summary>
public class SpecialEventSystem : IContractStage
{
    private HeContractContext context;
    private HeContractUIManager uiManager;
    private HeContractGameConfig gameConfig;
    private bool completed = false;
    private bool failed = false;
    private EventData currentEvent;
    private float eventTimer;
    
    public bool IsCompleted => completed;
    public bool HasFailed => failed;
    public string StageName => "特殊事件";

    public void Enter()
    {
        context = GameObject.FindFirstObjectByType<SigningFlowManager>()?.ctx;

        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        //uiManager.ResotreContractDocumentsGameObject();
        gameConfig = GameObject.FindFirstObjectByType<SigningFlowManager>()?.gameConfig;
        
        Debug.Log("=== 开始特殊事件阶段 ===");
        
        // 随机决定是否触发事件
        float triggerChance = gameConfig?.eventTriggerChance ?? 0.3f;
        if (UnityEngine.Random.value < triggerChance&&false)//直接跳过特殊事件
        {
            TriggerRandomEvent();
        }
        else
        {
            // 没有事件，直接完成
            completed = true;
            context.eventHandled = true;
        }
    }

    public void Update()
    {
        if (completed || failed || currentEvent == null) return;
        
        eventTimer -= Time.deltaTime;
        
        // 处理事件输入
        HandleEventInput();
        
        // 事件超时
        if (eventTimer <= 0)
        {
            Debug.Log("特殊事件处理超时");
            currentEvent.OnFail?.Invoke(context);
            failed = true;
        }
    }

    public void Exit()
    {
        Debug.Log("=== 特殊事件阶段结束 ===");
    }

    private void TriggerRandomEvent()
    {
        var eventTypes = System.Enum.GetValues(typeof(ContractEventType));
        var randomEventType = (ContractEventType)eventTypes.GetValue(UnityEngine.Random.Range(0, eventTypes.Length));
        
        currentEvent = CreateEventData(randomEventType);
        eventTimer = currentEvent.duration;
        
        Debug.Log($"触发特殊事件: {currentEvent.description}");
        
        // 特殊事件UI
        //uiManager?.ShowSpecialEvent(currentEvent);
    }

    private EventData CreateEventData(ContractEventType type)
    {
        switch (type)
        {
            case ContractEventType.Phone:
                return new EventData
                {
                    type = ContractEventType.Phone,
                    description = "电话突然响起，需要接听",
                    duration = 5f,
                    OnResolve = (ctx) => { Debug.Log("成功接听电话"); completed = true; ctx.eventHandled = true; },
                    OnFail = (ctx) => { Debug.Log("未能及时接听电话"); ctx.DecreaseSatisfaction(); }
                };
                
            case ContractEventType.Gun:
                return new EventData
                {
                    type = ContractEventType.Gun,
                    description = "顾客突然拔枪，需要迅速应对!",
                    duration = 3f,
                    OnResolve = (ctx) => { Debug.Log("成功化解枪械威胁"); completed = true; ctx.eventHandled = true; ctx.IncreaseSatisfaction(); },
                    OnFail = (ctx) => { Debug.Log("未能应对枪械威胁"); ctx.AddFailure(); }
                };
                
            case ContractEventType.Epilepsy:
                return new EventData
                {
                    type = ContractEventType.Epilepsy,
                    description = "顾客突然癫痫发作，需要紧急救助",
                    duration = 8f,
                    OnResolve = (ctx) => { Debug.Log("成功救助癫痫顾客"); completed = true; ctx.eventHandled = true; },
                    OnFail = (ctx) => { Debug.Log("未能及时救助"); ctx.DecreaseSatisfaction(); }
                };
                
            case ContractEventType.Transform:
                return new EventData
                {
                    type = ContractEventType.Transform,
                    description = "顾客显露非人类特征，正在变形!",
                    duration = 4f,
                    OnResolve = (ctx) => { Debug.Log("镇定应对非人类顾客"); completed = true; ctx.eventHandled = true; },
                    OnFail = (ctx) => { Debug.Log("被非人类特征吓到"); ctx.DecreaseSatisfaction(2); }
                };
                
            case ContractEventType.Dialogue:
                return new EventData
                {
                    type = ContractEventType.Dialogue,
                    description = "顾客突然开始对话，需要适当回应",
                    duration = 6f,
                    OnResolve = (ctx) => { Debug.Log("恰当回应顾客对话"); completed = true; ctx.eventHandled = true; },
                    OnFail = (ctx) => { Debug.Log("回应不当"); ctx.DecreaseSatisfaction(); }
                };
                
            default:
                return null;
        }
    }

    private void HandleEventInput()
    {
        if (currentEvent == null) return;
        
        // 根据事件类型处理不同的输入
        switch (currentEvent.type)
        {
            case ContractEventType.Phone:
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    currentEvent.OnResolve?.Invoke(context);
                }
                break;
                
            case ContractEventType.Gun:
                if (Input.GetKeyDown(KeyCode.F))
                {
                    currentEvent.OnResolve?.Invoke(context);
                }
                break;
                
            case ContractEventType.Epilepsy:
                if (Input.GetKeyDown(KeyCode.H))
                {
                    currentEvent.OnResolve?.Invoke(context);
                }
                break;
                
            case ContractEventType.Transform:
                if (Input.GetKeyDown(KeyCode.C))
                {
                    currentEvent.OnResolve?.Invoke(context);
                }
                break;
                
            case ContractEventType.Dialogue:
                if (Input.GetKeyDown(KeyCode.R))
                {
                    currentEvent.OnResolve?.Invoke(context);
                }
                break;
        }
    }
}
#endregion

#region StampSystem Stages




/// <summary>
/// 盖章系统
/// </summary>
/// 
public class StampSystem : IContractStage
{
    private HeContractContext context;
    private HeContractUIManager uiManager;
    private HeContractGameConfig gameConfig;
    private bool completed = false;
    private bool failed = false;
    private bool stampSelected = false;
    private bool isCharging = false;
    private float chargeTime = 0f;
    private HeContractType selectedStampType;
    private HeContractType neededStampType;
    public bool IsCompleted => completed;
    public bool HasFailed => failed;
    public string StageName => "契约盖印";
    private StampType stampType = StampType.None;
    public void Enter()
    {
        context = GameObject.FindFirstObjectByType<SigningFlowManager>()?.ctx;
        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
     
        uiManager.ResotreContractDocumentsGameObject();
        gameConfig = GameObject.FindFirstObjectByType<SigningFlowManager>()?.gameConfig;
        neededStampType = HeContractType.Event; // TODO: 根据契约类型设置需要的印章类型
        Debug.Log("=== 开始契约盖印阶段 ===");


        SlotCenter.Instance.add_listener<HeSuccessLayer>(HeEventNames.OnChargingGameEnd, OnHandCharginSth);

        uiManager.ChargingGroupGameObject.GetComponent<HeChargingGame>()
            .SetHandle(new HeChargingGameHandler());


    }

    public void Update()
    {
        if (completed || failed) return;

        if (!stampSelected)
        {

        }
        else if (isCharging)
        {

        }
    }





    public void Exit()
    {
        Debug.Log("=== 契约盖印阶段结束 ===");

        ;
    }


    private void OnHandCharginSth(HeSuccessLayer type)
    {
        switch (type)
        {
            case HeSuccessLayer.BigSuccess:
                Debug.Log("盖章大成功!");
                // 处理大成功
                break;
            case HeSuccessLayer.Normal:
                // 处理占位符
                break;
            case HeSuccessLayer.Success:
                Debug.Log("盖章成功!");
                // 处理成功
                break;
            case HeSuccessLayer.Fail:
                Debug.Log("盖章失败!");
                // 处理失败
                break;
            case HeSuccessLayer.BigFail:
                // 处理大失败
                Debug.Log("盖章大失败!");
                break;
            default:
                // 处理未知类型
                break;
        }
    }
}
#endregion


#region SoulHarvestSystem Stages
/// <summary>
/// 灵魂收取系统
/// </summary>
public class SoulHarvestSystem : IContractStage
{
    private HeContractContext context;
    private HeContractUIManager uiManager;
    private HeContractGameConfig gameConfig;
    private bool completed = false;
    private bool failed = false;
    private bool cutterActive = false;
    private float targetPercentage;
    private float currentCutPosition = 0.5f;
    private float cutterShake = 0f;
    
    public bool IsCompleted => completed;
    public bool HasFailed => failed;
    public string StageName => "灵魂收取";

    public void Enter()
    {
        context = GameObject.FindFirstObjectByType<SigningFlowManager>()?.ctx;

        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        gameConfig = GameObject.FindFirstObjectByType<SigningFlowManager>()?.gameConfig;
        
        Debug.Log("=== 开始灵魂收取阶段 ===");
        
        targetPercentage = context.document.soulPercentage;
        Debug.Log($"需要收取 {targetPercentage * 100}% 的灵魂");
        
        // 显示灵魂分割界面
        uiManager?.ShowSoulHarvest(targetPercentage);
        cutterActive = true;
    }

    public void Update()
    {
        if (completed || failed) return;
        
        if (cutterActive)
        {
            HandleSoulCutting();
        }
    }

    public void Exit()
    {
        Debug.Log("=== 灵魂收取阶段结束 ===");
    }

    private void HandleSoulCutting()
    {
        // 左右移动分灵刀
        float moveInput = Input.GetAxis("Horizontal");
        float moveSpeed = gameConfig?.soulCutterMoveSpeed ?? 0.5f;
        currentCutPosition = Mathf.Clamp01(currentCutPosition + moveInput * Time.deltaTime * moveSpeed);
        
        // 添加手部颤抖效果
        cutterShake += Time.deltaTime;
        float shakeAmount = gameConfig?.soulCutterShake ?? 0.02f;
        float shakeOffset = Mathf.Sin(cutterShake * 10f) * shakeAmount;
        float actualPosition = currentCutPosition + shakeOffset;
        
        // TODO: 更新分灵刀位置显示
        
        // 点击进行切割
        if (Input.GetMouseButtonDown(0))
        {
            PerformSoulCut(currentCutPosition);
        }
    }

    private void PerformSoulCut(float cutPosition)
    {
        Debug.Log($"在位置 {cutPosition * 100}% 处切割灵魂");
        
        // 计算误差
        float error = Mathf.Abs(cutPosition - targetPercentage);
        float allowedError = gameConfig?.soulHarvestAccuracy ?? 0.05f; // 半成
        
        if (error <= allowedError)
        {
            Debug.Log("灵魂收取成功!");
            completed = true;
            context.soulHarvested = true;
        }
        else if (cutPosition > targetPercentage)
        {
            Debug.Log("收取过多灵魂，顾客感到被欺骗");
            context.DecreaseSatisfaction();
            failed = true;
        }
        else
        {
            Debug.Log("收取过少灵魂，浪费公司资产");
            context.AddFailure();
            failed = true;
        }
        
        cutterActive = false;
    }
}

#endregion
#endregion


#region Main Manager

/// <summary>
/// 签约流程管理器 - 
/// </summary>
public class SigningFlowManager : MonoBehaviour
{


    public void DebugStageStart()
    {
        currentStage?.Exit();
        debugStage.Enter();
        currentStage = debugStage;
    }

    public void RuneInputStageStart()
    {
        currentStage?.Exit();
        runeInputStage.Enter();
        currentStage = runeInputStage;
    }
    public void SpecialEventSystem()
    {
        currentStage?.Exit();
        specialEventStage.Enter();
        currentStage = specialEventStage;
    }
    public void StampStageStart()
    {
        currentStage?.Exit();
        stampStage.Enter();
        currentStage = stampStage;
    }
    public void SoulHarvestStageStart()
    {
        currentStage?.Exit();
        soulHarvestSystem.Enter();
        currentStage = soulHarvestSystem;
    }
    public void DocumentVerifierStageStart()
    {
        currentStage?.Exit();
        documentVerifierStage.Enter();
        currentStage = documentVerifierStage;
    }
    /// <summary>
    /// 初始化设置
    /// 传入天数[0,n]
    /// </summary>
    public void UpdataConfig(int day)
    {
        if (gameConfig == null)
        {
            Debug.LogWarning("未设置 HeContractGameConfig，将使用默认设置");


            gameConfig = ScriptableObject.CreateInstance<HeContractGameConfig>();
            if (fallbackConfig != null)
            {

                gameConfig.initialSatisfaction = fallbackConfig.initialSatisfaction;
                gameConfig.maxFailCount = fallbackConfig.maxFailCount;
                gameConfig.runeInputTimeLimit = fallbackConfig.runeInputTimeLimit;

            }

            gameConfig.ResetToDefault();
        }

        Debug.Log($"游戏配置已初始化 - 初始满意度: {gameConfig.initialSatisfaction}, 最大失败次数: {gameConfig.maxFailCount}");
    }
    [Header("=== 游戏配置 ===")]
    [Tooltip("游戏配置资源 (建议使用ScriptableObject)")]
    public HeContractGameConfig gameConfig;
    
    [Header("=== 备用配置 ===")]
    [Tooltip("如果没有配置资源，使用此内嵌配置")]
    public GameConfig fallbackConfig;
    
    [Header("=== 测试数据 ===")]
    [Tooltip("测试用的契约文书")]
    public ContractDocument testDocument;
    [Tooltip("测试用的顾客信息")]
    public Customer testCustomer;
    
    [Header("=== 随机生成设置 ===")]
    [Tooltip("是否使用随机生成的契约数据")]
    public bool useRandomGeneration = true;
    [Tooltip("随机顾客姓名列表")]
    public string[] customerNames = { "艾莉丝", "托马斯", "玛丽", "约翰", "凯瑟琳", "威廉" };
    [Tooltip("随机职业列表")]
    public string[] occupations = { "铁匠", "商人", "学者", "农夫", "工匠", "守卫" };
    [Tooltip("顾客照片资源")]
    public Sprite[] customerPhotos;
    
    // 私有变量
    private IContractStage currentStage;
    private Queue<IContractStage> stages;
    public HeContractContext ctx;
    private HeContractUIManager uiManager;





    private DebugStage debugStage = new DebugStage();
    private DocumentVerifier documentVerifierStage = new DocumentVerifier();
    private RuneInputManager runeInputStage = new RuneInputManager();
    private SpecialEventSystem specialEventStage = new SpecialEventSystem();
    private  StampSystem stampStage = new StampSystem();
    private SoulHarvestSystem soulHarvestSystem= new SoulHarvestSystem();

    void Start()
    {
        // 获取UI管理器
        uiManager = FindFirstObjectByType<HeContractUIManager>();
        
        // 初始化配置
        UpdataConfig(0);
        
        // 初始化契约
        InitializeContract();

        var st = SlotCenter.Instance;
        if (st != null)
        {
            st.add_listener(HeEventNames.TriggerDebugStage, DebugStageStart);
            st.add_listener(HeEventNames.TriggerRuneInputStage, RuneInputStageStart);
            st.add_listener(HeEventNames.TriggerStampStage, StampStageStart);   
            st.add_listener(HeEventNames.TriggerSoulHarvestStage, SoulHarvestStageStart);   
            st.add_listener(HeEventNames.TriggerSpecialEventStage, SpecialEventSystem);
            st.add_listener(HeEventNames.TriggerDocumentVerifierStage,DocumentVerifierStageStart);
        }
        else
        {
            Debug.LogError("SlotCenter实例未找到，事件系统可能无法正常工作");
        }
    }
    

    /// <summary>
    /// 概率判定rcx=0-100
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static bool ProbabilityDetermine(float s)
    {
        return UnityEngine.Random.Range(1, 101) > s;
    }
  


    private void InitializeContract()
    {
        ctx = new HeContractContext();
        
        // 决定使用测试数据还是随机生成
        if (!useRandomGeneration && testCustomer != null && testDocument != null)
        {
            ctx.customer = testCustomer;
            ctx.document = testDocument;
            Debug.Log("使用测试数据初始化契约");
        }
        else
        {
            GenerateRandomContract();
            Debug.Log("使用随机数据初始化契约");
        }
        
        ctx.satisfaction = gameConfig?.initialSatisfaction ?? 3;
        Debug.Log($"契约初始化完成 - 顾客: {ctx.customer.name}, 契约类型: {ctx.document.HeContractType}");
    }

    //private void SetupStages()
    //{
    //    stages = new Queue<IContractStage>(new IContractStage[] {
         
    //    });
        
    //    Debug.Log($"签约流程已设置，共{stages.Count}个阶段");
    //}

    //void NextStage()
    //{
    //    if (stages.Count == 0) 
    //    { 
    //        EndGame(true); 
    //        return; 
    //    }
        
    //    currentStage?.Exit();
    //    currentStage = stages.Dequeue();
    //    Debug.Log($"进入阶段: {currentStage.StageName}");
    //    StartCoroutine(HeCoroutineUtil.Run(() => {
    //        return Inner();
    //        System.Collections.IEnumerator Inner()
    //        {
    //            yield return new WaitForSeconds(1f);
    //            currentStage.Enter();
    //        }
    //    }));




    //}

    public void EndGame(bool success)
    {
        Debug.Log($"=== 游戏结束 ===");
        Debug.Log($"结果: {(success ? "签约成功" : "签约失败")}");
        Debug.Log($"最终满意度: {ctx.satisfaction}");
        Debug.Log($"失败次数: {ctx.failCount}");
        
        // 显示结果界面
        uiManager?.ShowGameResult(success, ctx);
        
        // TODO: 保存游戏结果、解锁成就等
    }

    private void GenerateRandomContract()
    {
        // 生成随机顾客
        ctx.customer = new Customer
        {
            name = GetRandomName(),
            occupation = GetRandomOccupation(),
            photo = GetRandomPhoto(),
            spokenRequest = GenerateRandomSpokenRequest()
        };
        
        // 生成随机契约
        HeContractType randomType = (HeContractType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(HeContractType)).Length);
        
        ctx.document = new ContractDocument
        {
            customerName = ctx.customer.name,
            occupation = ctx.customer.occupation,
            customerPhoto = ctx.customer.photo,
            appointmentDate = DateTime.Today,
            contractDescription = GenerateContractDescription(randomType),
            soulPercentage = gameConfig?.GetRandomSoulPercentage(randomType) ?? UnityEngine.Random.Range(0.2f, 0.6f),
            HeContractType = randomType
        };
        
        // 随机生成一些文书问题 (用于增加游戏难度)
        GenerateDocumentIssues();
    }

    private string GetRandomName()
    {
        if (customerNames.Length == 0) return "未知顾客";
        return customerNames[UnityEngine.Random.Range(0, customerNames.Length)];
    }

    private string GetRandomOccupation()
    {
        if (occupations.Length == 0) return "无业";
        return occupations[UnityEngine.Random.Range(0, occupations.Length)];
    }

    private Sprite GetRandomPhoto()
    {
        if (customerPhotos.Length == 0) return null;
        return customerPhotos[UnityEngine.Random.Range(0, customerPhotos.Length)];
    }

    private string GenerateRandomSpokenRequest()
    {
        string[] requests = {
            "我希望获得更多的财富",
            "我想要变得更有名气",
            "我渴望学会新的技能",
            "我需要改变我的命运",
            "我想要获得力量"
        };
        
        return requests[UnityEngine.Random.Range(0, requests.Length)];
    }

    private string GenerateContractDescription(HeContractType type)
    {
        return type switch
        {
            HeContractType.Money => "渴望获得财富",
            HeContractType.Fame => "期望获得名望",
            HeContractType.Skill => "渴望识读文字",
            HeContractType.Event => "希望改变命运",
            _ => "未知需求"
        };
    }

    /// <summary>
    /// 生成文书问题(用于测试和随机化)
    /// </summary>
    private void GenerateDocumentIssues()
    {
        // 使用枚举来更清晰地管理错误生成
        var errorGenerationRules = new Dictionary<DocumentError, float>
        {
            { DocumentError.BrokenSeal, 0.1f },              // 10%几率封蜡破损
            { DocumentError.ForgeryDocument, 0.05f },        // 5%几率伪造文书
            { DocumentError.MissingWatermark, 0.03f },       // 3%几率缺少水印
            { DocumentError.FakeInk, 0.03f },                // 3%几率假冒墨水
            { DocumentError.ContentMismatch, 0.12f },        // 12%几率内容不匹配
            { DocumentError.IncorrectDate, 0.1f },           // 10%几率日期错误
            { DocumentError.DisguisedCustomer, 0.05f },      // 5%几率伪装顾客
            { DocumentError.DangerousCustomer, 0.02f }       // 2%几率危险人物
        };
        
        foreach (var rule in errorGenerationRules)
        {
            if (UnityEngine.Random.value < rule.Value)
            {
                ApplyDocumentError(rule.Key);
            }
        }
    }
    
    /// <summary>
    /// 应用特定的文书错误
    /// </summary>
    private void ApplyDocumentError(DocumentError error)
    {
        switch (error)
        {
            case DocumentError.BrokenSeal:
                ctx.document.isSealed = false;
                Debug.Log($"生成文书问题: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.ForgeryDocument:
                ctx.document.isGenuine = false;
                Debug.Log($"生成文书问题: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.MissingWatermark:
                ctx.document.hasITCWatermark = false;
                Debug.Log($"生成文书问题: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.FakeInk:
                ctx.document.isInkGenuine = false;
                Debug.Log($"生成文书问题: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.ContentMismatch:
                ctx.document.isContentMatched = false;
                Debug.Log($"生成文书问题: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.IncorrectDate:
                ctx.document.isDateCorrect = false;
                ctx.document.appointmentDate = DateTime.Today.AddDays(UnityEngine.Random.Range(-3, 4));
                Debug.Log($"生成文书问题: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.DisguisedCustomer:
                ctx.customer.isDisguised = true;
                ctx.document.isIdentityMatched = false;
                Debug.Log($"生成文书问题: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.DangerousCustomer:
                ctx.customer.isClocardalMember = true;
                Debug.Log($"生成文书问题: {DocumentVerifier.GetErrorDescription(error)}");
                break;
        }
    }
    
    /// <summary>
    /// 获取当前契约的所有文书错误
    /// </summary>
    public List<DocumentError> GetCurrentDocumentErrors()
    {
        var currentVerifier = currentStage as DocumentVerifier;
        return currentVerifier?.DetectedErrors ?? new List<DocumentError>();
    }
    
    /// <summary>
    /// 公共方法：重新开始契约
    /// </summary>
    public void RestartContract()
    {
        Debug.Log("重新开始契约签约流程");
        
        // 重置游戏状态
        ctx = null;
        currentStage = null;
        stages?.Clear();
        
        // 重新开始
        Start();
    }

    /// <summary>
    /// 公共方法：退出游戏
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("退出游戏");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// 公共方法：暂停游戏
    /// </summary>
    public void PauseGame()
    {
        Time.timeScale = Time.timeScale > 0 ? 0 : 1;
    }

    /// <summary>
    /// 获取当前游戏状态信息 (供调试或UI使用)
    /// </summary>
    public string GetGameStateInfo()
    {
        if (ctx == null) return "游戏未初始化";
        
        return $"当前阶段: {currentStage?.StageName ?? "无"}\n" +
               $"满意度: {ctx.satisfaction}\n" +
               $"失败次数: {ctx.failCount}\n" +
               $"顾客: {ctx.customer?.name ?? "未知"}";
    }
}
#endregion

