using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Unity.Collections.Unicode;

/// <summary>
/// 契约UI管理器 - 处理所有UI显示和交互
/// </summary>
public class HeContractUIManager : MonoBehaviour
{


    [Header("输入配置")]
    public InputActionReference moveAction;

    public InputActionReference interactAction;
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

    public GameObject circularRunaGameObject;
    public GameObject diamondRunaGameObject;
    public GameObject triangularRunaGameObject;
    public GameObject sphericalRunaGameObject;


  





    public GameObject pneumaticChannelGameObject;

    public GameObject leftBooklGameObject;
    public GameObject rightBookGameObject;
    public GameObject typewriterGameObject;
    public GameObject telephoneGameObject;
    public GameObject canGameObject;
    public GameObject contractDocumentsnGameObject;
    public GameObject CopperRuneSelectorGameObject;

    [Header("=== 动画设置 ===")]


    public Animator sphericalRunaAnimator;
    public Animator diamondRunaAnimator;
    public Animator circularRunaAnimator;
    public Animator triangularRunaAnimator;
    public Animator pneumaticChannelAnimator;

    public Animator leftBooklAnimator;
    public Animator rightBookAnimator;
    public Animator typewriterAnimator;
    public Animator telephoneAnimator;
    public Animator canAnimator;

    [Header("=== 动画设置 ===")]
    public float panelTransitionTime = 0.5f;
    public float uiElementFadeTime = 0.3f;

    // 私有变量
    private HeContractContext currentContext;
    private HeContractGameConfig gameConfig;
    private SigningFlowManager flowManager;
    private UIState currentActivePanel=UIState.None;
    private List<GameObject> runeGridItems = new List<GameObject>();


    public event System.Action<DocumentError> OnDocumentClicked;
    private void Awake()
    {
        
    }
    void Start()
    {
        flowManager = FindFirstObjectByType<SigningFlowManager>();
        gameConfig = flowManager?.gameConfig;
        
        InitializeUI();

    }



    /// <summary>
    /// 文书判断判定，这个最后绑定到签约书的点击事件上
    /// </summary>

    void determineError()
    {




        OnDocumentClicked?.Invoke(DocumentError.DangerousCustomer);

    }
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
        if (targetPanel!=UIState.None)
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


                pneumaticChannelAnimator.SetTrigger("Open");
                yield return new WaitForSeconds(3.0f);  
                pneumaticChannelAnimator.SetTrigger("Close");
          
             






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




    #endregion
    #region 印章UI




    /// <summary>
    /// 更新符文输入进度
    /// 
    /// </summary>
    public void UpdateRuneInputProgress(int inputCount, int errorCount,bool isCorrect)
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

    /// <summary>
    /// 符文核对计时器
    /// </summary>
    /// <returns></returns>
    //private IEnumerator RuneVerificationTimer()
    //{


    //}


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


    private void OnStampSelected(int stampIndex)
    {
        Debug.Log($"选择了印章 {stampIndex}");
        
        // 开始蓄力阶段
        StartStampCharging();
    }

    /// <summary>
    /// 开始盖章蓄力
    /// </summary>
    public void StartStampCharging()
    {
       
    }

    //private IEnumerator UpdateStampCharging()
    //{
     
    //}

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