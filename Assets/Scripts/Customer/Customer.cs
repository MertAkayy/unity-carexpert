using System;
using Core;
using Economy;
using Inspection;
using Report;
using UnityEngine;

namespace Customer
{
    /// <summary>
    /// States a customer can be in during their visit.
    /// </summary>
    public enum CustomerState
    {
        /// <summary>Customer is arriving at the workshop</summary>
        Arriving,
        /// <summary>Customer is waiting in queue</summary>
        Waiting,
        /// <summary>Customer is being served</summary>
        BeingServed,
        /// <summary>Customer is reviewing the inspection results</summary>
        ReviewingResults,
        /// <summary>Customer is leaving the workshop</summary>
        Leaving
    }

    /// <summary>
    /// Represents a customer instance in the game.
    /// Tracks state, satisfaction, patience, and handles interactions.
    /// </summary>
    public class Customer : MonoBehaviour, IInteractable
    {
        #region Events

        /// <summary>Fired when customer state changes.</summary>
        public event Action<Customer, CustomerState, CustomerState> OnStateChanged;

        /// <summary>Fired when satisfaction level changes.</summary>
        public event Action<Customer, float> OnSatisfactionChanged;

        /// <summary>Fired when patience depletes completely.</summary>
        public event Action<Customer> OnPatienceDepleted;

        /// <summary>Fired when customer is ready to leave.</summary>
        public event Action<Customer> OnReadyToLeave;

        #endregion

        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region Properties

        /// <summary>Unique identifier for this customer instance.</summary>
        public Guid CustomerInstanceId { get; private set; }

        /// <summary>Reference to customer data template.</summary>
        public CustomerData Data { get; private set; }

        /// <summary>The customer's inspection request.</summary>
        public CustomerRequest Request { get; private set; }

        /// <summary>Current state of the customer.</summary>
        public CustomerState State
        {
            get => _currentState;
            private set
            {
                if (_currentState != value)
                {
                    CustomerState previousState = _currentState;
                    _currentState = value;
                    OnStateChanged?.Invoke(this, previousState, value);
                    HandleStateChange(previousState, value);
                }
            }
        }

        /// <summary>Current satisfaction level (0-100).</summary>
        public float Satisfaction
        {
            get => _satisfaction;
            private set
            {
                float previousSatisfaction = _satisfaction;
                _satisfaction = Mathf.Clamp(value, 0f, 100f);
                if (Math.Abs(previousSatisfaction - _satisfaction) > 0.1f)
                {
                    OnSatisfactionChanged?.Invoke(this, _satisfaction);
                }
            }
        }

        /// <summary>Current patience remaining in seconds.</summary>
        public float PatienceRemaining
        {
            get => _patienceRemaining;
            private set
            {
                _patienceRemaining = Mathf.Max(0, value);
                if (_patienceRemaining <= 0 && !_patienceDepleted)
                {
                    _patienceDepleted = true;
                    OnPatienceDepleted?.Invoke(this);
                }
            }
        }

        /// <summary>Patience as percentage (0-1).</summary>
        public float PatiencePercent => MaxPatience > 0 ? PatienceRemaining / MaxPatience : 0f;

        /// <summary>Maximum patience in seconds.</summary>
        public float MaxPatience { get; private set; }

        /// <summary>Whether patience has been depleted.</summary>
        public bool IsPatienceDepleted => _patienceDepleted;

        /// <summary>Time spent waiting in queue.</summary>
        public float WaitingTime { get; private set; }

        /// <summary>Time spent being served.</summary>
        public float ServiceTime { get; private set; }

        /// <summary>Whether the customer has been served.</summary>
        public bool HasBeenServed { get; private set; }

        /// <summary>Final inspection report (if completed).</summary>
        public InspectionReport FinalReport { get; private set; }

        /// <summary>Total payment received from this customer.</summary>
        public float TotalPayment { get; private set; }

        #endregion

        #region Private Fields

        private CustomerState _currentState = CustomerState.Arriving;
        private float _satisfaction = 100f;
        private float _patienceRemaining;
        private bool _patienceDepleted = false;
        private float _patienceDecayRate = 1f;
        private ITimeSystem _timeSystem;
        private IInspectionService _inspectionService;
        private IReportService _reportService;
        private IEconomySystem _economySystem;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CustomerInstanceId = Guid.NewGuid();
        }

        private void Update()
        {
            UpdatePatience();
            UpdateState();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the customer with data and request.
        /// </summary>
        public void Initialize(CustomerData data, CustomerRequest request)
        {
            Data = data;
            Request = request;

            // Set max patience based on customer data
            MaxPatience = data.GetMaxWaitTime();
            PatienceRemaining = MaxPatience;

            // Set patience decay rate based on personality
            _patienceDecayRate = GetPatienceDecayRate(data.Personality);

            // Initialize satisfaction based on initial impression
            Satisfaction = CalculateInitialSatisfaction();

            // Get service references
            if (ServiceLocator.TryGet(out _timeSystem)) { }
            if (ServiceLocator.TryGet(out _inspectionService)) { }
            if (ServiceLocator.TryGet(out _reportService)) { }
            if (ServiceLocator.TryGet(out _economySystem)) { }

            State = CustomerState.Waiting;

            if (_debugMode)
            {
                Debug.Log($"[Customer] Initialized {Data.CustomerName} with {Request.GetDescription()}");
            }
        }

        private float GetPatienceDecayRate(CustomerPersonality personality)
        {
            return personality switch
            {
                CustomerPersonality.Impatient => 1.5f,
                CustomerPersonality.Skeptical => 1.2f,
                CustomerPersonality.Expert => 1.1f,
                CustomerPersonality.Neutral => 1.0f,
                CustomerPersonality.Novice => 0.9f,
                CustomerPersonality.Friendly => 0.8f,
                _ => 1.0f
            };
        }

        private float CalculateInitialSatisfaction()
        {
            float baseSatisfaction = 75f;

            // Adjust based on personality
            baseSatisfaction += Data.Personality switch
            {
                CustomerPersonality.Friendly => 10f,
                CustomerPersonality.Novice => 5f,
                CustomerPersonality.Skeptical => -10f,
                CustomerPersonality.Impatient => -5f,
                _ => 0f
            };

            return Mathf.Clamp(baseSatisfaction, 0f, 100f);
        }

        #endregion

        #region State Management

        private void HandleStateChange(CustomerState previousState, CustomerState newState)
        {
            if (_debugMode)
            {
                Debug.Log($"[Customer] {Data.CustomerName} state changed: {previousState} -> {newState}");
            }

            switch (newState)
            {
                case CustomerState.Waiting:
                    // Reset waiting time when entering wait state
                    WaitingTime = 0f;
                    break;

                case CustomerState.BeingServed:
                    // Stop waiting, start service timer
                    ServiceTime = 0f;
                    break;

                case CustomerState.ReviewingResults:
                    // Show dialogue about completion
                    ShowCompletionDialogue();
                    break;

                case CustomerState.Leaving:
                    // Finalize everything
                    OnReadyToLeave?.Invoke(this);
                    break;
            }
        }

        private void UpdateState()
        {
            switch (State)
            {
                case CustomerState.Waiting:
                    WaitingTime += Time.deltaTime;
                    break;

                case CustomerState.BeingServed:
                    ServiceTime += Time.deltaTime;
                    break;
            }
        }

        private void UpdatePatience()
        {
            if (State == CustomerState.Waiting || State == CustomerState.BeingServed)
            {
                PatienceRemaining -= Time.deltaTime * _patienceDecayRate;

                // Satisfaction decreases as patience drops
                if (PatiencePercent < 0.5f)
                {
                    Satisfaction -= Time.deltaTime * 2f;
                }
                else if (PatiencePercent < 0.25f)
                {
                    Satisfaction -= Time.deltaTime * 5f;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called when the player presses E on this customer.
        /// Waiting → Start Service, BeingServed → Complete Service.
        /// </summary>
        public void Interact()
        {
            if (!ServiceLocator.TryGet<ICustomerManager>(out var customerManager))
            {
                Debug.LogWarning("[Customer] Cannot interact - CustomerManager not found");
                return;
            }

            switch (State)
            {
                case CustomerState.Waiting:
                    customerManager.StartServingNextCustomer();
                    Debug.Log($"[Customer] Player started serving {Data?.CustomerName}");
                    break;

                case CustomerState.BeingServed:
                    customerManager.CompleteCurrentCustomerService();
                    Debug.Log($"[Customer] Player completed service for {Data?.CustomerName}");
                    break;

                default:
                    Debug.Log($"[Customer] Cannot interact - customer is {State}");
                    break;
            }
        }

        /// <summary>
        /// Starts serving this customer.
        /// </summary>
        public void StartService()
        {
            if (State != CustomerState.Waiting)
            {
                Debug.LogWarning($"[Customer] Cannot start service - customer is not waiting (State: {State})");
                return;
            }

            State = CustomerState.BeingServed;
            HasBeenServed = true;

            // Bonus for starting quickly
            if (PatiencePercent > 0.8f)
            {
                Satisfaction += 5f;
            }

            if (_debugMode)
            {
                Debug.Log($"[Customer] Started serving {Data.CustomerName}");
            }
        }

        /// <summary>
        /// Completes the service for this customer.
        /// </summary>
        public void CompleteService(InspectionReport report)
        {
            if (State != CustomerState.BeingServed)
            {
                Debug.LogWarning($"[Customer] Cannot complete service - customer not being served (State: {State})");
                return;
            }

            FinalReport = report;
            State = CustomerState.ReviewingResults;

            // Calculate satisfaction based on report accuracy
            CalculateFinalSatisfaction(report);

            if (_debugMode)
            {
                Debug.Log($"[Customer] Completed service for {Data.CustomerName}. Accuracy: {report.AccuracyPercentage:F1}%");
            }
        }

        /// <summary>
        /// Processes the payment and tips from this customer.
        /// </summary>
        public float ProcessPayment()
        {
            if (_economySystem == null || Request == null)
            {
                Debug.LogWarning("[Customer] Cannot process payment - missing economy system or request");
                return 0f;
            }

            // Calculate base reward
            float baseReward = Request.CalculateBaseReward(_economySystem);

            // Calculate tip based on satisfaction
            float tip = 0f;
            if (Satisfaction >= 50f)
            {
                tip = _economySystem.CalculateTip(baseReward, Satisfaction);
                tip *= Data.TipModifier;
            }

            // Calculate speed bonus
            float speedBonus = Data.CalculateSpeedBonus(PatiencePercent) * baseReward - baseReward;

            // Process payment through economy system
            string inspectionId = FinalReport?.ReportId.ToString() ?? Guid.NewGuid().ToString();
            TotalPayment = _economySystem.ProcessInspectionPayment(
                Request.InspectionType,
                Satisfaction,
                FinalReport?.AccuracyPercentage ?? 0f,
                inspectionId
            );

            // Add speed bonus if applicable
            if (speedBonus > 0)
            {
                _economySystem.AddIncome(
                    speedBonus,
                    TransactionCategory.Bonus,
                    "Speed bonus",
                    inspectionId
                );
                TotalPayment += speedBonus;
            }

            if (_debugMode)
            {
                Debug.Log($"[Customer] {Data.CustomerName} paid ${TotalPayment} (Tip: ${tip:F2}, Speed Bonus: ${speedBonus:F2})");
            }

            return TotalPayment;
        }

        /// <summary>
        /// Makes the customer leave the workshop.
        /// </summary>
        public void Leave()
        {
            State = CustomerState.Leaving;

            if (_debugMode)
            {
                Debug.Log($"[Customer] {Data.CustomerName} is leaving. Final satisfaction: {Satisfaction:F1}%");
            }
        }

        /// <summary>
        /// Increases customer satisfaction.
        /// </summary>
        public void IncreaseSatisfaction(float amount)
        {
            Satisfaction += amount;
        }

        /// <summary>
        /// Decreases customer satisfaction.
        /// </summary>
        public void DecreaseSatisfaction(float amount)
        {
            Satisfaction -= amount;
        }

        /// <summary>
        /// Gets the current dialogue based on state.
        /// </summary>
        public string GetCurrentDialogue()
        {
            if (Data == null) return "...";

            return State switch
            {
                CustomerState.Arriving => Data.GetRandomGreeting(),
                CustomerState.Waiting when PatiencePercent < 0.3f => Data.GetRandomImpatient(),
                CustomerState.Waiting => Data.GetRandomWaiting(),
                CustomerState.ReviewingResults when Satisfaction >= Data.SatisfactionThreshold => Data.GetRandomSatisfied(),
                CustomerState.ReviewingResults => Data.GetRandomDissatisfied(),
                CustomerState.Leaving => Data.GetRandomLeaving(),
                _ => "..."
            };
        }

        #endregion

        #region Private Methods

        private void CalculateFinalSatisfaction(InspectionReport report)
        {
            if (report == null) return;

            float accuracy = report.AccuracyPercentage;

            // Base satisfaction from accuracy
            float accuracyContribution = accuracy * 0.5f; // Up to 50 points from accuracy

            // Patience bonus
            float patienceContribution = PatiencePercent * 20f; // Up to 20 points from patience

            // Speed bonus
            float speedContribution = PatiencePercent > 0.5f ? 10f : 0f;

            // Calculate new satisfaction
            float newSatisfaction = 50f + accuracyContribution + patienceContribution + speedContribution;

            // Adjust based on customer personality
            newSatisfaction *= Data.Personality switch
            {
                CustomerPersonality.Friendly => 1.1f,
                CustomerPersonality.Skeptical => 0.85f,
                CustomerPersonality.Expert => 0.9f,
                CustomerPersonality.Impatient => 0.9f,
                _ => 1.0f
            };

            Satisfaction = Mathf.Clamp(newSatisfaction, 0f, 100f);
        }

        private void ShowCompletionDialogue()
        {
            // This would trigger the dialogue system
            string dialogue = Satisfaction >= Data.SatisfactionThreshold
                ? Data.GetRandomSatisfied()
                : Data.GetRandomDissatisfied();

            if (_debugMode)
            {
                Debug.Log($"[Customer] {Data.CustomerName}: \"{dialogue}\"");
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Force Complete Service")]
        private void DebugForceCompleteService()
        {
            if (_reportService != null)
            {
                var report = _reportService.GenerateReportFromVehicle(Request?.AssignedVehicle);
                CompleteService(report);
            }
        }

        [ContextMenu("Debug: Force Leave")]
        private void DebugForceLeave()
        {
            Leave();
        }

        [ContextMenu("Debug: Log Status")]
        private void DebugLogStatus()
        {
            Debug.Log($"[Customer] {Data?.CustomerName ?? "Unknown"}");
            Debug.Log($"  State: {State}");
            Debug.Log($"  Satisfaction: {Satisfaction:F1}%");
            Debug.Log($"  Patience: {PatiencePercent * 100:F1}% ({PatienceRemaining:F1}s / {MaxPatience:F1}s)");
            Debug.Log($"  Waiting Time: {WaitingTime:F1}s");
            Debug.Log($"  Service Time: {ServiceTime:F1}s");
        }

        #endregion
    }
}
