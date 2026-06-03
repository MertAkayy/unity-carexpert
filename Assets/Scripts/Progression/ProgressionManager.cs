using System;
using System.Collections.Generic;
using UnityEngine;
using Core;
using PlayerScripts;

namespace Progression
{
    /// <summary>
    /// Interface for the ProgressionManager system.
    /// Handles XP calculation, level management, and unlocks.
    /// </summary>
    public interface IProgressionManager : ISystem
    {
        int CurrentLevel { get; }
        int CurrentXP { get; }
        int XPToNextLevel { get; }
        float LevelProgress { get; }

        event Action<int> OnLevelUp;
        event Action<int, int> OnXPChanged;

        void AddXP(int amount, string reason);
        void CalculateAndAwardInspectionXP(float accuracy, int issuesFound, int totalIssues);
        int GetXPThresholdForLevel(int level);
        bool IsToolUnlocked(string toolId);
        bool IsIssueTypeUnlocked(string issueType);
        IReadOnlyList<string> GetUnlockedTools();
        IReadOnlyList<string> GetUnlockedIssueTypes();
    }

    /// <summary>
    /// Manages player progression including XP, leveling, and unlocks.
    /// Registers with ServiceLocator as IProgressionManager.
    /// </summary>
    public class ProgressionManager : MonoBehaviour, IProgressionManager
    {
        #region Serialized Fields

        [Header("XP Settings")]
        [SerializeField] private int baseXPPerInspection = 50;
        [SerializeField] private float accuracyMultiplier = 100f;
        [SerializeField] private float levelBonusMultiplier = 10f;
        [SerializeField] private int baseXPThreshold = 100;
        [SerializeField] private float thresholdGrowthRate = 1.5f;

        [Header("Level Unlocks")]
        [SerializeField] private LevelUnlockData[] levelUnlocks;

        #endregion

        #region Properties

        public int Priority => 10;

        public int CurrentLevel => PlayerDataManager.Instance.playerData.level;
        public int CurrentXP => PlayerDataManager.Instance.playerData.experience;
        public int XPToNextLevel => GetXPThresholdForLevel(CurrentLevel + 1) - GetXPThresholdForLevel(CurrentLevel);
        public float LevelProgress => (float)(CurrentXP - GetXPThresholdForLevel(CurrentLevel)) / XPToNextLevel;

        private void Awake()
        {
            ServiceLocator.Register<IProgressionManager>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IProgressionManager>();
        }

        #endregion

        #region Events

        public event Action<int> OnLevelUp;
        public event Action<int, int> OnXPChanged; // (currentXP, delta)

        #endregion

        #region Private Fields

        private readonly List<string> _unlockedTools = new List<string>();
        private readonly List<string> _unlockedIssueTypes = new List<string>();
        private readonly Dictionary<int, LevelUnlockData> _unlockCache = new Dictionary<int, LevelUnlockData>();

        #endregion

        #region ISystem Implementation

        public void OnRegistered()
        {
            BuildUnlockCache();
            ApplyInitialUnlocks();
            Debug.Log("[ProgressionManager] Registered with ServiceLocator");
        }

        public void Initialize()
        {
            Debug.Log($"[ProgressionManager] Initialized. Level: {CurrentLevel}, XP: {CurrentXP}");
        }

        public void Shutdown()
        {
            Debug.Log("[ProgressionManager] Shutdown complete");
        }

        #endregion

        #region XP Management

        /// <summary>
        /// Adds XP to the player with a reason for logging/debugging.
        /// </summary>
        public void AddXP(int amount, string reason)
        {
            if (amount <= 0) return;

            PlayerDataManager.Instance.playerData.experience += amount;

            Debug.Log($"[ProgressionManager] Gained {amount} XP for: {reason}. Total: {CurrentXP}");

            CheckForLevelUp();
            OnXPChanged?.Invoke(CurrentXP, amount);
        }

        /// <summary>
        /// Calculates and awards XP based on inspection performance.
        /// Formula: Base XP + (Accuracy * 100) + (Level * 10)
        /// </summary>
        public void CalculateAndAwardInspectionXP(float accuracy, int issuesFound, int totalIssues)
        {
            // Base XP for completing inspection
            int baseXP = baseXPPerInspection;

            // Accuracy bonus (0-100 based on 0-1 accuracy)
            int accuracyBonus = Mathf.RoundToInt(accuracy * accuracyMultiplier);

            // Level bonus (current level * multiplier)
            int levelBonus = Mathf.RoundToInt(CurrentLevel * levelBonusMultiplier);

            // Perfect inspection bonus
            int perfectBonus = 0;
            if (accuracy >= 1.0f && issuesFound == totalIssues)
            {
                perfectBonus = 25; // Bonus for perfect inspection
            }

            int totalXP = baseXP + accuracyBonus + levelBonus + perfectBonus;

            string reason = $"Inspection (Accuracy: {accuracy:P0}, Issues: {issuesFound}/{totalIssues})";
            AddXP(totalXP, reason);
        }

        /// <summary>
        /// Gets the total XP required to reach a specific level.
        /// Uses a linear formula so progression feels consistent.
        /// XP per level = 460 + 15 * (level - 1)
        /// Cumulative: threshold(L) = (L-1) * (460 + 15*(L-2)/2)
        ///
        /// Cars needed per level (XP per car: perfect=115, good=100, avg=63, bad=44):
        /// Level 1->2:   460 XP → 4 perfect,  5 good,  8 avg, 11 bad
        /// Level 10->11: 595 XP → 6 perfect,  6 good, 10 avg, 14 bad
        /// Level 20->21: 745 XP → 7 perfect,  8 good, 12 avg, 17 bad
        /// Level 33->34: 940 XP → 9 perfect, 10 good, 15 avg, 22 bad
        /// </summary>
        public int GetXPThresholdForLevel(int level)
        {
            if (level <= 1) return 0;
            // Linear growth: each level costs 460 + 15*(level-2) XP
            // Sum formula: total = (L-1) * 460 + 15 * (L-1)*(L-2)/2
            const int basePerLevel = 460;
            const int growthPerLevel = 15;
            int n = level - 1;
            return n * basePerLevel + growthPerLevel * n * (n - 1) / 2;
        }

        private void CheckForLevelUp()
        {
            int newLevel = CurrentLevel;

            while (CurrentXP >= GetXPThresholdForLevel(newLevel + 1))
                newLevel++;

            if (newLevel > CurrentLevel)
            {
                int oldLevel = CurrentLevel;
                PlayerDataManager.Instance.playerData.level = newLevel;
                PlayerDataManager.Instance.playerData.UpdateHighestLevel(newLevel);

                for (int lvl = oldLevel + 1; lvl <= newLevel; lvl++)
                    ApplyLevelUnlocks(lvl);

                Debug.Log($"[ProgressionManager] LEVEL UP! {oldLevel} -> {CurrentLevel}");
                OnLevelUp?.Invoke(CurrentLevel);
                EventManager.TriggerEvent("OnPlayerLevelUp");
            }
        }

        #endregion

        #region Unlock System

        /// <summary>
        /// Checks if a tool is unlocked for the current level.
        /// </summary>
        public bool IsToolUnlocked(string toolId)
        {
            return _unlockedTools.Contains(toolId);
        }

        /// <summary>
        /// Checks if an issue type is unlocked for the current level.
        /// </summary>
        public bool IsIssueTypeUnlocked(string issueType)
        {
            return _unlockedIssueTypes.Contains(issueType);
        }

        /// <summary>
        /// Gets all unlocked tools.
        /// </summary>
        public IReadOnlyList<string> GetUnlockedTools()
        {
            return _unlockedTools.AsReadOnly();
        }

        /// <summary>
        /// Gets all unlocked issue types.
        /// </summary>
        public IReadOnlyList<string> GetUnlockedIssueTypes()
        {
            return _unlockedIssueTypes.AsReadOnly();
        }

        private void BuildUnlockCache()
        {
            _unlockCache.Clear();

            if (levelUnlocks != null)
            {
                foreach (var unlock in levelUnlocks)
                {
                    _unlockCache[unlock.level] = unlock;
                }
            }
        }

        private void ApplyInitialUnlocks()
        {
            // Apply all unlocks for current level and below
            for (int lvl = 1; lvl <= CurrentLevel; lvl++)
            {
                ApplyLevelUnlocks(lvl);
            }
        }

        private void ApplyLevelUnlocks(int level)
        {
            if (!_unlockCache.TryGetValue(level, out LevelUnlockData unlock))
                return;

            // Unlock tools
            if (unlock.unlockedTools != null)
            {
                foreach (string tool in unlock.unlockedTools)
                {
                    if (!_unlockedTools.Contains(tool))
                    {
                        _unlockedTools.Add(tool);
                        Debug.Log($"[ProgressionManager] Unlocked tool: {tool} at level {level}");
                    }
                }
            }

            // Unlock issue types
            if (unlock.unlockedIssueTypes != null)
            {
                foreach (string issueType in unlock.unlockedIssueTypes)
                {
                    if (!_unlockedIssueTypes.Contains(issueType))
                    {
                        _unlockedIssueTypes.Add(issueType);
                        Debug.Log($"[ProgressionManager] Unlocked issue type: {issueType} at level {level}");
                    }
                }
            }
        }

        #endregion


        #region Debug

        [ContextMenu("Add Test XP (100)")]
        private void DebugAddXP()
        {
            AddXP(100, "Debug Test");
        }

        [ContextMenu("Force Level Up")]
        private void DebugForceLevelUp()
        {
            int xpNeeded = GetXPThresholdForLevel(CurrentLevel + 1);
            int delta = xpNeeded - CurrentXP;
            if (delta > 0)
            {
                AddXP(delta, "Debug Force Level Up");
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Defines what unlocks at a specific level.
    /// </summary>
    [Serializable]
    public struct LevelUnlockData
    {
        public int level;
        public string[] unlockedTools;
        public string[] unlockedIssueTypes;
    }

    #endregion
}
