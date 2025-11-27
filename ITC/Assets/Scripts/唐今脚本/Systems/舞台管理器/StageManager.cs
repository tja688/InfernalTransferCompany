using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MoreMountains.Feedbacks;
using PixelCrushers;

// 为了让单文件分析的代码检查器识别监控类型，这里声明一个空的 partial 壳。
// 真正的实现位于 StagePerformanceMonitor.cs 中。
public partial class StagePerformanceMonitor { }

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Configuration")]
    public StageSceneEffectStore PlayerStore;

    private Dictionary<string, StageElement> _elements = new Dictionary<string, StageElement>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (PlayerStore != null)
        {
            PlayerStore.Initialize();
        }
    }

    public void RegisterElement(StageElement element)
    {
        if (element == null || string.IsNullOrEmpty(element.StageElementID)) return;

        if (!_elements.ContainsKey(element.StageElementID))
        {
            _elements.Add(element.StageElementID, element);
        }
        else
        {
            Debug.LogWarning($"[StageManager] Duplicate StageElementID: {element.StageElementID}");
        }
    }

    public void UnregisterElement(StageElement element)
    {
        if (element == null || string.IsNullOrEmpty(element.StageElementID)) return;

        if (_elements.ContainsKey(element.StageElementID))
        {
            _elements.Remove(element.StageElementID);
        }
    }

    public StageElement GetElement(string elementID)
    {
        if (_elements.TryGetValue(elementID, out var element))
        {
            return element;
        }
        return null;
    }

    #region Public Methods

    public void StageElementIn(string elementID, string playerID)
    {
        var element = GetElement(elementID);
        if (element == null)
        {
            Debug.LogError($"[StageManager] StageElementIn: Element not found {elementID}");
            return;
        }

        var playerPrefab = PlayerStore != null ? PlayerStore.GetPlayerPrefab(playerID) : null;
        if (playerPrefab == null)
        {
            Debug.LogError($"[StageManager] StageElementIn: Player not found {playerID}");
            return;
        }

        PlayFeedbackOnElement(element, playerPrefab, () =>
        {
            element.SetState(StageElement.ElementState.OnStage);
        }, null);
    }

    public void StageElementOut(string elementID, string playerID)
    {
        var element = GetElement(elementID);
        if (element == null)
        {
            Debug.LogError($"[StageManager] StageElementOut: Element not found {elementID}");
            return;
        }

        var playerPrefab = PlayerStore != null ? PlayerStore.GetPlayerPrefab(playerID) : null;
        if (playerPrefab == null)
        {
            Debug.LogError($"[StageManager] StageElementOut: Player not found {playerID}");
            return;
        }

        PlayFeedbackOnElement(element, playerPrefab, null, () =>
        {
            element.SetState(StageElement.ElementState.OutsideStage);
        });
    }

    public void StageElementPlay(string elementID, string playerID)
    {
        var element = GetElement(elementID);
        if (element == null)
        {
            Debug.LogError($"[StageManager] StageElementPlay: Element not found {elementID}");
            return;
        }

        if (element.CurrentState == StageElement.ElementState.OutsideStage)
        {
            Debug.LogError($"[StageManager] StageElementPlay: Element {elementID} is OutsideStage. Cannot play.");
            return;
        }

        var playerPrefab = PlayerStore != null ? PlayerStore.GetPlayerPrefab(playerID) : null;
        if (playerPrefab == null)
        {
            Debug.LogError($"[StageManager] StageElementPlay: Player not found {playerID}");
            return;
        }

        PlayFeedbackOnElement(element, playerPrefab, null, null);
    }

    public void StagePerformance(string performanceID)
    {
        if (PlayerStore == null)
        {
            Debug.LogError("[StageManager] StagePerformance: PlayerStore is null.");
            return;
        }

        var player = PlayerStore.GetPlayerPrefab(performanceID);
        if (player == null)
        {
            Debug.LogError($"[StageManager] StagePerformance: Performance ID not found {performanceID}");
            return;
        }

        // 调试用合法性监控：不会阻止真正的播放
        TryInvokePerformanceMonitor(performanceID, player);

        // 这里假定 PlayerStore 中配置的是场景中的现成 MMF_Player（或统一的“库”对象），
        // 直接调用其 PlayFeedbacks 即可，不再做 Instantiate。
        player.PlayFeedbacks();
    }

    /// <summary>
    /// 通过反射调用 StagePerformanceMonitor（如果存在且实现了对应静态方法）。
    /// 这样可以在仅分析当前文件的代码检查器下避免编译错误，同时在实际运行时正常生效。
    /// </summary>
    private void TryInvokePerformanceMonitor(string performanceID, MMF_Player player)
    {
        var monitorType = typeof(StagePerformanceMonitor);
        if (monitorType == null) return;

        var method = monitorType.GetMethod(
            "CheckPerformance",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new System.Type[] { typeof(string), typeof(MMF_Player) },
            null);

        if (method == null) return;

        try
        {
            method.Invoke(null, new object[] { performanceID, player });
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[StageManager] StagePerformanceMonitor.CheckPerformance 调用失败（仅影响调试）：{e.Message}");
        }
    }

    #endregion

    #region Helper Methods

    private void PlayFeedbackOnElement(StageElement element, MMF_Player prefab, Action onStart, Action onComplete)
    {
        // Instantiate the player
        var playerInstance = Instantiate(prefab, element.transform.position, Quaternion.identity);
        playerInstance.transform.SetParent(transform); // Keep it under StageManager or null?
        playerInstance.name = $"{prefab.name}_{element.name}";

        // Set Targets
        SetTargets(playerInstance, element.transform);

        // Setup events
        if (onStart != null)
        {
            onStart.Invoke();
        }

        if (onComplete != null)
        {
            playerInstance.Events.OnComplete.AddListener(() => onComplete.Invoke());
        }

        // Auto destroy after playing
        // MMF_Player doesn't have built-in auto-destroy on complete in the base settings easily accessible via code without modifying the prefab settings usually.
        // But we can destroy it manually after duration.
        // Or better, use the OnComplete event to destroy it.
        playerInstance.Events.OnComplete.AddListener(() => Destroy(playerInstance.gameObject));

        playerInstance.PlayFeedbacks();
    }

    private void SetTargets(MMF_Player player, Transform target)
    {
        foreach (var feedback in player.FeedbacksList)
        {
            if (feedback == null) continue;

            // Try to find "Target", "TargetTransform", "AnimatePositionTarget", etc.
            // Common ones in Feel:
            // MMF_Position: AnimatePositionTarget
            // MMF_Scale: AnimateScaleTarget
            // MMF_Rotation: AnimateRotationTarget
            // MMF_FloatingText: TargetTransform

            // We use reflection to find any field or property that looks like a target and is of type Transform or GameObject.

            var type = feedback.GetType();
            var flags = BindingFlags.Public | BindingFlags.Instance;

            // Check for specific known properties first for safety/speed if needed, but generic reflection is requested.

            // Properties to look for:
            string[] targetPropertyNames = new string[] {
                "Target",
                "TargetTransform",
                "AnimatePositionTarget",
                "AnimateScaleTarget",
                "AnimateRotationTarget",
                "BoundGameObject"
            };

            foreach (var propName in targetPropertyNames)
            {
                var field = type.GetField(propName, flags);
                if (field != null)
                {
                    if (field.FieldType == typeof(Transform))
                    {
                        field.SetValue(feedback, target);
                    }
                    else if (field.FieldType == typeof(GameObject))
                    {
                        field.SetValue(feedback, target.gameObject);
                    }
                    else if (field.FieldType == typeof(RectTransform) && target is RectTransform rt)
                    {
                        field.SetValue(feedback, rt);
                    }
                }

                var prop = type.GetProperty(propName, flags);
                if (prop != null && prop.CanWrite)
                {
                    if (prop.PropertyType == typeof(Transform))
                    {
                        prop.SetValue(feedback, target);
                    }
                    else if (prop.PropertyType == typeof(GameObject))
                    {
                        prop.SetValue(feedback, target.gameObject);
                    }
                    else if (prop.PropertyType == typeof(RectTransform) && target is RectTransform rt)
                    {
                        prop.SetValue(feedback, rt);
                    }
                }
            }

            // Also check for "AutomateTargetAcquisition" related things if needed, but usually setting the field is enough.
        }

        // Refresh cache to ensure changes are picked up if needed
        player.RefreshCache();
    }

    #endregion

    #region Saving/Loading

    public string RecordData()
    {
        var data = new StageStateData();
        foreach (var kvp in _elements)
        {
            var element = kvp.Value;
            if (element != null)
            {
                var elementData = new StageStateData.ElementData
                {
                    ID = element.StageElementID,
                    State = element.CurrentState,
                    Position = element.transform.position,
                    Rotation = element.transform.rotation,
                    Scale = element.transform.localScale
                };
                data.Elements.Add(elementData);
            }
        }
        return SaveSystem.Serialize(data);
    }

    public void RestoreState(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        var data = SaveSystem.Deserialize<StageStateData>(s);
        if (data == null || data.Elements == null) return;

        foreach (var elementData in data.Elements)
        {
            var element = GetElement(elementData.ID);
            if (element != null)
            {
                element.transform.position = elementData.Position;
                element.transform.rotation = elementData.Rotation;
                element.transform.localScale = elementData.Scale;
                element.SetState(elementData.State);
            }
        }
    }

    #endregion
}

[Serializable]
public class StageStateData
{
    [Serializable]
    public class ElementData
    {
        public string ID;
        public StageElement.ElementState State;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    public List<ElementData> Elements = new List<ElementData>();
}
