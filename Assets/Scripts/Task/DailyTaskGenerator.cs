using System;
using System.Collections.Generic;
using UnityEngine;
using Core;
using Progression;

namespace Task
{
    /// <summary>
    /// Configuration for task generation.
    /// </summary>
    [Serializable]
    public class TaskGenerationConfig
    {
        [Header("Daily Tasks")]
        [Tooltip("Number of daily tasks to generate")]
        [Range(1, 10)]
        public int dailyTaskCount = 3;
        [Tooltip("Minimum tasks based on player level")]
        public int minDailyTasks = 2;
        [Tooltip("Maximum daily tasks regardless of level")]
        public int maxDailyTasks = 5;

        [Header("Weekly Tasks")]
        [Range(1, 5)]
        public int weeklyTaskCount = 2;

        [Header("Difficulty Scaling")]
        [Tooltip("Base difficulty multiplier at level 1")]
        public float baseDifficultyMultiplier = 1f;
        [Tooltip("Additional difficulty per player level")]
        public float difficultyPerLevel = 0.1f;
        [Tooltip("Maximum difficulty multiplier")]
        public float maxDifficultyMultiplier = 3f;

        [Header("Requirement Ranges")]
        public InspectionRequirementRange inspectionRange = new InspectionRequirementRange
        {
            minBase = 1,
            maxBase = 3,
            perLevelIncrease = 1,
            maxValue = 20
        };
        public MoneyRequirementRange moneyRange = new MoneyRequirementRange
        {
            minBase = 100,
            maxBase = 500,
            perLevelIncrease = 50,
            maxValue = 5000
        };
        public AccuracyRequirementRange accuracyRange = new AccuracyRequirementRange
        {
            minBase = 60f,
            maxBase = 85f,
            perLevelIncrease = 2f,
            maxValue = 95f
        };
        public TimeRequirementRange timeRange = new TimeRequirementRange
        {
            minBaseMinutes = 5,
            maxBaseMinutes = 15,
            perLevelDecrease = 1,
            minValue = 3
        };
    }

    [Serializable]
    public struct InspectionRequirementRange
    {
        public int minBase;
        public int maxBase;
        public int perLevelIncrease;
        public int maxValue;
    }

    [Serializable]
    public struct MoneyRequirementRange
    {
        public int minBase;
        public int maxBase;
        public int perLevelIncrease;
        public int maxValue;
    }

    [Serializable]
    public struct AccuracyRequirementRange
    {
        public float minBase;
        public float maxBase;
        public float perLevelIncrease;
        public float maxValue;
    }

    [Serializable]
    public struct TimeRequirementRange
    {
        public int minBaseMinutes;
        public int maxBaseMinutes;
        public int perLevelDecrease;
        public int minValue;
    }

    /// <summary>
    /// Generates balanced daily and weekly tasks based on player level and progression.
    /// Integrates with ProgressionManager and PlayerStats for dynamic difficulty.
    /// </summary>
    public class DailyTaskGenerator
    {
        private readonly TaskGenerationConfig _config;
        private readonly List<TaskData> _availableTaskTemplates;
        private readonly System.Random _random;

        // Track which task groups have been used
        private readonly HashSet<string> _usedGroups = new HashSet<string>();
        // Track task IDs already generated this period
        private readonly HashSet<string> _generatedTaskIds = new HashSet<string>();

        public DailyTaskGenerator(TaskGenerationConfig config, List<TaskData> taskTemplates)
        {
            _config = config ?? new TaskGenerationConfig();
            _availableTaskTemplates = taskTemplates ?? new List<TaskData>();
            _random = new System.Random();
        }

        public DailyTaskGenerator(TaskGenerationConfig config, List<TaskData> taskTemplates, int seed)
        {
            _config = config ?? new TaskGenerationConfig();
            _availableTaskTemplates = taskTemplates ?? new List<TaskData>();
            _random = new System.Random(seed);
        }

        /// <summary>
        /// Generates daily tasks based on player level and progression.
        /// </summary>
        public List<ActiveTask> GenerateDailyTasks(int playerLevel, int totalInspections, HashSet<string> completedTaskIds)
        {
            _usedGroups.Clear();
            _generatedTaskIds.Clear();

            var tasks = new List<ActiveTask>();
            int taskCount = CalculateDailyTaskCount(playerLevel);

            // Get eligible templates
            var eligibleTemplates = GetEligibleTemplates(
                TaskType.Daily,
                playerLevel,
                totalInspections,
                completedTaskIds
            );

            // Shuffle for randomness
            ShuffleList(eligibleTemplates);

            // Select varied task types
            var selectedTemplates = SelectVariedTasks(eligibleTemplates, taskCount);

            // Create active tasks from templates
            foreach (var template in selectedTemplates)
            {
                var activeTask = CreateActiveTaskFromTemplate(template, playerLevel);
                if (activeTask != null)
                {
                    tasks.Add(activeTask);
                    _generatedTaskIds.Add(template.taskId);
                }
            }

            Debug.Log($"[DailyTaskGenerator] Generated {tasks.Count} daily tasks for level {playerLevel}");
            return tasks;
        }

        /// <summary>
        /// Generates weekly tasks based on player level and progression.
        /// </summary>
        public List<ActiveTask> GenerateWeeklyTasks(int playerLevel, int totalInspections, HashSet<string> completedTaskIds)
        {
            _usedGroups.Clear();
            _generatedTaskIds.Clear();

            var tasks = new List<ActiveTask>();
            int taskCount = _config.weeklyTaskCount;

            var eligibleTemplates = GetEligibleTemplates(
                TaskType.Weekly,
                playerLevel,
                totalInspections,
                completedTaskIds
            );

            ShuffleList(eligibleTemplates);
            var selectedTemplates = SelectVariedTasks(eligibleTemplates, taskCount);

            foreach (var template in selectedTemplates)
            {
                var activeTask = CreateActiveTaskFromTemplate(template, playerLevel);
                if (activeTask != null)
                {
                    // Weekly tasks have scaled-up requirements
                    activeTask = ScaleWeeklyTask(activeTask, playerLevel);
                    tasks.Add(activeTask);
                    _generatedTaskIds.Add(template.taskId);
                }
            }

            Debug.Log($"[DailyTaskGenerator] Generated {tasks.Count} weekly tasks for level {playerLevel}");
            return tasks;
        }

        /// <summary>
        /// Generates a special task for events or milestones.
        /// </summary>
        public ActiveTask GenerateSpecialTask(string specialTaskId, int playerLevel)
        {
            var template = _availableTaskTemplates.Find(t => t.taskId == specialTaskId && t.taskType == TaskType.Special);
            if (template == null)
            {
                Debug.LogWarning($"[DailyTaskGenerator] Special task template not found: {specialTaskId}");
                return null;
            }

            return CreateActiveTaskFromTemplate(template, playerLevel);
        }

        private int CalculateDailyTaskCount(int playerLevel)
        {
            // Scale task count with level
            int levelBonus = Mathf.FloorToInt(playerLevel / 5f);
            int count = _config.minDailyTasks + levelBonus;
            return Mathf.Clamp(count, _config.minDailyTasks, _config.maxDailyTasks);
        }

        private List<TaskData> GetEligibleTemplates(
            TaskType taskType,
            int playerLevel,
            int totalInspections,
            HashSet<string> completedTaskIds)
        {
            var eligible = new List<TaskData>();

            foreach (var template in _availableTaskTemplates)
            {
                if (template.taskType != taskType) continue;
                if (!template.IsValid()) continue;

                // Check unlock conditions
                if (!template.unlockCondition.IsUnlocked(playerLevel, totalInspections, completedTaskIds))
                    continue;

                eligible.Add(template);
            }

            return eligible;
        }

        private List<TaskData> SelectVariedTasks(List<TaskData> templates, int count)
        {
            var selected = new List<TaskData>();
            var usedRequirementTypes = new HashSet<TaskRequirementType>();

            foreach (var template in templates)
            {
                if (selected.Count >= count) break;

                // Check mutual exclusivity groups
                if (template.mutuallyExclusiveGroups != null)
                {
                    bool hasConflict = false;
                    foreach (string group in template.mutuallyExclusiveGroups)
                    {
                        if (_usedGroups.Contains(group))
                        {
                            hasConflict = true;
                            break;
                        }
                    }
                    if (hasConflict) continue;
                }

                // Check for requirement type variety (prefer different types)
                TaskRequirementType primaryType = template.requirements[0].requirementType;

                // Allow some overlap but prefer variety
                int sameTypeCount = 0;
                foreach (var selectedTask in selected)
                {
                    if (selectedTask.requirements[0].requirementType == primaryType)
                    {
                        sameTypeCount++;
                    }
                }

                // Allow up to 2 tasks of same type
                if (sameTypeCount >= 2) continue;

                selected.Add(template);

                // Mark groups as used
                if (template.mutuallyExclusiveGroups != null)
                {
                    foreach (string group in template.mutuallyExclusiveGroups)
                    {
                        _usedGroups.Add(group);
                    }
                }
            }

            return selected;
        }

        private ActiveTask CreateActiveTaskFromTemplate(TaskData template, int playerLevel)
        {
            var activeTask = new ActiveTask
            {
                taskId = template.taskId,
                taskName = template.taskName,
                description = template.description,
                taskType = template.taskType,
                icon = template.icon,
                reward = template.reward,
                assignedDate = DateTime.Now,
                isCompleted = false,
                isClaimed = false
            };

            // Scale requirements based on player level
            float difficultyMultiplier = GetDifficultyMultiplier(playerLevel);
            activeTask.requirements = ScaleRequirements(template.requirements, difficultyMultiplier, playerLevel);

            // Initialize progress
            activeTask.currentProgress = new int[activeTask.requirements.Length];
            activeTask.currentFloatProgress = new float[activeTask.requirements.Length];

            return activeTask;
        }

        private float GetDifficultyMultiplier(int playerLevel)
        {
            float multiplier = _config.baseDifficultyMultiplier + (playerLevel * _config.difficultyPerLevel);
            return Mathf.Min(multiplier, _config.maxDifficultyMultiplier);
        }

        private TaskRequirement[] ScaleRequirements(TaskRequirement[] original, float multiplier, int playerLevel)
        {
            var scaled = new TaskRequirement[original.Length];

            for (int i = 0; i < original.Length; i++)
            {
                scaled[i] = original[i];

                switch (original[i].requirementType)
                {
                    case TaskRequirementType.Inspections:
                        int baseInspections = original[i].targetValue;
                        int levelScaled = baseInspections + Mathf.RoundToInt(playerLevel * _config.inspectionRange.perLevelIncrease);
                        scaled[i].targetValue = Mathf.Clamp(
                            Mathf.RoundToInt(levelScaled * multiplier),
                            1,
                            _config.inspectionRange.maxValue
                        );
                        break;

                    case TaskRequirementType.Money:
                        int baseMoney = original[i].targetValue;
                        int moneyScaled = baseMoney + Mathf.RoundToInt(playerLevel * _config.moneyRange.perLevelIncrease);
                        scaled[i].targetValue = Mathf.Clamp(
                            Mathf.RoundToInt(moneyScaled * multiplier),
                            _config.moneyRange.minBase,
                            _config.moneyRange.maxValue
                        );
                        break;

                    case TaskRequirementType.Accuracy:
                        float baseAccuracy = original[i].floatValue;
                        float accuracyScaled = baseAccuracy + (playerLevel * _config.accuracyRange.perLevelIncrease);
                        scaled[i].floatValue = Mathf.Clamp(accuracyScaled, _config.accuracyRange.minBase, _config.accuracyRange.maxValue);
                        break;

                    case TaskRequirementType.Time:
                        int baseTime = original[i].targetValue;
                        int timeScaled = baseTime - Mathf.RoundToInt(playerLevel * _config.timeRange.perLevelDecrease);
                        scaled[i].targetValue = Mathf.Clamp(timeScaled, _config.timeRange.minValue, _config.timeRange.maxBaseMinutes);
                        break;

                    case TaskRequirementType.PerfectInspections:
                        int basePerfect = original[i].targetValue;
                        scaled[i].targetValue = Mathf.Clamp(
                            Mathf.RoundToInt(basePerfect * multiplier),
                            1,
                            10
                        );
                        break;

                    case TaskRequirementType.IssuesFound:
                        int baseIssues = original[i].targetValue;
                        scaled[i].targetValue = Mathf.Clamp(
                            Mathf.RoundToInt(baseIssues * multiplier),
                            1,
                            50
                        );
                        break;

                    // Specific tool and vehicle type use original values
                    default:
                        scaled[i].targetValue = original[i].targetValue;
                        break;
                }
            }

            return scaled;
        }

        private ActiveTask ScaleWeeklyTask(ActiveTask dailyTask, int playerLevel)
        {
            // Weekly tasks are approximately 5x harder than daily
            float weeklyMultiplier = 5f;

            for (int i = 0; i < dailyTask.requirements.Length; i++)
            {
                switch (dailyTask.requirements[i].requirementType)
                {
                    case TaskRequirementType.Inspections:
                    case TaskRequirementType.PerfectInspections:
                    case TaskRequirementType.IssuesFound:
                        dailyTask.requirements[i].targetValue = Mathf.RoundToInt(dailyTask.requirements[i].targetValue * weeklyMultiplier);
                        break;
                    case TaskRequirementType.Money:
                        dailyTask.requirements[i].targetValue = Mathf.RoundToInt(dailyTask.requirements[i].targetValue * weeklyMultiplier);
                        break;
                    // Accuracy remains same, time gets harder (less time)
                    case TaskRequirementType.Time:
                        dailyTask.requirements[i].targetValue = Mathf.Max(1, Mathf.RoundToInt(dailyTask.requirements[i].targetValue * 1.5f));
                        break;
                }
            }

            // Scale rewards too
            dailyTask.reward = new TaskReward
            {
                xpAmount = Mathf.RoundToInt(dailyTask.reward.xpAmount * weeklyMultiplier),
                moneyAmount = Mathf.RoundToInt(dailyTask.reward.moneyAmount * weeklyMultiplier),
                itemIds = dailyTask.reward.itemIds
            };

            return dailyTask;
        }

        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        #region Utility Methods

        /// <summary>
        /// Creates a random task from scratch (for procedural generation).
        /// Use sparingly - prefer templates for consistent game design.
        /// </summary>
        public ActiveTask CreateRandomTask(TaskType taskType, int playerLevel)
        {
            // Pick a random requirement type
            var requirementTypes = Enum.GetValues(typeof(TaskRequirementType));
            var randomType = (TaskRequirementType)requirementTypes.GetValue(_random.Next(requirementTypes.Length));

            var requirement = new TaskRequirement
            {
                requirementType = randomType,
                targetValue = GenerateRandomTarget(randomType, playerLevel),
                floatValue = randomType == TaskRequirementType.Accuracy ? GenerateRandomAccuracy(playerLevel) : 0f
            };

            string taskId = $"proc_{taskType}_{randomType}_{DateTime.Now.Ticks}";
            string taskName = GenerateTaskName(randomType, requirement.targetValue);

            return new ActiveTask
            {
                taskId = taskId,
                taskName = taskName,
                description = GenerateTaskDescription(randomType, requirement.targetValue, requirement.floatValue),
                taskType = taskType,
                requirements = new[] { requirement },
                currentProgress = new int[1],
                currentFloatProgress = new float[1],
                reward = GenerateReward(taskType, playerLevel),
                assignedDate = DateTime.Now,
                isCompleted = false,
                isClaimed = false
            };
        }

        private int GenerateRandomTarget(TaskRequirementType type, int playerLevel)
        {
            float multiplier = GetDifficultyMultiplier(playerLevel);

            return type switch
            {
                TaskRequirementType.Inspections => Mathf.Clamp(
                    Mathf.RoundToInt((_config.inspectionRange.minBase + _random.Next(_config.inspectionRange.maxBase - _config.inspectionRange.minBase)) * multiplier),
                    1, _config.inspectionRange.maxValue),
                TaskRequirementType.Money => Mathf.Clamp(
                    Mathf.RoundToInt((_config.moneyRange.minBase + _random.Next(_config.moneyRange.maxBase - _config.moneyRange.minBase)) * multiplier),
                    _config.moneyRange.minBase, _config.moneyRange.maxValue),
                TaskRequirementType.Time => Mathf.Clamp(
                    _config.timeRange.maxBaseMinutes - _random.Next(_config.timeRange.maxBaseMinutes - _config.timeRange.minValue),
                    _config.timeRange.minValue, _config.timeRange.maxBaseMinutes),
                TaskRequirementType.PerfectInspections => Mathf.Clamp(
                    1 + _random.Next(Mathf.FloorToInt(playerLevel / 3f)), 1, 5),
                TaskRequirementType.IssuesFound => Mathf.Clamp(
                    3 + _random.Next(playerLevel * 2), 3, 30),
                _ => 1
            };
        }

        private float GenerateRandomAccuracy(int playerLevel)
        {
            float baseAccuracy = _config.accuracyRange.minBase + (playerLevel * _config.accuracyRange.perLevelIncrease);
            float variance = (_config.accuracyRange.maxBase - _config.accuracyRange.minBase) * 0.5f;
            return Mathf.Clamp(baseAccuracy + (float)_random.NextDouble() * variance, _config.accuracyRange.minBase, _config.accuracyRange.maxValue);
        }

        private string GenerateTaskName(TaskRequirementType type, int target)
        {
            return type switch
            {
                TaskRequirementType.Inspections => $"Inspect {target} Vehicles",
                TaskRequirementType.Accuracy => "Accuracy Challenge",
                TaskRequirementType.Money => $"Earn ${target:N0}",
                TaskRequirementType.Time => "Speed Run",
                TaskRequirementType.PerfectInspections => $"Perfect {target} Times",
                TaskRequirementType.IssuesFound => $"Find {target} Issues",
                TaskRequirementType.SpecificTool => "Tool Master",
                TaskRequirementType.VehicleType => "Variety Inspector",
                _ => "Mystery Task"
            };
        }

        private string GenerateTaskDescription(TaskRequirementType type, int target, float floatValue)
        {
            return type switch
            {
                TaskRequirementType.Inspections => $"Complete {target} vehicle inspections today.",
                TaskRequirementType.Accuracy => $"Maintain an average accuracy of at least {floatValue:F0}% across your inspections.",
                TaskRequirementType.Money => $"Earn a total of ${target:N0} from your inspections.",
                TaskRequirementType.Time => $"Complete an inspection in under {target} minutes.",
                TaskRequirementType.PerfectInspections => $"Achieve {target} perfect inspections (100% accuracy).",
                TaskRequirementType.IssuesFound => $"Find a total of {target} issues across all inspections.",
                TaskRequirementType.SpecificTool => $"Use the designated tool {target} times.",
                TaskRequirementType.VehicleType => $"Inspect {target} different vehicles.",
                _ => "Complete this challenge."
            };
        }

        private TaskReward GenerateReward(TaskType taskType, int playerLevel)
        {
            float multiplier = taskType == TaskType.Weekly ? 5f : 1f;

            return new TaskReward
            {
                xpAmount = Mathf.RoundToInt((25 + playerLevel * 10 + _random.Next(20)) * multiplier),
                moneyAmount = Mathf.RoundToInt((50 + playerLevel * 25 + _random.Next(50)) * multiplier),
                itemIds = null
            };
        }

        #endregion
    }
}
