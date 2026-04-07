using System;
using System.Collections;
using System.Collections.Generic;
using ITC.UIFX;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 为暂停菜单里的存档/读档条目补回悬停预览翻牌逻辑。
/// 迁移后部分条目丢失了原本的 Hover 事件链，这里在运行时统一重绑。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(5101)]
public sealed class SaveLoadSlotPreviewBinder : MonoBehaviour
{
    [Serializable]
    private sealed class PanelBinding
    {
        public string panelName;
        public string buttonsRootPath;
        public string previewBoardPath;
        public int defaultTextureIndex;

        [NonSerialized] public Transform buttonsRoot;
        [NonSerialized] public UISolariBoard previewBoard;
    }

    private sealed class ProxyBinding
    {
        public string PanelName;
        public UIBehaviourProxy Proxy;
        public UISolariBoard PreviewBoard;
        public int TextureIndex;
        public Action<PointerEventData> EnterHandler;
        public Action<PointerEventData> ExitHandler;
    }

    [SerializeField] private bool autoResolveFromRootCanvas = true;
    [SerializeField, Min(0.1f)] private float rebindInterval = 0.5f;
    [SerializeField] private bool enableDebugLog = false;

    [Header("存档面板")]
    [SerializeField] private PanelBinding saveBinding = new PanelBinding
    {
        panelName = "Save Panel",
        buttonsRootPath = "存档菜单Canvas/存档菜单面板/界面组件/存储条",
        previewBoardPath = "存档菜单Canvas/存档菜单面板/背景板/提示图翻牌器-存档菜单",
        defaultTextureIndex = 1
    };

    [Header("读档面板")]
    [SerializeField] private PanelBinding loadBinding = new PanelBinding
    {
        panelName = "Load Panel",
        buttonsRootPath = "加载菜单Canvas/存档菜单面板/界面组件/存储条",
        previewBoardPath = "加载菜单Canvas/存档菜单面板/背景板/提示图翻牌器-加载菜单",
        defaultTextureIndex = 2
    };

    private readonly Dictionary<UIBehaviourProxy, ProxyBinding> _proxyBindings = new Dictionary<UIBehaviourProxy, ProxyBinding>();
    private UIBehaviourProxy _currentHoverProxy;
    private UISolariBoard _currentHoverBoard;
    private Coroutine _pendingClearRoutine;
    private UISolariBoard _pendingClearBoard;
    private float _nextRebindTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var rootCanvas = GameObject.Find("根Canvas");
        if (rootCanvas == null)
        {
            return;
        }

        var binder = rootCanvas.GetComponent<SaveLoadSlotPreviewBinder>();
        if (binder == null)
        {
            binder = rootCanvas.AddComponent<SaveLoadSlotPreviewBinder>();
        }

        binder.Rebind();
    }

    private void Awake()
    {
        ResolveBindings();
    }

    private void OnEnable()
    {
        Rebind();
        _nextRebindTime = Time.unscaledTime + rebindInterval;
    }

    private void OnDisable()
    {
        CancelPendingClear();
        UnbindAll();
        _currentHoverProxy = null;
        _currentHoverBoard = null;
    }

    private void Update()
    {
        if (!Application.isPlaying || rebindInterval <= 0f)
        {
            return;
        }

        if (Time.unscaledTime < _nextRebindTime)
        {
            return;
        }

        _nextRebindTime = Time.unscaledTime + rebindInterval;
        Rebind();
    }

    [ContextMenu("Rebind Save/Load Preview Hover")]
    public void Rebind()
    {
        ResolveBindings();
        CleanupDestroyedBindings();
        BindPanel(saveBinding);
        BindPanel(loadBinding);
    }

    private void ResolveBindings()
    {
        if (!autoResolveFromRootCanvas)
        {
            return;
        }

        ResolveBinding(saveBinding);
        ResolveBinding(loadBinding);
    }

    private void ResolveBinding(PanelBinding binding)
    {
        if (binding == null)
        {
            return;
        }

        binding.buttonsRoot = ResolveTransform(binding.buttonsRootPath);

        var boardTransform = ResolveTransform(binding.previewBoardPath);
        binding.previewBoard = boardTransform != null
            ? boardTransform.GetComponent<UISolariBoard>()
            : null;
    }

    private Transform ResolveTransform(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        return transform.Find(relativePath);
    }

    private void BindPanel(PanelBinding binding)
    {
        if (binding == null || binding.buttonsRoot == null || binding.previewBoard == null)
        {
            return;
        }

        var buttons = binding.buttonsRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            var proxy = button.GetComponent<UIBehaviourProxy>();
            if (proxy == null)
            {
                proxy = button.gameObject.AddComponent<UIBehaviourProxy>();
            }

            UISolariBoard previewBoard = ResolvePreviewBoardForButton(button.transform, binding);
            if (previewBoard == null)
            {
                continue;
            }

            RebindProxy(binding, proxy, previewBoard);
        }
    }

    private UISolariBoard ResolvePreviewBoardForButton(Transform buttonTransform, PanelBinding fallbackBinding)
    {
        if (buttonTransform == null)
        {
            return null;
        }

        Transform saveCanvas = ResolveTransform("存档菜单Canvas");
        if (saveCanvas != null && buttonTransform.IsChildOf(saveCanvas))
        {
            return saveBinding.previewBoard;
        }

        Transform loadCanvas = ResolveTransform("加载菜单Canvas");
        if (loadCanvas != null && buttonTransform.IsChildOf(loadCanvas))
        {
            return loadBinding.previewBoard;
        }

        return fallbackBinding != null ? fallbackBinding.previewBoard : null;
    }

    private void RebindProxy(PanelBinding binding, UIBehaviourProxy proxy, UISolariBoard previewBoard)
    {
        if (proxy == null || previewBoard == null)
        {
            return;
        }

        if (_proxyBindings.TryGetValue(proxy, out var existing))
        {
            proxy.onEnter -= existing.EnterHandler;
            proxy.onExit -= existing.ExitHandler;
        }

        var runtimeBinding = new ProxyBinding
        {
            PanelName = binding.panelName,
            Proxy = proxy,
            PreviewBoard = previewBoard,
            TextureIndex = binding.defaultTextureIndex
        };

        runtimeBinding.EnterHandler = _ => HandlePointerEnter(runtimeBinding);
        runtimeBinding.ExitHandler = _ => HandlePointerExit(runtimeBinding);

        proxy.onEnter += runtimeBinding.EnterHandler;
        proxy.onExit += runtimeBinding.ExitHandler;
        _proxyBindings[proxy] = runtimeBinding;
    }

    private void HandlePointerEnter(ProxyBinding binding)
    {
        if (binding == null || binding.PreviewBoard == null)
        {
            return;
        }

        CancelPendingClear(binding.PreviewBoard);
        _currentHoverProxy = binding.Proxy;
        _currentHoverBoard = binding.PreviewBoard;
        binding.PreviewBoard.StartFlipTransition(binding.TextureIndex);

        if (enableDebugLog)
        {
            Debug.Log($"[SaveLoadSlotPreviewBinder] Hover enter: {binding.Proxy.name} -> {binding.PanelName} texture {binding.TextureIndex} on {binding.PreviewBoard.name}", binding.Proxy);
        }
    }

    private void HandlePointerExit(ProxyBinding binding)
    {
        if (binding == null || binding.PreviewBoard == null)
        {
            return;
        }

        if (_currentHoverProxy == binding.Proxy)
        {
            _currentHoverProxy = null;
            _currentHoverBoard = null;
        }

        QueueClear(binding.PreviewBoard);
    }

    private void QueueClear(UISolariBoard previewBoard)
    {
        if (previewBoard == null)
        {
            return;
        }

        CancelPendingClear();
        _pendingClearBoard = previewBoard;
        _pendingClearRoutine = StartCoroutine(ClearPreviewNextFrame(previewBoard));
    }

    private IEnumerator ClearPreviewNextFrame(UISolariBoard previewBoard)
    {
        yield return null;

        if (_currentHoverProxy == null && _currentHoverBoard == null && previewBoard != null)
        {
            previewBoard.StartFlipToClearAndDeselect();

            if (enableDebugLog)
            {
                Debug.Log($"[SaveLoadSlotPreviewBinder] Hover exit clear: {previewBoard.name}", this);
            }
        }

        _pendingClearRoutine = null;
        _pendingClearBoard = null;
    }

    private void CancelPendingClear()
    {
        if (_pendingClearRoutine == null)
        {
            return;
        }

        StopCoroutine(_pendingClearRoutine);
        _pendingClearRoutine = null;
        _pendingClearBoard = null;
    }

    private void CancelPendingClear(UISolariBoard previewBoard)
    {
        if (_pendingClearRoutine == null || _pendingClearBoard != previewBoard)
        {
            return;
        }

        CancelPendingClear();
    }

    private void CleanupDestroyedBindings()
    {
        if (_proxyBindings.Count == 0)
        {
            return;
        }

        var staleKeys = ListPool<UIBehaviourProxy>.Get();

        foreach (var pair in _proxyBindings)
        {
            if (pair.Key != null)
            {
                continue;
            }

            staleKeys.Add(pair.Key);
        }

        for (int i = 0; i < staleKeys.Count; i++)
        {
            _proxyBindings.Remove(staleKeys[i]);
        }

        ListPool<UIBehaviourProxy>.Release(staleKeys);
    }

    private void UnbindAll()
    {
        foreach (var pair in _proxyBindings)
        {
            if (pair.Key == null)
            {
                continue;
            }

            pair.Key.onEnter -= pair.Value.EnterHandler;
            pair.Key.onExit -= pair.Value.ExitHandler;
        }

        _proxyBindings.Clear();
    }

    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new Stack<List<T>>();

        public static List<T> Get()
        {
            return Pool.Count > 0 ? Pool.Pop() : new List<T>();
        }

        public static void Release(List<T> list)
        {
            if (list == null)
            {
                return;
            }

            list.Clear();
            Pool.Push(list);
        }
    }
}
