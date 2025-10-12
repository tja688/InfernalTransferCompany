using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class UIRouter : MonoBehaviour
{
    private static UIRouter _instance;

    public static UIRouter Instance
    {
        get
        {
            if (_instance == null)
            {
                var existing = FindObjectOfType<UIRouter>();
                if (existing != null)
                {
                    _instance = existing;
                    _instance.InitializeSingleton();
                }
            }

            return _instance;
        }
    }

    public static bool HasInstance => Instance != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureRouter()
    {
        if (_instance == null)
        {
            var go = new GameObject("[UIRouter]");
            go.AddComponent<UIRouter>();
        }
    }

    private readonly Queue<UIRouteRequest> _requestQueue = new();
    private readonly List<UIHierarchyLayerManager> _layerManagers = new();
    private Dictionary<UIHierarchyLevel, Stack<UIRouteNode>> _stacks = new();
    private UIRoute _currentRoute = new();
    private bool _isTransitioning;

    public bool IsTransitioning => _isTransitioning;
    public UIRoute CurrentRoute => _currentRoute;

    public event Action<UIRouteChangeContext> OnRouteWillChange;
    public event Action<UIRouteChangeContext> OnRouteChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        InitializeSingleton();
    }

    private void InitializeSingleton()
    {
        DontDestroyOnLoad(gameObject);
        if (_stacks == null)
        {
            _stacks = new Dictionary<UIHierarchyLevel, Stack<UIRouteNode>>();
        }

        if (_currentRoute == null)
        {
            _currentRoute = new UIRoute();
        }
    }

    public void RegisterLayerManager(UIHierarchyLayerManager manager)
    {
        if (manager == null) return;
        if (_layerManagers.Contains(manager)) return;
        _layerManagers.Add(manager);
        manager.SyncImmediately(_currentRoute);
    }

    public void UnregisterLayerManager(UIHierarchyLayerManager manager)
    {
        if (manager == null) return;
        _layerManagers.Remove(manager);
    }

    public void GoTo(string path, UIHierarchyLevel startLevel = UIHierarchyLevel.GameUI, object payload = null)
    {
        EnqueueRequest(new UIRouteRequest
        {
            Command = UIRouteCommand.Replace,
            Path = path,
            StartLevel = startLevel,
            Payload = payload
        });
    }

    public void Push(string path, UIHierarchyLevel level, object payload = null)
    {
        EnqueueRequest(new UIRouteRequest
        {
            Command = UIRouteCommand.Push,
            Path = path,
            ModalLevel = level,
            Payload = payload
        });
    }

    public void Pop(UIHierarchyLevel level)
    {
        EnqueueRequest(new UIRouteRequest
        {
            Command = UIRouteCommand.Pop,
            ModalLevel = level
        });
    }

    private void EnqueueRequest(UIRouteRequest request)
    {
        if (request == null) return;
        _requestQueue.Enqueue(request);
        TryProcessNext();
    }

    private void TryProcessNext()
    {
        if (_isTransitioning) return;
        if (_requestQueue.Count == 0) return;

        var request = _requestQueue.Dequeue();
        StartCoroutine(ProcessRequest(request));
    }

    private IEnumerator ProcessRequest(UIRouteRequest request)
    {
        _isTransitioning = true;

        var previousRoute = _currentRoute?.Clone() ?? new UIRoute();
        var stacksCopy = CloneStacks();
        ApplyRequestToStacks(stacksCopy, request);
        var nextRoute = BuildRouteSnapshot(stacksCopy);

        var context = new UIRouteChangeContext(previousRoute, nextRoute, request);

        foreach (var manager in _layerManagers)
        {
            manager.CancelActiveTransitions();
        }

        OnRouteWillChange?.Invoke(context);
        foreach (var manager in _layerManagers)
        {
            manager.HandleRouteWillChange(context);
        }

        float maxDuration = 0f;
        foreach (var manager in _layerManagers)
        {
            maxDuration = Mathf.Max(maxDuration, manager.HandleRouteChanged(context));
        }

        _stacks = stacksCopy;
        _currentRoute = nextRoute;

        OnRouteChanged?.Invoke(context);

        if (maxDuration > 0f)
        {
            yield return new WaitForSeconds(maxDuration);
        }
        else
        {
            yield return null;
        }

        _isTransitioning = false;
        TryProcessNext();
    }

    private Dictionary<UIHierarchyLevel, Stack<UIRouteNode>> CloneStacks()
    {
        var copy = new Dictionary<UIHierarchyLevel, Stack<UIRouteNode>>();
        foreach (var pair in _stacks)
        {
            copy[pair.Key] = new Stack<UIRouteNode>(new Stack<UIRouteNode>(pair.Value));
        }

        return copy;
    }

    private void ApplyRequestToStacks(Dictionary<UIHierarchyLevel, Stack<UIRouteNode>> stacks, UIRouteRequest request)
    {
        if (request == null) return;

        switch (request.Command)
        {
            case UIRouteCommand.Replace:
                ApplyReplace(stacks, request);
                break;
            case UIRouteCommand.Push:
                ApplyPush(stacks, request);
                break;
            case UIRouteCommand.Pop:
                ApplyPop(stacks, request);
                break;
        }
    }

    private void ApplyReplace(Dictionary<UIHierarchyLevel, Stack<UIRouteNode>> stacks, UIRouteRequest request)
    {
        ClearStacksFromLevel(stacks, request.StartLevel);

        if (string.IsNullOrEmpty(request.Path))
        {
            return;
        }

        var segments = request.Path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
        {
            int levelValue = (int)request.StartLevel + i;
            if (levelValue > (int)UIHierarchyLevelUtility.Highest) break;
            var level = (UIHierarchyLevel)levelValue;
            var node = new UIRouteNode(segments[i], i == segments.Length - 1 ? request.Payload : null);
            var stack = GetOrCreateStack(stacks, level);
            stack.Push(node);
        }

        ClearStacksFromLevel(stacks, NextLevel((UIHierarchyLevel)((int)request.StartLevel + segments.Length - 1)));
    }

    private void ApplyPush(Dictionary<UIHierarchyLevel, Stack<UIRouteNode>> stacks, UIRouteRequest request)
    {
        if (string.IsNullOrEmpty(request.Path)) return;
        var level = request.ModalLevel.IsWithinBounds() ? request.ModalLevel : UIHierarchyLevel.PrimaryMenu;
        var stack = GetOrCreateStack(stacks, level);
        stack.Push(new UIRouteNode(request.Path, request.Payload));
        ClearStacksFromLevel(stacks, NextLevel(level));
    }

    private void ApplyPop(Dictionary<UIHierarchyLevel, Stack<UIRouteNode>> stacks, UIRouteRequest request)
    {
        var level = request.ModalLevel.IsWithinBounds() ? request.ModalLevel : UIHierarchyLevel.PrimaryMenu;
        if (!stacks.TryGetValue(level, out var stack) || stack.Count == 0) return;
        stack.Pop();
        if (stack.Count == 0)
        {
            stacks.Remove(level);
        }

        ClearStacksFromLevel(stacks, NextLevel(level));
    }

    private void ClearStacksFromLevel(Dictionary<UIHierarchyLevel, Stack<UIRouteNode>> stacks, UIHierarchyLevel level)
    {
        if (!level.IsWithinBounds())
        {
            return;
        }

        for (int value = (int)level; value <= (int)UIHierarchyLevelUtility.Highest; value++)
        {
            var key = (UIHierarchyLevel)value;
            if (stacks.TryGetValue(key, out var stack))
            {
                stack.Clear();
            }

            stacks.Remove(key);
        }
    }

    private UIHierarchyLevel NextLevel(UIHierarchyLevel level)
    {
        return level.TryGetNext(out var next) ? next : UIHierarchyLevelUtility.Highest;
    }

    private Stack<UIRouteNode> GetOrCreateStack(Dictionary<UIHierarchyLevel, Stack<UIRouteNode>> stacks, UIHierarchyLevel level)
    {
        if (!stacks.TryGetValue(level, out var stack))
        {
            stack = new Stack<UIRouteNode>();
            stacks[level] = stack;
        }

        return stack;
    }

    private UIRoute BuildRouteSnapshot(Dictionary<UIHierarchyLevel, Stack<UIRouteNode>> stacks)
    {
        var route = new UIRoute();
        foreach (var pair in stacks)
        {
            if (pair.Value.Count == 0) continue;
            var node = pair.Value.Peek();
            route.SetNode(pair.Key, node);
        }

        return route;
    }
}

public enum UIRouteCommand
{
    Replace,
    Push,
    Pop
}

public class UIRouteRequest
{
    public UIRouteCommand Command;
    public string Path;
    public object Payload;
    public UIHierarchyLevel StartLevel = UIHierarchyLevel.GameUI;
    public UIHierarchyLevel ModalLevel = UIHierarchyLevel.PrimaryMenu;
}
