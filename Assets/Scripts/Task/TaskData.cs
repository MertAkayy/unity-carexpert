using System;
using UnityEngine;

namespace Task
{
    /// <summary>
    /// Types of tasks available in the game.
    /// </summary>
    public enum TaskType
    {
        Daily,
        Weekly,
        Special
    }

    /// <summary>
    /// Types of task requirements/conditions.
    /// </summary>
    public enum TaskRequirementType
    {
        Inspections,        // Complete X inspections
        Accuracy,           // Achieve X% average accuracy
        Money,              // Earn $X
        Time,               // Complete in under X minutes
        PerfectInspections, // Get X perfect inspections
        IssuesFound,        // Find X total issues
        SpecificTool,       // Use a specific tool X times
        VehicleType         // Inspect X vehicles of a specific type
    }

    /// <summary>
    /// Types of rewards that can be granted for completing tasks.
    /// </summary>
    [Serializable]
    public struct TaskReward
    {
        public int xpAmount;
        public int moneyAmount;
        public string[] itemIds;

        public bool HasRewards => xpAmount > 0 || moneyAmount > 0 || (itemIds != null && itemIds.Length > 0);

        public static TaskReward Empty => new TaskReward { xpAmount = 0, moneyAmount = 0, itemIds = null };
    }

    /// <summary>
    /// Defines a single requirement for a task.
    /// </summary>
    [Serializable]
    public struct TaskRequirement
    {
        public TaskRequirementType requirementType;
        public int targetValue;
        [Tooltip("For percentage-based requirements (e.g., accuracy), stored as 0-100")]
        public float floatValue;

        public bool IsCompleted(int currentValue, float currentFloatValue = 0f)
        {
            switch (requirementType)
            {
                case TaskRequirementType.Accuracy:
                    return currentFloatValue >= floatValue;
                case TaskRequirementType.Time:
                    return currentValue <= targetValue; // Time tasks: complete faster
                default:
                    return currentValue >= targetValue;
            }
        }

        public float GetProgress(int currentValue, float currentFloatValue = 0f)
        {
            switch (requirementType)
            {
                case TaskRequirementType.Accuracy:
                    return floatValue > 0 ? Mathf.Clamp01(currentFloatValue / floatValue) : 0f;
                case TaskRequirementType.Time:
                    return currentValue > 0 ? 1f : 0f; // Binary for time
                default:
                    return targetValue > 0 ? Mathf.Clamp01((float)currentValue / targetValue) : 0f;
            }
        }
    }

    /// <summary>
    /// Unlock conditions for tasks.
    /// </summary>
    [Serializable]
    public struct TaskUnlockCondition
    {
        [Tooltip("Minimum player level required")]
        public int minLevel;
        [Tooltip("Maximum player level (0 = no limit)")]
        public int maxLevel;
        [Tooltip("Required task IDs that must be completed first")]
        public string[] prerequisiteTaskIds;
        [Tooltip("Total inspections required before this task unlocks")]
        public int minTotalInspections;

        public bool IsUnlocked(int playerLevel, int totalInspections, System.Collections.Generic.HashSet<string> completedTaskIds)
        {
            // Check level requirements
            if (playerLevel < minLevel) return false;
            if (maxLevel > 0 && playerLevel > maxLevel) return false;

            // Check inspection requirement
            if (totalInspections < minTotalInspections) return false;

            // Check prerequisite tasks
            if (prerequisiteTaskIds != null)
            {
                foreach (string taskId in prerequisiteTaskIds)
                {
                    if (!completedTaskIds.Contains(taskId)) return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// ScriptableObject definition for a task.
    /// Create via Create > Game > Task Data menu.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTask", menuName = "Game/Task Data", order = 1)]
    public class TaskData : ScriptableObject
    {
        #region Task Identity

        [Header("Task Identity")]
        [Tooltip("Unique identifier for this task")]
        public string taskId;
        [Tooltip("Display name shown to player")]
        public string taskName;
        [Tooltip("Detailed description of the task")]
        [TextArea(2, 5)]
        public string description;
        [Tooltip("Type determines reset behavior and UI placement")]
        public TaskType taskType = TaskType.Daily;

        #endregion

        #region Requirements

        [Header("Requirements")]
        [Tooltip("List of requirements that must be met to complete this task")]
        public TaskRequirement[] requirements;

        #endregion

        #region Rewards

        [Header("Rewards")]
        [Tooltip("Rewards granted upon task completion")]
        public TaskReward reward;

        #endregion

        #region Unlock Conditions

        [Header("Unlock Conditions")]
        [Tooltip("Conditions that must be met before this task becomes available")]
        public TaskUnlockCondition unlockCondition;

        #endregion

        #region Display Settings

        [Header("Display Settings")]
        [Tooltip("Icon displayed in task UI")]
        public Sprite icon;
        [Tooltip("Sort order within task type")]
        public int sortOrder;
        [Tooltip("Whether this task should be highlighted")]
        public bool isFeatured;

        #endregion

        #region Generation Weights

        [Header("Generation Settings (For DailyTaskGenerator)")]
        [Tooltip("Relative weight for random selection (higher = more likely)")]
        [Range(0f, 100f)]
        public float generationWeight = 50f;
        [Tooltip("Maximum times this task can be generated per period")]
        [Range(1, 10)]
        public int maxOccurrences = 1;
        [Tooltip("Task groups - tasks in same group won't generate together")]
        public string[] mutuallyExclusiveGroups;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Validates the task data configuration.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(taskId))
            {
                Debug.LogWarning($"[TaskData] Task has no ID assigned: {name}");
                return false;
            }

            if (string.IsNullOrEmpty(taskName))
            {
                Debug.LogWarning($"[TaskData] Task {taskId} has no name");
                return false;
            }

            if (requirements == null || requirements.Length == 0)
            {
                Debug.LogWarning($"[TaskData] Task {taskId} has no requirements");
                return false;
            }

            foreach (var req in requirements)
            {
                if (req.targetValue <= 0 && req.requirementType != TaskRequirementType.Accuracy)
                {
                    Debug.LogWarning($"[TaskData] Task {taskId} has invalid requirement target");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a formatted description of requirements for UI display.
        /// </summary>
        public string GetRequirementDescription()
        {
            if (requirements == null || requirements.Length == 0)
                return "No requirements";

            var parts = new System.Collections.Generic.List<string>();

            foreach (var req in requirements)
            {
                string desc = req.requirementType switch
                {
                    TaskRequirementType.Inspections => $"Complete {req.targetValue} inspections",
                    TaskRequirementType.Accuracy => $"Achieve {req.floatValue:F0}% average accuracy",
                    TaskRequirementType.Money => $"Earn ${req.targetValue:N0}",
                    TaskRequirementType.Time => $"Complete in under {req.targetValue} minutes",
                    TaskRequirementType.PerfectInspections => $"Get {req.targetValue} perfect inspections",
                    TaskRequirementType.IssuesFound => $"Find {req.targetValue} total issues",
                    TaskRequirementType.SpecificTool => $"Use tool {req.targetValue} times",
                    TaskRequirementType.VehicleType => $"Inspect {req.targetValue} vehicles",
                    _ => "Unknown requirement"
                };
                parts.Add(desc);
            }

            return string.Join("\n", parts);
        }

        /// <summary>
        /// Gets a formatted description of rewards for UI display.
        /// </summary>
        public string GetRewardDescription()
        {
            var parts = new System.Collections.Generic.List<string>();

            if (reward.xpAmount > 0)
                parts.Add($"{reward.xpAmount} XP");
            if (reward.moneyAmount > 0)
                parts.Add($"${reward.moneyAmount:N0}");
            if (reward.itemIds != null)
            {
                foreach (string itemId in reward.itemIds)
                {
                    parts.Add(itemId);
                }
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "No rewards";
        }

        #endregion

        #region Editor Validation

        private void OnValidate()
        {
            // Auto-generate task ID if empty
            if (string.IsNullOrEmpty(taskId))
            {
                taskId = name.Replace(" ", "_").ToLowerInvariant();
            }

            // Ensure at least some default values
            if (requirements == null || requirements.Length == 0)
            {
                requirements = new TaskRequirement[]
                {
                    new TaskRequirement { requirementType = TaskRequirementType.Inspections, targetValue = 1 }
                };
            }
        }

        #endregion
    }
}
