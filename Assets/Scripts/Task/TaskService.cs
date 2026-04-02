using System;
using System.Collections.Generic;
using UnityEngine;
using Core;
using Progression;

namespace Task
{
    /// <summary>
    /// Represents an active task instance with progress tracking.
    /// </summary>
    [Serializable]
    public class ActiveTask
    {
        public string taskId;
        public string taskName;
        public string description;
        public TaskType taskType;
        public Sprite icon;
        public TaskRequirement[] requirements;
        public int[] currentProgress;
        public float[] currentFloatProgress;
        public TaskReward reward;
        public DateTime assignedDate;
        public DateTime? completedDate;
        public bool isCompleted;
        public bool isClaimed;

        public float OverallProgress
        {
            get
            {
                if (requirements == null || requirements.Length == 0) return 0f;

                float totalProgress = 0f;
                for (int i = 0; i < requirements.Length; i++)
                {
                    totalProgress += requirements[i].GetProgress(
                        currentProgress != null && i < currentProgress.Length ? currentProgress[i] : 0,
                        currentFloatProgress != null && i < currentFloatProgress.Length ? currentFloatProgress[i] : 0f
                    );
                }
                return totalProgress / requirements.Length;
            }
        }

        public bool IsFullyCompleted
        {
            get
            {
                if (requirements == null) return false;
                for (int i = 0; i < requirements.Length; i++)
                {
                    int current = currentProgress != null && i < currentProgress.Length ? currentProgress[i] : 0;
                    float currentFloat = currentFloatProgress != null && i < currentFloatProgress.Length ? currentFloatProgress[i] : 0f;
                    if (!requirements[i].IsCompleted(current, currentFloat))
                        return false;
                }
                return true;
            }
        }
    }

    /// <summary>
    /// Serializable data for saving/loading task state.
    /// </summary>
    [Serializable]
    public struct TaskSaveData
    {
        public List<ActiveTask> dailyTasks;
        public List<ActiveTask> weeklyTasks;
        public List<ActiveTask> specialTasks;
        public List<string> completedTaskIds;
        public DateTime lastDailyReset;
        public DateTime lastWeeklyReset;
    }

    /// <summary>
    /// Interface for the TaskService system.
    /// </summary>
    public interface ITaskService : ISystem
    {
        // Task Access
        IReadOnlyList<ActiveTask> DailyTasks { get; }
        IReadOnlyList<ActiveTask> WeeklyTasks { get; }
        IReadOnlyList<ActiveTask> SpecialTasks { get; }
        IReadOnlyList<ActiveTask> CompletedTasks { get; }

        // Events
        event Action<ActiveTask> OnTaskCompleted;
        event Action<ActiveTask> OnTaskClaimed;
        event Action<ActiveTask> OnTaskProgressUpdated;
        event Action OnTasksReset;

        // Task Management
        void AddProgress(TaskRequirementType type, int amount, float floatAmount = 0f);
        void CompleteInspection(float accuracy, int issuesFound, float moneyEarned, float timeMinutes);
        bool ClaimReward(string taskId);
        ActiveTask GetTask(string taskId);

        // Task Generation
        void GenerateDailyTasks();
        void GenerateWeeklyTasks();
        void AddSpecialTask(string taskDataId);

        // Queries
        int GetCompletedTaskCount(TaskType? filterType = null);
        bool HasUnclaimedRewards();
    }

    /// <summary>
    /// Manages active tasks, tracks progress, and handles task completion/rewards.
    /// Registers with ServiceLocator as ITaskService.
    /// </summary>
    public class TaskService : MonoBehaviour, ITaskService
    {
        #region Serialized Fields

        [Header("Task Templates")]
        [SerializeField] private TaskData[] taskTemplates;
        [SerializeField] private TaskGenerationConfig generationConfig;

        [Header("Reset Settings")]
        [SerializeField] private int dailyResetHour = 0; // Midnight
        [SerializeField] private DayOfWeek weeklyResetDay = DayOfWeek.Monday;

        #endregion

        #region Properties

        public int Priority => 15;

        public IReadOnlyList<ActiveTask> DailyTasks => _dailyTasks;
        public IReadOnlyList<ActiveTask> WeeklyTasks => _weeklyTasks;
        public IReadOnlyList<ActiveTask> SpecialTasks => _specialTasks;
        public IReadOnlyList<ActiveTask> CompletedTasks => _completedTasks;

        #endregion

        #region Events

        public event Action<ActiveTask> OnTaskCompleted;
        public event Action<ActiveTask> OnTaskClaimed;
        public event Action<ActiveTask> OnTaskProgressUpdated;
        public event Action OnTasksReset;

        #endregion

        #region Private Fields

        private readonly List<ActiveTask> _dailyTasks = new List<ActiveTask>();
        private readonly List<ActiveTask> _weeklyTasks = new List<ActiveTask>();
        private readonly List<ActiveTask> _specialTasks = new List<ActiveTask>();
        private readonly List<ActiveTask> _completedTasks = new List<ActiveTask>();
        private readonly HashSet<string> _completedTaskIds = new HashSet<string>();

        private DailyTaskGenerator _taskGenerator;
        private IProgressionManager _progressionManager;
        private IPlayerStats _playerStats;

        private DateTime _lastDailyReset;
        private DateTime _lastWeeklyReset;

        // Session tracking for accuracy calculations
        private int _sessionInspections;
        private float _sessionAccuracySum;
        private float _sessionMoneyEarned;
        private int _sessionPerfectInspections;

        private const string SAVE_KEY = "TaskService_Data";

        #endregion

        #region ISystem Implementation

        public void OnRegistered()
        {
            Debug.Log("[TaskService] Registered with ServiceLocator");
        }

        public void Initialize()
        {
            // Get dependencies
            if (ServiceLocator.TryGet(out _progressionManager))
            {
                Debug.Log("[TaskService] Connected to ProgressionManager");
            }

            if (ServiceLocator.TryGet(out _playerStats))
            {
                Debug.Log("[TaskService] Connected to PlayerStats");
            }

            // Initialize task generator
            var templateList = new List<TaskData>();
            if (taskTemplates != null)
            {
                foreach (var template in taskTemplates)
                {
                    if (template != null && template.IsValid())
                    {
                        templateList.Add(template);
                    }
                }
            }
            _taskGenerator = new DailyTaskGenerator(generationConfig, templateList);

            // Load saved data
            LoadTaskData();

            // Check for resets
            CheckAndPerformResets();

            // Subscribe to events
            SubscribeToEvents();

            Debug.Log($"[TaskService] Initialized. Daily: {_dailyTasks.Count}, Weekly: {_weeklyTasks.Count}, Special: {_specialTasks.Count}");
        }

        public void Shutdown()
        {
            UnsubscribeFromEvents();
            SaveTaskData();
            Debug.Log("[TaskService] Shutdown complete");
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            // Subscribe to time changes for reset checking
            EventManager.StartListening("OnTimeChanged", OnTimeChanged);

            // Subscribe to day changes (if using TimeManager)
            EventManager.StartListening("OnNewDay", OnNewDay);
        }

        private void UnsubscribeFromEvents()
        {
            EventManager.StopListening("OnTimeChanged", OnTimeChanged);
            EventManager.StopListening("OnNewDay", OnNewDay);
        }

        private void OnTimeChanged()
        {
            CheckAndPerformResets();
        }

        private void OnNewDay()
        {
            CheckAndPerformResets();
        }

        #endregion

        #region Reset Logic

        private void CheckAndPerformResets()
        {
            DateTime now = DateTime.Now;

            // Check daily reset
            if (ShouldResetDaily(now))
            {
                ResetDailyTasks();
                _lastDailyReset = now;
            }

            // Check weekly reset
            if (ShouldResetWeekly(now))
            {
                ResetWeeklyTasks();
                _lastWeeklyReset = now;
            }
        }

        private bool ShouldResetDaily(DateTime now)
        {
            // Reset if we've crossed the reset hour on a new day
            DateTime lastResetDate = _lastDailyReset.Date;
            DateTime todayAtReset = now.Date.AddHours(dailyResetHour);

            if (now.Date > lastResetDate)
            {
                // We're on a new day
                if (now.Hour >= dailyResetHour)
                {
                    return true;
                }
            }
            return false;
        }

        private bool ShouldResetWeekly(DateTime now)
        {
            // Find the most recent reset day
            int daysSinceMonday = (int)now.DayOfWeek - (int)weeklyResetDay;
            if (daysSinceMonday < 0) daysSinceMonday += 7;

            DateTime thisWeekReset = now.Date.AddDays(-daysSinceMonday).AddHours(dailyResetHour);

            return _lastWeeklyReset < thisWeekReset && now >= thisWeekReset;
        }

        private void ResetDailyTasks()
        {
            // Archive any completed tasks
            foreach (var task in _dailyTasks)
            {
                if (task.isCompleted && !task.isClaimed)
                {
                    // Keep unclaimed completed tasks
                }
            }

            _dailyTasks.Clear();

            // Reset session stats
            _sessionInspections = 0;
            _sessionAccuracySum = 0f;
            _sessionMoneyEarned = 0f;
            _sessionPerfectInspections = 0;

            GenerateDailyTasks();

            OnTasksReset?.Invoke();
            EventManager.TriggerEvent("OnDailyTasksReset");

            Debug.Log("[TaskService] Daily tasks reset");
        }

        private void ResetWeeklyTasks()
        {
            _weeklyTasks.Clear();
            GenerateWeeklyTasks();

            OnTasksReset?.Invoke();
            EventManager.TriggerEvent("OnWeeklyTasksReset");

            Debug.Log("[TaskService] Weekly tasks reset");
        }

        #endregion

        #region Task Generation

        public void GenerateDailyTasks()
        {
            int playerLevel = _progressionManager?.CurrentLevel ?? 1;
            int totalInspections = _playerStats?.TotalInspections ?? 0;

            var newTasks = _taskGenerator.GenerateDailyTasks(playerLevel, totalInspections, _completedTaskIds);

            _dailyTasks.Clear();
            _dailyTasks.AddRange(newTasks);

            Debug.Log($"[TaskService] Generated {newTasks.Count} daily tasks");
        }

        public void GenerateWeeklyTasks()
        {
            int playerLevel = _progressionManager?.CurrentLevel ?? 1;
            int totalInspections = _playerStats?.TotalInspections ?? 0;

            var newTasks = _taskGenerator.GenerateWeeklyTasks(playerLevel, totalInspections, _completedTaskIds);

            _weeklyTasks.Clear();
            _weeklyTasks.AddRange(newTasks);

            Debug.Log($"[TaskService] Generated {newTasks.Count} weekly tasks");
        }

        public void AddSpecialTask(string taskDataId)
        {
            int playerLevel = _progressionManager?.CurrentLevel ?? 1;

            var specialTask = _taskGenerator.GenerateSpecialTask(taskDataId, playerLevel);
            if (specialTask != null)
            {
                _specialTasks.Add(specialTask);
                Debug.Log($"[TaskService] Added special task: {specialTask.taskName}");
            }
        }

        #endregion

        #region Progress Tracking

        /// <summary>
        /// Adds progress to all tasks with matching requirement type.
        /// </summary>
        public void AddProgress(TaskRequirementType type, int amount, float floatAmount = 0f)
        {
            AddProgressToTasks(_dailyTasks, type, amount, floatAmount);
            AddProgressToTasks(_weeklyTasks, type, amount, floatAmount);
            AddProgressToTasks(_specialTasks, type, amount, floatAmount);
        }

        private void AddProgressToTasks(List<ActiveTask> tasks, TaskRequirementType type, int amount, float floatAmount)
        {
            for (int i = tasks.Count - 1; i >= 0; i--)
            {
                var task = tasks[i];
                if (task.isCompleted) continue;

                bool updated = false;

                for (int j = 0; j < task.requirements.Length; j++)
                {
                    if (task.requirements[j].requirementType == type)
                    {
                        task.currentProgress[j] += amount;
                        task.currentFloatProgress[j] = floatAmount; // For accuracy, this replaces
                        updated = true;
                    }
                }

                if (updated)
                {
                    OnTaskProgressUpdated?.Invoke(task);
                    CheckTaskCompletion(task, tasks, i);
                }
            }
        }

        /// <summary>
        /// Called when an inspection is completed. Updates all relevant task progress.
        /// </summary>
        public void CompleteInspection(float accuracy, int issuesFound, float moneyEarned, float timeMinutes)
        {
            // Update session stats
            _sessionInspections++;
            _sessionAccuracySum += accuracy;
            _sessionMoneyEarned += moneyEarned;

            if (accuracy >= 1.0f)
            {
                _sessionPerfectInspections++;
            }

            // Add progress to different task types
            AddProgress(TaskRequirementType.Inspections, 1);
            AddProgress(TaskRequirementType.Accuracy, 0, _sessionAccuracySum / _sessionInspections);
            AddProgress(TaskRequirementType.Money, Mathf.RoundToInt(moneyEarned), _sessionMoneyEarned);
            AddProgress(TaskRequirementType.IssuesFound, issuesFound);
            AddProgress(TaskRequirementType.PerfectInspections, accuracy >= 1.0f ? 1 : 0);
            AddProgress(TaskRequirementType.Time, 0, timeMinutes); // Use float for time tracking

            Debug.Log($"[TaskService] Recorded inspection. Session: {_sessionInspections} inspections, Avg Accuracy: {(_sessionAccuracySum / _sessionInspections):P0}");
        }

        private void CheckTaskCompletion(ActiveTask task, List<ActiveTask> taskList, int index)
        {
            if (task.IsFullyCompleted && !task.isCompleted)
            {
                task.isCompleted = true;
                task.completedDate = DateTime.Now;

                // Move to completed list
                _completedTasks.Add(task);
                _completedTaskIds.Add(task.taskId);
                taskList.RemoveAt(index);

                OnTaskCompleted?.Invoke(task);
                EventManager.TriggerEvent("OnTaskCompleted");

                Debug.Log($"[TaskService] Task completed: {task.taskName}");
            }
        }

        #endregion

        #region Reward Claiming

        public bool ClaimReward(string taskId)
        {
            var task = GetTask(taskId);
            if (task == null || !task.isCompleted || task.isClaimed)
            {
                Debug.LogWarning($"[TaskService] Cannot claim reward for task: {taskId}");
                return false;
            }

            task.isClaimed = true;

            // Award XP
            if (task.reward.xpAmount > 0 && _progressionManager != null)
            {
                _progressionManager.AddXP(task.reward.xpAmount, $"Task: {task.taskName}");
            }

            // Award money via PlayerDataManager if available
            if (task.reward.moneyAmount > 0)
            {
                PlayerScripts.PlayerDataManager.Instance?.AddMoney(task.reward.moneyAmount);
            }

            // Award items (implementation depends on inventory system)
            if (task.reward.itemIds != null)
            {
                foreach (string itemId in task.reward.itemIds)
                {
                    Debug.Log($"[TaskService] Awarded item: {itemId}");
                    // PlayerDataManager.Instance?.AddItem(itemId);
                }
            }

            OnTaskClaimed?.Invoke(task);
            EventManager.TriggerEvent("OnTaskRewardClaimed");

            Debug.Log($"[TaskService] Claimed reward for task: {task.taskName}. XP: {task.reward.xpAmount}, Money: {task.reward.moneyAmount}");

            return true;
        }

        public bool HasUnclaimedRewards()
        {
            foreach (var task in _completedTasks)
            {
                if (task.isCompleted && !task.isClaimed)
                    return true;
            }
            return false;
        }

        #endregion

        #region Queries

        public ActiveTask GetTask(string taskId)
        {
            // Check all task lists
            var task = _dailyTasks.Find(t => t.taskId == taskId);
            if (task != null) return task;

            task = _weeklyTasks.Find(t => t.taskId == taskId);
            if (task != null) return task;

            task = _specialTasks.Find(t => t.taskId == taskId);
            if (task != null) return task;

            task = _completedTasks.Find(t => t.taskId == taskId);
            return task;
        }

        public int GetCompletedTaskCount(TaskType? filterType = null)
        {
            if (filterType.HasValue)
            {
                int count = 0;
                foreach (var task in _completedTasks)
                {
                    if (task.taskType == filterType.Value)
                        count++;
                }
                return count;
            }
            return _completedTaskIds.Count;
        }

        #endregion

        #region Save/Load

        private void SaveTaskData()
        {
            var data = new TaskSaveData
            {
                dailyTasks = new List<ActiveTask>(_dailyTasks),
                weeklyTasks = new List<ActiveTask>(_weeklyTasks),
                specialTasks = new List<ActiveTask>(_specialTasks),
                completedTaskIds = new List<string>(_completedTaskIds),
                lastDailyReset = _lastDailyReset,
                lastWeeklyReset = _lastWeeklyReset
            };

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();

            Debug.Log("[TaskService] Task data saved");
        }

        private void LoadTaskData()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY))
            {
                // First run - generate initial tasks
                _lastDailyReset = DateTime.Now;
                _lastWeeklyReset = DateTime.Now;
                GenerateDailyTasks();
                GenerateWeeklyTasks();
                return;
            }

            try
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                var data = JsonUtility.FromJson<TaskSaveData>(json);

                _dailyTasks.Clear();
                _weeklyTasks.Clear();
                _specialTasks.Clear();
                _completedTaskIds.Clear();

                if (data.dailyTasks != null)
                    _dailyTasks.AddRange(data.dailyTasks);

                if (data.weeklyTasks != null)
                    _weeklyTasks.AddRange(data.weeklyTasks);

                if (data.specialTasks != null)
                    _specialTasks.AddRange(data.specialTasks);

                if (data.completedTaskIds != null)
                {
                    foreach (string id in data.completedTaskIds)
                    {
                        _completedTaskIds.Add(id);
                    }
                }

                _lastDailyReset = data.lastDailyReset;
                _lastWeeklyReset = data.lastWeeklyReset;

                Debug.Log("[TaskService] Task data loaded");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TaskService] Failed to load task data: {ex.Message}");
                GenerateDailyTasks();
                GenerateWeeklyTasks();
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Log All Tasks")]
        private void DebugLogTasks()
        {
            Debug.Log($"[TaskService] === Daily Tasks ({_dailyTasks.Count}) ===");
            foreach (var task in _dailyTasks)
            {
                Debug.Log($"  - {task.taskName}: {task.OverallProgress:P0}");
            }

            Debug.Log($"[TaskService] === Weekly Tasks ({_weeklyTasks.Count}) ===");
            foreach (var task in _weeklyTasks)
            {
                Debug.Log($"  - {task.taskName}: {task.OverallProgress:P0}");
            }

            Debug.Log($"[TaskService] === Special Tasks ({_specialTasks.Count}) ===");
            foreach (var task in _specialTasks)
            {
                Debug.Log($"  - {task.taskName}: {task.OverallProgress:P0}");
            }

            Debug.Log($"[TaskService] === Completed Tasks ({_completedTasks.Count}) ===");
            foreach (var task in _completedTasks)
            {
                Debug.Log($"  - {task.taskName} (Claimed: {task.isClaimed})");
            }
        }

        [ContextMenu("Force Daily Reset")]
        private void DebugForceDailyReset()
        {
            _lastDailyReset = DateTime.MinValue;
            CheckAndPerformResets();
        }

        [ContextMenu("Add Test Progress")]
        private void DebugAddProgress()
        {
            CompleteInspection(0.85f, 3, 150f, 5f);
        }

        [ContextMenu("Complete All Daily Tasks")]
        private void DebugCompleteAllDaily()
        {
            foreach (var task in _dailyTasks.ToArray())
            {
                for (int i = 0; i < task.requirements.Length; i++)
                {
                    task.currentProgress[i] = task.requirements[i].targetValue;
                    task.currentFloatProgress[i] = task.requirements[i].floatValue;
                }
                CheckTaskCompletion(task, _dailyTasks, _dailyTasks.IndexOf(task));
            }
        }

        #endregion
    }
}
