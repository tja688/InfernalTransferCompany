using System;
using PixelCrushers;
using UnityEngine;

namespace ITC.Core.GameFlow
{
    public class GameFlowSaver : Saver
    {
        [Serializable]
        public class GameFlowData
        {
            public GameState State;
            public int Day;
            public bool IsFinished;
        }

        public override string RecordData()
        {
            if (GameFlowManager.Instance == null) return string.Empty;

            var data = new GameFlowData
            {
                State = GameFlowManager.Instance.CurrentState,
                Day = GameFlowManager.Instance.CurrentDay,
                IsFinished = GameFlowManager.Instance.IsGameFinished
            };

            return SaveSystem.Serialize(data);
        }

        public override void ApplyData(string s)
        {
            if (GameFlowManager.Instance == null || string.IsNullOrEmpty(s)) return;

            var data = SaveSystem.Deserialize<GameFlowData>(s);
            if (data == null) return;

            GameFlowManager.Instance.SetStateData(data.State, data.Day, data.IsFinished);
        }
    }
}
