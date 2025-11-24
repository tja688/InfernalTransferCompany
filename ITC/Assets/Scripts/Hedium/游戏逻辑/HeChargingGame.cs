using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HeChargingGame : MonoBehaviour
{
    [SerializeField]
    private GameObject JudgmentGroupArea;
    [SerializeField]
    private GameObject JudgmentImagePrefab;
    [SerializeField]
    private GameObject StampTool;
    [SerializeField]
    private MMF_Player player;




    private void EnterAnimation()
    {
        player.PlayFeedbacks();

    }
    private void InitChargingGame()
    {
        EnterAnimation();
        Instantiate(JudgmentImagePrefab, JudgmentGroupArea.transform);

    }

    private void StartChargingGame()
    {

        InitChargingGame();
        SlotCenter.Instance.add_listener<PointerEventData>(HeEventNames.EndDragEvent, OnEndDragEvent);
        Debug.Log("HeChargingGame StartChargingGame");
    }

   public void OnEndDragEvent(PointerEventData eventData)
    {

    }



    public HeChargingGameHandler handle { get;private set; }

    public void SetHandle(HeChargingGameHandler handler)
    {
        handle = handler;
        StartChargingGame();
    }


}

public class HeChargingGameHandler
{
   public HeChargingGameHandler()
    {

    }
}
