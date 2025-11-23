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





    private void EnterAnimation()
    {

    }
    private void InitChargingGame()
    {
        EnterAnimation();




        Instantiate(JudgmentImagePrefab, JudgmentGroupArea.transform);
    }

    private void StartChargingGame()
    {





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
