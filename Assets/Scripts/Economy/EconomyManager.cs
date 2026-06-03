using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

namespace Economy
{
    /// <summary>
    /// Interface for the Economy system to enable dependency injection via ServiceLocator.
    /// </summary>
    public interface IEconomySystem : ISystem
    {
        /// <summary>Current player balance.</summary>
        float Balance { get; }

        /// <summary>Total money earned today.</summary>
        float TodayIncome { get; }

        /// <summary>Total money spent today.</summary>
        float TodayExpenses { get; }

        /// <summary>Total daily expenses (rent, salaries, etc.).</summary>
        float DailyExpenses { get; }

        /// <summary>Event fired when balance changes.</summary>
        event Action<float, Transaction> OnBalanceChanged;

        /// <summary>Event fired when a transaction is recorded.</summary>
        event Action<Transaction> OnTransactionRecorded;

        /// <summary>Event fired when daily expenses are deducted.</summary>
        event Action<float> OnDailyExpensesDeducted;

        /// <summary>Adds income to the player balance.</summary>
        bool AddIncome(float amount, TransactionCategory category, string description, string referenceId = null);

        /// <summary>Deducts an expense from the player balance.</summary>
        bool DeductExpense(float amount, TransactionCategory category, string description, string referenceId = null);

        /// <summary>Checks if player can afford a purchase.</summary>
        bool CanAfford(float amount);

        /// <summary>Gets all transactions.</summary>
        IReadOnlyList<Transaction> GetTransactionHistory();

        /// <summary>Gets transactions filtered by type.</summary>
        IEnumerable<Transaction> GetTransactionsByType(TransactionType type);

        /// <summary>Gets transactions filtered by category.</summary>
        IEnumerable<Transaction> GetTransactionsByCategory(TransactionCategory category);

        /// <summary>Gets transactions for a specific game day.</summary>
        IEnumerable<Transaction> GetTransactionsByDay(int gameDay);

        /// <summary>Calculates and applies daily expenses.</summary>
        void ApplyDailyExpenses();

        /// <summary>Sets the daily rent amount.</summary>
        void SetDailyRent(float rent);

        /// <summary>Sets the daily salary amount.</summary>
        void SetDailySalary(float salary);

        /// <summary>Calculates inspection fee based on vehicle type and inspection level.</summary>
        float CalculateInspectionFee(InspectionType inspectionType);

        /// <summary>Calculates tip amount based on customer satisfaction.</summary>
        float CalculateTip(float baseAmount, float satisfactionPercentage);

        /// <summary>Calculates bonus for perfect accuracy.</summary>
        float CalculateAccuracyBonus(float baseAmount, float accuracyPercentage);

        /// <summary>Resets daily tracking (call at start of new day).</summary>
        void ResetDailyTracking();

        /// <summary>Gets total income for a date range.</summary>
        float GetTotalIncome(int fromDay, int toDay);

        /// <summary>Gets total expenses for a date range.</summary>
        float GetTotalExpenses(int fromDay, int toDay);

        /// <summary>Processes a complete inspection payment with tips and bonuses.</summary>
        float ProcessInspectionPayment(InspectionType inspectionType, float satisfactionPercentage, float accuracyPercentage, string inspectionId);
    }

    /// <summary>
    /// Types of inspections with different pricing.
    /// </summary>
    public enum InspectionType
    {
        Basic,
        Standard,
        Advanced,
        Comprehensive
    }

    /// <summary>
    /// Manages all financial aspects of the game including income, expenses,
    /// and transaction tracking. Registers with ServiceLocator as IEconomySystem.
    /// </summary>
    public class EconomyManager : MonoBehaviour, IEconomySystem
    {
        #region Singleton
        public static EconomyManager Instance { get; private set; }
        #endregion

        #region ISystem Implementation
        public int Priority => 15; // After PlayerDataSystem (10) and TimeSystem (5)

        public void OnRegistered()
        {
            Debug.Log("[EconomyManager] Registered with ServiceLocator");
        }

        public void Initialize()
        {
            // Resolve PlayerDataSystem here — all systems are registered by now
            if (_playerDataSystem == null && ServiceLocator.TryGet<IPlayerDataSystem>(out var playerDataSystem))
            {
                _playerDataSystem = playerDataSystem;
            }

            Debug.Log("[EconomyManager] Initializing...");
            InitializeEconomy();
        }

        public void Shutdown()
        {
            Debug.Log("[EconomyManager] Shutting down...");
            SaveBalance();
            SaveTransactionHistory();
        }
        #endregion

        #region Events
        /// <summary>
        /// Fired when the balance changes. Parameters: new balance, transaction that caused change.
        /// </summary>
        public event Action<float, Transaction> OnBalanceChanged;

        /// <summary>
        /// Fired when a new transaction is recorded.
        /// </summary>
        public event Action<Transaction> OnTransactionRecorded;

        /// <summary>
        /// Fired when daily expenses are deducted.
        /// </summary>
        public event Action<float> OnDailyExpensesDeducted;
        #endregion

        #region Configuration
        [Header("Inspection Pricing")]
        [SerializeField] private float _basicInspectionFee = 50f;
        [SerializeField] private float _standardInspectionFee = 75f;
        [SerializeField] private float _advancedInspectionFee = 100f;
        [SerializeField] private float _comprehensiveInspectionFee = 150f;

        [Header("Bonus Settings")]
        [SerializeField] [Range(0f, 1f)] private float _perfectAccuracyBonusPercent = 0.20f;
        [SerializeField] [Range(0f, 1f)] private float _minTipPercent = 0.05f;
        [SerializeField] [Range(0f, 1f)] private float _maxTipPercent = 0.15f;

        [Header("Daily Expenses")]
        [SerializeField] private float _dailyRent = 100f;
        [SerializeField] private float _dailySalary = 50f;
        [SerializeField] private bool _autoDeductDailyExpenses = true;

        [Header("Starting Balance")]
        [SerializeField] private float _startingBalance = 500f;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        #endregion

        #region Properties
        /// <summary>
        /// Current player balance.
        /// </summary>
        public float Balance => _currentBalance;

        /// <summary>
        /// Total income earned today.
        /// </summary>
        public float TodayIncome => _todayIncome;

        /// <summary>
        /// Total expenses spent today.
        /// </summary>
        public float TodayExpenses => _todayExpenses;

        /// <summary>
        /// Total daily expenses (rent + salaries).
        /// </summary>
        public float DailyExpenses => _dailyRent + _dailySalary;

        /// <summary>
        /// Current game day from TimeManager.
        /// </summary>
        public int CurrentGameDay
        {
            get
            {
                if (ServiceLocator.TryGet<ITimeSystem>(out var timeSystem))
                {
                    return timeSystem.CurrentDay;
                }
                return _fallbackGameDay;
            }
        }
        #endregion

        #region Private Fields
        private float _currentBalance;
        private float _todayIncome;
        private float _todayExpenses;
        private int _fallbackGameDay = 1;
        private int _lastProcessedDay = -1;
        private List<Transaction> _transactionHistory;
        private IPlayerDataSystem _playerDataSystem;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize collections
            _transactionHistory = new List<Transaction>();

            // Register with ServiceLocator
            ServiceLocator.Register<IEconomySystem>(this);
        }

        private void Start()
        {
            // Try to get PlayerDataSystem reference
            if (ServiceLocator.TryGet<IPlayerDataSystem>(out var playerDataSystem))
            {
                _playerDataSystem = playerDataSystem;
            }
        }

        private void Update()
        {
            // Check for day change to apply daily expenses
            if (_autoDeductDailyExpenses && CurrentGameDay != _lastProcessedDay)
            {
                if (_lastProcessedDay > 0) // Don't deduct on first day
                {
                    ApplyDailyExpenses();
                }
                _lastProcessedDay = CurrentGameDay;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<IEconomySystem>();
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the economy system.
        /// </summary>
        private void InitializeEconomy()
        {
            // Use PlayerData (JSON) as the primary source of truth.
            // Only fall back to _startingBalance when no saved data exists.
            if (_playerDataSystem != null)
            {
                _currentBalance = _playerDataSystem.PlayerData.money;
            }
            else if (TryLoadBalanceFromPrefs(out float savedBalance))
            {
                _currentBalance = savedBalance;
            }
            else
            {
                _currentBalance = _startingBalance;
            }
            Debug.Log($"[EconomyManager] Loaded balance: ${_currentBalance}");

            // Load saved transaction history
            LoadTransactionHistory();

            // Initialize daily tracking
            _lastProcessedDay = CurrentGameDay;
            ResetDailyTracking();

            Debug.Log($"[EconomyManager] Initialized. Balance: ${_currentBalance}, Daily Expenses: ${DailyExpenses}");
        }
        #endregion

        #region Core Financial Operations
        /// <summary>
        /// Adds income to the player's balance.
        /// </summary>
        /// <param name="amount">Amount to add (must be positive)</param>
        /// <param name="category">Transaction category</param>
        /// <param name="description">Description of the income</param>
        /// <param name="referenceId">Optional reference ID</param>
        /// <returns>True if successful</returns>
        public bool AddIncome(float amount, TransactionCategory category, string description, string referenceId = null)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[EconomyManager] Cannot add non-positive income: ${amount}");
                return false;
            }

            _currentBalance += amount;
            _todayIncome += amount;

            var transaction = new Transaction(
                TransactionType.Income,
                category,
                amount,
                description,
                CurrentGameDay,
                _currentBalance,
                referenceId
            );

            RecordTransaction(transaction);
            UpdatePlayerData();

            if (_debugMode)
            {
                Debug.Log($"[EconomyManager] Income: +${amount} ({category}) - {description}. New Balance: ${_currentBalance}");
            }

            return true;
        }

        /// <summary>
        /// Deducts an expense from the player's balance.
        /// </summary>
        /// <param name="amount">Amount to deduct (must be positive)</param>
        /// <param name="category">Transaction category</param>
        /// <param name="description">Description of the expense</param>
        /// <param name="referenceId">Optional reference ID</param>
        /// <returns>True if successful, false if insufficient funds</returns>
        public bool DeductExpense(float amount, TransactionCategory category, string description, string referenceId = null)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[EconomyManager] Cannot deduct non-positive expense: ${amount}");
                return false;
            }

            if (!CanAfford(amount))
            {
                Debug.LogWarning($"[EconomyManager] Insufficient funds. Need ${amount}, have ${_currentBalance}");
                return false;
            }

            _currentBalance -= amount;
            _todayExpenses += amount;

            var transaction = new Transaction(
                TransactionType.Expense,
                category,
                amount,
                description,
                CurrentGameDay,
                _currentBalance,
                referenceId
            );

            RecordTransaction(transaction);
            UpdatePlayerData();

            if (_debugMode)
            {
                Debug.Log($"[EconomyManager] Expense: -${amount} ({category}) - {description}. New Balance: ${_currentBalance}");
            }

            return true;
        }

        /// <summary>
        /// Checks if the player can afford a purchase.
        /// </summary>
        /// <param name="amount">Amount to check</param>
        /// <returns>True if player has enough money</returns>
        public bool CanAfford(float amount)
        {
            return _currentBalance >= amount;
        }

        /// <summary>
        /// Records a transaction and fires relevant events.
        /// </summary>
        private void RecordTransaction(Transaction transaction)
        {
            _transactionHistory.Add(transaction);
            OnTransactionRecorded?.Invoke(transaction);
            OnBalanceChanged?.Invoke(_currentBalance, transaction);
        }

        /// <summary>
        /// Updates the PlayerData money field.
        /// </summary>
        private void UpdatePlayerData()
        {
            if (_playerDataSystem != null)
            {
                _playerDataSystem.PlayerData.money = _currentBalance;
            }
        }
        #endregion

        #region Transaction History
        /// <summary>
        /// Gets all recorded transactions.
        /// </summary>
        public IReadOnlyList<Transaction> GetTransactionHistory()
        {
            return _transactionHistory.AsReadOnly();
        }

        /// <summary>
        /// Gets transactions filtered by type.
        /// </summary>
        public IEnumerable<Transaction> GetTransactionsByType(TransactionType type)
        {
            return _transactionHistory.Where(t => t.Type == type);
        }

        /// <summary>
        /// Gets transactions filtered by category.
        /// </summary>
        public IEnumerable<Transaction> GetTransactionsByCategory(TransactionCategory category)
        {
            return _transactionHistory.Where(t => t.Category == category);
        }

        /// <summary>
        /// Gets transactions for a specific game day.
        /// </summary>
        public IEnumerable<Transaction> GetTransactionsByDay(int gameDay)
        {
            return _transactionHistory.Where(t => t.GameDay == gameDay);
        }

        /// <summary>
        /// Gets total income for a date range (inclusive).
        /// </summary>
        public float GetTotalIncome(int fromDay, int toDay)
        {
            return _transactionHistory
                .Where(t => t.Type == TransactionType.Income && t.GameDay >= fromDay && t.GameDay <= toDay)
                .Sum(t => t.Amount);
        }

        /// <summary>
        /// Gets total expenses for a date range (inclusive).
        /// </summary>
        public float GetTotalExpenses(int fromDay, int toDay)
        {
            return _transactionHistory
                .Where(t => t.Type == TransactionType.Expense && t.GameDay >= fromDay && t.GameDay <= toDay)
                .Sum(t => t.Amount);
        }
        #endregion

        #region Daily Expenses
        /// <summary>
        /// Calculates and applies daily expenses (rent, salaries, etc.).
        /// </summary>
        public void ApplyDailyExpenses()
        {
            float totalExpenses = DailyExpenses;

            if (totalExpenses <= 0)
            {
                return;
            }

            // Deduct rent
            if (_dailyRent > 0)
            {
                DeductExpense(_dailyRent, TransactionCategory.Rent, "Daily workshop rent", $"rent_day_{CurrentGameDay}");
            }

            // Deduct salaries
            if (_dailySalary > 0)
            {
                DeductExpense(_dailySalary, TransactionCategory.Salary, "Employee salaries", $"salary_day_{CurrentGameDay}");
            }

            OnDailyExpensesDeducted?.Invoke(totalExpenses);

            Debug.Log($"[EconomyManager] Applied daily expenses: ${totalExpenses}. Remaining balance: ${_currentBalance}");
        }

        /// <summary>
        /// Sets the daily rent amount.
        /// </summary>
        public void SetDailyRent(float rent)
        {
            _dailyRent = Mathf.Max(0, rent);
            Debug.Log($"[EconomyManager] Daily rent set to: ${_dailyRent}");
        }

        /// <summary>
        /// Sets the daily salary amount.
        /// </summary>
        public void SetDailySalary(float salary)
        {
            _dailySalary = Mathf.Max(0, salary);
            Debug.Log($"[EconomyManager] Daily salary set to: ${_dailySalary}");
        }

        /// <summary>
        /// Resets daily tracking counters. Call at the start of a new day.
        /// </summary>
        public void ResetDailyTracking()
        {
            _todayIncome = 0f;
            _todayExpenses = 0f;
            Debug.Log($"[EconomyManager] Daily tracking reset for day {CurrentGameDay}");
        }
        #endregion

        #region Fee Calculations
        /// <summary>
        /// Calculates the inspection fee based on inspection type.
        /// </summary>
        /// <param name="inspectionType">Type of inspection</param>
        /// <returns>The base fee for the inspection</returns>
        public float CalculateInspectionFee(InspectionType inspectionType)
        {
            return inspectionType switch
            {
                InspectionType.Basic => _basicInspectionFee,
                InspectionType.Standard => _standardInspectionFee,
                InspectionType.Advanced => _advancedInspectionFee,
                InspectionType.Comprehensive => _comprehensiveInspectionFee,
                _ => _basicInspectionFee
            };
        }

        /// <summary>
        /// Calculates tip amount based on customer satisfaction.
        /// </summary>
        /// <param name="baseAmount">The base inspection amount</param>
        /// <param name="satisfactionPercentage">Satisfaction from 0-100</param>
        /// <returns>Tip amount</returns>
        public float CalculateTip(float baseAmount, float satisfactionPercentage)
        {
            // Map satisfaction (0-100) to tip percentage (5%-15%)
            float normalizedSatisfaction = Mathf.Clamp01(satisfactionPercentage / 100f);
            float tipPercent = Mathf.Lerp(_minTipPercent, _maxTipPercent, normalizedSatisfaction);
            return baseAmount * tipPercent;
        }

        /// <summary>
        /// Calculates bonus for perfect accuracy.
        /// </summary>
        /// <param name="baseAmount">The base inspection amount</param>
        /// <param name="accuracyPercentage">Accuracy from 0-100</param>
        /// <returns>Bonus amount</returns>
        public float CalculateAccuracyBonus(float baseAmount, float accuracyPercentage)
        {
            // Only give bonus for high accuracy (90%+)
            if (accuracyPercentage >= 90f)
            {
                // Scale bonus from 0% at 90% to full bonus at 100%
                float bonusMultiplier = (accuracyPercentage - 90f) / 10f;
                return baseAmount * _perfectAccuracyBonusPercent * bonusMultiplier;
            }
            return 0f;
        }
        #endregion

        #region Convenience Methods
        /// <summary>
        /// Processes payment for an inspection with optional tip and bonus.
        /// </summary>
        /// <param name="inspectionType">Type of inspection performed</param>
        /// <param name="satisfactionPercentage">Customer satisfaction (0-100)</param>
        /// <param name="accuracyPercentage">Inspection accuracy (0-100)</param>
        /// <param name="inspectionId">Reference ID for the inspection</param>
        /// <returns>Total amount earned</returns>
        public float ProcessInspectionPayment(
            InspectionType inspectionType,
            float satisfactionPercentage,
            float accuracyPercentage,
            string inspectionId = null)
        {
            float baseFee = CalculateInspectionFee(inspectionType);
            float totalEarned = 0f;

            // Add base inspection fee
            string categoryStr = inspectionType == InspectionType.Basic ? "Basic" :
                                 inspectionType == InspectionType.Standard ? "Standard" :
                                 inspectionType == InspectionType.Advanced ? "Advanced" : "Comprehensive";

            AddIncome(baseFee, TransactionCategory.Inspection, $"{categoryStr} inspection fee", inspectionId);
            totalEarned += baseFee;

            // Add tip if satisfaction is high enough
            if (satisfactionPercentage >= 50f)
            {
                float tip = CalculateTip(baseFee, satisfactionPercentage);
                if (tip > 0)
                {
                    AddIncome(tip, TransactionCategory.Tip, $"Customer tip ({satisfactionPercentage:F0}% satisfaction)", inspectionId);
                    totalEarned += tip;
                }
            }

            // Add accuracy bonus if applicable
            float bonus = CalculateAccuracyBonus(baseFee, accuracyPercentage);
            if (bonus > 0)
            {
                AddIncome(bonus, TransactionCategory.Bonus, $"Accuracy bonus ({accuracyPercentage:F0}%)", inspectionId);
                totalEarned += bonus;
            }

            return totalEarned;
        }

        /// <summary>
        /// Processes a tool purchase from the store.
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <param name="price">Price of the tool</param>
        /// <returns>True if purchase successful</returns>
        public bool PurchaseTool(string toolName, float price)
        {
            return DeductExpense(price, TransactionCategory.ToolPurchase, $"Purchased tool: {toolName}");
        }

        /// <summary>
        /// Processes an upgrade purchase.
        /// </summary>
        /// <param name="upgradeName">Name of the upgrade</param>
        /// <param name="price">Price of the upgrade</param>
        /// <returns>True if purchase successful</returns>
        public bool PurchaseUpgrade(string upgradeName, float price)
        {
            return DeductExpense(price, TransactionCategory.Upgrade, $"Purchased upgrade: {upgradeName}");
        }

        /// <summary>
        /// Processes a supply purchase.
        /// </summary>
        /// <param name="supplyName">Name of the supply</param>
        /// <param name="price">Price of the supply</param>
        /// <returns>True if purchase successful</returns>
        public bool PurchaseSupplies(string supplyName, float price)
        {
            return DeductExpense(price, TransactionCategory.Supplies, $"Purchased supplies: {supplyName}");
        }

        /// <summary>
        /// Charges a penalty (for incorrect inspections, etc.).
        /// </summary>
        /// <param name="reason">Reason for the penalty</param>
        /// <param name="amount">Penalty amount</param>
        /// <returns>True if penalty applied (may be false if insufficient funds)</returns>
        public bool ChargePenalty(string reason, float amount)
        {
            // Penalties can go into negative balance
            _currentBalance -= amount;
            _todayExpenses += amount;

            var transaction = new Transaction(
                TransactionType.Expense,
                TransactionCategory.Penalty,
                amount,
                $"Penalty: {reason}",
                CurrentGameDay,
                _currentBalance
            );

            RecordTransaction(transaction);
            UpdatePlayerData();

            Debug.Log($"[EconomyManager] Penalty charged: -${amount} - {reason}. New Balance: ${_currentBalance}");
            return true;
        }
        #endregion

        #region Save/Load
        private const string TRANSACTION_SAVE_KEY = "TransactionHistory";
        private const string BALANCE_SAVE_KEY = "Economy_Balance";

        /// <summary>
        /// Saves current balance to PlayerPrefs.
        /// </summary>
        private void SaveBalance()
        {
            PlayerPrefs.SetFloat(BALANCE_SAVE_KEY, _currentBalance);
            PlayerPrefs.Save();
            Debug.Log($"[EconomyManager] Saved balance: ${_currentBalance}");
        }

        /// <summary>
        /// Loads balance from PlayerPrefs.
        /// </summary>
        private bool TryLoadBalanceFromPrefs(out float balance)
        {
            if (PlayerPrefs.HasKey(BALANCE_SAVE_KEY))
            {
                balance = PlayerPrefs.GetFloat(BALANCE_SAVE_KEY);
                return true;
            }
            balance = 0f;
            return false;
        }

        /// <summary>
        /// Saves transaction history to PlayerPrefs (in a real implementation, use a file).
        /// </summary>
        private void SaveTransactionHistory()
        {
            try
            {
                // Convert transactions to DTOs for serialization
                var dtoList = _transactionHistory.Select(TransactionDTO.FromTransaction).ToList();
                string json = JsonUtility.ToJson(new TransactionListWrapper { transactions = dtoList });
                PlayerPrefs.SetString(TRANSACTION_SAVE_KEY, json);
                PlayerPrefs.Save();
                Debug.Log($"[EconomyManager] Saved {_transactionHistory.Count} transactions");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EconomyManager] Failed to save transactions: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads transaction history from PlayerPrefs.
        /// </summary>
        private void LoadTransactionHistory()
        {
            try
            {
                if (PlayerPrefs.HasKey(TRANSACTION_SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(TRANSACTION_SAVE_KEY);
                    var wrapper = JsonUtility.FromJson<TransactionListWrapper>(json);

                    _transactionHistory.Clear();
                    foreach (var dto in wrapper.transactions)
                    {
                        _transactionHistory.Add(dto.ToTransaction());
                    }

                    Debug.Log($"[EconomyManager] Loaded {_transactionHistory.Count} transactions");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EconomyManager] Failed to load transactions: {ex.Message}");
                _transactionHistory = new List<Transaction>();
            }
        }
        #endregion

        #region Debug Methods
        /// <summary>
        /// Logs a summary of all transactions (for debugging).
        /// </summary>
        [ContextMenu("Log Transaction Summary")]
        public void LogTransactionSummary()
        {
            Debug.Log($"[EconomyManager] === Transaction Summary ===");
            Debug.Log($"[EconomyManager] Current Balance: ${_currentBalance}");
            Debug.Log($"[EconomyManager] Today's Income: ${_todayIncome}");
            Debug.Log($"[EconomyManager] Today's Expenses: ${_todayExpenses}");
            Debug.Log($"[EconomyManager] Daily Expenses: ${DailyExpenses}");
            Debug.Log($"[EconomyManager] Total Transactions: {_transactionHistory.Count}");

            float totalIncome = _transactionHistory.Where(t => t.IsIncome).Sum(t => t.Amount);
            float totalExpense = _transactionHistory.Where(t => t.IsExpense).Sum(t => t.Amount);
            Debug.Log($"[EconomyManager] Total Income: ${totalIncome}");
            Debug.Log($"[EconomyManager] Total Expenses: ${totalExpense}");
        }

        /// <summary>
        /// Adds test money (for debugging).
        /// </summary>
        [ContextMenu("Add Test Money ($1000)")]
        public void AddTestMoney()
        {
            AddIncome(1000f, TransactionCategory.OtherIncome, "Debug: Test money added");
        }
        #endregion
    }

    /// <summary>
    /// Wrapper class for transaction list serialization.
    /// </summary>
    [Serializable]
    public class TransactionListWrapper
    {
        public List<TransactionDTO> transactions;
    }
}
