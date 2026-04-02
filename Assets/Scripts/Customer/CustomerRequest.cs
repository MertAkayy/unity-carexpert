using System;
using System.Collections.Generic;
using Economy;
using UnityEngine;

namespace Customer
{
    /// <summary>
    /// Special requirements that can be attached to a customer request.
    /// </summary>
    [Flags]
    public enum SpecialRequirement
    {
        None = 0,
        /// <summary>Customer wants the inspection done quickly</summary>
        RushJob = 1 << 0,
        /// <summary>Customer is concerned about specific issue</summary>
        SpecificConcern = 1 << 1,
        /// <summary>Customer needs detailed documentation</summary>
        DetailedReport = 1 << 2,
        /// <summary>Customer wants photos of all issues</summary>
        PhotoDocumentation = 1 << 3,
        /// <summary>Customer is selling the vehicle and needs thorough check</summary>
        PreSaleInspection = 1 << 4,
        /// <summary>Customer just bought the vehicle and wants inspection</summary>
        PostPurchaseInspection = 1 << 5,
        /// <summary>Insurance required inspection</summary>
        InsuranceInspection = 1 << 6
    }

    /// <summary>
    /// Request priority levels.
    /// </summary>
    public enum RequestPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }

    /// <summary>
    /// Represents a customer's inspection request.
    /// Contains vehicle requirements, inspection type, and deadline.
    /// </summary>
    [Serializable]
    public class CustomerRequest
    {
        #region Properties

        /// <summary>Unique identifier for this request.</summary>
        public Guid RequestId { get; private set; }

        /// <summary>The customer who made this request.</summary>
        public CustomerData Customer { get; private set; }

        /// <summary>Requested vehicle type (null = any available).</summary>
        public VehicleData RequestedVehicleType { get; private set; }

        /// <summary>Type of inspection requested.</summary>
        public InspectionType InspectionType { get; private set; }

        /// <summary>Special requirements for this request.</summary>
        public SpecialRequirement SpecialRequirements { get; private set; }

        /// <summary>Priority of this request.</summary>
        public RequestPriority Priority { get; private set; }

        /// <summary>Deadline in game minutes (0 = no deadline).</summary>
        public float DeadlineMinutes { get; private set; }

        /// <summary>Time when the request was made.</summary>
        public DateTime RequestTime { get; private set; }

        /// <summary>Customer's specific concern description.</summary>
        public string SpecificConcernDescription { get; private set; }

        /// <summary>Bonus reward for completing within deadline.</summary>
        public float DeadlineBonus { get; private set; }

        /// <summary>Penalty for missing deadline.</summary>
        public float DeadlinePenalty { get; private set; }

        /// <summary>Whether this request has been accepted.</summary>
        public bool IsAccepted { get; set; }

        /// <summary>Whether this request has been completed.</summary>
        public bool IsCompleted { get; set; }

        /// <summary>Whether this request was cancelled.</summary>
        public bool IsCancelled { get; set; }

        /// <summary>The actual vehicle assigned to this request.</summary>
        public Vehicle AssignedVehicle { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new customer request with the specified parameters.
        /// </summary>
        public CustomerRequest(
            CustomerData customer,
            InspectionType inspectionType,
            VehicleData vehicleType = null,
            SpecialRequirement specialRequirements = SpecialRequirement.None,
            RequestPriority priority = RequestPriority.Normal,
            float deadlineMinutes = 0f,
            string specificConcern = null)
        {
            RequestId = Guid.NewGuid();
            Customer = customer;
            RequestedVehicleType = vehicleType;
            InspectionType = inspectionType;
            SpecialRequirements = specialRequirements;
            Priority = priority;
            DeadlineMinutes = deadlineMinutes;
            SpecificConcernDescription = specificConcern;
            RequestTime = DateTime.Now;
            IsAccepted = false;
            IsCompleted = false;
            IsCancelled = false;

            // Calculate deadline bonus/penalty based on priority
            CalculateDeadlineRewards();
        }

        /// <summary>
        /// Parameterless constructor for serialization.
        /// </summary>
        public CustomerRequest()
        {
            RequestId = Guid.NewGuid();
            RequestTime = DateTime.Now;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Calculates the deadline bonus and penalty based on priority.
        /// </summary>
        private void CalculateDeadlineRewards()
        {
            DeadlineBonus = Priority switch
            {
                RequestPriority.Urgent => 50f,
                RequestPriority.High => 25f,
                RequestPriority.Normal => 10f,
                RequestPriority.Low => 5f,
                _ => 0f
            };

            DeadlinePenalty = Priority switch
            {
                RequestPriority.Urgent => 30f,
                RequestPriority.High => 20f,
                RequestPriority.Normal => 10f,
                RequestPriority.Low => 5f,
                _ => 0f
            };
        }

        /// <summary>
        /// Checks if this request has a special requirement.
        /// </summary>
        public bool HasRequirement(SpecialRequirement requirement)
        {
            return (SpecialRequirements & requirement) != 0;
        }

        /// <summary>
        /// Checks if the deadline has passed.
        /// </summary>
        public bool IsDeadlinePassed(float elapsedMinutes)
        {
            if (DeadlineMinutes <= 0) return false;
            return elapsedMinutes > DeadlineMinutes;
        }

        /// <summary>
        /// Gets the remaining time in minutes.
        /// </summary>
        public float GetRemainingMinutes(float elapsedMinutes)
        {
            if (DeadlineMinutes <= 0) return float.MaxValue;
            return Mathf.Max(0, DeadlineMinutes - elapsedMinutes);
        }

        /// <summary>
        /// Gets the deadline progress (0-1, where 1 is expired).
        /// </summary>
        public float GetDeadlineProgress(float elapsedMinutes)
        {
            if (DeadlineMinutes <= 0) return 0f;
            return Mathf.Clamp01(elapsedMinutes / DeadlineMinutes);
        }

        /// <summary>
        /// Calculates the base reward for this request.
        /// </summary>
        public float CalculateBaseReward(IEconomySystem economySystem)
        {
            if (economySystem == null) return 0f;

            float baseFee = economySystem.CalculateInspectionFee(InspectionType);

            // Apply customer modifier
            baseFee *= Customer?.BaseRewardModifier ?? 1.0f;

            // Apply priority modifier
            float priorityModifier = Priority switch
            {
                RequestPriority.Urgent => 1.5f,
                RequestPriority.High => 1.25f,
                RequestPriority.Normal => 1.0f,
                RequestPriority.Low => 0.9f,
                _ => 1.0f
            };

            return baseFee * priorityModifier;
        }

        /// <summary>
        /// Marks this request as completed.
        /// </summary>
        public void Complete()
        {
            IsCompleted = true;
            IsAccepted = false;
        }

        /// <summary>
        /// Cancels this request.
        /// </summary>
        public void Cancel()
        {
            IsCancelled = true;
            IsAccepted = false;
        }

        /// <summary>
        /// Gets a description of the request for UI display.
        /// </summary>
        public string GetDescription()
        {
            string vehicleDesc = RequestedVehicleType != null
                ? RequestedVehicleType.VehicleName
                : "Any Vehicle";

            string requirementsDesc = SpecialRequirements != SpecialRequirement.None
                ? $" ({SpecialRequirements})"
                : "";

            return $"{InspectionType} inspection for {vehicleDesc}{requirementsDesc}";
        }

        /// <summary>
        /// Gets the priority as a display string.
        /// </summary>
        public string GetPriorityDisplay()
        {
            return Priority switch
            {
                RequestPriority.Urgent => "URGENT",
                RequestPriority.High => "High Priority",
                RequestPriority.Normal => "Normal",
                RequestPriority.Low => "Low Priority",
                _ => "Normal"
            };
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a basic inspection request.
        /// </summary>
        public static CustomerRequest CreateBasicRequest(CustomerData customer, VehicleData vehicleType = null)
        {
            return new CustomerRequest(customer, InspectionType.Basic, vehicleType);
        }

        /// <summary>
        /// Creates a standard inspection request.
        /// </summary>
        public static CustomerRequest CreateStandardRequest(CustomerData customer, VehicleData vehicleType = null)
        {
            return new CustomerRequest(customer, InspectionType.Standard, vehicleType);
        }

        /// <summary>
        /// Creates an advanced inspection request.
        /// </summary>
        public static CustomerRequest CreateAdvancedRequest(CustomerData customer, VehicleData vehicleType = null)
        {
            return new CustomerRequest(customer, InspectionType.Advanced, vehicleType);
        }

        /// <summary>
        /// Creates a comprehensive inspection request.
        /// </summary>
        public static CustomerRequest CreateComprehensiveRequest(CustomerData customer, VehicleData vehicleType = null)
        {
            return new CustomerRequest(customer, InspectionType.Comprehensive, vehicleType);
        }

        /// <summary>
        /// Creates a rush job request with deadline.
        /// </summary>
        public static CustomerRequest CreateRushRequest(
            CustomerData customer,
            InspectionType inspectionType,
            float deadlineMinutes,
            VehicleData vehicleType = null)
        {
            return new CustomerRequest(
                customer,
                inspectionType,
                vehicleType,
                SpecialRequirement.RushJob,
                RequestPriority.Urgent,
                deadlineMinutes);
        }

        /// <summary>
        /// Creates a pre-sale inspection request.
        /// </summary>
        public static CustomerRequest CreatePreSaleRequest(CustomerData customer, VehicleData vehicleType = null)
        {
            return new CustomerRequest(
                customer,
                InspectionType.Comprehensive,
                vehicleType,
                SpecialRequirement.PreSaleInspection | SpecialRequirement.DetailedReport,
                RequestPriority.High);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Converts this request to a DTO for serialization.
        /// </summary>
        public CustomerRequestDto ToDTO()
        {
            return new CustomerRequestDto
            {
                requestId = RequestId.ToString(),
                customerName = Customer?.CustomerName ?? "Unknown",
                inspectionType = (int)InspectionType,
                specialRequirements = (int)SpecialRequirements,
                priority = (int)Priority,
                deadlineMinutes = DeadlineMinutes,
                requestTime = RequestTime.ToString("o"),
                specificConcernDescription = SpecificConcernDescription,
                isAccepted = IsAccepted,
                isCompleted = IsCompleted,
                isCancelled = IsCancelled
            };
        }

        #endregion
    }

    /// <summary>
    /// Data transfer object for CustomerRequest serialization.
    /// </summary>
    [Serializable]
    public class CustomerRequestDto
    {
        public string requestId;
        public string customerName;
        public int inspectionType;
        public int specialRequirements;
        public int priority;
        public float deadlineMinutes;
        public string requestTime;
        public string specificConcernDescription;
        public bool isAccepted;
        public bool isCompleted;
        public bool isCancelled;
    }
}
