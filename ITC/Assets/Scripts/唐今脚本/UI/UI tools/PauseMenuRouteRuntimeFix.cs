using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 为旧版暂停菜单补回 Save/Load 路由与子菜单显隐链路。
/// 当前场景里 Pause 菜单仍在使用 PanelManager，但存储/加载按钮与子菜单关闭按钮的绑定已经缺失。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(5000)]
public sealed class PauseMenuRouteRuntimeFix : MonoBehaviour
{
    private const string PausePanelName = "Pause Panel";
    private const string SavePanelName = "Save Panel";
    private const string LoadPanelName = "Load Panel";

    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private GameObject saveCanvas;
    [SerializeField] private GameObject loadCanvas;
    [SerializeField] private GraphicRaycaster pauseRaycaster;
    [SerializeField] private SlotMachinePicker pausePicker;

    private string _lastAppliedPanel = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var rootCanvas = GameObject.Find("根Canvas");
        if (rootCanvas == null)
        {
            return;
        }

        var fix = rootCanvas.GetComponent<PauseMenuRouteRuntimeFix>();
        if (fix == null)
        {
            fix = rootCanvas.AddComponent<PauseMenuRouteRuntimeFix>();
        }

        fix.Rebind();
    }

    private void Awake()
    {
        Rebind();
    }

    private void OnEnable()
    {
        ApplyPanelState(true);
    }

    private void Update()
    {
        ApplyPanelState(false);
    }

    public void RefreshNow()
    {
        ApplyPanelState(true);
    }

    public void Rebind()
    {
        pauseCanvas = FindChild("暂停菜单Canvas")?.gameObject;
        saveCanvas = FindChild("存档菜单Canvas")?.gameObject;
        loadCanvas = FindChild("加载菜单Canvas")?.gameObject;

        if (pauseCanvas != null)
        {
            pauseRaycaster = pauseCanvas.GetComponent<GraphicRaycaster>();
            pausePicker = pauseCanvas.GetComponentInChildren<SlotMachinePicker>(true);
        }
        else
        {
            pauseRaycaster = null;
            pausePicker = null;
        }

        BindPanelButton("暂停菜单Canvas/暂停菜单面板/暂停菜单按钮/View/暂停菜单按钮集合/存储", SavePanelName);
        BindPanelButton("暂停菜单Canvas/暂停菜单面板/暂停菜单按钮/View/暂停菜单按钮集合/加载", LoadPanelName);
        BindPanelButton("存档菜单Canvas/存档菜单面板/关闭按钮", PausePanelName, true);
        BindPanelButton("加载菜单Canvas/存档菜单面板/关闭按钮", PausePanelName, true);

        ApplyPanelState(true);
    }

    private void ApplyPanelState(bool force)
    {
        var panelManager = PanelManager.Instance;
        if (panelManager == null)
        {
            return;
        }

        string currentPanel = panelManager.CurrentPanel;
        if (!force && string.Equals(_lastAppliedPanel, currentPanel, System.StringComparison.Ordinal))
        {
            return;
        }

        _lastAppliedPanel = currentPanel;

        bool saveVisible = string.Equals(currentPanel, SavePanelName, System.StringComparison.Ordinal);
        bool loadVisible = string.Equals(currentPanel, LoadPanelName, System.StringComparison.Ordinal);
        bool pauseCanvasRaycastEnabled = !saveVisible && !loadVisible;
        bool pausePickerInputEnabled = string.Equals(currentPanel, PausePanelName, System.StringComparison.Ordinal);

        SetCanvasVisible(saveCanvas, saveVisible);
        SetCanvasVisible(loadCanvas, loadVisible);

        if (pauseRaycaster != null)
        {
            pauseRaycaster.enabled = pauseCanvasRaycastEnabled;
        }

        if (pausePicker != null)
        {
            pausePicker.MouseInputEnabled = pausePickerInputEnabled;
        }
    }

    private void BindPanelButton(string relativePath, string targetPanel, bool addButtonIfMissing = false)
    {
        Transform target = FindChild(relativePath);
        if (target == null)
        {
            return;
        }

        var button = target.GetComponent<Button>();
        if (button == null && addButtonIfMissing)
        {
            button = target.gameObject.AddComponent<Button>();
            button.targetGraphic = target.GetComponent<Graphic>();
        }

        if (button == null)
        {
            return;
        }

        if (button.targetGraphic == null)
        {
            button.targetGraphic = target.GetComponent<Graphic>();
        }

        var jumpButton = target.GetComponent<PauseMenuPanelJumpButton>();
        if (jumpButton == null)
        {
            jumpButton = target.gameObject.AddComponent<PauseMenuPanelJumpButton>();
        }

        jumpButton.Configure(button, targetPanel);
    }

    private void SetCanvasVisible(GameObject canvasObject, bool visible)
    {
        if (canvasObject == null || canvasObject.activeSelf == visible)
        {
            return;
        }

        canvasObject.SetActive(visible);
    }

    private Transform FindChild(string relativePath)
    {
        return transform.Find(relativePath);
    }
}

[DisallowMultipleComponent]
public sealed class PauseMenuPanelJumpButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private string targetPanel;

    private bool _registered;

    public void Configure(Button sourceButton, string panelName)
    {
        if (button != sourceButton)
        {
            Unregister();
            button = sourceButton;
        }

        targetPanel = panelName;
        Register();
    }

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void Register()
    {
        if (_registered || button == null)
        {
            return;
        }

        button.onClick.AddListener(HandleClick);
        _registered = true;
    }

    private void Unregister()
    {
        if (!_registered || button == null)
        {
            return;
        }

        button.onClick.RemoveListener(HandleClick);
        _registered = false;
    }

    private void HandleClick()
    {
        if (PanelManager.Instance == null || string.IsNullOrEmpty(targetPanel))
        {
            return;
        }

        PanelManager.Instance.ChangePanel(targetPanel);
        var runtimeFix = GetComponentInParent<PauseMenuRouteRuntimeFix>();
        if (runtimeFix != null)
        {
            runtimeFix.RefreshNow();
        }
    }
}
