using System;
using System.Collections.Generic;
using UnityEngine;
using Core;

namespace Progression
{
    /// <summary>
    /// Interface for the PlayerStats system.
    /// Tracks player statistics and performance metrics.
    /// </summary>
    public interface IPlayerStats : ISystem
    {
        // Properties
        int TotalInspections { get; }
        int SuccessfulInspections { get; }
        int PerfectInspections { get; }
        float TotalAccuracy { get; }
        float AverageAccuracy { get; }
        float TotalMoneyEarned { get; }
        float TotalPlayTimeSeconds { get; }
        TimeSpan TotalPlayTime { get; }
        int HighestLevelReached { get; }
        int TotalIssuesFound { get; }
        int TotalIssuesMissed { get; }

        // Events
        event Action<PlayerStatsData> OnStatsUpdated;
        event Action<int> OnMilestoneReached;

        // Methods
        void RecordInspection(float accuracy, int issuesFound, int totalIssues, float moneyEarned);
        void RecordMoneyEarned(float amount);
        void UpdatePlayTime(float deltaTime);
        void UpdateHighestLevel(int level);
        void ResetStats();
        PlayerStatsData GetStatsData();
        Dictionary<string, object> GetStatsForAchievements();
    }

    /// <summary>
    /// Tracks and manages player statistics.
    /// Records inspections, money, play time, and performance metrics.
    /// </summary>
    public class PlayerStats : MonoBehaviour, IPlayerStats
    {
        #region Serialized Fields

        [Header("Milestones")]
        [SerializeField] private int[] inspectionMilestones = { 10, 50, 100, 250, 500, 1000 };
        [SerializeField] private float[] moneyMilestones = { 1000f, 5000f, 10000f, 25000f, 50000f, 100000f };
        [SerializeField] private float[] accuracyMilestones = { 0.75f, 0.85f, 0.90f, 0.95f, 0.99f };

        #endregion

        #region Properties

        public int Priority => 11;

        // Inspection stats
        public int TotalInspections { get; private set; }
        public int SuccessfulInspections { get; private set; }
        public int PerfectInspections { get; private set; }
        public float TotalAccuracy { get; private set; }
        public float AverageAccuracy => TotalInspections > 0 ? TotalAccuracy / TotalInspections : 0f;

        // Economy stats
        public float TotalMoneyEarned { get; private set; }

        // Time stats
        public float TotalPlayTimeSeconds { get; private set; }
        public TimeSpan TotalPlayTime => TimeSpan.FromSeconds(TotalPlayTimeSeconds);

        // Progression stats
        public int HighestLevelReached { get; private set; }

        // Issue stats
        public int TotalIssuesFound { get; private set; }
        public int TotalIssuesMissed { get; private set; }

        #endregion

        #region Events

        public event Action<PlayerStatsData> OnStatsUpdated;
        public event Action<int> OnMilestoneReached;

        #endregion

        #region Private Fields

        private readonly HashSet<int> _reachedMilestones = new HashSet<int>();
        private float _sessionStartTime;

        #endregion

        #region ISystem Implementation

        public void OnRegistered()
        {
            Debug.Log("[PlayerStats] Registered with ServiceLocator");
        }

        public void Initialize()
        {
            _sessionStartTime = Time.time;
            LoadStats();
            Debug.Log($"[PlayerStats] Initialized. Total Inspections: {TotalInspections}, Play Time: {TotalPlayTime:hh\\:mm\\:ss}");
        }

        public void Shutdown()
        {
            // Record final session time
            float sessionTime = Time.time - _sessionStartTime;
            UpdatePlayTime(sessionTime);

            SaveStats();
            Debug.Log("[PlayerStats] Shutdown complete");
        }

        #endregion

        #region Stat Recording

        /// <summary>
        /// Records a completed inspection with all relevant metrics.
        /// </summary>
        public void RecordInspection(float accuracy, int issuesFound, int totalIssues, float moneyEarned)
        {
            TotalInspections++;

            // Track accuracy
            TotalAccuracy += accuracy;

            // Track success (accuracy >= 70%)
            if (accuracy >= 0.7f)
            {
                SuccessfulInspections++;
            }

            // Track perfect inspections (100% accuracy, all issues found)
            if (accuracy >= 1.0f && issuesFound == totalIssues)
            {
                PerfectInspections++;
            }

            // Track issues
            TotalIssuesFound += issuesFound;
            TotalIssuesMissed += (totalIssues - issuesFound);

            // Track money
            RecordMoneyEarned(moneyEarned);

            // Check for milestones
            CheckMilestones();

            // Fire update event
            OnStatsUpdated?.Invoke(GetStatsData());

            Debug.Log($"[PlayerStats] Recorded inspection #{TotalInspections}. Accuracy: {accuracy:P0}, Issues: {issuesFound}/{totalIssues}");
        }

        /// <summary>
        /// Records money earned from any source.
        /// </summary>
        public void RecordMoneyEarned(float amount)
        {
            if (amount <= 0) return;

            float previousTotal = TotalMoneyEarned;
            TotalMoneyEarned += amount;

            // Check money milestones
            foreach (float milestone in moneyMilestones)
            {
                if (previousTotal < milestone && TotalMoneyEarned >= milestone)
                {
                    int milestoneIndex = Array.IndexOf(moneyMilestones, milestone);
                    TriggerMilestone(milestoneIndex + 100); // Money milestones start at 100
                }
            }
        }

        /// <summary>
        /// Updates the total play time. Call this periodically or on shutdown.
        /// </summary>
        public void UpdatePlayTime(float deltaTime)
        {
            TotalPlayTimeSeconds += deltaTime;
        }

        /// <summary>
        /// Updates the highest level reached if the new level is higher.
        /// </summary>
        public void UpdateHighestLevel(int level)
        {
            if (level > HighestLevelReached)
            {
                HighestLevelReached = level;
                Debug.Log($"[PlayerStats] New highest level reached: {HighestLevelReached}");
            }
        }

        /// <summary>
        /// Resets all statistics to default values.
        /// </summary>
        public void ResetStats()
        {
            TotalInspections = 0;
            SuccessfulInspections = 0;
            PerfectInspections = 0;
            TotalAccuracy = 0f;
            TotalMoneyEarned = 0f;
            TotalPlayTimeSeconds = 0f;
            HighestLevelReached = 1;
            TotalIssuesFound = 0;
            TotalIssuesMissed = 0;
            _reachedMilestones.Clear();

            SaveStats();
            OnStatsUpdated?.Invoke(GetStatsData());

            Debug.Log("[PlayerStats] All stats reset");
        }

        #endregion

        #region Data Access

        /// <summary>
        /// Gets a snapshot of current stats.
        /// </summary>
        public PlayerStatsData GetStatsData()
        {
            return new PlayerStatsData
            {
                totalInspections = TotalInspections,
                successfulInspections = SuccessfulInspections,
                perfectInspections = PerfectInspections,
                averageAccuracy = AverageAccuracy,
                totalMoneyEarned = TotalMoneyEarned,
                totalPlayTimeSeconds = TotalPlayTimeSeconds,
                highestLevelReached = HighestLevelReached,
                totalIssuesFound = TotalIssuesFound,
                totalIssuesMissed = TotalIssuesMissed
            };
        }

        /// <summary>
        /// Gets stats in a dictionary format for achievement checking.
        /// </summary>
        public Dictionary<string, object> GetStatsForAchievements()
        {
            return new Dictionary<string, object>
            {
                { "totalInspections", TotalInspections },
                { "successfulInspections", SuccessfulInspections },
                { "perfectInspections", PerfectInspections },
                { "averageAccuracy", AverageAccuracy },
                { "totalMoneyEarned", TotalMoneyEarned },
                { "totalPlayTimeSeconds", TotalPlayTimeSeconds },
                { "highestLevelReached", HighestLevelReached },
                { "totalIssuesFound", TotalIssuesFound },
                { "totalIssuesMissed", TotalIssuesMissed }
            };
        }

        #endregion

        #region Milestones

        private void CheckMilestones()
        {
            // Check inspection milestones
            foreach (int milestone in inspectionMilestones)
            {
                if (TotalInspections >= milestone && !_reachedMilestones.Contains(milestone))
                {
                    _reachedMilestones.Add(milestone);
                    int milestoneIndex = Array.IndexOf(inspectionMilestones, milestone);
                    TriggerMilestone(milestoneIndex);
                }
            }

            // Check accuracy milestones (based on having at least 10 inspections)
            if (TotalInspections >= 10)
            {
                foreach (float milestone in accuracyMilestones)
                {
                    int milestoneKey = Mathf.RoundToInt(milestone * 1000);
                    if (AverageAccuracy >= milestone && !_reachedMilestones.Contains(milestoneKey + 200))
                    {
                        _reachedMilestones.Add(milestoneKey + 200); // Accuracy milestones start at 200
                        int milestoneIndex = Array.IndexOf(accuracyMilestones, milestone);
                        TriggerMilestone(milestoneIndex + 200);
                    }
                }
            }
        }

        private void TriggerMilestone(int milestoneId)
        {
            Debug.Log($"[PlayerStats] Milestone reached: {milestoneId}");
            OnMilestoneReached?.Invoke(milestoneId);
            EventManager.TriggerEvent($"Milestone_{milestoneId}");
        }

        #endregion

        #region Save/Load

        private const string SAVE_KEY = "PlayerStats_Data";

        private void SaveStats()
        {
            var data = GetStatsData();
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadStats()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                var data = JsonUtility.FromJson<PlayerStatsData>(json);

                TotalInspections = data.totalInspections;
                SuccessfulInspections = data.successfulInspections;
                PerfectInspections = data.perfectInspections;
                TotalAccuracy = data.averageAccuracy * TotalInspections; // Reconstruct total from average
                TotalMoneyEarned = data.totalMoneyEarned;
                TotalPlayTimeSeconds = data.totalPlayTimeSeconds;
                HighestLevelReached = data.highestLevelReached;
                TotalIssuesFound = data.totalIssuesFound;
                TotalIssuesMissed = data.totalIssuesMissed;
            }
            else
            {
                // Set defaults
                HighestLevelReached = 1;
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Log Current Stats")]
        private void DebugLogStats()
        {
            Debug.Log($"[PlayerStats] " +
                $"Inspections: {TotalInspections} " +
                $"(Perfect: {PerfectInspections}, Success: {SuccessfulInspections})\n" +
                $"Average Accuracy: {AverageAccuracy:P2}\n" +
                $"Money Earned: ${TotalMoneyEarned:N0}\n" +
                $"Play Time: {TotalPlayTime:hh\\:mm\\:ss}\n" +
                $"Highest Level: {HighestLevelReached}\n" +
                $"Issues Found/Missed: {TotalIssuesFound}/{TotalIssuesMissed}");
        }

        [ContextMenu("Simulate Inspection")]
        private void DebugSimulateInspection()
        {
            RecordInspection(0.85f, 3, 4, 150f);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Serializable container for player statistics.
    /// </summary>
    [Serializable]
    public struct PlayerStatsData
    {
        public int totalInspections;
        public int successfulInspections;
        public int perfectInspections;
        public float averageAccuracy;
        public float totalMoneyEarned;
        public float totalPlayTimeSeconds;
        public int highestLevelReached;
        public int totalIssuesFound;
        public int totalIssuesMissed;
    }

    #endregion
}
