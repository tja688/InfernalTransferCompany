// AIPauseOnMapOpen.cs
using UnityEngine;
using Pathfinding;            // A* Pathfinding Project 命名空间

public class AIPauseOnMapOpen : MonoBehaviour {
    [Header("Hook")]
    public MapIconController mapIconController;      // 拖你的 MapIconController
    public MonoBehaviour agentComponent;             // 拖 AIPath / RichAI 组件

    IAstarAI agent;

    void Awake() {
        if (!agentComponent) {
            Debug.LogError("[AIPauseOnMapOpen] 请把 AIPath/RichAI 拖到 agentComponent。");
            enabled = false; return;
        }
        agent = agentComponent as IAstarAI;
        if (agent == null) {
            Debug.LogError("[AIPauseOnMapOpen] 提供的组件不实现 IAstarAI。");
            enabled = false; return;
        }
        if (!mapIconController) {
            mapIconController = FindObjectOfType<MapIconController>();
        }
    }

    void OnEnable() {
        if (mapIconController != null) {
            mapIconController.onMapOpened.AddListener(PauseAgent);
            mapIconController.onMapClosed.AddListener(ResumeAgent);
        }
    }

    void OnDisable() {
        if (mapIconController != null) {
            mapIconController.onMapOpened.RemoveListener(PauseAgent);
            mapIconController.onMapClosed.RemoveListener(ResumeAgent);
        }
    }

    void PauseAgent() {
        agent.isStopped = true;   // 暂停移动（保留目标点）
        agent.canMove   = false;  // 可选：彻底禁走
        agent.canSearch = false;  // 可选：停止重新计算路径
    }

    void ResumeAgent() {
        agent.canSearch = true;
        agent.canMove   = true;
        agent.isStopped = false;
        // 可选：强制立即刷新一次路径
        agent.SearchPath();
    }
}