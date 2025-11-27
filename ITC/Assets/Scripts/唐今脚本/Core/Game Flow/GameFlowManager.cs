using System;
using UnityEngine;

namespace ITC.Core.GameFlow
{
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        [Header("Debug Info")]
        [SerializeField] private GameState currentState = GameState.Prologue;
        [SerializeField] private int currentDay = 1;
        [SerializeField] private bool isGameFinished = false;

        public GameState CurrentState => currentState;
        public int CurrentDay => currentDay;
        public bool IsGameFinished => isGameFinished;

        // Event triggered when state changes
        public event Action<GameState> OnStateChanged;
        // Event triggered when day changes (optional, but good for UI)
        public event Action<int> OnDayChanged;

        private const int MaxDays = 13;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Initialize state
            NotifyStateChange(currentState);
        }

        /// <summary>
        /// Calculates and advances to the next logical state.
        /// </summary>
        public void RequestAdvanceState()
        {
            if (isGameFinished) return;

            GameState nextState = currentState;

            switch (currentState)
            {
                case GameState.Prologue:
                    nextState = GameState.PreWork;
                    currentDay = 1;
                    OnDayChanged?.Invoke(currentDay);
                    break;

                case GameState.PreWork:
                    nextState = GameState.Work;
                    break;

                case GameState.Work:
                    nextState = GameState.PostWork;
                    break;

                case GameState.PostWork:
                    // Check if we should loop to next day or end game
                    if (currentDay >= MaxDays)
                    {
                        nextState = GameState.Ending;
                        isGameFinished = true;
                    }
                    else
                    {
                        nextState = GameState.PreWork;
                        currentDay++;
                        OnDayChanged?.Invoke(currentDay);
                    }
                    break;

                case GameState.Ending:
                    // Already at ending
                    break;
            }

            if (nextState != currentState)
            {
                ChangeState(nextState);
            }
        }

        /// <summary>
        /// Forcefully sets the state to a specific target.
        /// </summary>
        /// <param name="targetState">The state to transition to.</param>
        public void RequestSetState(GameState targetState)
        {
            if (currentState != targetState)
            {
                ChangeState(targetState);
            }
        }

        private void ChangeState(GameState newState)
        {
            currentState = newState;
            Debug.Log($"[GameFlowManager] State Changed: {currentState} (Day: {currentDay})");
            NotifyStateChange(currentState);
        }

        private void NotifyStateChange(GameState state)
        {
            OnStateChanged?.Invoke(state);
        }

        /// <summary>
        /// Restores the game flow state from saved data.
        /// </summary>
        /// <param name="state">The saved game state.</param>
        /// <param name="day">The saved day.</param>
        /// <param name="finished">Whether the game is finished.</param>
        public void SetStateData(GameState state, int day, bool finished)
        {
            currentState = state;
            currentDay = day;
            isGameFinished = finished;

            // Notify listeners of the restored state
            NotifyStateChange(currentState);
            OnDayChanged?.Invoke(currentDay);

            Debug.Log($"[GameFlowManager] State Restored: {currentState}, Day: {currentDay}, Finished: {isGameFinished}");
        }
    }
}
