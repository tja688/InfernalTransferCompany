// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Settings used by DialogueSystemController to set up the PersistentDataManager.
    /// </summary>
    [System.Serializable]
    public class PersistentDataSettings
    {
        [Tooltip("- All Game Objects：向场景中所有 GameObject 上的所有脚本发送通知，以便在支持时记录和/或应用其持久化数据。\n- Only Registered Game Objects：仅向显式注册的 GameObject 发送通知。\n- No Game Objects：不向场景中的任何 GameObject 发送通知。")]
        public PersistentDataManager.RecordPersistentDataOn recordPersistentDataOn = PersistentDataManager.RecordPersistentDataOn.AllGameObjects;

        [Tooltip("勾选以在存档数据中包含 Actor[] 表。")]
        public bool includeActorData = true;

        [Tooltip("勾选以包含所有 Item[] 和 Quest[] 字段。若未勾选，则只记录任务状态和任务跟踪状态以减小体积。")]
        public bool includeAllItemData = false;

        [Tooltip("勾选以包含 Location[] 表。")]
        public bool includeLocationData = false;

        [Tooltip("勾选以在存档数据中包含状态和关系表。")]
        public bool includeStatusAndRelationshipData = true;

        [Tooltip("勾选以包含所有对话字段。")]
        public bool includeAllConversationFields = false;

        [Tooltip("用于保存对话 SimStatus 信息的可选字段（例如 Title）。如果为空，则使用对话 ID。")]
        public string saveConversationSimStatusWithField = string.Empty;

        [Tooltip("用于保存对话条目 SimStatus 信息的可选字段（例如 Title）。如果为空，则使用条目 ID。")]
        public string saveDialogueEntrySimStatusWithField = string.Empty;

        [Tooltip("每帧向多少个场景 GameObject 发送 OnRecordPersistentData。")]
        public int asyncGameObjectBatchSize = 1000;

        [Tooltip("每帧记录多少个对话条目的 SimStatus 值；仅在保存 SimStatus 时使用。")]
        public int asyncDialogueEntryBatchSize = 100;

        [Tooltip("初始化在存档之后添加到数据库中的变量和任务。")]
        public bool initializeNewVariables = true;
    }

}
