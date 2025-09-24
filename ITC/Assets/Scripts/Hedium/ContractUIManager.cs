using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 契约UI管理器 - 处理所有UI显示和交互
/// </summary>
public class HeContractUIManager : MonoBehaviour
{
    [Header("=== UI面板引用 ===")]
    public GameObject mainGamePanel;
    public GameObject documentVerificationPanel;
    public GameObject runeInputPanel;
    public GameObject eventPanel;
    public GameObject stampPanel;
    public GameObject soulHarvestPanel;
    public GameObject resultPanel;



    [Header("=== 动画设置 ===")]
    public float panelTransitionTime = 0.5f;
    public float uiElementFadeTime = 0.3f;

    // 私有变量
    private HeContractContext currentContext;
    private HeContractGameConfig gameConfig;
    private SigningFlowManager flowManager;
    private GameObject currentActivePanel;
    private List<GameObject> runeGridItems = new List<GameObject>();

    void Start()
    {
        flowManager = FindFirstObjectByType<SigningFlowManager>();
        gameConfig = flowManager?.gameConfig;
        
        InitializeUI();

    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 隐藏所有面板
        HideAllPanels();
        
        // 显示主游戏面板
        if (mainGamePanel) mainGamePanel.SetActive(true);
        
   
       
        
        Debug.Log("Contract UI Manager initialized");
    }

   
    /// <summary>
    /// 隐藏所有面板
    /// </summary>
    private void HideAllPanels()
    {
        var panels = new GameObject[] 
        {
            documentVerificationPanel, runeInputPanel, eventPanel, 
            stampPanel, soulHarvestPanel, resultPanel
        };
        
        foreach (var panel in panels)
        {
            if (panel) panel.SetActive(false);
        }
    }

    /// <summary>
    /// 切换面板
    /// </summary>
    public void SwitchPanel(GameObject targetPanel)
    {
        if (currentActivePanel == targetPanel) return;
        
        // 隐藏当前面板
        if (currentActivePanel)
        {
            StartCoroutine(HidePanelCoroutine(currentActivePanel));
        }
        
        // 显示目标面板
        if (targetPanel)
        {
            currentActivePanel = targetPanel;
            StartCoroutine(ShowPanelCoroutine(targetPanel));
        }
    }

    private IEnumerator HidePanelCoroutine(GameObject panel)
    {
       
        Vector3 originalScale = panel.transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < panelTransitionTime / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (panelTransitionTime / 2);
            panel.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }
        
        panel.SetActive(false);
        panel.transform.localScale = originalScale; // 重置缩放
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="panel"></param>
    /// <returns></returns>
    private IEnumerator ShowPanelCoroutine(GameObject panel)
    {
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
        SwitchPanel(documentVerificationPanel);


    }


    #endregion
    #region 印章UI




    /// <summary>
    /// 更新符文输入进度
    /// </summary>
    public void UpdateRuneInputProgress(int inputCount, int errorCount)
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
        SwitchPanel(stampPanel);
        

     
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
        SwitchPanel(soulHarvestPanel);
        
       
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
        SwitchPanel(resultPanel);
      
        
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