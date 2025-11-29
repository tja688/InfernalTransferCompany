using System;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using PixelCrushers;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Configuration")]
    public StageSceneEffectStore PlayerStore;

    [Header("Stage Simulation")]
    [Tooltip("舞台的世界空间 Bounds。中心与尺寸均使用世界坐标。")]
    public Bounds StageBounds = new Bounds(Vector3.zero, new Vector3(10f, 5f, 10f));

    [Tooltip("舞台外沿的缓冲距离，用于离场判定的迟滞处理。")]
    [Min(0f)]
    public float ExitHysteresis = 1f;

    [Tooltip("舞台检测的帧间隔。1 表示每帧检测。")]
    [Min(1)]
    public int SimulationIntervalFrames = 1;

    [Tooltip("若为 true，当检测到无效引用时将重新扫描所有 StageElement。")]
    public bool AutoRefreshOnNullEntries = true;

    [SerializeField]
    private List<StageElement> _allElements = new List<StageElement>();

    private readonly Dictionary<string, StageElement> _elementsById = new Dictionary<string, StageElement>();
    private Bounds _bufferedStageBounds;
    private int _simulationFrameTicker;

    public IReadOnlyList<StageElement> AllElements => _allElements;

    private void Reset()
    {
        StageBounds = new Bounds(transform.position, new Vector3(10f, 5f, 10f));
        ExitHysteresis = 1f;
        SimulationIntervalFrames = 1;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (PlayerStore != null)
        {
            PlayerStore.Initialize();
        }

        RefreshElementCache();
        UpdateBufferedBounds();
    }

    private void OnValidate()
    {
        ExitHysteresis = Mathf.Max(0f, ExitHysteresis);
        SimulationIntervalFrames = Mathf.Max(1, SimulationIntervalFrames);
        EnsureBoundsValid();
        UpdateBufferedBounds();
    }

    private void Update()
    {
        if (_allElements == null || _allElements.Count == 0)
        {
            return;
        }

        if (SimulationIntervalFrames > 1)
        {
            _simulationFrameTicker++;
            if (_simulationFrameTicker % SimulationIntervalFrames != 0)
            {
                return;
            }
        }

        RunSimulationStep();
    }

    #region Stage Element Tracking

    public void RefreshElementCache()
    {
        _allElements.Clear();
        _elementsById.Clear();

        var elements = Resources.FindObjectsOfTypeAll<StageElement>();
        foreach (var element in elements)
        {
            if (!IsSceneElement(element))
            {
                continue;
            }

            if (!_allElements.Contains(element))
            {
                _allElements.Add(element);
            }

            if (string.IsNullOrEmpty(element.StageElementID))
            {
                continue;
            }

            if (_elementsById.TryGetValue(element.StageElementID, out var existing) && existing != null && existing != element)
            {
                Debug.LogWarning($"[StageManager] Duplicate StageElementID detected: {element.StageElementID}", element);
                continue;
            }

            _elementsById[element.StageElementID] = element;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh Stage Elements")]
    private void ContextRefreshElements()
    {
        RefreshElementCache();
    }
#endif

    internal void HandleElementDestroyed(StageElement element)
    {
        if (element == null)
        {
            return;
        }

        _allElements.Remove(element);

        if (!string.IsNullOrEmpty(element.StageElementID) &&
            _elementsById.TryGetValue(element.StageElementID, out var existing) &&
            existing == element)
        {
            _elementsById.Remove(element.StageElementID);
        }
    }

    public StageElement GetElement(string elementID)
    {
        if (string.IsNullOrEmpty(elementID))
        {
            return null;
        }

        if (_elementsById.TryGetValue(elementID, out var element))
        {
            return element;
        }

        return null;
    }

    #endregion

    #region Simulation

    private void RunSimulationStep()
    {
        bool hasNullReference = false;

        foreach (var element in _allElements)
        {
            if (element == null)
            {
                hasNullReference = true;
                continue;
            }

            var elementTransform = element.StageTransform;
            if (elementTransform == null)
            {
                hasNullReference = true;
                continue;
            }

            var position = elementTransform.position;
            bool isInsideCoreBounds = StageBounds.Contains(position);
            bool isInsideBufferedBounds = _bufferedStageBounds.Contains(position);
            bool isActive = element.gameObject.activeSelf;

            if (!isActive && isInsideCoreBounds)
            {
                ActivateElement(element);
            }
            else if (isActive && !isInsideBufferedBounds)
            {
                DeactivateElement(element);
            }
        }

        if (hasNullReference)
        {
            if (AutoRefreshOnNullEntries)
            {
                RefreshElementCache();
            }
            else
            {
                CleanupElementCache();
            }
        }
    }

    private void ActivateElement(StageElement element)
    {
        element.gameObject.SetActive(true);
        element.OnStageEnter();
    }

    private void DeactivateElement(StageElement element)
    {
        element.OnStageExit();
        element.gameObject.SetActive(false);
    }

    private void ForceRemoveElement(StageElement element)
    {
        if (element == null)
        {
            return;
        }

        if (element.gameObject.activeSelf)
        {
            element.OnStageExit();
            element.gameObject.SetActive(false);
        }
        else
        {
            element.ApplyState(StageElement.ElementState.OutsideStage);
            element.RestoreSubStateSnapshot(StageElementSubStates.Outside);
        }
    }

    private void ForcePlaceElement(StageElement element, StageStateData.ElementData elementData)
    {
        if (element == null || elementData == null)
        {
            return;
        }

        var elementTransform = element.StageTransform;
        elementTransform.position = elementData.Position;
        elementTransform.rotation = elementData.Rotation;
        elementTransform.localScale = elementData.Scale;

        if (!element.gameObject.activeSelf)
        {
            element.gameObject.SetActive(true);
        }

        element.OnStageEnter();
    }

    private void CleanupElementCache()
    {
        _allElements.RemoveAll(e => e == null || !IsSceneElement(e));
        RebuildLookupDictionary();
    }

    private void RebuildLookupDictionary()
    {
        _elementsById.Clear();
        foreach (var element in _allElements)
        {
            if (element == null) continue;
            if (string.IsNullOrEmpty(element.StageElementID)) continue;

            if (_elementsById.TryGetValue(element.StageElementID, out var existing) && existing != element)
            {
                continue;
            }

            _elementsById[element.StageElementID] = element;
        }
    }

    private static bool IsSceneElement(StageElement element)
    {
        if (element == null)
        {
            return false;
        }

        var go = element.gameObject;
        if (go == null)
        {
            return false;
        }

        return go.scene.IsValid();
    }

    private void EnsureBoundsValid()
    {
        var bounds = StageBounds;
        var size = bounds.size;
        const float minSize = 0.01f;
        size.x = Mathf.Max(minSize, size.x);
        size.y = Mathf.Max(minSize, size.y);
        size.z = Mathf.Max(minSize, size.z);
        bounds.size = size;
        StageBounds = bounds;
    }

    private void UpdateBufferedBounds()
    {
        _bufferedStageBounds = StageBounds;
        if (ExitHysteresis > 0f)
        {
            _bufferedStageBounds.Expand(ExitHysteresis * 2f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(StageBounds.center, StageBounds.size);

        if (ExitHysteresis > 0f)
        {
            var buffered = StageBounds;
            buffered.Expand(ExitHysteresis * 2f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(buffered.center, buffered.size);
        }
    }

    #endregion

    #region Public Methods

    public void StageElementPlay(string playerID, bool forceSimulationSync = true)
    {
        if (string.IsNullOrEmpty(playerID))
        {
            Debug.LogWarning("[StageManager] StageElementPlay: playerID is empty.");
            return;
        }

        if (PlayerStore == null)
        {
            Debug.LogError("[StageManager] StageElementPlay: PlayerStore is null.");
            return;
        }

        if (forceSimulationSync)
        {
            RunSimulationStep();
        }

        var sourcePlayer = PlayerStore.GetPlayerPrefab(playerID);
        if (sourcePlayer == null)
        {
            Debug.LogError($"[StageManager] StageElementPlay: Player not found {playerID}");
            return;
        }

        var playerInstance = CreatePlayerInstance(sourcePlayer, out bool isRuntimeInstance);
        if (playerInstance == null)
        {
            Debug.LogError($"[StageManager] StageElementPlay: Failed to create player instance for {playerID}");
            return;
        }

        if (isRuntimeInstance)
        {
            playerInstance.Events.OnComplete.AddListener(() =>
            {
                if (playerInstance != null)
                {
                    Destroy(playerInstance.gameObject);
                }
            });
        }

        playerInstance.PlayFeedbacks();
    }

    public void ForceSimulationStep()
    {
        RunSimulationStep();
    }

    #endregion

    #region Helper Methods

    private MMF_Player CreatePlayerInstance(MMF_Player source, out bool isRuntimeInstance)
    {
        isRuntimeInstance = false;

        if (source == null)
        {
            return null;
        }

        if (source.gameObject.scene.IsValid())
        {
            return source;
        }

        var instance = Instantiate(source, transform, false);
        instance.name = $"{source.name}_Runtime";
        isRuntimeInstance = true;
        return instance;
    }

    #endregion

    #region Saving/Loading

    public string RecordData()
    {
        var data = new StageStateData();
        foreach (var element in _allElements)
        {
            if (element == null) continue;
            if (string.IsNullOrEmpty(element.StageElementID)) continue;
            if (!element.gameObject.activeSelf) continue;

            var elementData = new StageStateData.ElementData
            {
                ID = element.StageElementID,
                Position = element.StageTransform.position,
                Rotation = element.StageTransform.rotation,
                Scale = element.StageTransform.localScale
            };
            data.Elements.Add(elementData);
        }
        return SaveSystem.Serialize(data);
    }

    public void RestoreState(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        var data = SaveSystem.Deserialize<StageStateData>(s);
        if (data == null || data.Elements == null) return;

        foreach (var element in _allElements)
        {
            ForceRemoveElement(element);
        }

        foreach (var elementData in data.Elements)
        {
            var element = GetElement(elementData.ID);
            if (element == null) continue;

            ForcePlaceElement(element, elementData);
        }

        RunSimulationStep();
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
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    public List<ElementData> Elements = new List<ElementData>();
}
