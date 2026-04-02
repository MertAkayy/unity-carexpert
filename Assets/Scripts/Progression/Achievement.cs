using System;
using System.Collections.Generic;
using UnityEngine;

namespace Progression
{
    /// <summary>
    /// Enumeration of achievement types.
    /// </summary>
    public enum AchievementType
    {
        Inspection,
        Accuracy,
        Level,
        Money,
        Time,
        Issue,
        Special
    }

    /// <summary>
    /// Enumeration of achievement rarity tiers.
    /// </summary>
    public enum AchievementRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// ScriptableObject definition for an achievement.
    /// Create via Create > Progression > Achievement menu.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAchievement", menuName = "Progression/Achievement")]
    public class Achievement : ScriptableObject
    {
        #region Serialized Fields

        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Visuals")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private AchievementRarity _rarity = AchievementRarity.Common;

        [Header("Type & Condition")]
        [SerializeField] private AchievementType _type = AchievementType.Inspection;
        [SerializeField] private AchievementCondition _condition;

        [Header("Rewards")]
        [SerializeField] private int _xpReward = 50;
        [SerializeField] private int _moneyReward = 100;
        [SerializeField] private string _unlockItemId;

        [Header("Settings")]
        [SerializeField] private bool _isHidden = false;
        [SerializeField] private Achievement _prerequisite;

        #endregion

        #region Properties

        /// <summary>
        /// Unique identifier for this achievement.
        /// </summary>
        public string Id => _id;

        /// <summary>
        /// Display name shown to players.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Description of how to unlock this achievement.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Icon displayed in UI.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Type category for grouping.
        /// </summary>
        public AchievementType Type => _type;

        /// <summary>
        /// Rarity tier for visual styling.
        /// </summary>
        public AchievementRarity Rarity => _rarity;

        /// <summary>
        /// Condition required to unlock.
        /// </summary>
        public AchievementCondition Condition => _condition;

        /// <summary>
        /// XP awarded on unlock.
        /// </summary>
        public int XPReward => _xpReward;

        /// <summary>
        /// Money awarded on unlock.
        /// </summary>
        public int MoneyReward => _moneyReward;

        /// <summary>
        /// Item unlocked (if any).
        /// </summary>
        public string UnlockItemId => _unlockItemId;

        /// <summary>
        /// Whether this achievement is hidden until unlocked.
        /// </summary>
        public bool IsHidden => _isHidden;

        /// <summary>
        /// Prerequisite achievement that must be unlocked first.
        /// </summary>
        public Achievement Prerequisite => _prerequisite;

        #endregion

        #region Methods

        /// <summary>
        /// Checks if this achievement can be unlocked with the given stats.
        /// </summary>
        public bool CheckCondition(Dictionary<string, object> stats, HashSet<string> unlockedAchievements)
        {
            // Check prerequisite
            if (_prerequisite != null && !unlockedAchievements.Contains(_prerequisite.Id))
            {
                return false;
            }

            return _condition.Check(stats);
        }

        /// <summary>
        /// Validates the achievement configuration.
        /// </summary>
        public bool Validate()
        {
            bool isValid = true;

            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogError($"[Achievement] Achievement '{name}' has no ID assigned!");
                isValid = false;
            }

            if (string.IsNullOrEmpty(_displayName))
            {
                Debug.LogWarning($"[Achievement] Achievement '{_id}' has no display name.");
            }

            if (_xpReward < 0)
            {
                Debug.LogWarning($"[Achievement] Achievement '{_id}' has negative XP reward.");
            }

            return isValid;
        }

        private void OnValidate()
        {
            // Auto-generate ID from name if empty
            if (string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(name))
            {
                _id = name.Replace(" ", "_").ToLowerInvariant();
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines a condition for unlocking an achievement.
    /// </summary>
    [Serializable]
    public struct AchievementCondition
    {
        [SerializeField] private string _statName;
        [SerializeField] private ComparisonType _comparison;
        [SerializeField] private float _targetValue;
        [SerializeField] private float _secondaryValue;

        /// <summary>
        /// The name of the stat to check.
        /// </summary>
        public string StatName => _statName;

        /// <summary>
        /// How to compare the stat value.
        /// </summary>
        public ComparisonType Comparison => _comparison;

        /// <summary>
        /// The target value to compare against.
        /// </summary>
        public float TargetValue => _targetValue;

        /// <summary>
        /// Secondary value for range comparisons.
        /// </summary>
        public float SecondaryValue => _secondaryValue;

        /// <summary>
        /// Checks if the condition is met with the given stats.
        /// </summary>
        public bool Check(Dictionary<string, object> stats)
        {
            if (string.IsNullOrEmpty(_statName) || !stats.TryGetValue(_statName, out object value))
            {
                return false;
            }

            float statValue = Convert.ToSingle(value);

            return _comparison switch
            {
                ComparisonType.Equals => Mathf.Approximately(statValue, _targetValue),
                ComparisonType.NotEquals => !Mathf.Approximately(statValue, _targetValue),
                ComparisonType.GreaterThan => statValue > _targetValue,
                ComparisonType.GreaterThanOrEqual => statValue >= _targetValue,
                ComparisonType.LessThan => statValue < _targetValue,
                ComparisonType.LessThanOrEqual => statValue <= _targetValue,
                ComparisonType.Range => statValue >= _targetValue && statValue <= _secondaryValue,
                _ => false
            };
        }
    }

    /// <summary>
    /// Types of comparisons for achievement conditions.
    /// </summary>
    public enum ComparisonType
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Range
    }

    /// <summary>
    /// Runtime data for an unlocked achievement.
    /// </summary>
    [Serializable]
    public struct UnlockedAchievementData
    {
        public string achievementId;
        public System.DateTime unlockedAt;

        public UnlockedAchievementData(string id)
        {
            achievementId = id;
            unlockedAt = System.DateTime.Now;
        }
    }
}
