using System;
using System.Collections.Generic;
using PlayerScripts;
using UnityEngine;
using ToolScripts.Base;

namespace ToolScripts.UI
{
    /// <summary>
    /// Centralized UI manager for all tool inspections.
    /// Handles progress bars, result display, and notifications.
    /// </summary>
    public class ToolUIManager : MonoBehaviour
    {
        private static ToolUIManager _instance;
        public static ToolUIManager Instance => _instance;

        [Header("UI References")]
        [SerializeField] private RectTransform progressPanel;
        [SerializeField] private RectTransform progressBarFill;
        [SerializeField] private RectTransform resultPanel;
        [SerializeField] private TMPro.TextMeshProUGUI resultTitleText;
        [SerializeField] private TMPro.TextMeshProUGUI resultMessageText;
        [SerializeField] private RectTransform measurementsContainer;
        [SerializeField] private RectTransform measurementItemPrefab;
        [SerializeField] private RectTransform addToNotesButton;
        [SerializeField] private RectTransform messagePanel;
        [SerializeField] private TMPro.TextMeshProUGUI messageText;
        [SerializeField] private TMPro.TextMeshProUGUI toolInstructionText;

        [Header("Settings")]
        [SerializeField] private float messageDisplayDuration = 3f;
        [SerializeField] private float resultDisplayDuration = 8f;

        private float _messageTimer = 0f;
        private float _resultTimer = 0f;
        private ToolInspectionResult _currentResult;

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Hide all panels initially
            HideAllPanels();
        }

        private void Update()
        {
            UpdateMessageDisplay();
            UpdateResultDisplay();
        }
        #endregion

        #region Progress Display
        public void ShowProgress(string toolName, float duration)
        {
            if (progressPanel != null)
            {
                progressPanel.gameObject.SetActive(true);
            }

            if (toolInstructionText != null)
            {
                toolInstructionText.text = $"Using {toolName}...";
            }
        }

        public void UpdateProgress(float progress)
        {
            if (progressBarFill != null)
            {
                progressBarFill.anchorMax = new Vector2(progress, 1f);
            }
        }

        public void HideProgress()
        {
            if (progressPanel != null)
            {
                progressPanel.gameObject.SetActive(false);
            }

            if (toolInstructionText != null)
            {
                toolInstructionText.text = string.Empty;
            }
        }
        #endregion

        #region Result Display
        public void ShowResult(ToolInspectionResult result)
        {
            _currentResult = result;
            _resultTimer = 0f;

            if (resultPanel == null) return;

            resultPanel.gameObject.SetActive(true);

            if (resultTitleText != null)
            {
                resultTitleText.text = result.Success ? "Inspection Complete" : "Inspection Failed";
            }

            if (resultMessageText != null)
            {
                resultMessageText.text = result.DisplayMessage;
            }

            DisplayMeasurements(result.Measurements);
        }

        private void DisplayMeasurements(Dictionary<string, string> measurements)
        {
            // Clear existing measurement items
            if (measurementsContainer != null)
            {
                foreach (Transform child in measurementsContainer)
                {
                    Destroy(child.gameObject);
                }

                // Add new measurement items
                if (measurements != null && measurementItemPrefab != null)
                {
                    foreach (var kvp in measurements)
                    {
                        RectTransform item = Instantiate(measurementItemPrefab, measurementsContainer);
                        var texts = item.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                        if (texts.Length >= 2)
                        {
                            texts[0].text = kvp.Key + ":";
                            texts[1].text = kvp.Value;
                        }
                    }
                }
            }
        }

        private void UpdateResultDisplay()
        {
            if (resultPanel != null && resultPanel.gameObject.activeSelf)
            {
                _resultTimer += Time.deltaTime;

                if (_resultTimer >= resultDisplayDuration)
                {
                    HideResult();
                }
            }
        }

        public void HideResult()
        {
            if (resultPanel != null)
            {
                resultPanel.gameObject.SetActive(false);
            }
            _currentResult = null;
        }

        public void AddCurrentResultToNotes()
        {
            if (_currentResult == null) return;

            // Add to vehicle notes through PlayerDataManager
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.AddNoteToVehicle(_currentResult.DisplayMessage);
                ShowMessage("Added to vehicle notes");
            }
        }
        #endregion

        #region Message Display
        public void ShowMessage(string message, float duration = 0f)
        {
            if (messagePanel != null)
            {
                messagePanel.gameObject.SetActive(true);
            }

            if (messageText != null)
            {
                messageText.text = message;
            }

            _messageTimer = 0f;
            if (duration > 0f)
            {
                messageDisplayDuration = duration;
            }
            else
            {
                messageDisplayDuration = 3f;
            }
        }

        private void UpdateMessageDisplay()
        {
            if (messagePanel != null && messagePanel.gameObject.activeSelf)
            {
                _messageTimer += Time.deltaTime;

                if (_messageTimer >= messageDisplayDuration)
                {
                    HideMessage();
                }
            }
        }

        public void HideMessage()
        {
            if (messagePanel != null)
            {
                messagePanel.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Utility
        private void HideAllPanels()
        {
            if (progressPanel != null) progressPanel.gameObject.SetActive(false);
            if (resultPanel != null) resultPanel.gameObject.SetActive(false);
            if (messagePanel != null) messagePanel.gameObject.SetActive(false);
        }

        public void ShowInstruction(string instruction)
        {
            if (toolInstructionText != null)
            {
                toolInstructionText.text = instruction;
            }
        }

        public void ClearInstruction()
        {
            if (toolInstructionText != null)
            {
                toolInstructionText.text = string.Empty;
            }
        }
        #endregion
    }
}
