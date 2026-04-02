using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Economy;
using Inspection;
using Progression;
using Report;
using UnityEngine;

namespace Customer
{
    /// <summary>
    /// Interface for the Customer Manager system.
    /// </summary>
    public interface ICustomerManager : ISystem
    {
        /// <summary>Current customer being served.</summary>
        Customer CurrentCustomer { get; }

        /// <summary>Number of customers waiting in queue.</summary>
        int QueueCount { get; }

        /// <summary>Maximum queue size.</summary>
        int MaxQueueSize { get; }

        /// <summary>All customers currently in the workshop.</summary>
        IReadOnlyList<Customer> AllCustomers { get; }

        /// <summary>Customers waiting in queue.</summary>
        IReadOnlyList<Customer> WaitingQueue { get; }

        /// <summary>Whether a customer is currently being served.</summary>
        bool IsServingCustomer { get; }

        /// <summary>Event fired when a customer arrives.</summary>
        event Action<Customer> OnCustomerArrived;

        /// <summary>Event fired when a customer starts being served.</summary>
        event Action<Customer> OnCustomerServiceStarted;

        /// <summary>Event fired when a customer leaves.</summary>
        event Action<Customer, float> OnCustomerLeft;

        /// <summary>Event fired when queue changes.</summary>
        event Action<int> OnQueueChanged;

        /// <summary>Spawns a new customer.</summary>
        Customer SpawnCustomer();

        /// <summary>Spawns a customer with specific data.</summary>
        Customer SpawnCustomer(CustomerData customerData, CustomerRequest request);

        /// <summary>Starts serving the next customer in queue.</summary>
        Customer StartServingNextCustomer();

        /// <summary>Completes service for the current customer.</summary>
        void CompleteCurrentCustomerService();

        /// <summary>Gets the next customer in queue without starting service.</summary>
        Customer GetNextInQueue();

        /// <summary>Removes a customer from the queue.</summary>
        bool RemoveFromQueue(Customer customer);

        /// <summary>Gets total customers served today.</summary>
        int GetCustomersServedToday();

        /// <summary>Gets average satisfaction for today.</summary>
        float GetAverageSatisfactionToday();

        /// <summary>Gets total earnings from customers today.</summary>
        float GetTotalEarningsToday();
    }

    /// <summary>
    /// Manages customer spawning, queue, and service flow.
    /// Coordinates between customer system and other game services.
    /// </summary>
    public class CustomerManager : MonoBehaviour, ICustomerManager
    {
        #region ISystem Implementation

        public int Priority => 40; // After VehicleFactory (30) but before InspectionService (100)

        public void OnRegistered()
        {
            Debug.Log("[CustomerManager] Registered with ServiceLocator");
        }

        public void Initialize()
        {
            GetServiceReferences();
            SubscribeToEvents();
            StartCoroutine(SpawnInitialCustomer());
            Debug.Log("[CustomerManager] Initialized");
        }

        public void Shutdown()
        {
            UnsubscribeFromEvents();
            ClearAllCustomers();
            Debug.Log("[CustomerManager] Shutdown complete");
        }

        #endregion

        #region Events

        public event Action<Customer> OnCustomerArrived;
        public event Action<Customer> OnCustomerServiceStarted;
        public event Action<Customer, float> OnCustomerLeft;
        public event Action<int> OnQueueChanged;

        #endregion

        #region Serialized Fields

        [Header("Customer Pool")]
        [SerializeField] private List<CustomerData> _customerPool = new List<CustomerData>();
        [SerializeField] private GameObject _customerPrefab;

        [Header("Queue Settings")]
        [SerializeField] private int _maxQueueSize = 5;
        [SerializeField] private float _minSpawnInterval = 30f;
        [SerializeField] private float _maxSpawnInterval = 90f;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _waitingArea;
        [SerializeField] private Transform _serviceArea;
        [SerializeField] private Transform _exitPoint;

        [Header("Auto-Spawn")]
        [SerializeField] private bool _autoSpawnCustomers = true;
        [SerializeField] private int _minCustomersInQueue = 1;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region Properties

        public Customer CurrentCustomer => _currentCustomer;
        public int QueueCount => _customerQueue.Count;
        public int MaxQueueSize => _maxQueueSize;
        public bool IsServingCustomer => _currentCustomer != null && _currentCustomer.State == CustomerState.BeingServed;

        public IReadOnlyList<Customer> AllCustomers => _allCustomers.AsReadOnly();
        public IReadOnlyList<Customer> WaitingQueue => _customerQueue.ToList().AsReadOnly();

        #endregion

        #region Private Fields

        private Customer _currentCustomer;
        private readonly Queue<Customer> _customerQueue = new Queue<Customer>();
        private readonly List<Customer> _allCustomers = new List<Customer>();

        private IInspectionService _inspectionService;
        private IReportService _reportService;
        private IEconomySystem _economySystem;
        private IProgressionManager _progressionManager;
        private IVehicleFactory _vehicleFactory;
        private IDialogueSystem _dialogueSystem;
        private ITimeSystem _timeSystem;

        private Coroutine _spawnCoroutine;
        private float _nextSpawnTime;

        // Daily statistics
        private int _customersServedToday;
        private float _totalSatisfactionToday;
        private float _totalEarningsToday;
        private int _currentGameDay;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Register with ServiceLocator
            ServiceLocator.Register<ICustomerManager>(this);
        }

        private void Update()
        {
            UpdateSpawnTimer();
            UpdateCustomerPatience();
        }

        private void OnDestroy()
        {
            if (ServiceLocator.IsRegistered<ICustomerManager>())
            {
                ServiceLocator.Unregister<ICustomerManager>();
            }
        }

        #endregion

        #region Initialization

        private void GetServiceReferences()
        {
            ServiceLocator.TryGet(out _inspectionService);
            ServiceLocator.TryGet(out _reportService);
            ServiceLocator.TryGet(out _economySystem);
            ServiceLocator.TryGet(out _progressionManager);
            ServiceLocator.TryGet(out _vehicleFactory);
            ServiceLocator.TryGet(out _dialogueSystem);
            ServiceLocator.TryGet(out _timeSystem);
        }

        private void SubscribeToEvents()
        {
            // Subscribe to time system for day changes
            if (_timeSystem != null)
            {
                // Would subscribe to day change events here
            }

            // Subscribe to inspection events
            if (_inspectionService != null)
            {
                _inspectionService.OnInspectionEnded += HandleInspectionEnded;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_inspectionService != null)
            {
                _inspectionService.OnInspectionEnded -= HandleInspectionEnded;
            }
        }

        private IEnumerator SpawnInitialCustomer()
        {
            yield return new WaitForSeconds(2f); // Wait for systems to initialize

            if (_autoSpawnCustomers && QueueCount < _minCustomersInQueue)
            {
                SpawnCustomer();
            }

            // Start auto-spawn coroutine
            if (_autoSpawnCustomers)
            {
                _spawnCoroutine = StartCoroutine(AutoSpawnCoroutine());
            }
        }

        #endregion

        #region ICustomerManager Implementation

        /// <summary>
        /// Spawns a new random customer.
        /// </summary>
        public Customer SpawnCustomer()
        {
            if (QueueCount >= _maxQueueSize)
            {
                Debug.LogWarning("[CustomerManager] Queue is full, cannot spawn more customers");
                return null;
            }

            // Get random customer data
            CustomerData customerData = GetRandomCustomerData();
            if (customerData == null)
            {
                Debug.LogWarning("[CustomerManager] No customer data available");
                return null;
            }

            // Create request for the customer
            CustomerRequest request = CreateCustomerRequest(customerData);

            return SpawnCustomer(customerData, request);
        }

        /// <summary>
        /// Spawns a customer with specific data and request.
        /// </summary>
        public Customer SpawnCustomer(CustomerData customerData, CustomerRequest request)
        {
            if (QueueCount >= _maxQueueSize)
            {
                Debug.LogWarning("[CustomerManager] Queue is full");
                return null;
            }

            // Instantiate customer
            Customer customer = CreateCustomerInstance();
            if (customer == null)
            {
                Debug.LogError("[CustomerManager] Failed to create customer instance");
                return null;
            }

            // Initialize customer
            customer.Initialize(customerData, request);

            // Subscribe to customer events
            SubscribeToCustomerEvents(customer);

            // Position customer
            PositionCustomer(customer, CustomerState.Waiting);

            // Add to queue and tracking
            _customerQueue.Enqueue(customer);
            _allCustomers.Add(customer);

            // Fire events
            OnCustomerArrived?.Invoke(customer);
            OnQueueChanged?.Invoke(QueueCount);

            // Show greeting dialogue
            if (_dialogueSystem != null)
            {
                _dialogueSystem.ShowDialogue(customer, customerData.GetRandomGreeting(), DialogueType.Greeting);
            }

            if (_debugMode)
            {
                Debug.Log($"[CustomerManager] Customer spawned: {customerData.CustomerName}. Queue: {QueueCount}");
            }

            return customer;
        }

        /// <summary>
        /// Starts serving the next customer in queue.
        /// </summary>
        public Customer StartServingNextCustomer()
        {
            if (_customerQueue.Count == 0)
            {
                Debug.LogWarning("[CustomerManager] No customers in queue");
                return null;
            }

            if (_currentCustomer != null && _currentCustomer.State != CustomerState.Leaving)
            {
                Debug.LogWarning("[CustomerManager] Already serving a customer");
                return null;
            }

            _currentCustomer = _customerQueue.Dequeue();

            // Update queue
            OnQueueChanged?.Invoke(QueueCount);

            // Position at service area
            PositionCustomer(_currentCustomer, CustomerState.BeingServed);

            // Start service
            _currentCustomer.StartService();

            // Spawn vehicle for customer
            SpawnVehicleForCustomer(_currentCustomer);

            // Fire event
            OnCustomerServiceStarted?.Invoke(_currentCustomer);

            if (_debugMode)
            {
                Debug.Log($"[CustomerManager] Started serving: {_currentCustomer.Data.CustomerName}");
            }

            return _currentCustomer;
        }

        /// <summary>
        /// Completes service for the current customer.
        /// </summary>
        public void CompleteCurrentCustomerService()
        {
            if (_currentCustomer == null)
            {
                Debug.LogWarning("[CustomerManager] No current customer to complete");
                return;
            }

            // Generate final report
            InspectionReport report = null;
            if (_reportService != null && _currentCustomer.Request?.AssignedVehicle != null)
            {
                report = _reportService.GenerateReportFromVehicle(_currentCustomer.Request.AssignedVehicle);
            }

            // Complete service
            _currentCustomer.CompleteService(report);

            // Show results to customer
            if (_dialogueSystem != null && report != null)
            {
                _dialogueSystem.ShowInspectionResults(_currentCustomer, report);
            }

            // Process payment
            float payment = _currentCustomer.ProcessPayment();

            // Update statistics
            _customersServedToday++;
            _totalSatisfactionToday += _currentCustomer.Satisfaction;
            _totalEarningsToday += payment;

            // Award XP
            AwardXPForService(_currentCustomer, report);

            if (_debugMode)
            {
                Debug.Log($"[CustomerManager] Service completed for {_currentCustomer.Data.CustomerName}. Payment: ${payment:F2}");
            }

            // Make customer leave after delay
            StartCoroutine(MakeCustomerLeaveAfterDelay(_currentCustomer, 3f));
        }

        /// <summary>
        /// Gets the next customer in queue without starting service.
        /// </summary>
        public Customer GetNextInQueue()
        {
            if (_customerQueue.Count == 0) return null;
            return _customerQueue.Peek();
        }

        /// <summary>
        /// Removes a customer from the queue.
        /// </summary>
        public bool RemoveFromQueue(Customer customer)
        {
            if (customer == null) return false;

            // Create new queue without the customer
            var newQueue = new Queue<Customer>();
            bool found = false;

            while (_customerQueue.Count > 0)
            {
                var c = _customerQueue.Dequeue();
                if (c == customer)
                {
                    found = true;
                }
                else
                {
                    newQueue.Enqueue(c);
                }
            }

            // Replace queue
            while (newQueue.Count > 0)
            {
                _customerQueue.Enqueue(newQueue.Dequeue());
            }

            if (found)
            {
                OnQueueChanged?.Invoke(QueueCount);
                CleanupCustomer(customer);
            }

            return found;
        }

        /// <summary>
        /// Gets total customers served today.
        /// </summary>
        public int GetCustomersServedToday()
        {
            return _customersServedToday;
        }

        /// <summary>
        /// Gets average satisfaction for today.
        /// </summary>
        public float GetAverageSatisfactionToday()
        {
            if (_customersServedToday == 0) return 0f;
            return _totalSatisfactionToday / _customersServedToday;
        }

        /// <summary>
        /// Gets total earnings from customers today.
        /// </summary>
        public float GetTotalEarningsToday()
        {
            return _totalEarningsToday;
        }

        #endregion

        #region Private Methods

        private CustomerData GetRandomCustomerData()
        {
            if (_customerPool == null || _customerPool.Count == 0)
            {
                Debug.LogWarning("[CustomerManager] Customer pool is empty");
                return null;
            }

            // Weighted random selection based on personality
            return Utilities.WeightedRandom(_customerPool, c => Mathf.RoundToInt(GetCustomerWeight(c) * 100));
        }

        private float GetCustomerWeight(CustomerData data)
        {
            // More friendly customers appear more often
            return data.Personality switch
            {
                CustomerPersonality.Friendly => 2f,
                CustomerPersonality.Neutral => 1.5f,
                CustomerPersonality.Novice => 1.2f,
                CustomerPersonality.Impatient => 0.8f,
                CustomerPersonality.Skeptical => 0.7f,
                CustomerPersonality.Expert => 0.5f,
                _ => 1f
            };
        }

        private CustomerRequest CreateCustomerRequest(CustomerData customerData)
        {
            // Get player level for appropriate vehicle selection
            int playerLevel = _progressionManager?.CurrentLevel ?? 1;

            // Select inspection type based on customer preferences
            InspectionType inspectionType = SelectInspectionType(customerData);

            // Select vehicle type based on player level
            VehicleData vehicleType = null;
            if (_vehicleFactory != null)
            {
                var availableVehicles = _vehicleFactory.GetAvailableVehiclesForLevel(playerLevel);
                if (availableVehicles.Count > 0)
                {
                    vehicleType = Utilities.WeightedRandom(availableVehicles, v => Mathf.RoundToInt(v.SpawnWeight * 100));
                }
            }

            // Create the request
            return new CustomerRequest(customerData, inspectionType, vehicleType);
        }

        private InspectionType SelectInspectionType(CustomerData customerData)
        {
            if (customerData.PreferredInspectionTypes == null || customerData.PreferredInspectionTypes.Count == 0)
            {
                return InspectionType.Standard;
            }

            // Random selection from preferred types
            return customerData.PreferredInspectionTypes[UnityEngine.Random.Range(0, customerData.PreferredInspectionTypes.Count)];
        }

        private Customer CreateCustomerInstance()
        {
            GameObject customerObj;

            if (_customerPrefab != null)
            {
                customerObj = Instantiate(_customerPrefab);
            }
            else
            {
                customerObj = new GameObject("Customer");
            }

            Customer customer = customerObj.GetComponent<Customer>();
            if (customer == null)
            {
                customer = customerObj.AddComponent<Customer>();
            }

            return customer;
        }

        private void SubscribeToCustomerEvents(Customer customer)
        {
            customer.OnPatienceDepleted += HandlePatienceDepleted;
            customer.OnReadyToLeave += HandleCustomerReadyToLeave;
            customer.OnSatisfactionChanged += HandleSatisfactionChanged;
        }

        private void UnsubscribeFromCustomerEvents(Customer customer)
        {
            customer.OnPatienceDepleted -= HandlePatienceDepleted;
            customer.OnReadyToLeave -= HandleCustomerReadyToLeave;
            customer.OnSatisfactionChanged -= HandleSatisfactionChanged;
        }

        private void PositionCustomer(Customer customer, CustomerState state)
        {
            if (customer == null) return;

            Transform targetTransform = state switch
            {
                CustomerState.Waiting => _waitingArea,
                CustomerState.BeingServed => _serviceArea,
                CustomerState.Leaving => _exitPoint,
                _ => _spawnPoint
            };

            if (targetTransform != null)
            {
                customer.transform.position = targetTransform.position;
                customer.transform.rotation = targetTransform.rotation;
            }
        }

        private void SpawnVehicleForCustomer(Customer customer)
        {
            if (_vehicleFactory == null || customer?.Request == null) return;

            int playerLevel = _progressionManager?.CurrentLevel ?? 1;
            Vector3 spawnPosition = _serviceArea != null ? _serviceArea.position + Vector3.right * 3f : Vector3.zero;

            Vehicle vehicle = _vehicleFactory.SpawnVehicleForCustomer(
                customer.Request.RequestedVehicleType,
                playerLevel,
                spawnPosition,
                Quaternion.identity
            );

            customer.Request.AssignedVehicle = vehicle;

            // Start inspection
            if (_inspectionService != null && vehicle != null)
            {
                _inspectionService.StartInspection(vehicle);
            }

            if (_debugMode)
            {
                Debug.Log($"[CustomerManager] Spawned vehicle for {customer.Data.CustomerName}");
            }
        }

        private void HandleInspectionEnded(Vehicle vehicle)
        {
            if (_currentCustomer == null) return;

            // Check if this is the current customer's vehicle
            if (_currentCustomer.Request?.AssignedVehicle == vehicle)
            {
                CompleteCurrentCustomerService();
            }
        }

        private void HandlePatienceDepleted(Customer customer)
        {
            if (_debugMode)
            {
                Debug.Log($"[CustomerManager] Customer patience depleted: {customer.Data.CustomerName}");
            }

            // Customer leaves angry
            customer.Leave();

            // Remove from queue if still in it
            RemoveFromQueue(customer);

            // If this was current customer, clear reference
            if (_currentCustomer == customer)
            {
                _currentCustomer = null;
            }
        }

        private void HandleCustomerReadyToLeave(Customer customer)
        {
            if (_debugMode)
            {
                Debug.Log($"[CustomerManager] Customer ready to leave: {customer.Data.CustomerName}");
            }

            // Fire event with satisfaction
            OnCustomerLeft?.Invoke(customer, customer.Satisfaction);

            // Return vehicle to factory
            if (_vehicleFactory != null && customer.Request?.AssignedVehicle != null)
            {
                _vehicleFactory.ReturnVehicle(customer.Request.AssignedVehicle);
            }

            // Clear current customer reference
            if (_currentCustomer == customer)
            {
                _currentCustomer = null;
            }

            // Cleanup
            StartCoroutine(CleanupCustomerAfterDelay(customer, 2f));
        }

        private void HandleSatisfactionChanged(Customer customer, float satisfaction)
        {
            if (_debugMode)
            {
                Debug.Log($"[CustomerManager] Satisfaction changed: {customer.Data.CustomerName} - {satisfaction:F1}%");
            }
        }

        private void AwardXPForService(Customer customer, InspectionReport report)
        {
            if (_progressionManager == null) return;

            float accuracy = report?.AccuracyPercentage / 100f ?? 0f;
            int issuesFound = report?.FoundIssuesCount ?? 0;
            int totalIssues = (report?.FoundIssuesCount ?? 0) + (report?.MissedIssuesCount ?? 0);

            // Base XP
            int baseXP = 25;

            // Accuracy bonus
            int accuracyXP = Mathf.RoundToInt(accuracy * 50);

            // Customer satisfaction bonus
            int satisfactionXP = Mathf.RoundToInt((customer.Satisfaction / 100f) * 25);

            // Speed bonus
            int speedXP = customer.PatiencePercent > 0.5f ? 15 : 0;

            int totalXP = baseXP + accuracyXP + satisfactionXP + speedXP;

            // Apply customer XP modifier
            totalXP = Mathf.RoundToInt(totalXP * (customer.Data?.XPModifier ?? 1f));

            _progressionManager.AddXP(totalXP, $"Customer service: {customer.Data?.CustomerName}");
        }

        private IEnumerator MakeCustomerLeaveAfterDelay(Customer customer, float delay)
        {
            yield return new WaitForSeconds(delay);
            customer.Leave();
        }

        private IEnumerator CleanupCustomerAfterDelay(Customer customer, float delay)
        {
            yield return new WaitForSeconds(delay);
            CleanupCustomer(customer);
        }

        private void CleanupCustomer(Customer customer)
        {
            if (customer == null) return;

            UnsubscribeFromCustomerEvents(customer);
            _allCustomers.Remove(customer);

            if (customer.gameObject != null)
            {
                Destroy(customer.gameObject);
            }
        }

        private void ClearAllCustomers()
        {
            // Clear queue
            while (_customerQueue.Count > 0)
            {
                var customer = _customerQueue.Dequeue();
                CleanupCustomer(customer);
            }

            // Clear current
            if (_currentCustomer != null)
            {
                CleanupCustomer(_currentCustomer);
                _currentCustomer = null;
            }

            // Clear all tracking
            foreach (var customer in _allCustomers.ToList())
            {
                CleanupCustomer(customer);
            }

            _allCustomers.Clear();
        }

        #endregion

        #region Spawn Management

        private void UpdateSpawnTimer()
        {
            // Check for day change to reset statistics
            int currentDay = _timeSystem?.CurrentDay ?? 0;
            if (currentDay != _currentGameDay)
            {
                ResetDailyStatistics();
                _currentGameDay = currentDay;
            }
        }

        private void UpdateCustomerPatience()
        {
            // Update patience for all waiting customers
            foreach (var customer in _customerQueue)
            {
                // Patience is updated in Customer.Update()
                // We just need to check for state changes here
            }
        }

        private IEnumerator AutoSpawnCoroutine()
        {
            while (_autoSpawnCustomers)
            {
                // Calculate next spawn interval
                float interval = UnityEngine.Random.Range(_minSpawnInterval, _maxSpawnInterval);
                yield return new WaitForSeconds(interval);

                // Spawn if needed
                if (QueueCount < _minCustomersInQueue && QueueCount < _maxQueueSize)
                {
                    SpawnCustomer();
                }
            }
        }

        private void ResetDailyStatistics()
        {
            _customersServedToday = 0;
            _totalSatisfactionToday = 0f;
            _totalEarningsToday = 0f;

            if (_debugMode)
            {
                Debug.Log("[CustomerManager] Daily statistics reset");
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Spawn Customer")]
        private void DebugSpawnCustomer()
        {
            SpawnCustomer();
        }

        [ContextMenu("Debug: Start Serving Next")]
        private void DebugStartServingNext()
        {
            StartServingNextCustomer();
        }

        [ContextMenu("Debug: Complete Current Service")]
        private void DebugCompleteCurrentService()
        {
            CompleteCurrentCustomerService();
        }

        [ContextMenu("Debug: Log Status")]
        private void DebugLogStatus()
        {
            Debug.Log($"[CustomerManager] Status:");
            Debug.Log($"  Queue: {QueueCount}/{MaxQueueSize}");
            Debug.Log($"  Current Customer: {(_currentCustomer?.Data?.CustomerName ?? "None")}");
            Debug.Log($"  Customers Served Today: {_customersServedToday}");
            Debug.Log($"  Average Satisfaction: {GetAverageSatisfactionToday():F1}%");
            Debug.Log($"  Total Earnings Today: ${_totalEarningsToday:F2}");
        }

        #endregion
    }
}
