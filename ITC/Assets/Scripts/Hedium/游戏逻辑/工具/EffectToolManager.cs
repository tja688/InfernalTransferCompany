using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

public class EffectToolManager : MonoBehaviour
{
    // Start is called before the first frame update 
    //UTF8编码
    #region EffectTurn
    public class EffectTurn
    {
        MMF_Player HoverEffect;
        MMF_Player HoverEffectRestore;
        public bool EnableAllEffect = true;
        private bool isLastTimeDirect = false;


        private void _playFeedBack()
        {

            HoverEffect?.PlayFeedbacks();
            HoverEffectRestore?.StopFeedbacks();

        }
        public void TurnOn()
        {
            if (!EnableAllEffect)
            {
                return;
            }
            if (!isLastTimeDirect)
            {
                isLastTimeDirect = true;
                _playFeedBack();
            }

        }
        public void TurnOff() {

            if (!EnableAllEffect)
            {
                return;
            }
            if (isLastTimeDirect)
            {
                isLastTimeDirect = false;
                _playRestoreFeedBack();
            }

        }
        private void _playRestoreFeedBack()
        {
            if (HoverEffectRestore != null)
            {
                HoverEffect.StopFeedbacks();
                HoverEffectRestore.PlayFeedbacks();

            }


        }
        public void TurnOver()
        {
            if (isLastTimeDirect)
            {

                TurnOff();

            }
            else
            {
                TurnOn();
            }
        }
        public EffectTurn  (MMF_Player effect,MMF_Player restore = null)
        {

            HoverEffect = effect;
            HoverEffectRestore = restore;

        }


    }
    #endregion

    public class EffectList { 
    
    
    
    
    }




}
