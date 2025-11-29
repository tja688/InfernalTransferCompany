using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeGameEnterMenu : MonoBehaviour
{
    // Start is called before the first frame update 
    //UTF8编码
    //TriggerDebugStage,
    //TriggerRuneInputStage,
    //TriggerStampStage,
    //TriggerSoulHarvestStage,
    //TriggerSpecialEventStage,
    //TriggerDocumentVerifierStage,


    public int day = 0;
    [ContextMenu("DebugStageStart")]
    public void DebugStageStart()
    {
        SlotCenter.Instance.trigger_event(HeEventNames.TriggerDebugStage);
    }

    [ContextMenu("RuneInputStage")]
    public void RuneInputStageStart()
    {
        SlotCenter.Instance.trigger_event(HeEventNames.TriggerRuneInputStage);
    }

    [ContextMenu("StampStageStart")]
    public void StampStageStart()
    {
        SlotCenter.Instance.trigger_event(HeEventNames.TriggerStampStage);
    }
    [ContextMenu("SoulHarvestStageStart")]
    public void SoulHarvestStageStart()
    {
        SlotCenter.Instance.trigger_event(HeEventNames.TriggerSoulHarvestStage);
    }
    [ContextMenu("SpecialEventStageStart")]
    public void HarvestStageStart()
    {
        SlotCenter.Instance.trigger_event(HeEventNames.TriggerSpecialEventStage);
    }
    [ContextMenu("DocumentVerifierStageStart")]
    public void DocumentVerifierStageStart()
    {
        SlotCenter.Instance.trigger_event(HeEventNames.TriggerDocumentVerifierStage);

    }
    /// <summary>
    /// 用来设置更新随天数变化的难度设置
    /// </summary>
    [ContextMenu("UpDataConfig")]
    public void UpDataConfig()
    {
       GameObject.FindFirstObjectByType<SigningFlowManager>()?.UpdataConfig(day);
    }

    }
