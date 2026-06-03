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
    ///
    /// Uses CanvasGroup (alpha 0/1) instead of SetActive to show/hide panels.
    /// This avoids TextMeshPro initialization issues with disabled GameObjects.
    /// Panels should START ENABLED in the editor — Awake() hides them via alpha.
    /// CanvasGroup components are added automatically at runtime if missing.
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
        [SerializeField] private RectTransform messagePanel;
        [SerializeField] private TMPro.TextMeshProUGUI messageText;
        [SerializeField] private TMPro.TextMeshProUGUI toolInstructionText;

        [Header("Settings")]
        [SerializeField] private float messageDisplayDuration = 3f;
        [SerializeField] private float resultDisplayDuration = 8f;

        private float _messageTimer = 0f;
        private float _resultTimer = 0f;
        private bool _messageVisible = false;
        private bool _resultVisible = false;
        private ToolInspectionResult _currentResult;

        // Cached CanvasGroup references
        private CanvasGroup _progressCanvasGroup;
        private CanvasGroup _resultCanvasGroup;
        private CanvasGroup _messageCanvasGroup;

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Ensure CanvasGroup on each panel (add if missing)
            _progressCanvasGroup = EnsureCanvasGroup(progressPanel);
            _resultCanvasGroup = EnsureCanvasGroup(resultPanel);
            _messageCanvasGroup = EnsureCanvasGroup(messagePanel);

            // Hide all panels initially (via alpha, not SetActive)
            HideAllPanels();
        }

        private void Update()
        {
            UpdateMessageDisplay();
            UpdateResultDisplay();
        }
        #endregion

        #region CanvasGroup Helpers

        /// <summary>
        /// Ensures a CanvasGroup exists on the panel. Adds one at runtime if missing.
        /// </summary>
        private CanvasGroup EnsureCanvasGroup(RectTransform panel)
        {
            if (panel == null) return null;

            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = panel.gameObject.AddComponent<CanvasGroup>();
            }
            return cg;
        }

        private void ShowPanel(CanvasGroup cg)
        {
            if (cg == null) return;
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        private void HidePanel(CanvasGroup cg)
        {
            if (cg == null) return;
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        private bool IsPanelVisible(CanvasGroup cg)
        {
            return cg != null && cg.alpha > 0f;
        }
        #endregion

        #region Progress Display
        public void ShowProgress(string toolName, float duration)
        {
            if (_progressCanvasGroup != null)
            {
                ShowPanel(_progressCanvasGroup);
            }

            if (toolInstructionText != null)
            {
                toolInstructionText.text = $"Using {toolName}...";
            }

            // Fall back to UIManager if progress panel is not set up
            if (progressPanel == null && UIManager.Instance != null)
            {
                UIManager.Instance.ShowInfo($"Using {toolName}...");
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
            if (_progressCanvasGroup != null)
            {
                HidePanel(_progressCanvasGroup);
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
            string defaultTitle = result.Success ? "Inspection Complete" : "Inspection Failed";
            ShowResult(result, defaultTitle);
        }

        public void ShowResult(ToolInspectionResult result, string title)
        {
            _currentResult = result;
            _resultTimer = 0f;

            // Fall back to UIManager if result panel is not set up
            if (resultPanel == null)
            {
                if (UIManager.Instance != null && !string.IsNullOrEmpty(result.DisplayMessage))
                {
                    DebugToScreen.ShowMessage(result.DisplayMessage, resultDisplayDuration);
                }
                return;
            }

            ShowPanel(_resultCanvasGroup);
            _resultVisible = true;

            if (resultTitleText != null)
            {
                resultTitleText.text = title;
            }

            if (resultMessageText != null)
            {
                resultMessageText.text = result.DisplayMessage;
            }

            DisplayMeasurements(result.Measurements);
        }

        private void DisplayMeasurements(Dictionary<string, string> measurements)
        {
            if (measurementsContainer != null)
            {
                foreach (Transform child in measurementsContainer)
                {
                    Destroy(child.gameObject);
                }

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
            if (_resultVisible)
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
            if (_resultCanvasGroup != null)
            {
                HidePanel(_resultCanvasGroup);
            }
            _resultVisible = false;
            _currentResult = null;
        }



        #endregion

        #region Message Display
        public void ShowMessage(string message, float duration = 0f)
        {
            float actualDuration = duration > 0f ? duration : 3f;

            // Fall back to UIManager if message panel is not set up
            if (messagePanel == null)
            {
                if (UIManager.Instance != null)
                {
                    DebugToScreen.ShowMessage(message, actualDuration);
                }
                return;
            }

            ShowPanel(_messageCanvasGroup);
            _messageVisible = true;

            if (messageText != null)
            {
                messageText.text = message;
            }

            _messageTimer = 0f;
            messageDisplayDuration = actualDuration;
        }

        private void UpdateMessageDisplay()
        {
            if (_messageVisible)
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
            if (_messageCanvasGroup != null)
            {
                HidePanel(_messageCanvasGroup);
            }
            _messageVisible = false;
        }
        #endregion

        #region Utility
        private void HideAllPanels()
        {
            HidePanel(_progressCanvasGroup);
            HidePanel(_resultCanvasGroup);
            HidePanel(_messageCanvasGroup);
            _messageVisible = false;
            _resultVisible = false;
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
