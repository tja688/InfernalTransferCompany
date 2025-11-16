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






    public enum UIState
    {
        None,
        DocumentVerification,
        RuneInput,
        EventHandling,
        StampSelection,
        SoulHarvest,

    }
    public Canvas gameCanvas;
    public event System.Action<bool> OnPlayerDecisionMade;
    [Header("=== 触发设置 ===")]

    public GameObject circularRunaSkeleton;       // 圆形符文（骨骼UI）
    public GameObject diamondRunaSkeleton;        // 菱形符文
    public GameObject triangularRunaSkeleton;     // 三角形符文
    public GameObject sphericalRunaSkeleton;      // 球形符文


    public GameObject pneumaticChannelSkeleton;   // 气动通道
    public GameObject copperRuneSelectorSkeleton; // 符文选择器

    // 可交互的骨骼UI（需绑定点击事件，结合Button组件）
    public GameObject leftBookSkeleton;           // 左书
    public GameObject rightBookSkeleton;          // 右书
    public GameObject typewriterSkeleton;         // 打字机
    public GameObject telephoneSkeleton;          // 电话
    public GameObject canSkeleton;                // 罐子
    public Image contractDocumentsImage;  // 合同文档
    public Image arrowImage;              // 箭头提示
    public Image roleImage;
    public GameObject ArrowGroupGameObject;
    public GameObject RingTempPrefab;
    public GameObject StampToolPrefab;
    private GameObject _ringTempInstance;
    private GameObject _stampToolInstance;

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
  
    private Coroutine _chargeCoroutine;




    private void Awake()
    {

    }
    void Start()
    {
        flowManager = FindFirstObjectByType<SigningFlowManager>();
        gameConfig = flowManager?.gameConfig;
        InitCheck();
        InitializeUI();
        RegisterAllEvent();
        
    }

    private void InitCheck()
    {
        if (gameCanvas == null)
        {
            Debug.LogError("Canvas未附加在UIManager");
        }
        return;
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
        SkeletonGraphic animate = pneumaticChannelSkeleton.GetComponent<SkeletonGraphic>();
        var trackEntry = animate.AnimationState.SetAnimation(0, "A开门", false);
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
        SkeletonGraphic animate = pneumaticChannelSkeleton.GetComponent<SkeletonGraphic>();
        var trackEntry = animate.AnimationState.SetAnimation(0, "B关门", false);

       
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

        contractDocumentsImage.GetComponent<EntryAnimation>();
        //var track =  entryAnimation.PlayEntryAnimation();
        //track.OnComplete(OncontractDocumentsImagePlayEntryAnimationEnd);
        
        


    }
    public void OncontractDocumentsImagePlayEntryAnimationEnd()
    {
        //打开可拖拽
        var draggable = contractDocumentsImage.GetComponent<DraggableUI>();
        draggable.isDraggable = true;



        var typewriter = typewriterSkeleton.GetComponent<SkeletonGraphicHighLightDragHover>();
        var role  = roleImage.GetComponent<ImageHighLightDragHover>();
        // 监听事件,可拖拽才能触发事件
        SlotCenter.Instance.add_listener<DocumentError>(typewriter.eventName, OnDragEndToChooseDocumentAction,true);
        SlotCenter.Instance.add_listener<DocumentError>(role.eventName, OnDragEndToChooseDocumentAction,true);
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
    public void OnTabChargeEnd(bool success, SuccessType type)
    {
        SlotCenter.Instance.trigger_event< SuccessType>(HeEventNames.OnChargingSth, type);   
    }
   

    public void InitRingPrefab()
    {
        if (RingTempPrefab == null)
        {
            Debug.LogError("InitRingPrefab: RingTempPrefab 未设置");
            return;
        }

       

        // 实例化预制体
        _ringTempInstance = GameObject.Instantiate(RingTempPrefab, gameCanvas.transform);
        RingChangeColor rg = _ringTempInstance.GetComponent<RingChangeColor>();
        rg.onJudgeResult+= OnTabChargeEnd;
        if (_ringTempInstance == null)
        {
            Debug.LogError("InitRingPrefab: 实例化失败");
            return;
        }

        // 确保是 UI 元素时重置 RectTransform，否则设置为本地原点
        RectTransform rt = _ringTempInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.SetAsLastSibling();
            rt.localScale = Vector3.one;
            rt.anchoredPosition = new Vector2(0, -192.5f);
            rt.localRotation = Quaternion.identity;
    
        }
    


    }

 
    public void DestroyRingPrefab()
    {


       
        
        if (_ringTempInstance != null)
        {
            RingChangeColor rg =  _ringTempInstance.GetComponent<RingChangeColor>();
            rg.onJudgeResult -= OnTabChargeEnd;
            GameObject.Destroy(_ringTempInstance);
            _ringTempInstance = null;
           
        }
    }
    public void DestroyStampToolTemp()
    {
        if (_stampToolInstance != null)
        {

            GameObject.Destroy(_stampToolInstance);
            _ringTempInstance = null;

        }
    }


    public void InitStampToolTemp()
    {
        if(StampToolPrefab == null)
        {
            Debug.LogError("InitStampToolTemp: StampToolPrefab 未设置");
            return;
        }


        // 实例化预制体
        _stampToolInstance = GameObject.Instantiate(StampToolPrefab, gameCanvas.transform);

        RectTransform rt = _stampToolInstance.GetComponent<RectTransform>();
        rt.SetAsLastSibling();
        rt.localScale = Vector3.one;
        rt.anchoredPosition =new Vector2(-103f, -212.1f);
        rt.localRotation = Quaternion.identity;
      
        DraggableUI db = _stampToolInstance.GetComponent<DraggableUI>();
        db.isDraggable = true; 

    }

    // 业务侧调用的UI启动方法（原uiManager?.StartStampCharging()）
    //目前实现为拖拽进入自动触发
    public void StartStampCharging(HeContractGameConfig gameConfig)
    {

        if (_stampToolInstance)
        {
            Debug.LogError("StartStampCharging: 盖章工具未初始化");
            return;

        }


        //RingChangeColor ringChangeColor = _stampToolInstance.GetComponent<RingChangeColor>();
        //ringChangeColor.StartAnimation();

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






    #endregion
}