using QFramework;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


//public interface IUIHeEventMetadata<T>
//{
//    string EventName { get; }
//    void TriggerEvent(T param);
//}

/// <summary>
/// 契约UI管理器 - 处理所有UI显示和交互
/// </summary>
public class HeContractUIManager : MonoBehaviour
{


    [Header("输入配置")]
    [SerializeField]
    private InputActionReference moveAction;
    [SerializeField]
    private InputActionReference interactAction;



    public enum UIState
    {
        None,
        DocumentVerification,
        RuneInput,
        EventHandling,
        StampSelection,
        SoulHarvest,

    }
    public event System.Action<bool> OnPlayerDecisionMade;
    [Header("=== 触发设置 ===")]

    public SkeletonGraphic circularRunaSkeleton;       // 圆形符文（骨骼UI）
    public SkeletonGraphic diamondRunaSkeleton;        // 菱形符文
    public SkeletonGraphic triangularRunaSkeleton;     // 三角形符文
    public SkeletonGraphic sphericalRunaSkeleton;      // 球形符文


    public SkeletonGraphic pneumaticChannelSkeleton;   // 气动通道
    public SkeletonGraphic copperRuneSelectorSkeleton; // 符文选择器

    // 可交互的骨骼UI（需绑定点击事件，结合Button组件）
    public SkeletonGraphic leftBookSkeleton;           // 左书
    public SkeletonGraphic rightBookSkeleton;          // 右书
    public SkeletonGraphic typewriterSkeleton;         // 打字机
    public SkeletonGraphic telephoneSkeleton;          // 电话
    public SkeletonGraphic canSkeleton;                // 罐子
    public Image contractDocumentsImage;  // 合同文档
    public Image arrowImage;              // 箭头提示
    public Image roleImage;
    public GameObject ArrowGroupGameObject;


    [Header("=== 动画设置 ===")]
    public float panelTransitionTime = 0.5f;
    public float uiElementFadeTime = 0.3f;

    [Header("Spine 控制设置")]
    [Tooltip("是否使用 SkeletonGraphic 的 AnimationState 控制气动通道动画（否则使用 Unity Animator）")]
    public bool useSkeletonGraphicForPneumatic = true;
    [Tooltip("当使用 Spine 动画时，估算的打开动画持续时间（秒）")]
    public float pneumaticOpenDuration = 2.0f;


    // 私有变量 
    private HeContractContext currentContext;
    private HeContractGameConfig gameConfig;
    private SigningFlowManager flowManager;
    private UIState currentActivePanel = UIState.None;
    private List<GameObject> runeGridItems = new List<GameObject>();
    private bool pneumaticChannelAnimatorOpen = false;



    private Image stampChargeRing;
    private RectTransform _ringRect;
    public Color initialColor = Color.blue; // 初始阶段（未到完美时机）
    public Color optimalColor = Color.green; // 完美时机阶段
    public Color warningColor = Color.yellow; // 过了完美时机（未超时）
    public Color overTimeColor = Color.red; // 超时阶段
    private float _maxChargeTime;       // 最大蓄力时间（默认3f）
    private float _optimalTiming;       // 完美时机比例（默认0.8f）
    private float _optimalTime;         // 完美时机具体时间（max*optimalTiming）
    private float _tolerance;           // 完美时机容错范围（默认0.1f）
    private float _overTimeThreshold;   // 超时阈值（max*1.2f）
    private Coroutine _chargeCoroutine;



    public event System.Action<int> OnMoveAction;
    public event System.Action OnInteractAction;

    private void Awake()
    {

    }
    void Start()
    {
        flowManager = FindFirstObjectByType<SigningFlowManager>();
        gameConfig = flowManager?.gameConfig;

        InitializeUI();
        RegisterAllEvent();

    }

    private void RegisterAllEvent()
    {
      
    }

    /// <summary>
    /// 文书判断判定，这个最后绑定到签约书的点击事件上
    /// </summary>

    //public void ChooseDocumentError(DocumentError err)
    //{
    //    OnJudge?.Invoke(err);
    //}


    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        Debug.Log("Contract UI Manager initialized");
    }


    /// <summary>
    /// 切换面板
    /// </summary>
    public void SwitchPanel(UIState targetPanel)
    {
        if (currentActivePanel == targetPanel) return;

        // 隐藏当前面板
        if (currentActivePanel != UIState.None)
        {
            StartCoroutine(HidePanelCoroutine(currentActivePanel));
        }

        // 显示目标面板
        if (targetPanel != UIState.None)
        {
            currentActivePanel = targetPanel;
            StartCoroutine(ShowPanelCoroutine(targetPanel));
        }
    }

    private IEnumerator HidePanelCoroutine(UIState panel)
    {

        switch (panel)
        {
            case UIState.DocumentVerification:
                // TODO: 取消文书验证面板显示逻辑
                break;
            case UIState.RuneInput:
                // TODO:  取消符文输入面板显示逻辑
                break;
            case UIState.EventHandling:
                // TODO: 取消事件处理面板显示逻辑
                break;
            case UIState.StampSelection:
                // TODO:  取消盖章选择面板显示逻辑
                break;
            case UIState.SoulHarvest:
                // TODO:  取消灵魂收取面板显示逻辑
                break;
            case UIState.None:
            default:
                break;
        }

        yield return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="panel"></param>
    /// <returns></returns>
    private IEnumerator ShowPanelCoroutine(UIState panel)
    {
        switch (panel)
        {
            case UIState.DocumentVerification:
                

               

                //pneumaticChannelAnimatorOpen = true;
                // TODO: 添加文书验证面板显示逻辑
                break;
            case UIState.RuneInput:
                // TODO: 添加符文输入面板显示逻辑
                break;
            case UIState.EventHandling:
                // TODO: 添加事件处理面板显示逻辑
                break;
            case UIState.StampSelection:
                // TODO: 添加盖章选择面板显示逻辑
                break;
            case UIState.SoulHarvest:
                // TODO: 添加灵魂收取面板显示逻辑
                break;
            case UIState.None:
            default:
                break;
        }
        yield return null;
    }

    /// <summary>
    /// 更新契约上下文
    /// </summary>
    public void UpdateContext(HeContractContext context)
    {
        currentContext = context;
        //UpdateStatusDisplay();
    }


    private void SetRuneAppearance(Image runeImage, RuneType runeType)
    {
        if (gameConfig && gameConfig.runeConfigs.Count > 0)
        {
            var config = gameConfig.runeConfigs.Find(r => r.type == runeType);
            if (config != null)
            {
                if (config.normalSprite) runeImage.sprite = config.normalSprite;
                runeImage.color = config.normalColor;
                return;
            }
        }

        // 默认颜色
        Color color = runeType switch
        {
            RuneType.Fire => Color.red,
            RuneType.Water => Color.blue,
            RuneType.Earth => new Color(0.6f, 0.4f, 0.2f),
            RuneType.Air => Color.cyan,
            RuneType.Light => Color.yellow,
            RuneType.Dark => new Color(0.3f, 0.3f, 0.3f),
            _ => Color.white
        };

        runeImage.color = color;
    }

    #region 文书UI
    public void ShowDocumentVerification(HeContractContext ctx)
    {
        SwitchPanel(UIState.DocumentVerification);
    }


    public void OnOpenPneumaticChannelClick()
    {

        if (pneumaticChannelAnimatorOpen) return;

        var trackEntry = pneumaticChannelSkeleton.AnimationState.SetAnimation(0, "A开门", false);
        trackEntry.Complete += OnOpenAnimationComplete;
        pneumaticChannelAnimatorOpen = true;

        //关闭悬浮高亮与按钮与高亮
        var Hover = pneumaticChannelSkeleton.GetComponent<SkeletonHoverHighLight>();
        Hover.enableHighLightOnHover = false;
        Hover.UnSetHighLight(); 
    

        var button = pneumaticChannelSkeleton.GetComponent<Button>();
        button.interactable = false;
    }
    private void OnOpenAnimationComplete(Spine.TrackEntry trackEntry)
    {
        //打开按钮与悬浮高亮
        var button = pneumaticChannelSkeleton.GetComponent<Button>();
        button.interactable = true;
        var Hover = pneumaticChannelSkeleton.GetComponent<SkeletonHoverHighLight>();
        Hover.enableHighLightOnHover = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClosePneumaticChannel);
    }


    public void OnClosePneumaticChannel()
    {
        if (!pneumaticChannelAnimatorOpen) return;

        var trackEntry = pneumaticChannelSkeleton.AnimationState.SetAnimation(0, "B关门", false);

       
        trackEntry.Complete += OnCloseAnimationComplete;
        //关闭按钮与悬浮高亮
        var button = pneumaticChannelSkeleton.GetComponent<Button>();
        button.interactable = false;

        var Hover = pneumaticChannelSkeleton.GetComponent<SkeletonHoverHighLight>();
        Hover.enableHighLightOnHover = false;
        Hover.UnSetHighLight();

    }
    private void OnCloseAnimationComplete(Spine.TrackEntry trackEntry)
    {

        var entryAnimation = contractDocumentsImage.GetComponent<EntryAnimation>();
        var track =  entryAnimation.PlayEntryAnimation();
        track.OnComplete(OncontractDocumentsImagePlayEntryAnimationEnd);
        
        


    }
    private void OncontractDocumentsImagePlayEntryAnimationEnd()
    {
        //打开可拖拽
        var draggable = contractDocumentsImage.GetComponent<DraggableUI>();
        draggable.isDraggable = true;



        var typewriter = typewriterSkeleton.GetComponent<SkeletonGraphicHighLightDragHover>();
        var role  = roleImage.GetComponent<ImageHighLightDragHover>();
        // 监听事件,可拖拽才能触发事件
        SlotCenter.Instance.add_listener<DocumentError>(typewriter.eventName, OnDragEndToChooseDocumentAction);
        SlotCenter.Instance.add_listener<DocumentError>(role.eventName, OnDragEndToChooseDocumentAction);
    }
    private void OnDragEndToChooseDocumentAction(DocumentError err)
    {



        var Hover = pneumaticChannelSkeleton.GetComponent<SkeletonHoverHighLight>();
        var entryAnimation = contractDocumentsImage.GetComponent<EntryAnimation>();
        entryAnimation.PlayExitAnimation();
        SlotCenter.Instance.trigger_event<DocumentError>(HeEventNames.DocumentErrorChosen, err);

    }





    #endregion
    #region 印章UI

    public void EnableAllStamp()
    {
        sphericalRunaSkeleton.GetComponent<SkeletonClick>().enableClick = true;
        diamondRunaSkeleton.GetComponent<SkeletonClick>().enableClick = true;
        triangularRunaSkeleton.GetComponent<SkeletonClick>().enableClick = true;
        circularRunaSkeleton.GetComponent<SkeletonClick>().enableClick = true;
    }
    public void DisableAllStamp()
    {
        sphericalRunaSkeleton.GetComponent<SkeletonClick>().enableClick = false;
        diamondRunaSkeleton.GetComponent<SkeletonClick>().enableClick = false;
        triangularRunaSkeleton.GetComponent<SkeletonClick>().enableClick = false;
        circularRunaSkeleton.GetComponent<SkeletonClick>().enableClick = false;
    }

    /// <summary>
    /// 更新符文输入进度
    /// 
    /// </summary>
    public void UpdateRuneInputProgress(int inputCount, int errorCount, bool isCorrect)
    {
    }

    /// <summary>
    /// 显示符文核对界面
    /// </summary>
    public void ShowRuneVerification(List<RuneData> runeGrid)
    {
    }

    /// <summary>
    /// 生成符文网格
    /// </summary>

    private void GenerateRuneGrid(List<RuneData> runeData)
    {
    }

    private void OnRuneGridItemClicked(int index)
    {
        Debug.Log($"点击符文网格项 {index}");

    }

    #endregion
    #region 特殊事件UI



    #endregion

    #region 盖章UI

    /// <summary>
    /// 显示盖章界面
    /// </summary>
    public void ShowStampSelection(HeContractType requiredType)
    {
        SwitchPanel(UIState.StampSelection);
    }

    private void HighlightCorrectStamp(HeContractType correctType)
    {
    }

   

    // 业务侧调用的UI启动方法（原uiManager?.StartStampCharging()）
    public void StartStampCharging(HeContractGameConfig gameConfig)
    {
   
        // 初始化参数（从gameConfig获取，确保和业务侧一致）
        _maxChargeTime = gameConfig?.stampChargeTime ?? 3f;
        _optimalTiming = gameConfig?.stampOptimalTiming ?? 0.8f; 
        _optimalTime = _maxChargeTime * _optimalTiming;
        _tolerance = gameConfig?.stampAccuracyTolerance ?? 0.1f;
        _overTimeThreshold = _maxChargeTime * 1.2f;

         
     
    }


    // 初始化圆环位置和状态（确保在typewriterSkeleton中央，倾斜80度）
    private void InitChargeRing()
    {
        if (stampChargeRing == null || typewriterSkeleton == null)
        {
            Debug.LogError("环形进度条或父物体未赋值！");
            return;
        }

        // 父物体设为typewriterSkeleton，确保在中央
        RectTransform ringRect = stampChargeRing.GetComponent<RectTransform>();
        ringRect.SetParent(typewriterSkeleton.GetComponent<RectTransform>(), false);

        // 位置居中（锚点和 pivot 都设为中心）
        ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.pivot = new Vector2(0.5f, 0.5f);
        ringRect.anchoredPosition = Vector2.zero;

        // 向内倾斜80度（Z轴旋转）
        ringRect.rotation = Quaternion.Euler(0, 0, 80f);

        // 初始状态（进度0，初始色，显示UI）
        stampChargeRing.fillAmount = 0f;
        stampChargeRing.color = initialColor;
        stampChargeRing.gameObject.SetActive(true);
    }


    // 协程：实时更新进度和颜色（核心逻辑）
    private IEnumerator UpdateChargeProgressCoroutine()
    {
        float currentChargeTime = 0f; // UI侧独立计时，不依赖业务侧的chargeTime

        while (true)
        {
            // 1. 实时累加时间（每帧更新，和Time.deltaTime同步）
            currentChargeTime += Time.deltaTime;
            yield return null; // 等待下一帧，确保实时性

            //// 2. 检查中断条件（完成/失败/尝试次数用完，停止协程）
            //if (context.isStampCompleted || context.isStampFailed || context.stampAttempts >= (context.gameConfig?.maxStampAttempts ?? 3))
            //{
            //    HideChargeRing();
            //    _chargeCoroutine = null;
            //    yield break;
            //}

            // 3. 计算进度并同步到环形条
            float chargeProgress = currentChargeTime / _maxChargeTime;
            if (chargeProgress > 1f) chargeProgress = 1f; // 进度不超过100%
            stampChargeRing.fillAmount = chargeProgress;

            // 4. 按时间阶段切换颜色（完全贴合原业务逻辑）
            UpdateRingColorByTime(currentChargeTime);

            // 5. 处理超时（和原逻辑一致：超时重置，尝试次数+1）
            if (currentChargeTime > _overTimeThreshold)
            {
                Debug.Log("UI侧：蓄力超时，重新开始");
                //context.stampAttempts++;
                currentChargeTime = 0f; // 重置计时
                stampChargeRing.fillAmount = 0f; // 重置进度
                stampChargeRing.color = initialColor; // 重置颜色
            }
        }
    }


    // 根据当前时间切换圆环颜色
    private void UpdateRingColorByTime(float currentChargeTime)
    {
        if (currentChargeTime < _overTimeThreshold) // 未超时
        {
            // 完美时机范围内（currentChargeTime在 [optimalTime-tolerance, optimalTime+tolerance]）
            if (Mathf.Abs(currentChargeTime - _optimalTime) < _tolerance)
            {
                stampChargeRing.color = optimalColor;
            }
            // 未到完美时机（currentChargeTime < optimalTime - tolerance）
            else if (currentChargeTime < _optimalTime - _tolerance)
            {
                stampChargeRing.color = initialColor;
            }
            // 过了完美时机但未超时（currentChargeTime > optimalTime + tolerance）
            else
            {
                stampChargeRing.color = warningColor;
            }
        }
        else // 超时
        {
            stampChargeRing.color = overTimeColor;
        }
    }


    // 隐藏圆环（完成/失败时调用）
    public void HideChargeRing()
    {
        if (stampChargeRing != null)
            stampChargeRing.gameObject.SetActive(false);

        // 停止协程
        if (_chargeCoroutine != null)
        {
            StopCoroutine(_chargeCoroutine);
            _chargeCoroutine = null;
        }
    }

    #endregion

    #region 灵魂收取UI

    /// <summary>
    /// 显示灵魂收取界面
    /// </summary>
    public void ShowSoulHarvest(float targetPercentage)
    {
        SwitchPanel(UIState.SoulHarvest);
        // 开始分灵刀移动
        StartCoroutine(UpdateSoulCutter());
    }

    private IEnumerator UpdateSoulCutter()
    {
        yield return null;
    }

    private void OnCutSoul()
    {
    }

    #endregion

    #region 结果界面

    /// <summary>
    /// 显示游戏结果
    /// </summary>
    public void ShowGameResult(bool success, HeContractContext finalContext)
    {
        SwitchPanel(UIState.RuneInput);
        // 生成结果总结
        GenerateResultSummary(success, finalContext);
    }

    private void GenerateResultSummary(bool success, HeContractContext context)
    {
    }

    private void OnRestart()
    {
        flowManager?.RestartContract();
    }

    private void OnQuit()
    {
        flowManager?.QuitGame();
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 延迟面板切换
    /// </summary>
    private IEnumerator DelayedPanelTransition()
    {
        yield return new WaitForSeconds(1f);
        // 这里可以通知流程管理器继续到下一阶段
    }

    /// <summary>
    /// 显示提示消息
    /// </summary>
    public void ShowTooltip(string message, float duration = 2f)
    {
        // TODO: 实现提示消息显示
        Debug.Log($"Tooltip: {message}");
    }

    /// <summary>
    /// 播放UI音效
    /// </summary>
    public void PlayUISound(AudioClip clip)
    {
    }


    private Dictionary<GameObject, Coroutine> fadeCoroutines = new Dictionary<GameObject, Coroutine>();
    private float fadeOutDuration = 0.8f;
    public void FadeOutArrow(GameObject spawnedArrows, float waitTime)
    {
        StartCoroutine(FadeOutArrowCoroutine(spawnedArrows, waitTime));
        
    }
    public void ShakeArrow(GameObject spawnedArrows)
    {
        StartCoroutine(ShakeArrowCoroutine(spawnedArrows));
    }
    /// <summary>
    /// 箭头碎裂淡出效果
    /// </summary>
    private System.Collections.IEnumerator FadeOutArrowCoroutine(GameObject arrow, float waitTime)
    {
        yield return new WaitForSeconds(waitTime); // 等待一段时间后开始淡出

        if (arrow == null) {
            Debug.Log("FadeOutArrowCoroutine接收参数为null");
            yield break; 
        }

        // 获取渲染组件
        Renderer renderer = arrow.GetComponent<Renderer>();
        CanvasGroup canvasGroup = arrow.GetComponent<CanvasGroup>();

        float elapsedTime = 0f;
        Vector3 originalScale = arrow.transform.localScale;

        // 碎裂效果：先稍微放大然后缩小并分裂
        while (elapsedTime < fadeOutDuration)
        {
            if (arrow == null) yield break;

            float progress = elapsedTime / fadeOutDuration;

            // 碎裂效果：随机偏移位置和旋转
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-0.1f, 0.1f) * progress,
                UnityEngine.Random.Range(-0.1f, 0.1f) * progress,
                0
            );

            arrow.transform.localPosition += randomOffset;
            arrow.transform.localEulerAngles += new Vector3(0, 0, UnityEngine.Random.Range(-5f, 5f) * progress);

            // 淡出和缩放
            float scale = Mathf.Lerp(1f, 0f, progress);
            arrow.transform.localScale = originalScale * scale;

            // 透明度淡出
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = 1f - progress;
                renderer.material.color = color;
            }
            else if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - progress;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 最终销毁对象
        if (arrow != null)
        {
            GameObject.Destroy(arrow);
        }
        SlotCenter.Instance.trigger_event(HeEventNames.ArrowFadeOutDelete);
    }
    /// <summary>
    /// 输入错误时的箭头震动效果
    /// </summary>
    private System.Collections.IEnumerator ShakeArrowCoroutine(GameObject arrow)
    {
        if (arrow == null) yield break;

        RectTransform rt = arrow.GetComponent<RectTransform>();
        if (rt == null) yield break;

 
        Image arrowImage = arrow.GetComponent<Image>();
        Color originalColor = Color.white; 
        if (arrowImage != null)
        {
            originalColor = arrowImage.color;
        
            arrowImage.color = Color.red;
        }

        Vector2 originalAnchoredPosition = rt.anchoredPosition;
        float shakeDuration = 0.3f;
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            if (arrow == null) yield break;

            float shakeAmount = 10f * (1f - elapsedTime / shakeDuration);
            Vector2 shakeOffset = new Vector2(
               UnityEngine.Random.Range(-shakeAmount, shakeAmount),
               UnityEngine.Random.Range(-shakeAmount, shakeAmount)
            );

            rt.anchoredPosition = originalAnchoredPosition + shakeOffset;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (arrow != null && rt != null)
        {
            rt.anchoredPosition = originalAnchoredPosition;
        }

        // 恢复原色
        if (arrowImage != null)
        {
            arrowImage.color = originalColor;
        }
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine( FadeOutArrowCoroutine(arrow,0));
    }
    public void EnableMoveAction()
    {
        moveAction.action.performed += OnMovePerformed;
        moveAction?.action.Enable();
    }
    public void DisableMoveAction()
    {
        moveAction.action.performed -= OnMovePerformed;
        moveAction.action.Disable();
    }

    public void EnableInteractAction()
    {
        interactAction.action.canceled += OnInteractPerformed;
        interactAction?.action.Enable();
    }

    public void DisableInteractAction()
    {
        interactAction.action.canceled -= OnInteractPerformed;
        interactAction?.action.Disable();
    }
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        string keyName = context.control.name.ToLower();

        // 映射为索引（W=0, A=1, S=2, D=3）
        var currentKeyIndex = keyName switch
        {
            "w" => 0,
            "s" => 1,
            "a" => 2,
            "d" => 3,
            _ => -1, // 其他按键不改变当前索引（如同时按多个键时保持优先）
        };

        Debug.Log($"移动输入（回调）：X={moveInput.x}, Y={moveInput.y},keyName={keyName},directIndex={currentKeyIndex}");
        OnMoveAction?.Invoke(currentKeyIndex);
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("交互按键按下（回调）");

        OnInteractAction?.Invoke();
    }

    #endregion
}