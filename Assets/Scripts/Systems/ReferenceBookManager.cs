using UnityEngine;
using TMPro;
using Systems;

namespace Systems
{
    /// <summary>
    /// Manager for the OBD reference book system.
    /// Provides access to OBD code database and handles reference book UI.
    /// </summary>
    public class ReferenceBookManager : MonoBehaviour
    {
        private static ReferenceBookManager _instance;
        public static ReferenceBookManager Instance => _instance;

        [Header("Database")]
        [SerializeField] private OBDCodeDatabase obdCodeDatabase;

        [Header("UI References")]
        [SerializeField] private RectTransform referenceBookPanel;
        [SerializeField] private TMPro.TextMeshProUGUI codeListText;
        [SerializeField] private TMPro.TextMeshProUGUI codeDetailText;
        [SerializeField] private TMPro.TMP_InputField searchInputField;

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        private bool _isOpen = false;
        private OBDCodeEntry _selectedCode;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleReferenceBook();
            }
        }

        /// <summary>
        /// Toggles the reference book open/closed
        /// </summary>
        public void ToggleReferenceBook()
        {
            _isOpen = !_isOpen;
            UpdateUI();
        }

        /// <summary>
        /// Opens the reference book to a specific code
        /// </summary>
        public void OpenToCode(string code)
        {
            _isOpen = true;
            _selectedCode = obdCodeDatabase?.GetCode(code);
            UpdateUI();
        }

        /// <summary>
        /// Opens the reference book to a specific category
        /// </summary>
        public void OpenToCategory(OBDCodeCategory category)
        {
            _isOpen = true;
            DisplayCategory(category);
            UpdateUI();
        }

        /// <summary>
        /// Searches for codes matching the term
        /// </summary>
        public void SearchCodes(string searchTerm)
        {
            if (obdCodeDatabase == null) return;

            var results = obdCodeDatabase.SearchCodes(searchTerm);
            DisplaySearchResults(results);
        }

        /// <summary>
        /// Gets an OBD code entry by code string
        /// </summary>
        public OBDCodeEntry GetCode(string code)
        {
            return obdCodeDatabase?.GetCode(code);
        }

        /// <summary>
        /// Gets all codes in the database
        /// </summary>
        public System.Collections.Generic.List<OBDCodeEntry> GetAllCodes()
        {
            return obdCodeDatabase?.Codes ?? new System.Collections.Generic.List<OBDCodeEntry>();
        }

        private void UpdateUI()
        {
            if (referenceBookPanel != null)
            {
                referenceBookPanel.gameObject.SetActive(_isOpen);
            }

            if (_isOpen)
            {
                if (_selectedCode != null)
                {
                    DisplayCodeDetail(_selectedCode);
                }
                else
                {
                    DisplayAllCodes();
                }
            }
        }

        private void DisplayAllCodes()
        {
            if (obdCodeDatabase == null || codeListText == null) return;

            string display = "OBD-II REFERENCE BOOK\n\n";
            display += "=== CATEGORIES ===\n\n";
            display += "[P] Powertrain (Engine, Transmission)\n";
            display += "[C] Chassis (Brakes, Suspension)\n";
            display += "[B] Body (HVAC, Seats, Airbags)\n";
            display += "[U] Network (Communication)\n\n";
            display += "=== ALL CODES ===\n\n";

            foreach (var code in obdCodeDatabase.Codes)
            {
                if (code != null)
                {
                    display += $"{code.code} - {code.description}\n";
                }
            }

            codeListText.text = display;
            if (codeDetailText != null)
            {
                codeDetailText.text = "Select a code to view details";
            }
        }

        private void DisplayCategory(OBDCodeCategory category)
        {
            if (obdCodeDatabase == null || codeListText == null) return;

            var codes = obdCodeDatabase.GetCodesByCategory(category);

            string display = $"OBD-II REFERENCE - {category}\n\n";

            foreach (var code in codes)
            {
                if (code != null)
                {
                    display += $"{code.code} - {code.description}\n";
                }
            }

            if (codes.Count == 0)
            {
                display += "No codes found in this category.";
            }

            codeListText.text = display;
        }

        private void DisplaySearchResults(System.Collections.Generic.List<OBDCodeEntry> results)
        {
            if (codeListText == null) return;

            string display = "SEARCH RESULTS:\n\n";

            if (results.Count == 0)
            {
                display += "No matching codes found.";
            }
            else
            {
                foreach (var code in results)
                {
                    if (code != null)
                    {
                        display += $"{code.code} - {code.description}\n";
                    }
                }
            }

            codeListText.text = display;
        }

        private void DisplayCodeDetail(OBDCodeEntry code)
        {
            if (code == null || codeDetailText == null) return;

            string display = $"=== {code.code} ===\n\n";
            display += $"Description: {code.description}\n";
            display += $"Category: {code.category}\n";
            display += $"Severity: {code.severity}\n\n";

            if (code.commonCauses != null && code.commonCauses.Length > 0)
            {
                display += "Common Causes:\n";
                foreach (var cause in code.commonCauses)
                {
                    display += $"  - {cause}\n";
                }
            }

            if (!string.IsNullOrEmpty(code.symptoms))
            {
                display += $"\nSymptoms:\n{code.symptoms}\n";
            }

            codeDetailText.text = display;
        }

        /// <summary>
        /// Called by UI search input
        /// </summary>
        public void OnSearchInput(string input)
        {
            SearchCodes(input);
        }
    }
}
