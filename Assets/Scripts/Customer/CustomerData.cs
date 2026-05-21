using System;
using System.Collections.Generic;
using UnityEngine;
using Economy;

namespace Customer
{
    /// <summary>
    /// Personality types that affect customer behavior and dialogue.
    /// </summary>
    public enum CustomerPersonality
    {
        /// <summary>Friendly and patient customer</summary>
        Friendly,
        /// <summary>Neutral, business-like customer</summary>
        Neutral,
        /// <summary>Impatient and demanding customer</summary>
        Impatient,
        /// <summary>Suspicious customer who questions everything</summary>
        Skeptical,
        /// <summary>Knowledgeable customer with high expectations</summary>
        Expert,
        /// <summary>New car owner, unsure and needs guidance</summary>
        Novice
    }

    /// <summary>
    /// Patience levels determining how long a customer will wait.
    /// </summary>
    public enum PatienceLevel
    {
        VeryLow = 5,    // 300 seconds  (5 min)
        Low = 7,        // 420 seconds  (7 min)
        Medium = 10,    // 600 seconds  (10 min)
        High = 13,      // 780 seconds  (13 min)
        VeryHigh = 15   // 900 seconds  (15 min)
    }

    /// <summary>
    /// ScriptableObject template for customer profiles.
    /// Defines customer personality, dialogue, and preferences.
    /// </summary>
    [CreateAssetMenu(fileName = "CustomerData", menuName = "Customer/CustomerData")]
    public class CustomerData : ScriptableObject
    {
        #region Basic Information

        [Header("Basic Information")]
        [SerializeField] private string customerName;
        [SerializeField] [TextArea(2, 4)] private string description;
        [SerializeField] private Sprite portrait;

        #endregion

        #region Personality Settings

        [Header("Personality")]
        [SerializeField] private CustomerPersonality personality = CustomerPersonality.Neutral;
        [SerializeField] private PatienceLevel patienceLevel = PatienceLevel.Medium;
        [SerializeField] [Range(0f, 1f)] private float tipTendency = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float satisfactionThreshold = 0.7f;

        #endregion

        #region Inspection Preferences

        [Header("Inspection Preferences")]
        [SerializeField] private List<InspectionType> preferredInspectionTypes = new List<InspectionType>
        {
            InspectionType.Standard
        };
        [SerializeField] private bool prefersBasicInspections = false;
        [SerializeField] private bool willingToPayExtraForSpeed = false;

        #endregion

        #region Reward Modifiers

        [Header("Reward Modifiers")]
        [SerializeField] [Range(0.5f, 1.5f)] private float baseRewardModifier = 1.0f;
        [SerializeField] [Range(0.5f, 2.0f)] private float tipModifier = 1.0f;
        [SerializeField] [Range(0f, 1f)] private float xpModifier = 1.0f;
        [SerializeField] [Range(0f, 0.5f)] private float patienceBonusReward = 0.1f;

        #endregion

        #region Dialogue

        [Header("Greeting Dialogue")]
        [SerializeField] [TextArea(1, 3)] private string[] greetingLines = new string[]
        {
            "Hello, I need my car inspected.",
            "Hi there! Can you take a look at my vehicle?",
            "Good day. I have a car that needs inspection."
        };

        [Header("Waiting Dialogue")]
        [SerializeField] [TextArea(1, 3)] private string[] waitingLines = new string[]
        {
            "Is it taking long?",
            "How much longer will this take?",
            "I'm waiting..."
        };

        [Header("Impatient Dialogue")]
        [SerializeField] [TextArea(1, 3)] private string[] impatientLines = new string[]
        {
            "This is taking too long!",
            "I don't have all day!",
            "Can you hurry up please?"
        };

        [Header("Completion Dialogue")]
        [SerializeField] [TextArea(1, 3)] private string[] completionLines = new string[]
        {
            "Thank you for the inspection!",
            "Great work, thanks!",
            "Appreciate your help."
        };

        [Header("Satisfied Dialogue")]
        [SerializeField] [TextArea(1, 3)] private string[] satisfiedLines = new string[]
        {
            "Excellent work! I'm very satisfied.",
            "You did a great job!",
            "This is exactly what I needed."
        };

        [Header("Dissatisfied Dialogue")]
        [SerializeField] [TextArea(1, 3)] private string[] dissatisfiedLines = new string[]
        {
            "I'm not happy with this service.",
            "This isn't what I expected.",
            "I expected better quality."
        };

        [Header("Leaving Dialogue")]
        [SerializeField] [TextArea(1, 3)] private string[] leavingLines = new string[]
        {
            "Goodbye!",
            "Take care!",
            "See you next time."
        };

        #endregion

        #region Properties

        /// <summary>Display name of the customer.</summary>
        public string CustomerName => customerName;

        /// <summary>Description of the customer.</summary>
        public string Description => description;

        /// <summary>Portrait sprite for UI display.</summary>
        public Sprite Portrait => portrait;

        /// <summary>Customer personality type.</summary>
        public CustomerPersonality Personality => personality;

        /// <summary>Patience level determining wait time tolerance.</summary>
        public PatienceLevel Patience => patienceLevel;

        /// <summary>Tendency to give tips (0-1).</summary>
        public float TipTendency => tipTendency;

        /// <summary>Satisfaction threshold for positive feedback.</summary>
        public float SatisfactionThreshold => satisfactionThreshold;

        /// <summary>List of preferred inspection types.</summary>
        public List<InspectionType> PreferredInspectionTypes => preferredInspectionTypes;

        /// <summary>Whether this customer prefers basic inspections.</summary>
        public bool PrefersBasicInspections => prefersBasicInspections;

        /// <summary>Whether customer will pay extra for faster service.</summary>
        public bool WillingToPayExtraForSpeed => willingToPayExtraForSpeed;

        /// <summary>Modifier applied to base reward amount.</summary>
        public float BaseRewardModifier => baseRewardModifier;

        /// <summary>Modifier applied to tip calculations.</summary>
        public float TipModifier => tipModifier;

        /// <summary>Modifier applied to XP gains.</summary>
        public float XPModifier => xpModifier;

        /// <summary>Bonus reward for completing service before patience runs low.</summary>
        public float PatienceBonusReward => patienceBonusReward;

        #endregion

        #region Dialogue Methods

        /// <summary>Gets a random greeting line.</summary>
        public string GetRandomGreeting()
        {
            return GetRandomLine(greetingLines);
        }

        /// <summary>Gets a random waiting line.</summary>
        public string GetRandomWaiting()
        {
            return GetRandomLine(waitingLines);
        }

        /// <summary>Gets a random impatient line.</summary>
        public string GetRandomImpatient()
        {
            return GetRandomLine(impatientLines);
        }

        /// <summary>Gets a random completion line.</summary>
        public string GetRandomCompletion()
        {
            return GetRandomLine(completionLines);
        }

        /// <summary>Gets a random satisfied line.</summary>
        public string GetRandomSatisfied()
        {
            return GetRandomLine(satisfiedLines);
        }

        /// <summary>Gets a random dissatisfied line.</summary>
        public string GetRandomDissatisfied()
        {
            return GetRandomLine(dissatisfiedLines);
        }

        /// <summary>Gets a random leaving line.</summary>
        public string GetRandomLeaving()
        {
            return GetRandomLine(leavingLines);
        }

        private string GetRandomLine(string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return "...";

            return lines[UnityEngine.Random.Range(0, lines.Length)];
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the maximum wait time in seconds based on patience level.
        /// </summary>
        public float GetMaxWaitTime()
        {
            return (int)Patience * 60f; // Each level = 60 seconds
        }

        /// <summary>
        /// Calculates the final reward modifier based on service speed.
        /// </summary>
        public float CalculateSpeedBonus(float remainingPatiencePercent)
        {
            if (remainingPatiencePercent >= 0.8f)
            {
                return 1.0f + patienceBonusReward;
            }
            return 1.0f;
        }

        /// <summary>
        /// Checks if this customer prefers a specific inspection type.
        /// </summary>
        public bool PrefersInspectionType(InspectionType type)
        {
            return preferredInspectionTypes.Contains(type);
        }

        #endregion
    }
}
