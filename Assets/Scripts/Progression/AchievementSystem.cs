using System;
using System.Collections.Generic;
using UnityEngine;
using Core;
using PlayerScripts;

namespace Progression
{
    /// <summary>
    /// Interface for the AchievementSystem.
    /// Manages achievement unlocking, tracking, and rewards.
    /// </summary>
    public interface IAchievementSystem : ISystem
    {
        int TotalAchievements { get; }
        int UnlockedCount { get; }
        float CompletionPercentage { get; }

        event Action<Achievement> OnAchievementUnlocked;
        event Action<int, int> OnAchievementProgressChanged; // (unlocked, total)

        bool IsUnlocked(string achievementId);
        bool IsUnlocked(Achievement achievement);
        Achievement GetAchievement(string achievementId);
        IReadOnlyList<Achievement> GetAllAchievements();
        IReadOnlyList<Achievement> GetUnlockedAchievements();
        IReadOnlyList<Achievement> GetLockedAchievements();
        IReadOnlyList<Achievement> GetAchievementsByType(AchievementType type);
        void CheckAllAchievements();
        void ResetAchievements();
        UnlockedAchievementData[] GetSaveData();
        void LoadSaveData(UnlockedAchievementData[] data);
    }

    /// <summary>
    /// Manages the achievement system including tracking, unlocking, and rewards.
    /// Registers with ServiceLocator as IAchievementSystem.
    /// </summary>
    public class AchievementSystem : MonoBehaviour, IAchievementSystem
    {
        #region Serialized Fields

        [Header("Achievement Database")]
        [SerializeField] private Achievement[] _achievements;

        [Header("Settings")]
        [SerializeField] private bool _showUnlockNotifications = true;
        [SerializeField] private float _checkInterval = 1f;

        #endregion

        #region Properties

        public int Priority => 12;

        public int TotalAchievements => _achievements?.Length ?? 0;
        public int UnlockedCount => _unlockedAchievements.Count;
        public float CompletionPercentage => TotalAchievements > 0
            ? (float)UnlockedCount / TotalAchievements * 100f
            : 0f;

        #endregion

        #region Events

        public event Action<Achievement> OnAchievementUnlocked;
        public event Action<int, int> OnAchievementProgressChanged;

        #endregion

        #region Private Fields

        private readonly HashSet<string> _unlockedAchievements = new HashSet<string>();
        private readonly Dictionary<string, Achievement> _achievementLookup = new Dictionary<string, Achievement>();
        private readonly List<Achievement> _unlockedCache = new List<Achievement>();
        private float _lastCheckTime;
        #endregion

        #region ISystem Implementation

        public void OnRegistered()
        {
            BuildLookupCache();
            ValidateAchievements();
            Debug.Log("[AchievementSystem] Registered with ServiceLocator");
        }

        public void Initialize()
        {
            LoadAchievements();
            SubscribeToEvents();
            Debug.Log($"[AchievementSystem] Initialized. Unlocked: {UnlockedCount}/{TotalAchievements}");
        }

        public void Shutdown()
        {
            UnsubscribeFromEvents();
            SaveAchievements();
            Debug.Log("[AchievementSystem] Shutdown complete");
        }

        #endregion

        #region Lookup & Validation

        private void BuildLookupCache()
        {
            _achievementLookup.Clear();

            if (_achievements != null)
            {
                foreach (var achievement in _achievements)
                {
                    if (achievement != null && !string.IsNullOrEmpty(achievement.Id))
                    {
                        _achievementLookup[achievement.Id] = achievement;
                    }
                }
            }
        }

        private void ValidateAchievements()
        {
            if (_achievements == null || _achievements.Length == 0)
            {
                Debug.LogWarning("[AchievementSystem] No achievements configured!");
                return;
            }

            HashSet<string> ids = new HashSet<string>();
            foreach (var achievement in _achievements)
            {
                if (achievement == null) continue;

                if (ids.Contains(achievement.Id))
                {
                    Debug.LogError($"[AchievementSystem] Duplicate achievement ID: {achievement.Id}");
                }
                else
                {
                    ids.Add(achievement.Id);
                    achievement.Validate();
                }
            }
        }

        #endregion

        #region Achievement Access

        /// <summary>
        /// Checks if an achievement is unlocked by ID.
        /// </summary>
        public bool IsUnlocked(string achievementId)
        {
            return !string.IsNullOrEmpty(achievementId) && _unlockedAchievements.Contains(achievementId);
        }

        /// <summary>
        /// Checks if an achievement is unlocked.
        /// </summary>
        public bool IsUnlocked(Achievement achievement)
        {
            return achievement != null && IsUnlocked(achievement.Id);
        }

        /// <summary>
        /// Gets an achievement by ID.
        /// </summary>
        public Achievement GetAchievement(string achievementId)
        {
            return _achievementLookup.TryGetValue(achievementId, out Achievement achievement)
                ? achievement
                : null;
        }

        /// <summary>
        /// Gets all achievements.
        /// </summary>
        public IReadOnlyList<Achievement> GetAllAchievements()
        {
            return _achievements;
        }

        /// <summary>
        /// Gets all unlocked achievements.
        /// </summary>
        public IReadOnlyList<Achievement> GetUnlockedAchievements()
        {
            _unlockedCache.Clear();
            foreach (var kvp in _achievementLookup)
            {
                if (_unlockedAchievements.Contains(kvp.Key))
                {
                    _unlockedCache.Add(kvp.Value);
                }
            }
            return _unlockedCache.AsReadOnly();
        }

        /// <summary>
        /// Gets all locked achievements (including hidden ones if showHidden is true).
        /// </summary>
        public IReadOnlyList<Achievement> GetLockedAchievements()
        {
            _unlockedCache.Clear();
            foreach (var kvp in _achievementLookup)
            {
                if (!_unlockedAchievements.Contains(kvp.Key))
                {
                    _unlockedCache.Add(kvp.Value);
                }
            }
            return _unlockedCache.AsReadOnly();
        }

        /// <summary>
        /// Gets achievements filtered by type.
        /// </summary>
        public IReadOnlyList<Achievement> GetAchievementsByType(AchievementType type)
        {
            _unlockedCache.Clear();
            if (_achievements != null)
            {
                foreach (var achievement in _achievements)
                {
                    if (achievement != null && achievement.Type == type)
                    {
                        _unlockedCache.Add(achievement);
                    }
                }
            }
            return _unlockedCache.AsReadOnly();
        }

        #endregion

        #region Achievement Checking

        /// <summary>
        /// Checks all achievements against current stats.
        /// </summary>
        public void CheckAllAchievements()
        {
            if (ServiceLocator.TryGet(out IPlayerStats playerStats))
            {
                var stats = playerStats.GetStatsForAchievements();
                CheckAchievementsAgainstStats(stats);
            }
        }

        private void CheckAchievementsAgainstStats(Dictionary<string, object> stats)
        {
            if (_achievements == null) return;

            int newlyUnlocked = 0;

            foreach (var achievement in _achievements)
            {
                if (achievement == null) continue;
                if (IsUnlocked(achievement)) continue;

                if (achievement.CheckCondition(stats, _unlockedAchievements))
                {
                    UnlockAchievement(achievement);
                    newlyUnlocked++;
                }
            }

            if (newlyUnlocked > 0)
            {
                OnAchievementProgressChanged?.Invoke(UnlockedCount, TotalAchievements);
            }
        }

        private void UnlockAchievement(Achievement achievement)
        {
            if (IsUnlocked(achievement)) return;

            _unlockedAchievements.Add(achievement.Id);
            Debug.Log($"[AchievementSystem] Achievement Unlocked: {achievement.DisplayName}!");

            // Award rewards
            AwardRewards(achievement);

            // Fire events
            OnAchievementUnlocked?.Invoke(achievement);
            EventManager.TriggerEvent("OnAchievementUnlocked");

            // Show notification if enabled
            if (_showUnlockNotifications)
            {
                ShowUnlockNotification(achievement);
            }
        }

        private void AwardRewards(Achievement achievement)
        {
            // Award XP
            if (achievement.XPReward > 0 && ServiceLocator.TryGet(out IProgressionManager progression))
            {
                progression.AddXP(achievement.XPReward, $"Achievement: {achievement.DisplayName}");
            }

            // Award money via PlayerDataManager singleton
            if (achievement.MoneyReward > 0 && PlayerDataManager.Instance != null)
            {
                var data = PlayerDataManager.Instance.playerData;
                if (data != null)
                {
                    data.money += achievement.MoneyReward;
                }
            }

            // Unlock item if specified
            if (!string.IsNullOrEmpty(achievement.UnlockItemId))
            {
                Debug.Log($"[AchievementSystem] Unlocked item: {achievement.UnlockItemId}");
                // Item unlocking would be handled by inventory system
            }
        }

        private void ShowUnlockNotification(Achievement achievement)
        {
            // This would integrate with a UI notification system
            Debug.Log($"[AchievementSystem] ACHIEVEMENT UNLOCKED: {achievement.DisplayName}\n{achievement.Description}");
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            // Subscribe to progression events
            if (ServiceLocator.TryGet(out IProgressionManager progression))
            {
                progression.OnLevelUp += OnLevelUp;
                progression.OnXPChanged += OnXPChanged;
            }

            // Subscribe to global events
            EventManager.StartListening("OnInspectionCompleted", OnInspectionCompleted);
        }

        private void UnsubscribeFromEvents()
        {
            if (ServiceLocator.TryGet(out IProgressionManager progression))
            {
                progression.OnLevelUp -= OnLevelUp;
                progression.OnXPChanged -= OnXPChanged;
            }

            EventManager.StopListening("OnInspectionCompleted", OnInspectionCompleted);
        }

        private void OnLevelUp(int newLevel)
        {
            // Update stats and check achievements
            if (ServiceLocator.TryGet(out IPlayerStats stats))
            {
                stats.UpdateHighestLevel(newLevel);
            }
            CheckAllAchievements();
        }

        private void OnXPChanged(int currentXP, int delta)
        {
            // Could check XP-based achievements here
            CheckAllAchievements();
        }

        private void OnInspectionCompleted()
        {
            CheckAllAchievements();
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // Periodic achievement checking
            if (Time.time - _lastCheckTime >= _checkInterval)
            {
                _lastCheckTime = Time.time;

                // Update play time stats
                if (ServiceLocator.TryGet(out IPlayerStats stats))
                {
                    stats.UpdatePlayTime(_checkInterval);
                }

                // Check achievements
                CheckAllAchievements();
            }
        }

        #endregion

        #region Save/Load

        private const string SAVE_KEY = "AchievementSystem_Unlocked";

        /// <summary>
        /// Gets the save data for persistence.
        /// </summary>
        public UnlockedAchievementData[] GetSaveData()
        {
            var data = new UnlockedAchievementData[_unlockedAchievements.Count];
            int index = 0;
            foreach (var id in _unlockedAchievements)
            {
                data[index++] = new UnlockedAchievementData(id);
            }
            return data;
        }

        /// <summary>
        /// Loads save data from persistence.
        /// </summary>
        public void LoadSaveData(UnlockedAchievementData[] data)
        {
            _unlockedAchievements.Clear();

            if (data != null)
            {
                foreach (var item in data)
                {
                    if (!string.IsNullOrEmpty(item.achievementId))
                    {
                        _unlockedAchievements.Add(item.achievementId);
                    }
                }
            }
        }

        private void SaveAchievements()
        {
            var data = GetSaveData();
            string json = JsonUtility.ToJson(new AchievementSaveWrapper { achievements = data });
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadAchievements()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                var wrapper = JsonUtility.FromJson<AchievementSaveWrapper>(json);
                LoadSaveData(wrapper.achievements);
            }
        }

        /// <summary>
        /// Resets all achievements.
        /// </summary>
        public void ResetAchievements()
        {
            _unlockedAchievements.Clear();
            PlayerPrefs.DeleteKey(SAVE_KEY);
            OnAchievementProgressChanged?.Invoke(0, TotalAchievements);
            Debug.Log("[AchievementSystem] All achievements reset");
        }

        #endregion

        #region Debug

        [ContextMenu("List All Achievements")]
        private void DebugListAchievements()
        {
            Debug.Log($"[AchievementSystem] Total: {TotalAchievements}, Unlocked: {UnlockedCount}");
            foreach (var achievement in _achievements)
            {
                if (achievement == null) continue;
                string status = IsUnlocked(achievement) ? "[UNLOCKED]" : "[LOCKED]";
                Debug.Log($"  {status} {achievement.DisplayName} ({achievement.Type})");
            }
        }

        [ContextMenu("Unlock Random Achievement")]
        private void DebugUnlockRandom()
        {
            var locked = GetLockedAchievements();
            if (locked.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, locked.Count);
                UnlockAchievement(locked[index]);
            }
            else
            {
                Debug.Log("[AchievementSystem] All achievements already unlocked!");
            }
        }

        [ContextMenu("Force Check Achievements")]
        private void DebugForceCheck()
        {
            CheckAllAchievements();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Wrapper for JSON serialization of achievement data.
    /// </summary>
    [Serializable]
    public class AchievementSaveWrapper
    {
        public UnlockedAchievementData[] achievements;
    }

    #endregion
}
