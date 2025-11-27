#if UNITY_EDITOR
using ITC.Core.GameFlow;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEngine;

public class SaveSystemSetup : MonoBehaviour
{
    [MenuItem("Tools/ITC/Setup Save System")]
    public static void SetupSaveSystem()
    {
        // 1. Setup Dialogue Manager / Save System
        var dialogueManager = FindObjectOfType<PixelCrushers.DialogueSystem.DialogueSystemController>();
        if (dialogueManager == null)
        {
            Debug.LogError("Dialogue Manager not found in scene!");
            return;
        }

        var saveSystem = dialogueManager.GetComponent<SaveSystem>();
        if (saveSystem == null)
        {
            saveSystem = dialogueManager.gameObject.AddComponent<SaveSystem>();
            Debug.Log("Added SaveSystem to Dialogue Manager.");
        }

        // Configure Save System (Basic defaults)
        // We can't easily set serialized fields via code without SerializedObject if we want to be robust, 
        // but adding the component is the main step.

        var dsSaver = dialogueManager.GetComponent<DialogueSystemSaver>();
        if (dsSaver == null)
        {
            dsSaver = dialogueManager.gameObject.AddComponent<DialogueSystemSaver>();
            Debug.Log("Added DialogueSystemSaver to Dialogue Manager.");
        }
        // Ensure unique key
        if (string.IsNullOrEmpty(dsSaver.key)) dsSaver.key = "DialogueSystem";

        var jsonDataSerializer = dialogueManager.GetComponent<JsonDataSerializer>();
        if (jsonDataSerializer == null)
        {
            jsonDataSerializer = dialogueManager.gameObject.AddComponent<JsonDataSerializer>();
            Debug.Log("Added JsonDataSerializer to Dialogue Manager.");
        }

        var playerPrefsSavedGameDataStorer = dialogueManager.GetComponent<PlayerPrefsSavedGameDataStorer>();
        if (playerPrefsSavedGameDataStorer == null)
        {
            playerPrefsSavedGameDataStorer = dialogueManager.gameObject.AddComponent<PlayerPrefsSavedGameDataStorer>();
            Debug.Log("Added PlayerPrefsSavedGameDataStorer to Dialogue Manager.");
        }

        // 2. Setup GameFlowManager
        var gameFlowManager = FindObjectOfType<GameFlowManager>();
        if (gameFlowManager != null)
        {
            var gfSaver = gameFlowManager.GetComponent<GameFlowSaver>();
            if (gfSaver == null)
            {
                gfSaver = gameFlowManager.gameObject.AddComponent<GameFlowSaver>();
                Debug.Log("Added GameFlowSaver to GameFlowManager.");
            }
            if (string.IsNullOrEmpty(gfSaver.key)) gfSaver.key = "GameFlow";
        }
        else
        {
            Debug.LogWarning("GameFlowManager not found in scene.");
        }

        // 3. Setup StageManager
        var stageManager = FindObjectOfType<StageManager>();
        if (stageManager != null)
        {
            var smSaver = stageManager.GetComponent<StageSystemSaver>();
            if (smSaver == null)
            {
                smSaver = stageManager.gameObject.AddComponent<StageSystemSaver>();
                Debug.Log("Added StageSystemSaver to StageManager.");
            }
            if (string.IsNullOrEmpty(smSaver.key)) smSaver.key = "StageSystem";
        }
        else
        {
            Debug.LogWarning("StageManager not found in scene.");
        }

        Debug.Log("Save System Setup Complete!");
    }
}
#endif
