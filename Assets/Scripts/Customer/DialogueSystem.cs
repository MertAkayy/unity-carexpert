using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Report;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Customer
{
    /// <summary>
    /// Types of dialogue that can be displayed.
    /// </summary>
    public enum DialogueType
    {
        Greeting,
        Waiting,
        Impatient,
        Completion,
        Satisfied,
        Dissatisfied,
        Leaving,
        Choice,
        InspectionResult
    }

    /// <summary>
    /// Represents a dialogue choice for player interaction.
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        public string ChoiceText;
        public UnityEvent OnSelected;
        public bool IsRecommended;
    }

    /// <summary>
    /// Interface for the Dialogue System.
    /// </summary>
    public interface IDialogueSystem : ISystem
    {
        /// <summary>Whether a dialogue is currently being displayed.</summary>
        bool IsDialogueActive { get; }

        /// <summary>Current customer being displayed.</summary>
        Customer CurrentCustomer { get; }

        /// <summary>Event fired when dialogue starts.</summary>
        event Action<Customer, DialogueType> OnDialogueStarted;

        /// <summary>Event fired when dialogue ends.</summary>
        event Action<Customer> OnDialogueEnded;

        /// <summary>Event fired when a choice is made.</summary>
        event Action<DialogueChoice> OnChoiceMade;

        /// <summary>Shows dialogue for a customer.</summary>
        void ShowDialogue(Customer customer, string text, DialogueType type);

        /// <summary>Shows dialogue with choices.</summary>
        void ShowDialogueWithChoices(Customer customer, string text, List<DialogueChoice> choices);

        /// <summary>Shows inspection results to customer.</summary>
        void ShowInspectionResults(Customer customer, InspectionReport report);

        /// <summary>Hides the current dialogue.</summary>
        void HideDialogue();

        /// <summary>Advances to the next dialogue line.</summary>
        void AdvanceDialogue();

        /// <summary>Shows a notification message.</summary>
        void ShowNotification(string message, float duration = 3f);
    }

    /// <summary>
    /// Manages dialogue UI for customer interactions.
    /// Handles displaying customer dialogue, choices, and inspection results.
    /// </summary>
    public class DialogueSystem : MonoBehaviour, IDialogueSystem
    {
        #region ISystem Implementation

        public int Priority => 50;

        public void OnRegistered()
        {
            Debug.Log("[DialogueSystem] Registered with ServiceLocator");
        }

        public void Initialize()
        {
            InitializeUI();
            Debug.Log("[DialogueSystem] Initialized");
        }

        public void Shutdown()
        {
            HideDialogue();
            Debug.Log("[DialogueSystem] Shutdown complete");
        }

        #endregion

        #region Events

        public event Action<Customer, DialogueType> OnDialogueStarted;
        public event Action<Customer> OnDialogueEnded;
        public event Action<DialogueChoice> OnChoiceMade;

        #endregion

        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private GameObject _dialoguePanel;
        [SerializeField] private Text _customerNameText;
        [SerializeField] private Text _dialogueText;
        [SerializeField] private Image _customerPortrait;
        [SerializeField] private GameObject _choicesContainer;
        [SerializeField] private GameObject _choiceButtonPrefab;
        [SerializeField] private GameObject _inspectionResultsPanel;
        [SerializeField] private Text _resultsSummaryText;
        [SerializeField] private GameObject _continueIndicator;
        [SerializeField] private Button _continueButton;

        [Header("Animation Settings")]
        [SerializeField] private float _typewriterSpeed = 0.03f;
        [SerializeField] private float _fadeSpeed = 0.3f;
        [SerializeField] private AnimationCurve _fadeCurve;

        [Header("Notification")]
        [SerializeField] private GameObject _notificationPanel;
        [SerializeField] private Text _notificationText;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region Properties

        public bool IsDialogueActive { get; private set; }
        public Customer CurrentCustomer { get; private set; }

        #endregion

        #region Private Fields

        private DialogueType _currentDialogueType;
        private List<DialogueChoice> _currentChoices = new List<DialogueChoice>();
        private Coroutine _typewriterCoroutine;
        private Coroutine _notificationCoroutine;
        private bool _isTypewriting = false;
        private string _fullDialogueText;
        private CanvasGroup _dialogueCanvasGroup;
        private Queue<string> _dialogueQueue = new Queue<string>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Register with ServiceLocator
            ServiceLocator.Register<IDialogueSystem>(this);

            // Get canvas group
            if (_dialoguePanel != null)
            {
                _dialogueCanvasGroup = _dialoguePanel.GetComponent<CanvasGroup>();
                if (_dialogueCanvasGroup == null)
                {
                    _dialogueCanvasGroup = _dialoguePanel.AddComponent<CanvasGroup>();
                }
            }
        }

        private void Start()
        {
            // Setup continue button
            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        private void OnDestroy()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }

        #endregion

        #region Initialization

        private void InitializeUI()
        {
            // Hide all panels initially
            if (_dialoguePanel != null)
            {
                _dialoguePanel.SetActive(false);
            }

            if (_inspectionResultsPanel != null)
            {
                _inspectionResultsPanel.SetActive(false);
            }

            if (_notificationPanel != null)
            {
                _notificationPanel.SetActive(false);
            }

            if (_choicesContainer != null)
            {
                _choicesContainer.SetActive(false);
            }

            if (_continueIndicator != null)
            {
                _continueIndicator.SetActive(false);
            }
        }

        #endregion

        #region IDialogueSystem Implementation

        /// <summary>
        /// Shows dialogue for a customer.
        /// </summary>
        public void ShowDialogue(Customer customer, string text, DialogueType type)
        {
            if (_debugMode)
            {
                Debug.Log($"[DialogueSystem] Showing dialogue: {type} - {text}");
            }

            CurrentCustomer = customer;
            _currentDialogueType = type;

            // Setup UI
            SetupDialogueUI(customer);

            // Show panel
            ShowPanel();

            // Start typewriter effect
            StartTypewriter(text);

            // Fire event
            OnDialogueStarted?.Invoke(customer, type);
        }

        /// <summary>
        /// Shows dialogue with player choices.
        /// </summary>
        public void ShowDialogueWithChoices(Customer customer, string text, List<DialogueChoice> choices)
        {
            CurrentCustomer = customer;
            _currentDialogueType = DialogueType.Choice;
            _currentChoices = choices ?? new List<DialogueChoice>();

            // Setup UI
            SetupDialogueUI(customer);

            // Show panel
            ShowPanel();

            // Start typewriter
            StartTypewriter(text);

            // Setup choices after text completes
            StartCoroutine(SetupChoicesAfterTypewriter());

            // Fire event
            OnDialogueStarted?.Invoke(customer, DialogueType.Choice);
        }

        /// <summary>
        /// Shows inspection results to the customer.
        /// </summary>
        public void ShowInspectionResults(Customer customer, InspectionReport report)
        {
            if (customer == null || report == null)
            {
                Debug.LogWarning("[DialogueSystem] Cannot show results - customer or report is null");
                return;
            }

            CurrentCustomer = customer;
            _currentDialogueType = DialogueType.InspectionResult;

            // Setup UI
            SetupDialogueUI(customer);

            // Show panel
            ShowPanel();

            // Format results summary
            string resultsText = FormatInspectionResults(report);

            // Start typewriter
            StartTypewriter(resultsText);

            // Show inspection panel if available
            if (_inspectionResultsPanel != null)
            {
                _inspectionResultsPanel.SetActive(true);
                if (_resultsSummaryText != null)
                {
                    _resultsSummaryText.text = GetResultsSummary(report);
                }
            }

            // Fire event
            OnDialogueStarted?.Invoke(customer, DialogueType.InspectionResult);
        }

        /// <summary>
        /// Hides the current dialogue.
        /// </summary>
        public void HideDialogue()
        {
            if (!IsDialogueActive) return;

            StartCoroutine(HidePanelCoroutine());

            var previousCustomer = CurrentCustomer;
            CurrentCustomer = null;
            _currentChoices.Clear();

            // Hide inspection results
            if (_inspectionResultsPanel != null)
            {
                _inspectionResultsPanel.SetActive(false);
            }

            // Hide choices
            if (_choicesContainer != null)
            {
                _choicesContainer.SetActive(false);
            }

            // Fire event
            OnDialogueEnded?.Invoke(previousCustomer);
        }

        /// <summary>
        /// Advances to the next dialogue line.
        /// </summary>
        public void AdvanceDialogue()
        {
            if (_isTypewriting)
            {
                // Complete typewriter immediately
                CompleteTypewriter();
                return;
            }

            if (_dialogueQueue.Count > 0)
            {
                // Show next line
                string nextLine = _dialogueQueue.Dequeue();
                StartTypewriter(nextLine);
            }
            else if (_currentChoices.Count == 0)
            {
                // No more dialogue, hide
                HideDialogue();
            }
        }

        /// <summary>
        /// Shows a notification message.
        /// </summary>
        public void ShowNotification(string message, float duration = 3f)
        {
            if (_notificationCoroutine != null)
            {
                StopCoroutine(_notificationCoroutine);
            }

            _notificationCoroutine = StartCoroutine(ShowNotificationCoroutine(message, duration));
        }

        #endregion

        #region Private Methods

        private void SetupDialogueUI(Customer customer)
        {
            // Set customer name
            if (_customerNameText != null && customer?.Data != null)
            {
                _customerNameText.text = customer.Data.CustomerName;
            }

            // Set portrait
            if (_customerPortrait != null && customer?.Data?.Portrait != null)
            {
                _customerPortrait.sprite = customer.Data.Portrait;
                _customerPortrait.gameObject.SetActive(true);
            }
            else if (_customerPortrait != null)
            {
                _customerPortrait.gameObject.SetActive(false);
            }
        }

        private void ShowPanel()
        {
            IsDialogueActive = true;

            if (_dialoguePanel != null)
            {
                _dialoguePanel.SetActive(true);
            }

            // Fade in
            if (_dialogueCanvasGroup != null)
            {
                StartCoroutine(FadeInCoroutine());
            }
        }

        private IEnumerator HidePanelCoroutine()
        {
            IsDialogueActive = false;

            // Fade out
            if (_dialogueCanvasGroup != null)
            {
                yield return FadeOutCoroutine();
            }

            if (_dialoguePanel != null)
            {
                _dialoguePanel.SetActive(false);
            }
        }

        private void StartTypewriter(string text)
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }

            _fullDialogueText = text;
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(text));

            // Hide continue indicator during typewriting
            if (_continueIndicator != null)
            {
                _continueIndicator.SetActive(false);
            }
        }

        private void CompleteTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }

            _isTypewriting = false;

            if (_dialogueText != null)
            {
                _dialogueText.text = _fullDialogueText;
            }

            // Show continue indicator or choices
            if (_currentChoices.Count > 0)
            {
                ShowChoices();
            }
            else if (_continueIndicator != null)
            {
                _continueIndicator.SetActive(true);
            }
        }

        private IEnumerator TypewriterCoroutine(string text)
        {
            _isTypewriting = true;

            if (_dialogueText != null)
            {
                _dialogueText.text = "";

                foreach (char c in text)
                {
                    _dialogueText.text += c;
                    yield return new WaitForSeconds(_typewriterSpeed);
                }
            }

            _isTypewriting = false;

            // Show continue indicator or choices
            if (_currentChoices.Count > 0)
            {
                ShowChoices();
            }
            else if (_continueIndicator != null)
            {
                _continueIndicator.SetActive(true);
            }
        }

        private IEnumerator SetupChoicesAfterTypewriter()
        {
            yield return new WaitUntil(() => !_isTypewriting);
            ShowChoices();
        }

        private void ShowChoices()
        {
            if (_choicesContainer == null || _choiceButtonPrefab == null) return;

            // Clear existing choices
            foreach (Transform child in _choicesContainer.transform)
            {
                Destroy(child.gameObject);
            }

            _choicesContainer.SetActive(true);

            // Create choice buttons
            foreach (var choice in _currentChoices)
            {
                GameObject buttonObj = Instantiate(_choiceButtonPrefab, _choicesContainer.transform);
                Button button = buttonObj.GetComponent<Button>();
                Text buttonText = buttonObj.GetComponentInChildren<Text>();

                if (buttonText != null)
                {
                    buttonText.text = choice.ChoiceText;

                    // Highlight recommended choice
                    if (choice.IsRecommended)
                    {
                        buttonText.fontStyle = FontStyle.Bold;
                    }
                }

                if (button != null)
                {
                    DialogueChoice capturedChoice = choice;
                    button.onClick.AddListener(() => OnChoiceSelected(capturedChoice));
                }
            }
        }

        private void OnChoiceSelected(DialogueChoice choice)
        {
            if (_debugMode)
            {
                Debug.Log($"[DialogueSystem] Choice selected: {choice.ChoiceText}");
            }

            // Hide choices
            if (_choicesContainer != null)
            {
                _choicesContainer.SetActive(false);
            }

            // Fire event
            OnChoiceMade?.Invoke(choice);

            // Execute choice action
            choice.OnSelected?.Invoke();

            // Clear choices
            _currentChoices.Clear();
        }

        private void OnContinueClicked()
        {
            AdvanceDialogue();
        }

        private string FormatInspectionResults(InspectionReport report)
        {
            string text = $"Here are the inspection results:\n\n";
            text += $"Overall Condition: {report.ConditionRating}\n";
            text += $"Accuracy: {report.AccuracyPercentage:F1}%\n\n";

            if (report.FoundIssuesCount > 0)
            {
                text += $"Issues Found: {report.FoundIssuesCount}\n";
            }

            if (report.MissedIssuesCount > 0)
            {
                text += $"Issues Missed: {report.MissedIssuesCount}\n";
            }

            if (report.FalsePositivesCount > 0)
            {
                text += $"False Positives: {report.FalsePositivesCount}\n";
            }

            text += $"\nEstimated Repair Cost: ${report.EstimatedRepairCost:F2}";

            return text;
        }

        private string GetResultsSummary(InspectionReport report)
        {
            return $"Inspection Complete\n" +
                   $"Accuracy: {report.AccuracyPercentage:F1}%\n" +
                   $"Condition: {report.ConditionRating}";
        }

        private IEnumerator ShowNotificationCoroutine(string message, float duration)
        {
            if (_notificationPanel != null && _notificationText != null)
            {
                _notificationText.text = message;
                _notificationPanel.SetActive(true);

                yield return new WaitForSeconds(duration);

                _notificationPanel.SetActive(false);
            }
        }

        private IEnumerator FadeInCoroutine()
        {
            if (_dialogueCanvasGroup == null) yield break;

            float elapsed = 0f;
            while (elapsed < _fadeSpeed)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _fadeSpeed;
                _dialogueCanvasGroup.alpha = _fadeCurve?.Evaluate(t) ?? t;
                yield return null;
            }

            _dialogueCanvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOutCoroutine()
        {
            if (_dialogueCanvasGroup == null) yield break;

            float elapsed = 0f;
            while (elapsed < _fadeSpeed)
            {
                elapsed += Time.deltaTime;
                float t = 1f - (elapsed / _fadeSpeed);
                _dialogueCanvasGroup.alpha = _fadeCurve?.Evaluate(t) ?? t;
                yield return null;
            }

            _dialogueCanvasGroup.alpha = 0f;
        }

        #endregion

        #region Public Helper Methods

        /// <summary>
        /// Queues multiple dialogue lines to be shown in sequence.
        /// </summary>
        public void QueueDialogue(Customer customer, List<string> lines, DialogueType type)
        {
            if (lines == null || lines.Count == 0) return;

            _dialogueQueue.Clear();

            foreach (string line in lines)
            {
                _dialogueQueue.Enqueue(line);
            }

            // Show first line
            string firstLine = _dialogueQueue.Dequeue();
            ShowDialogue(customer, firstLine, type);
        }

        /// <summary>
        /// Shows a simple message without customer context.
        /// </summary>
        public void ShowMessage(string message, float duration = 3f)
        {
            ShowNotification(message, duration);
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Test Dialogue")]
        private void DebugTestDialogue()
        {
            ShowNotification("This is a test notification!", 3f);
        }

        [ContextMenu("Debug: Test Choice")]
        private void DebugTestChoice()
        {
            var choices = new List<DialogueChoice>
            {
                new DialogueChoice { ChoiceText = "Option A - Accept", IsRecommended = true },
                new DialogueChoice { ChoiceText = "Option B - Decline", IsRecommended = false },
                new DialogueChoice { ChoiceText = "Option C - Negotiate", IsRecommended = false }
            };

            // Create a dummy customer for testing
            // ShowDialogueWithChoices(null, "What would you like to do?", choices);
        }

        #endregion
    }
}
