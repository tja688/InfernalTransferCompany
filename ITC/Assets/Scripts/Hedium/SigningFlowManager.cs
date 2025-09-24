using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#region Contract Stages

/// <summary>
/// 文书核验系统
/// </summary>
public class DocumentVerifier : IContractStage
{
    private HeContractContext context;
    private HeContractUIManager uiManager;
    private bool completed = false;
    private bool failed = false;
    
    public bool IsCompleted => completed;
    public bool HasFailed => failed;
    public string StageName => "文书核验";

    public void Enter(HeContractContext ctx)
    {
        context = ctx;
        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        
        Debug.Log("=== 开始文书核验阶段 ===");
        
        // 显示文书核验UI
        uiManager?.ShowDocumentVerification(ctx);
        
        // 自动执行核验逻辑
        PerformDocumentVerification();
    }

    public void Update()
    {
        // 等待UI交互或自动完成
    }

    public void Exit()
    {
        Debug.Log("=== 文书核验阶段结束 ===");
    }

    private void PerformDocumentVerification()
    {
        var doc = context.document;
        var customer = context.customer;
        
        // 检查封蜡
        if (!doc.isSealed)
        {
            Debug.Log("文书核验失败: 封蜡已破损");
            failed = true;
            return;
        }
        
        // 检查是否伪造
        if (!doc.isGenuine || !doc.hasITCWatermark || !doc.isInkGenuine)
        {
            Debug.Log("文书核验失败: 发现伪造文书");
            failed = true;
            return;
        }
        
        // 检查内容匹配
        if (!doc.isContentMatched)
        {
            Debug.Log("文书核验失败: 口述内容与契约不符");
            failed = true;
            return;
        }
        
        // 检查日期
        if (!doc.isDateCorrect)
        {
            Debug.Log("文书核验失败: 预约日期不符");
            failed = true;
            return;
        }
        
        // 检查身份
        if (!doc.isIdentityMatched || customer.isDisguised)
        {
            Debug.Log("文书核验失败: 身份不符或发现伪装");
            failed = true;
            return;
        }
        
        // 所有检查通过
        Debug.Log("文书核验成功");
        context.documentVerified = true;
        completed = true;
    }
}

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
    private List<RuneType> requiredRunes;
    private List<RuneType> inputRunes;
    private float timeRemaining;
    
    public bool IsCompleted => completed;
    public bool HasFailed => failed;
    public string StageName => "符文输入";

    public void Enter(HeContractContext ctx)
    {
        context = ctx;
        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        gameConfig = GameObject.FindFirstObjectByType<SigningFlowManager>()?.gameConfig;
        
        Debug.Log("=== 开始符文输入阶段 ===");
        
        GenerateRequiredRunes();
        inputRunes = new List<RuneType>();
        timeRemaining = gameConfig?.runeInputTimeLimit ?? 10f;
        
        // 符文输入UI
        //uiManager?.ShowRuneInput(requiredRunes);
    }

    public void Update()
    {
        if (completed || failed) return;
        
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0)
        {
            Debug.Log("符文输入超时");
            failed = true;
            context.AddFailure();
            return;
        }
        
        // 检查输入完成
        if (inputRunes.Count >= requiredRunes.Count)
        {
            CheckRuneSequence();
        }
        
        // 处理输入
        HandleRuneInput();
    }

    public void Exit()
    {
        Debug.Log("=== 符文输入阶段结束 ===");
    }

    private void GenerateRequiredRunes()
    {
        // 从配置文件获取符文序列
        if (gameConfig != null)
        {
            requiredRunes = gameConfig.GetRuneSequenceForContract(context.document.HeContractType);
        }
        else
        {
            // 默认序列
            requiredRunes = GetDefaultRuneSequence(context.document.HeContractType);
        }
        
        Debug.Log($"需要输入符文序列: {string.Join(", ", requiredRunes)}");
    }

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

    private void HandleRuneInput()
    {
        // WASD键输入处理
        if (Input.GetKeyDown(KeyCode.W)) ProcessRuneInput(RuneType.Fire);
        if (Input.GetKeyDown(KeyCode.A)) ProcessRuneInput(RuneType.Water);
        if (Input.GetKeyDown(KeyCode.S)) ProcessRuneInput(RuneType.Earth);
        if (Input.GetKeyDown(KeyCode.D)) ProcessRuneInput(RuneType.Air);
        if (Input.GetKeyDown(KeyCode.Q)) ProcessRuneInput(RuneType.Light);
        if (Input.GetKeyDown(KeyCode.E)) ProcessRuneInput(RuneType.Dark);
    }

    private void ProcessRuneInput(RuneType rune)
    {
        inputRunes.Add(rune);
        Debug.Log($"输入符文: {rune}");
        
        // 检查是否错误
        int currentIndex = inputRunes.Count - 1;
        if (currentIndex < requiredRunes.Count && inputRunes[currentIndex] != requiredRunes[currentIndex])
        {
            context.runeErrors++;
            Debug.Log($"符文输入错误! 错误次数: {context.runeErrors}");
            
            if (context.runeErrors >= 2)
            {
                context.DecreaseSatisfaction();
            }
            
            if (context.runeErrors >= (gameConfig?.maxRuneErrors ?? 3))
            {
                Debug.Log("符文错误次数过多，签约失败");
                failed = true;
                context.AddFailure();
            }
        }
        
        // 更新UI
        uiManager?.UpdateRuneInputProgress(inputRunes.Count, context.runeErrors);
    }

    private void CheckRuneSequence()
    {
        bool isCorrect = true;
        for (int i = 0; i < requiredRunes.Count; i++)
        {
            if (i >= inputRunes.Count || inputRunes[i] != requiredRunes[i])
            {
                isCorrect = false;
                break;
            }
        }
        
        if (isCorrect)
        {
            Debug.Log("符文输入完成!");
            context.runesCompleted = true;
            completed = true;
            
            // 30%几率触发符文核对
            if (UnityEngine.Random.value < (gameConfig?.runeVerificationTriggerChance ?? 0.3f))
            {
                TriggerRuneVerification();
            }
        }
    }

    private void TriggerRuneVerification()
    {
        Debug.Log("触发符文核对环节");
        // TODO: 创建符文核对数据并显示UI
        // uiManager?.ShowRuneVerification(runeGridData);
    }
}

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

    public void Enter(HeContractContext ctx)
    {
        context = ctx;
        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        gameConfig = GameObject.FindFirstObjectByType<SigningFlowManager>()?.gameConfig;
        
        Debug.Log("=== 开始特殊事件阶段 ===");
        
        // 随机决定是否触发事件
        float triggerChance = gameConfig?.eventTriggerChance ?? 0.3f;
        if (UnityEngine.Random.value < triggerChance)
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

/// <summary>
/// 盖章系统
/// </summary>
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
    
    public bool IsCompleted => completed;
    public bool HasFailed => failed;
    public string StageName => "契约盖印";

    public void Enter(HeContractContext ctx)
    {
        context = ctx;
        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        gameConfig = GameObject.FindFirstObjectByType<SigningFlowManager>()?.gameConfig;
        
        Debug.Log("=== 开始契约盖印阶段 ===");
        
        // 显示印章选择UI
        uiManager?.ShowStampSelection(ctx.document.HeContractType);
    }

    public void Update()
    {
        if (completed || failed) return;
        
        if (!stampSelected)
        {
            HandleStampSelection();
        }
        else if (isCharging)
        {
            HandleStampCharging();
        }
    }

    public void Exit()
    {
        Debug.Log("=== 契约盖印阶段结束 ===");
    }

    private void HandleStampSelection()
    {
        // 数字键选择印章
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectStamp(HeContractType.Money);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectStamp(HeContractType.Fame);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectStamp(HeContractType.Skill);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectStamp(HeContractType.Event);
    }

    private void SelectStamp(HeContractType stampType)
    {
        selectedStampType = stampType;
        
        // 检查印章类型是否正确
        if (stampType != context.document.HeContractType)
        {
            Debug.Log($"印章类型错误! 选择了{stampType}，应该是{context.document.HeContractType}");
            context.AddFailure();
            failed = true;
            return;
        }
        
        Debug.Log($"正确选择了{stampType}印章");
        stampSelected = true;
        StartStampCharging();
    }

    private void StartStampCharging()
    {
        Debug.Log("开始盖印仪式，符文开始发光...");
        isCharging = true;
        chargeTime = 0f;
        
        // 通知UI开始蓄力
        uiManager?.StartStampCharging();
    }

    private void HandleStampCharging()
    {
        chargeTime += Time.deltaTime;
        
        float maxChargeTime = gameConfig?.stampChargeTime ?? 3f;
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 检查蓄力时机
            float optimalTiming = gameConfig?.stampOptimalTiming ?? 0.8f;
            float optimalTime = maxChargeTime * optimalTiming;
            float timeDiff = Mathf.Abs(chargeTime - optimalTime);
            float tolerance = gameConfig?.stampAccuracyTolerance ?? 0.1f;
            
            if (timeDiff < tolerance)
            {
                Debug.Log("完美盖章! 顾客满意度提升");
                context.IncreaseSatisfaction();
                completed = true;
                context.stampApplied = true;
            }
            else
            {
                context.stampAttempts++;
                Debug.Log($"盖章时机不准确，尝试次数: {context.stampAttempts}");
                
                int maxAttempts = gameConfig?.maxStampAttempts ?? 3;
                if (context.stampAttempts >= maxAttempts)
                {
                    Debug.Log("盖章失败次数过多，顾客满意度下降");
                    context.DecreaseSatisfaction();
                    failed = true;
                }
                else
                {
                    // 重新开始蓄力
                    chargeTime = 0f;
                    uiManager?.StartStampCharging();
                }
            }
        }
        
        // 蓄力超时
        if (chargeTime > maxChargeTime * 1.2f)
        {
            Debug.Log("蓄力超时，需要重新开始");
            chargeTime = 0f;
            context.stampAttempts++;
        }
    }
}

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

    public void Enter(HeContractContext ctx)
    {
        context = ctx;
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

#region Main Manager

/// <summary>
/// 签约流程管理器 - 游戏主控制器
/// </summary>
public class SigningFlowManager : MonoBehaviour
{
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

    void Start()
    {
        // 获取UI管理器
        uiManager = FindFirstObjectByType<HeContractUIManager>();
        
        // 初始化配置
        InitializeConfig();
        
        // 初始化契约
        InitializeContract();
        
        // 设置流程阶段
        SetupStages();
        
        // 开始第一个阶段
        NextStage();
    }
    
    void Update()
    {
        if (currentStage == null) return;
        
        currentStage.Update();
        
        // 更新UI状态
        uiManager?.UpdateContext(ctx);
       // uiManager?.UpdateCurrentStage(currentStage.StageName);
        
        if (currentStage.IsCompleted)
        {
            // 检查失败条件
            int maxFailCount = gameConfig?.maxFailCount ?? fallbackConfig?.maxFailCount ?? 3;
            int minSatisfaction = gameConfig?.minSatisfaction ?? fallbackConfig?.minSatisfaction ?? 1;
            
            if (currentStage.HasFailed || ctx.failCount >= maxFailCount || ctx.satisfaction <= minSatisfaction)
            {
                EndGame(false);
                return;
            }
            NextStage();
        }
    }

    /// <summary>
    /// 初始化配置
    /// </summary>
    private void InitializeConfig()
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

    private void SetupStages()
    {
        stages = new Queue<IContractStage>(new IContractStage[] {
            new DocumentVerifier(),
            new RuneInputManager(),
            new SpecialEventSystem(),
            new StampSystem(),
            new SoulHarvestSystem()
        });
        
        Debug.Log($"签约流程已设置，共{stages.Count}个阶段");
    }

    void NextStage()
    {
        if (stages.Count == 0) 
        { 
            EndGame(true); 
            return; 
        }
        
        currentStage?.Exit();
        currentStage = stages.Dequeue();
        Debug.Log($"进入阶段: {currentStage.StageName}");
        currentStage.Enter(ctx);
    }

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

    private void GenerateDocumentIssues()
    {
        // 10%几率生成各种文书问题
        if (UnityEngine.Random.value < 0.1f)
        {
            ctx.document.isSealed = false;
            Debug.Log("生成文书问题: 封蜡破损");
        }
        
        if (UnityEngine.Random.value < 0.05f)
        {
            ctx.document.isGenuine = false;
            Debug.Log("生成文书问题: 伪造文书");
        }
        
        if (UnityEngine.Random.value < 0.1f)
        {
            ctx.document.isDateCorrect = false;
            Debug.Log("生成文书问题: 日期不符");
        }
        
        if (UnityEngine.Random.value < 0.05f)
        {
            ctx.customer.isDisguised = true;
            Debug.Log("生成文书问题: 顾客身份不符");
        }
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