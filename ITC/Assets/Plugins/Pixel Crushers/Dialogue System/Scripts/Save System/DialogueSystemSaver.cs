// Copyright (c) Pixel Crushers. All rights reserved.

using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This is a saver that saves the Dialogue System's save data 
    /// to the Pixel Crushers Common Library Save System.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class DialogueSystemSaver : Saver
    {

        [Serializable]
        public class RawData
        {
            public byte[] bytes;
        }

        [Tooltip("如果数据在加载场景后已立即恢复，则在保存系统等待指定帧数让其他脚本初始化后，不要再次应用它。")]
        public bool skipApplyDataAfterFramesIfApplyImmediate = true;

        [Tooltip("使用原始数据转储保存。如果数据库极大，这种方法更快，但会生成更大的存档数据。如果使用此选项，请使用 BinaryDataSerializer 而不是 JsonDataSerializer，否则数据会大得离谱。")]
        public bool saveRawData = false;

        private bool m_appliedImmediate = false;

        public override void Reset()
        {
            base.Reset();
            saveAcrossSceneChanges = true;
            skipSaveWhenChangingScenes = true;
        }

        public override void Start()
        {
            base.Start();
            SaveSystem.loadStarted += OnLoadGameStarted;
        }

        public override void OnDestroy()
        {
            SaveSystem.loadStarted -= OnLoadGameStarted;
            base.OnDestroy();
        }

        private void OnLoadGameStarted()
        {
            DialogueManager.StopAllConversations();
        }

        public override string RecordData()
        {
            if (saveRawData)
            {
                var rawData = new RawData();
                rawData.bytes = PersistentDataManager.GetRawData();
                return SaveSystem.Serialize(rawData);
            }
            else
            {
                return PersistentDataManager.GetSaveData();
            }
        }

        public override void ApplyDataImmediate()
        {
            // Immediately restore Lua in case other scripts'
            // Start() methods need to read values from it.
            var data = SaveSystem.currentSavedGameData.GetData(key);
            if (string.IsNullOrEmpty(data)) return;
            if (saveRawData)
            {
                var rawData = SaveSystem.Deserialize<RawData>(data);
                if (rawData != null && rawData.bytes != null) PersistentDataManager.ApplyRawData(rawData.bytes);
            }
            else
            {
                PersistentDataManager.ApplyLuaInternal(data, false);
            }
            m_appliedImmediate = true;
        }

        public override void ApplyData(string data)
        {
            if (m_appliedImmediate)
            {
                m_appliedImmediate = false;
                if (skipApplyDataAfterFramesIfApplyImmediate)
                {
                    PersistentDataManager.Apply();
                    return;
                }
            }
            if (saveRawData)
            {
                var rawData = SaveSystem.Deserialize<RawData>(data);
                if (rawData != null && rawData.bytes != null) PersistentDataManager.ApplyRawData(rawData.bytes);
            }
            else
            {
                PersistentDataManager.ApplySaveData(data);
            }
        }

        public override void OnBeforeSceneChange()
        {
            PersistentDataManager.LevelWillBeUnloaded();
        }

        public override void OnRestartGame()
        {
            DialogueManager.StopAllConversations();
            DialogueManager.ResetDatabase();
            DialogueManager.SendUpdateTracker();
        }

    }

}
