using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;


///
///
// public const bool ProbabilityDetermine(float s)
//
// {
//    return true;
//     }



/// <summary>
/// �����������ö��
/// ���ڱ�ʶ�������������п��ܳ��ֵĸ�������
/// </summary>
public enum DocumentError
{
    /// <summary>�������� - ����ķ���������</summary>
    BrokenSeal,

    /// <summary>α������ - ���鱾��ϵα��</summary>
    ForgeryDocument,

    /// <summary>ȱ��ˮӡ - ����ȱ��ITC��˾�ٷ�ˮӡ</summary>
    MissingWatermark,

    /// <summary>��ðīˮ - ʹ���˷ǹٷ�īˮ</summary>
    FakeInk,

    /// <summary>���ݲ��� - �˿Ϳ�����������Լ���ݲ�ƥ��</summary>
    ContentMismatch,

    /// <summary>���ڴ��� - ԤԼ�����뵱ǰ���ڲ���</summary>
    IncorrectDate,

    /// <summary>��ݲ��� - �����¼�������ʵ�ʲ���</summary>
    IdentityMismatch,

    /// <summary>αװ�˿� - �˿�ʹ�ü����</summary>
    DisguisedCustomer,

    /// <summary>Σ������ - ����˴�����Ա������Σ������</summary>
    DangerousCustomer
}
#region Contract Stages

/// <summary>
/// �������ϵͳ
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
    public string StageName => "�������";
    public List<DocumentError> DetectedErrors => detectedErrors;

    public void Enter(HeContractContext ctx)
    {
        context = ctx;
        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        
        Debug.Log("=== ��ʼ�������׶� ===");
        
        // ��ʾ�������UI
        uiManager?.ShowDocumentVerification(ctx);
        
        // ִ�к����߼�
        PerformDocumentVerification();
        Debug.Assert(detectedErrors.Count>0);
    
       
         Debug.Log($"��⵽{detectedErrors.Count}�����⣬�ȴ���Ҿ���...");


       
        Action<DocumentError> documentClickedHandler = null;

      
        documentClickedHandler = (error) =>
        {
            

            Debug.Log($"��ҷ������������: {error}");
            if (detectedErrors.Contains(error))
            {
                detectedErrors.Remove(error);
                Debug.Log($"����{error}�Ѵ���,�������鲻ͨ��");
                uiManager.SwitchPanel(HeContractUIManager.UIState.None);
                uiManager.OnDocumentClicked -= documentClickedHandler;
                detectedErrors.Clear();
            }
            else
            {
                Debug.Log($"����{error}���ڴ������б���");
                //TODO�������ж�ʧ���߼�





            }
        };
        Action<DocumentError> nextStageClickedHandler = null;


        nextStageClickedHandler = (error) =>
        {

            if (detectedErrors.Count()>0)
            {
                Debug.Log($"����{error}�����������ʧ��,��һ�δ���");
                ctx.AddFailure();

            }
            else
            {

                Debug.Log($"û���κδ����������ͨ��");


            }
        };
        uiManager.OnDocumentClicked += documentClickedHandler;


    }

    public void Update()
    {
    


    }

    public void Exit()
    {
        Debug.Log("=== �������׶ν��� ===");
        if (detectedErrors.Count > 0)
        {
            Debug.Log($"��⵽�Ĵ���: {string.Join(", ", detectedErrors)}");
        }
    }

    private void PerformDocumentVerification()
    {
        var doc = context.document;
        var customer = context.customer;
        detectedErrors.Clear();
        
        // ������
        if (!doc.isSealed)
        {
            detectedErrors.Add(DocumentError.BrokenSeal);
            Debug.Log($"�������: {GetErrorDescription(DocumentError.BrokenSeal)}");
        }
        
        // ���������α
        if (!doc.isGenuine)
        {
            detectedErrors.Add(DocumentError.ForgeryDocument);
            Debug.Log($"�������: {GetErrorDescription(DocumentError.ForgeryDocument)}");
        }
        
        if (!doc.hasITCWatermark)
        {
            detectedErrors.Add(DocumentError.MissingWatermark);
            Debug.Log($"�������: {GetErrorDescription(DocumentError.MissingWatermark)}");
        }
        
        if (!doc.isInkGenuine)
        {
            detectedErrors.Add(DocumentError.FakeInk);
            Debug.Log($"�������: {GetErrorDescription(DocumentError.FakeInk)}");
        }
        
        // �������ƥ��
        if (!doc.isContentMatched)
        {
            detectedErrors.Add(DocumentError.ContentMismatch);
            Debug.Log($"�������: {GetErrorDescription(DocumentError.ContentMismatch)}");
        }
        
        // �������
        if (!doc.isDateCorrect)
        {
            detectedErrors.Add(DocumentError.IncorrectDate);
            Debug.Log($"�������: {GetErrorDescription(DocumentError.IncorrectDate)}");
        }
        
        // ������
        if (!doc.isIdentityMatched)
        {
            detectedErrors.Add(DocumentError.IdentityMismatch);
            Debug.Log($"�������: {GetErrorDescription(DocumentError.IdentityMismatch)}");
        }
        
        if (customer.isDisguised)
        {
            detectedErrors.Add(DocumentError.DisguisedCustomer);
            Debug.Log($"�������: {GetErrorDescription(DocumentError.DisguisedCustomer)}");
        }
        
        if (customer.isClocardalMember)
        {
            detectedErrors.Add(DocumentError.DangerousCustomer);
            Debug.Log($"�������: {GetErrorDescription(DocumentError.DangerousCustomer)}");
        }
        
        // �ж���֤���
        if (detectedErrors.Count > 0)
        {
            Debug.Log($"������鷢�� {detectedErrors.Count} �����⣬��Ҫ�ܾ�ǩԼ");
            // �����Ҫѡ���Ƿ�ܾ��������ȱ��Ϊ��Ҫ����
            // ʵ����Ϸ��Ӧ�õȴ���Ҿ���
        }
        else
        {
            Debug.Log("�������ͨ�������м����Ŀ����");
            context.documentVerified = true;
            completed = true;
        }
    }
    
    /// <summary>
    /// ��ȡ��������
    /// </summary>
    public static string GetErrorDescription(DocumentError error)
    {
        return error switch
        {
            DocumentError.BrokenSeal => "����������",
            DocumentError.ForgeryDocument => "����ϵα��",
            DocumentError.MissingWatermark => "ȱ��ITCˮӡ",
            DocumentError.FakeInk => "īˮϵ��ð",
            DocumentError.ContentMismatch => "������������Լ����",
            DocumentError.IncorrectDate => "ԤԼ���ڲ���ȷ",
            DocumentError.IdentityMismatch => "���֤������",
            DocumentError.DisguisedCustomer => "�˿�������",
            DocumentError.DangerousCustomer => "Σ������(����˴�����Ա)",
            _ => "δ֪����"
        };
    }

    public static bool ProbabilityDetermine(float s)
    {
        return UnityEngine.Random.Range(1, 101) < s;
    }
}

/// <summary>
/// �������������
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
    public string StageName => "��������";

    public void Enter(HeContractContext ctx)
    {
        context = ctx;
        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        gameConfig = GameObject.FindFirstObjectByType<SigningFlowManager>()?.gameConfig;
        
        Debug.Log("=== ��ʼ��������׶� ===");
        
        GenerateRequiredRunes();
        inputRunes = new List<RuneType>();
        timeRemaining = gameConfig?.runeInputTimeLimit ?? 10f;
        
        // ��������UI
        //uiManager?.ShowRuneInput(requiredRunes);
    }

    public void Update()
    {
        if (completed || failed) return;
        
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0)
        {
            Debug.Log("�������볬ʱ");
            failed = true;
            context.AddFailure();
            return;
        }
        
        // ����������
        if (inputRunes.Count >= requiredRunes.Count)
        {
            CheckRuneSequence();
        }
        
        // ��������
        HandleRuneInput();
    }

    public void Exit()
    {
        Debug.Log("=== ��������׶ν��� ===");
    }

    private void GenerateRequiredRunes()
    {
        // �������ļ���ȡ��������
        if (gameConfig != null)
        {
            requiredRunes = gameConfig.GetRuneSequenceForContract(context.document.HeContractType);
        }
        else
        {
            // Ĭ������
            requiredRunes = GetDefaultRuneSequence(context.document.HeContractType);
        }
        
        Debug.Log($"��Ҫ�����������: {string.Join(", ", requiredRunes)}");
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
        // WASD�����봦��
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
        Debug.Log($"�������: {rune}");
        
        // ����Ƿ����
        int currentIndex = inputRunes.Count - 1;
        if (currentIndex < requiredRunes.Count && inputRunes[currentIndex] != requiredRunes[currentIndex])
        {
            context.runeErrors++;
            Debug.Log($"�����������! �������: {context.runeErrors}");
            
            if (context.runeErrors >= 2)
            {
                context.DecreaseSatisfaction();
            }
            
            if (context.runeErrors >= (gameConfig?.maxRuneErrors ?? 3))
            {
                Debug.Log("���Ĵ���������࣬ǩԼʧ��");
                failed = true;
                context.AddFailure();
            }
        }
        
        // ����UI
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
            Debug.Log("�����������!");
            context.runesCompleted = true;
            completed = true;
            
            // 30%���ʴ������ĺ˶�
            if (UnityEngine.Random.value < (gameConfig?.runeVerificationTriggerChance ?? 0.3f))
            {
                TriggerRuneVerification();
            }
        }
    }

    private void TriggerRuneVerification()
    {
        Debug.Log("�������ĺ˶Ի���");
        // TODO: �������ĺ˶����ݲ���ʾUI
        // uiManager?.ShowRuneVerification(runeGridData);
    }
}

/// <summary>
/// �����¼�ϵͳ
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
    public string StageName => "�����¼�";

    public void Enter(HeContractContext ctx)
    {
        context = ctx;
        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        gameConfig = GameObject.FindFirstObjectByType<SigningFlowManager>()?.gameConfig;
        
        Debug.Log("=== ��ʼ�����¼��׶� ===");
        
        // ��������Ƿ񴥷��¼�
        float triggerChance = gameConfig?.eventTriggerChance ?? 0.3f;
        if (UnityEngine.Random.value < triggerChance)
        {
            TriggerRandomEvent();
        }
        else
        {
            // û���¼���ֱ�����
            completed = true;
            context.eventHandled = true;
        }
    }

    public void Update()
    {
        if (completed || failed || currentEvent == null) return;
        
        eventTimer -= Time.deltaTime;
        
        // �����¼�����
        HandleEventInput();
        
        // �¼���ʱ
        if (eventTimer <= 0)
        {
            Debug.Log("�����¼�����ʱ");
            currentEvent.OnFail?.Invoke(context);
            failed = true;
        }
    }

    public void Exit()
    {
        Debug.Log("=== �����¼��׶ν��� ===");
    }

    private void TriggerRandomEvent()
    {
        var eventTypes = System.Enum.GetValues(typeof(ContractEventType));
        var randomEventType = (ContractEventType)eventTypes.GetValue(UnityEngine.Random.Range(0, eventTypes.Length));
        
        currentEvent = CreateEventData(randomEventType);
        eventTimer = currentEvent.duration;
        
        Debug.Log($"���������¼�: {currentEvent.description}");
        
        // �����¼�UI
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
                    description = "�绰ͻȻ������Ҫ����",
                    duration = 5f,
                    OnResolve = (ctx) => { Debug.Log("�ɹ������绰"); completed = true; ctx.eventHandled = true; },
                    OnFail = (ctx) => { Debug.Log("δ�ܼ�ʱ�����绰"); ctx.DecreaseSatisfaction(); }
                };
                
            case ContractEventType.Gun:
                return new EventData
                {
                    type = ContractEventType.Gun,
                    description = "�˿�ͻȻ��ǹ����ҪѸ��Ӧ��!",
                    duration = 3f,
                    OnResolve = (ctx) => { Debug.Log("�ɹ�����ǹе��в"); completed = true; ctx.eventHandled = true; ctx.IncreaseSatisfaction(); },
                    OnFail = (ctx) => { Debug.Log("δ��Ӧ��ǹе��в"); ctx.AddFailure(); }
                };
                
            case ContractEventType.Epilepsy:
                return new EventData
                {
                    type = ContractEventType.Epilepsy,
                    description = "�˿�ͻȻ��﷢������Ҫ��������",
                    duration = 8f,
                    OnResolve = (ctx) => { Debug.Log("�ɹ��������˿�"); completed = true; ctx.eventHandled = true; },
                    OnFail = (ctx) => { Debug.Log("δ�ܼ�ʱ����"); ctx.DecreaseSatisfaction(); }
                };
                
            case ContractEventType.Transform:
                return new EventData
                {
                    type = ContractEventType.Transform,
                    description = "�˿���¶���������������ڱ���!",
                    duration = 4f,
                    OnResolve = (ctx) => { Debug.Log("��Ӧ�Է�����˿�"); completed = true; ctx.eventHandled = true; },
                    OnFail = (ctx) => { Debug.Log("�������������ŵ�"); ctx.DecreaseSatisfaction(2); }
                };
                
            case ContractEventType.Dialogue:
                return new EventData
                {
                    type = ContractEventType.Dialogue,
                    description = "�˿�ͻȻ��ʼ�Ի�����Ҫ�ʵ���Ӧ",
                    duration = 6f,
                    OnResolve = (ctx) => { Debug.Log("ǡ����Ӧ�˿ͶԻ�"); completed = true; ctx.eventHandled = true; },
                    OnFail = (ctx) => { Debug.Log("��Ӧ����"); ctx.DecreaseSatisfaction(); }
                };
                
            default:
                return null;
        }
    }

    private void HandleEventInput()
    {
        if (currentEvent == null) return;
        
        // �����¼����ʹ���ͬ������
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
/// ����ϵͳ
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
    public string StageName => "��Լ��ӡ";

    public void Enter(HeContractContext ctx)
    {
        context = ctx;
        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        gameConfig = GameObject.FindFirstObjectByType<SigningFlowManager>()?.gameConfig;
        
        Debug.Log("=== ��ʼ��Լ��ӡ�׶� ===");
        
        // ��ʾӡ��ѡ��UI
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
        Debug.Log("=== ��Լ��ӡ�׶ν��� ===");
    }

    private void HandleStampSelection()
    {
        // ���ּ�ѡ��ӡ��
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectStamp(HeContractType.Money);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectStamp(HeContractType.Fame);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectStamp(HeContractType.Skill);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectStamp(HeContractType.Event);
    }

    private void SelectStamp(HeContractType stampType)
    {
        selectedStampType = stampType;
        
        // ���ӡ�������Ƿ���ȷ
        if (stampType != context.document.HeContractType)
        {
            Debug.Log($"ӡ�����ʹ���! ѡ����{stampType}��Ӧ����{context.document.HeContractType}");
            context.AddFailure();
            failed = true;
            return;
        }
        
        Debug.Log($"��ȷѡ����{stampType}ӡ��");
        stampSelected = true;
        StartStampCharging();
    }

    private void StartStampCharging()
    {
        Debug.Log("��ʼ��ӡ��ʽ�����Ŀ�ʼ����...");
        isCharging = true;
        chargeTime = 0f;
        
        // ֪ͨUI��ʼ����
        uiManager?.StartStampCharging();
    }

    private void HandleStampCharging()
    {
        chargeTime += Time.deltaTime;
        
        float maxChargeTime = gameConfig?.stampChargeTime ?? 3f;
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // �������ʱ��
            float optimalTiming = gameConfig?.stampOptimalTiming ?? 0.8f;
            float optimalTime = maxChargeTime * optimalTiming;
            float timeDiff = Mathf.Abs(chargeTime - optimalTime);
            float tolerance = gameConfig?.stampAccuracyTolerance ?? 0.1f;
            
            if (timeDiff < tolerance)
            {
                Debug.Log("��������! �˿����������");
                context.IncreaseSatisfaction();
                completed = true;
                context.stampApplied = true;
            }
            else
            {
                context.stampAttempts++;
                Debug.Log($"����ʱ����׼ȷ�����Դ���: {context.stampAttempts}");
                
                int maxAttempts = gameConfig?.maxStampAttempts ?? 3;
                if (context.stampAttempts >= maxAttempts)
                {
                    Debug.Log("����ʧ�ܴ������࣬�˿�������½�");
                    context.DecreaseSatisfaction();
                    failed = true;
                }
                else
                {
                    // ���¿�ʼ����
                    chargeTime = 0f;
                    uiManager?.StartStampCharging();
                }
            }
        }
        
        // ������ʱ
        if (chargeTime > maxChargeTime * 1.2f)
        {
            Debug.Log("������ʱ����Ҫ���¿�ʼ");
            chargeTime = 0f;
            context.stampAttempts++;
        }
    }
}

/// <summary>
/// �����ȡϵͳ
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
    public string StageName => "�����ȡ";

    public void Enter(HeContractContext ctx)
    {
        context = ctx;
        uiManager = GameObject.FindFirstObjectByType<HeContractUIManager>();
        gameConfig = GameObject.FindFirstObjectByType<SigningFlowManager>()?.gameConfig;
        
        Debug.Log("=== ��ʼ�����ȡ�׶� ===");
        
        targetPercentage = context.document.soulPercentage;
        Debug.Log($"��Ҫ��ȡ {targetPercentage * 100}% �����");
        
        // ��ʾ���ָ����
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
        Debug.Log("=== �����ȡ�׶ν��� ===");
    }

    private void HandleSoulCutting()
    {
        // �����ƶ����鵶
        float moveInput = Input.GetAxis("Horizontal");
        float moveSpeed = gameConfig?.soulCutterMoveSpeed ?? 0.5f;
        currentCutPosition = Mathf.Clamp01(currentCutPosition + moveInput * Time.deltaTime * moveSpeed);
        
        // ����ֲ�����Ч��
        cutterShake += Time.deltaTime;
        float shakeAmount = gameConfig?.soulCutterShake ?? 0.02f;
        float shakeOffset = Mathf.Sin(cutterShake * 10f) * shakeAmount;
        float actualPosition = currentCutPosition + shakeOffset;
        
        // TODO: ���·��鵶λ����ʾ
        
        // ��������и�
        if (Input.GetMouseButtonDown(0))
        {
            PerformSoulCut(currentCutPosition);
        }
    }

    private void PerformSoulCut(float cutPosition)
    {
        Debug.Log($"��λ�� {cutPosition * 100}% ���и����");
        
        // �������
        float error = Mathf.Abs(cutPosition - targetPercentage);
        float allowedError = gameConfig?.soulHarvestAccuracy ?? 0.05f; // ���
        
        if (error <= allowedError)
        {
            Debug.Log("�����ȡ�ɹ�!");
            completed = true;
            context.soulHarvested = true;
        }
        else if (cutPosition > targetPercentage)
        {
            Debug.Log("��ȡ������꣬�˿͸е�����ƭ");
            context.DecreaseSatisfaction();
            failed = true;
        }
        else
        {
            Debug.Log("��ȡ������꣬�˷ѹ�˾�ʲ�");
            context.AddFailure();
            failed = true;
        }
        
        cutterActive = false;
    }
}

#endregion

#region Main Manager

/// <summary>
/// ǩԼ���̹����� - ��Ϸ��������
/// </summary>
public class SigningFlowManager : MonoBehaviour
{
 

    [Header("=== ��Ϸ���� ===")]
    [Tooltip("��Ϸ������Դ (����ʹ��ScriptableObject)")]
    public HeContractGameConfig gameConfig;
    
    [Header("=== �������� ===")]
    [Tooltip("���û��������Դ��ʹ�ô���Ƕ����")]
    public GameConfig fallbackConfig;
    
    [Header("=== �������� ===")]
    [Tooltip("�����õ���Լ����")]
    public ContractDocument testDocument;
    [Tooltip("�����õĹ˿���Ϣ")]
    public Customer testCustomer;
    
    [Header("=== ����������� ===")]
    [Tooltip("�Ƿ�ʹ��������ɵ���Լ����")]
    public bool useRandomGeneration = true;
    [Tooltip("����˿������б�")]
    public string[] customerNames = { "����˿", "����˹", "����", "Լ��", "��ɪ��", "����" };
    [Tooltip("���ְҵ�б�")]
    public string[] occupations = { "����", "����", "ѧ��", "ũ��", "����", "����" };
    [Tooltip("�˿���Ƭ��Դ")]
    public Sprite[] customerPhotos;
    
    // ˽�б���
    private IContractStage currentStage;
    private Queue<IContractStage> stages;
    public HeContractContext ctx;
    private HeContractUIManager uiManager;

    void Start()
    {
        // ��ȡUI������
        uiManager = FindFirstObjectByType<HeContractUIManager>();
        
        // ��ʼ������
        InitializeConfig();
        
        // ��ʼ����Լ
        InitializeContract();
        
        // �������̽׶�
        SetupStages();
        
        // ��ʼ��һ���׶�
        NextStage();
    }
    
    void Update()
    {
        if (currentStage == null) return;
        
        currentStage.Update();
        
        // ����UI״̬
        uiManager?.UpdateContext(ctx);
       // uiManager?.UpdateCurrentStage(currentStage.StageName);
        
        if (currentStage.IsCompleted)
        {
            // ���ʧ������
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
    /// ��ʼ������
    /// </summary>
    private void InitializeConfig()
    {
        if (gameConfig == null)
        {
            Debug.LogWarning("δ���� HeContractGameConfig����ʹ��Ĭ������");
            
           
            gameConfig = ScriptableObject.CreateInstance<HeContractGameConfig>();
            if (fallbackConfig != null)
            {
              
                gameConfig.initialSatisfaction = fallbackConfig.initialSatisfaction;
                gameConfig.maxFailCount = fallbackConfig.maxFailCount;
                gameConfig.runeInputTimeLimit = fallbackConfig.runeInputTimeLimit;
               
            }
            
            gameConfig.ResetToDefault();
        }
        
        Debug.Log($"��Ϸ�����ѳ�ʼ�� - ��ʼ�����: {gameConfig.initialSatisfaction}, ���ʧ�ܴ���: {gameConfig.maxFailCount}");
    }


    private void InitializeContract()
    {
        ctx = new HeContractContext();
        
        // ����ʹ�ò������ݻ����������
        if (!useRandomGeneration && testCustomer != null && testDocument != null)
        {
            ctx.customer = testCustomer;
            ctx.document = testDocument;
            Debug.Log("ʹ�ò������ݳ�ʼ����Լ");
        }
        else
        {
            GenerateRandomContract();
            Debug.Log("ʹ��������ݳ�ʼ����Լ");
        }
        
        ctx.satisfaction = gameConfig?.initialSatisfaction ?? 3;
        Debug.Log($"��Լ��ʼ����� - �˿�: {ctx.customer.name}, ��Լ����: {ctx.document.HeContractType}");
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
        
        Debug.Log($"ǩԼ���������ã���{stages.Count}���׶�");
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
        Debug.Log($"����׶�: {currentStage.StageName}");
        currentStage.Enter(ctx);
    }

    public void EndGame(bool success)
    {
        Debug.Log($"=== ��Ϸ���� ===");
        Debug.Log($"���: {(success ? "ǩԼ�ɹ�" : "ǩԼʧ��")}");
        Debug.Log($"���������: {ctx.satisfaction}");
        Debug.Log($"ʧ�ܴ���: {ctx.failCount}");
        
        // ��ʾ�������
        uiManager?.ShowGameResult(success, ctx);
        
        // TODO: ������Ϸ����������ɾ͵�
    }

    private void GenerateRandomContract()
    {
        // ��������˿�
        ctx.customer = new Customer
        {
            name = GetRandomName(),
            occupation = GetRandomOccupation(),
            photo = GetRandomPhoto(),
            spokenRequest = GenerateRandomSpokenRequest()
        };
        
        // ���������Լ
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
        
        // �������һЩ�������� (����������Ϸ�Ѷ�)
        GenerateDocumentIssues();
    }

    private string GetRandomName()
    {
        if (customerNames.Length == 0) return "δ֪�˿�";
        return customerNames[UnityEngine.Random.Range(0, customerNames.Length)];
    }

    private string GetRandomOccupation()
    {
        if (occupations.Length == 0) return "��ҵ";
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
            "��ϣ����ø���ĲƸ�",
            "����Ҫ��ø�������",
            "�ҿ���ѧ���µļ���",
            "����Ҫ�ı��ҵ�����",
            "����Ҫ�������"
        };
        
        return requests[UnityEngine.Random.Range(0, requests.Length)];
    }

    private string GenerateContractDescription(HeContractType type)
    {
        return type switch
        {
            HeContractType.Money => "������òƸ�",
            HeContractType.Fame => "�����������",
            HeContractType.Skill => "����ʶ������",
            HeContractType.Event => "ϣ���ı�����",
            _ => "δ֪����"
        };
    }

    /// <summary>
    /// ������������(���ڲ��Ժ������)
    /// </summary>
    private void GenerateDocumentIssues()
    {
        // ʹ��ö�����������ع����������
        var errorGenerationRules = new Dictionary<DocumentError, float>
        {
            { DocumentError.BrokenSeal, 0.1f },              // 10%���ʷ�������
            { DocumentError.ForgeryDocument, 0.05f },        // 5%����α������
            { DocumentError.MissingWatermark, 0.03f },       // 3%����ȱ��ˮӡ
            { DocumentError.FakeInk, 0.03f },                // 3%���ʼ�ðīˮ
            { DocumentError.ContentMismatch, 0.12f },        // 12%�������ݲ�ƥ��
            { DocumentError.IncorrectDate, 0.1f },           // 10%�������ڴ���
            { DocumentError.DisguisedCustomer, 0.05f },      // 5%����αװ�˿�
            { DocumentError.DangerousCustomer, 0.02f }       // 2%����Σ������
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
    /// Ӧ���ض����������
    /// </summary>
    private void ApplyDocumentError(DocumentError error)
    {
        switch (error)
        {
            case DocumentError.BrokenSeal:
                ctx.document.isSealed = false;
                Debug.Log($"������������: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.ForgeryDocument:
                ctx.document.isGenuine = false;
                Debug.Log($"������������: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.MissingWatermark:
                ctx.document.hasITCWatermark = false;
                Debug.Log($"������������: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.FakeInk:
                ctx.document.isInkGenuine = false;
                Debug.Log($"������������: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.ContentMismatch:
                ctx.document.isContentMatched = false;
                Debug.Log($"������������: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.IncorrectDate:
                ctx.document.isDateCorrect = false;
                ctx.document.appointmentDate = DateTime.Today.AddDays(UnityEngine.Random.Range(-3, 4));
                Debug.Log($"������������: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.DisguisedCustomer:
                ctx.customer.isDisguised = true;
                ctx.document.isIdentityMatched = false;
                Debug.Log($"������������: {DocumentVerifier.GetErrorDescription(error)}");
                break;
                
            case DocumentError.DangerousCustomer:
                ctx.customer.isClocardalMember = true;
                Debug.Log($"������������: {DocumentVerifier.GetErrorDescription(error)}");
                break;
        }
    }
    
    /// <summary>
    /// ��ȡ��ǰ��Լ�������������
    /// </summary>
    public List<DocumentError> GetCurrentDocumentErrors()
    {
        var currentVerifier = currentStage as DocumentVerifier;
        return currentVerifier?.DetectedErrors ?? new List<DocumentError>();
    }
    
    /// <summary>
    /// �������������¿�ʼ��Լ
    /// </summary>
    public void RestartContract()
    {
        Debug.Log("���¿�ʼ��ԼǩԼ����");
        
        // ������Ϸ״̬
        ctx = null;
        currentStage = null;
        stages?.Clear();
        
        // ���¿�ʼ
        Start();
    }

    /// <summary>
    /// �����������˳���Ϸ
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("�˳���Ϸ");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// ������������ͣ��Ϸ
    /// </summary>
    public void PauseGame()
    {
        Time.timeScale = Time.timeScale > 0 ? 0 : 1;
    }

    /// <summary>
    /// ��ȡ��ǰ��Ϸ״̬��Ϣ (�����Ի�UIʹ��)
    /// </summary>
    public string GetGameStateInfo()
    {
        if (ctx == null) return "��Ϸδ��ʼ��";
        
        return $"��ǰ�׶�: {currentStage?.StageName ?? "��"}\n" +
               $"�����: {ctx.satisfaction}\n" +
               $"ʧ�ܴ���: {ctx.failCount}\n" +
               $"�˿�: {ctx.customer?.name ?? "δ֪"}";
    }
}

#endregion