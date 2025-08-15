using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager2D : MonoBehaviour
{
    public static TransitionManager2D Instance { get; private set; }

    [Header("Prefab & Parent")]
    public TransitionSlice slicePrefab;        
    public RectTransform spawnParent;          

    [Header("Motion")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0,0,1,1);
    public float slideDuration = 0.6f;
    public Vector2 inStartPos = new Vector2(0f, 1100f);
    public Vector2 inEndPos   = Vector2.zero;
    public Vector2 outEndPos  = new Vector2(0f, 1100f);

    [Header("Activation Timing")]
    [Tooltip("在视频结束前多少秒提前激活新场景（仍被幕布遮住）。")]
    public float preActivateLeadSeconds = 0.25f;

    [Header("Safety / Debug")]
    public float prepareTimeout = 3f;          // 预加载视频超时（秒）
    public float activateTimeout = 5f;         // 允许激活后最多等待完成的时间（秒）
    public bool  debugLogs = true;

    [Header("Test (press N)")]
    public string testSceneName = "Scene_B";

    bool busy;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (!spawnParent) spawnParent = transform as RectTransform;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) && !string.IsNullOrEmpty(testSceneName))
            StartTransition(testSceneName);
    }

    public void StartTransition(string targetScene)
    {
        if (busy) return;
        if (!slicePrefab) { DebugLogError("未绑定 slicePrefab。"); return; }
        if (string.IsNullOrEmpty(targetScene)) { DebugLogError("目标场景为空。"); return; }
        StartCoroutine(Co_StartTransition(targetScene));
    }

    IEnumerator Co_StartTransition(string targetScene)
    {
        busy = true;

        // 1) 生成切片 & 入场
        var slice = Instantiate(slicePrefab, spawnParent);
        slice.gameObject.SetActive(true);
        slice.Configure(easeCurve, slideDuration, prepareTimeout, debugLogs);
        slice.SetPositions(inStartPos, inEndPos, outEndPos);

        yield return slice.PlayIn();

        // 2) 开始加载目标场景（先不激活），同时播放视频
        var op = SceneManager.LoadSceneAsync(targetScene);
        if (op == null) { DebugLogError($"LoadSceneAsync 返回 null，检查场景名：{targetScene}"); Cleanup(slice); yield break; }
        op.allowSceneActivation = false;

        slice.PlayConfigured();

        // 3) 预激活策略：到片尾前阈值 或 加载已到 0.9（二者先到先触发）
        //    触发时立即 allowSceneActivation=true（仍被幕布遮住）
        bool preActivated = false;

        IEnumerator PreActivator()
        {
            // 等到片尾前阈值
            yield return slice.WaitUntilPreActivatePoint(preActivateLeadSeconds);
            if (!preActivated)
            {
                if (debugLogs) Debug.Log("[Transition] 到达预激活时间点。");
                preActivated = true;
                op.allowSceneActivation = true;
            }
        }

        IEnumerator LoadWatcher()
        {
            // 等加载到 0.9
            yield return new WaitUntil(() => op.progress >= 0.9f);
            if (!preActivated)
            {
                if (debugLogs) Debug.Log("[Transition] 加载达到 0.9，提前激活。");
                preActivated = true;
                op.allowSceneActivation = true;
            }
        }

        // 并行等待任一条件
        yield return StartCoroutine(Race(PreActivator(), LoadWatcher()));

        // 4) 等视频自然结束
        yield return slice.WaitForVideoFinished();

        // 兜底：若此刻仍未完成（极慢设备），再等一段时间
        if (!op.isDone)
        {
            if (debugLogs) Debug.LogWarning("[Transition] 视频结束但场景未完成，进入激活兜底等待。");
            op.allowSceneActivation = true;
            float t = 0f;
            while (!op.isDone && t < activateTimeout) { t += Time.unscaledDeltaTime; yield return null; }
            if (!op.isDone) DebugLogError("激活超时未完成，请检查场景是否在 Build Settings 中且名称正确。");
        }

        // 5) 出场 & 清理
        yield return slice.PlayOut();
        Cleanup(slice);
        busy = false;
    }

    IEnumerator Race(IEnumerator a, IEnumerator b)
    {
        bool done = false;
        IEnumerator Wrap(IEnumerator e) { while (e.MoveNext() && !done) yield return e.Current; done = true; }
        yield return StartCoroutine(Wrap(a));
        // 停掉另一个
    }

    void Cleanup(TransitionSlice slice)
    {
        if (slice) Destroy(slice.gameObject);
    }

    void DebugLogError(string msg) { if (debugLogs) Debug.LogError("[Transition] " + msg); }
}
