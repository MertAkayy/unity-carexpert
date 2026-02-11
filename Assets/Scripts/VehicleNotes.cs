
using System;
using System.Collections.Generic;
using System.Linq;
using PlayerScripts;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class VehicleNotes : MonoBehaviour
{
    public static VehicleNotes Instance;
    [Header("References")]
    [SerializeField] private IssueDataBase issuePool;
    [SerializeField] private TMP_Dropdown partSelector;
    [SerializeField] private TMP_Dropdown availableIssues;
    [SerializeField] private TMP_Dropdown selectedIssues;
    [SerializeField] private TMP_Text selectedPartName;
    [SerializeField] private Button saveButton;

    private readonly List<Issue> _filteredIssues = new List<Issue>();
    private readonly List<Issue> _selectedIssues = new List<Issue>();

    public List<string> partNames;

    private const string DefaultOption = "Select ...";

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        partNames.Clear();
        foreach (var carPartType in Enum.GetValues(typeof(VehiclePartUniqueType)).Cast<VehiclePartUniqueType>())
        {
            partNames.Add(Utilities.AddSpacesBeforeCapitals(carPartType.ToString()));
        }
        InitializeDropdown(partSelector, partNames);
        InitializeDropdown(availableIssues);
        InitializeDropdown(selectedIssues);

        partSelector.onValueChanged.AddListener(OnPartSelected);
        availableIssues.onValueChanged.AddListener(OnIssueSelected);
        selectedIssues.onValueChanged.AddListener(OnSelectedIssueChanged);
        saveButton.onClick.AddListener(OnSaveClicked);
    }

    private void OnSaveClicked()
    {
        GameObject selectedPartObject = null;
        VehiclePart[] vehicleParts = FindObjectsByType<VehiclePart>(FindObjectsSortMode.None);
        foreach (var part in vehicleParts)
        {
            if (part.partUniqueType ==
                Utilities.StringToEnum<VehiclePartUniqueType>(Utilities.RemoveSpaces(partSelector.options[partSelector.value].text)))
            {
                selectedPartObject = part.gameObject;
            }
        }
        
        
        if (selectedPartObject != null)
        {
            selectedPartObject.GetComponentInChildren<VehiclePart>().predictedIssues=_selectedIssues;
        }
    }

    private void InitializeDropdown(TMP_Dropdown dropdown, IEnumerable<string> options = null)
    {
        dropdown.ClearOptions();
        var optionList = new List<TMP_Dropdown.OptionData> { new(DefaultOption) };

        if (options != null)
        {
            optionList.AddRange(options.Select(text => new TMP_Dropdown.OptionData(text)));
        }

        dropdown.AddOptions(optionList);
        dropdown.value = 0;
    }
    private void OnPartSelected(int index)
    {
        if (index == 0) return;

        string selectedPart = partSelector.options[index].text;
        selectedPartName.text = selectedPart;

        GameObject foundPart=null;
        VehiclePart[] vehicleParts = FindObjectsByType<VehiclePart>(FindObjectsSortMode.None);
        foreach (var part in vehicleParts)
        {
            if (part.partUniqueType ==
                Utilities.StringToEnum<VehiclePartUniqueType>(Utilities.RemoveSpaces(selectedPart)))
            {
                foundPart = part.gameObject;
            }
        }
        if (foundPart == null) return;
        _selectedIssues.Clear();
        _filteredIssues.Clear();
        UpdateAvailableIssuesDropDown();
        UpdateSelectedIssuesDropDown();
        _filteredIssues.AddRange(GetIssuesForPart(foundPart));

        InitializeDropdown(availableIssues, _filteredIssues.Select(i => i.FailureName.Replace("_", " ")));
    }

    private IEnumerable<Issue> GetIssuesForPart(GameObject part)
    {
        if (part.TryGetComponent(out ExteriorPart exterior))
        {
            var issues = issuePool.GetByPartType(AffectedPartType.Exterior).ToList();
            if (!exterior.hingedPart)
                issues.Remove(issuePool.GetByName("Lock_Actuator_Failure"));
            return issues;
        }
        if (part.TryGetComponent(out VehicleWheel _))
            return GetFilteredIssues(AffectedPartType.Wheel);
        if (part.TryGetComponent(out VehicleGlass _))
            return GetFilteredIssues(AffectedPartType.Glass);
        if (part.TryGetComponent(out VehicleLight _))
            return GetFilteredIssues(AffectedPartType.Light);
        if (part.TryGetComponent(out VehicleEngine _))
            return GetFilteredIssues(AffectedPartType.Engine);
        if (part.TryGetComponent(out VehicleBattery _))
            return GetFilteredIssues(AffectedPartType.Battery);
        if (part.TryGetComponent(out VehicleRadiator _))
            return GetFilteredIssues(AffectedPartType.Radiator);

        return Enumerable.Empty<Issue>();
    }

    private IEnumerable<Issue> GetFilteredIssues(AffectedPartType type)
    {
        return issuePool.GetByPartType(type)
            .Where(issue => issue.AvailableLevel <= PlayerDataManager.Instance.playerData.level);
    }

    private void OnIssueSelected(int index)
    {
        if (index == 0) return;

        string issueName = availableIssues.options[index].text.Replace(" ", "_");
        Issue issue = _filteredIssues.FirstOrDefault(i => i.FailureName == issueName);
        if (issue == null) return;

        _selectedIssues.Add(issue);
        _filteredIssues.Remove(issue);
        
        UpdateSelectedIssuesDropDown();
        UpdateAvailableIssuesDropDown();
    }
    private void OnSelectedIssueChanged(int index)
    {
        if (index == 0) return;

        string issueName = selectedIssues.options[index].text.Replace(" ", "_");
        Issue issue = _selectedIssues.FirstOrDefault(i => i.FailureName == issueName);
        if (issue == null) return;

        _filteredIssues.Add(issue);
        _selectedIssues.Remove(issue);

        UpdateSelectedIssuesDropDown();
        UpdateAvailableIssuesDropDown();
    }
    private void UpdateSelectedIssuesDropDown()
    {
        List<string> options = _selectedIssues.Select(i => i.FailureName.Replace("_"," ")).ToList();
        options.Insert(0, DefaultOption);
        selectedIssues.ClearOptions();
        if(options.Count > 0)
            selectedIssues.AddOptions(options);
        else
            selectedIssues.options.Add(new TMP_Dropdown.OptionData("No Selected Issues"));
    }

    private void UpdateAvailableIssuesDropDown()
    {
        List<string> options = _filteredIssues.Select(i => i.FailureName.Replace("_", " ")).ToList();
        options.Insert(0, DefaultOption);
        availableIssues.ClearOptions();
        if (options.Count > 0)
            availableIssues.AddOptions(options);
        else
            availableIssues.options.Add(new TMP_Dropdown.OptionData("No Available Issues"));
    }

    public void ClearNotes()
    {
        _filteredIssues.Clear();
        _selectedIssues.Clear();
    }
}
