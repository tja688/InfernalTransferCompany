using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HeKeyInput : MonoBehaviour
{
    // Start is called before the first frame update 
    //UTF8编码

    [SerializeField]
    private InputActionReference moveAction;
    [SerializeField]
    private InputActionReference interactAction;




    public event System.Action<int> OnMoveAction;
    public event System.Action OnInteractAction;





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







    public static HeKeyInput Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            EnableMoveAction();
            EnableInteractAction();
        }
        else
        {
            Destroy(gameObject);
        }

    }
    void Start()
    {
        

    }


 
}
