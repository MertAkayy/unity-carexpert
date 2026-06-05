using Core;
using Customer;
using Economy;
using PlayerScripts;
using Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField]
    private TextMeshProUGUI infoText;
    [SerializeField]
    private TextMeshProUGUI currentLevel;
    [SerializeField]
    private TextMeshProUGUI currentMoney;
    [SerializeField]
    private TextMeshProUGUI currentClock;
    [SerializeField]
    private TextMeshProUGUI currentDay;
    [SerializeField]
    private TextMeshProUGUI customerTimerText;
    [SerializeField]
    private TextMeshProUGUI actionHintsText;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        //else
          //  Destroy(gameObject);


    }

    private ICustomerManager _customerManager;

    void Start()
    {
        EventManager.StartListening("OnTimeChanged",UpdateClockUI);
        infoText.enabled = false;
        if (customerTimerText != null)
            customerTimerText.enabled = false;
        if (actionHintsText != null)
            actionHintsText.enabled = false;

        // Subscribe to system events
        ServiceLocator.OnAllSystemsInitialized += OnSystemsReady;

        // If systems were already initialized before we subscribed, call it now
        if (ServiceLocator.IsInitialized)
            OnSystemsReady();
    }

    private void OnSystemsReady()
    {
        ServiceLocator.OnAllSystemsInitialized -= OnSystemsReady;

        if (ServiceLocator.TryGet(out IProgressionManager progression))
        {
            currentLevel.text = "Level   : " + progression.CurrentLevel;
            progression.OnLevelUp += UpdateLevelUI;
            progression.OnXPChanged += UpdateXPUi;
        }
        else
        {
            currentLevel.text = "Level   : " + PlayerDataManager.Instance.playerData.level;
        }

        if (ServiceLocator.TryGet(out IEconomySystem economy))
        {
            currentMoney.text = economy.Balance + " $ ";
            economy.OnBalanceChanged += UpdateMoneyUI;
        }
        else
        {
            currentMoney.text = PlayerDataManager.Instance.playerData.money + " $ ";
        }

        if (ServiceLocator.TryGet(out _customerManager))
        {
            _customerManager.OnCustomerServiceStarted += OnServiceStarted;
            _customerManager.OnCustomerLeft += OnCustomerLeft;
        }
    }

    private void OnServiceStarted(Customer.Customer customer)
    {
        if (customerTimerText != null)
            customerTimerText.enabled = true;
    }

    private void OnCustomerLeft(Customer.Customer customer, float satisfaction)
    {
        if (customerTimerText != null)
            customerTimerText.enabled = false;
    }

    private void Update()
    {
        UpdateCustomerTimer();
    }

    private void UpdateCustomerTimer()
    {
        if (customerTimerText == null || !customerTimerText.enabled) return;
        if (_customerManager == null || _customerManager.CurrentCustomer == null)
        {
            customerTimerText.enabled = false;
            return;
        }

        var customer = _customerManager.CurrentCustomer;
        float remaining = customer.PatienceRemaining;
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        float patiencePercent = customer.PatiencePercent;

        // Change color based on urgency
        if (patiencePercent > 0.5f)
            customerTimerText.color = Color.white;
        else if (patiencePercent > 0.25f)
            customerTimerText.color = Color.yellow;
        else
            customerTimerText.color = Color.red;

        customerTimerText.text = $"Inspection Time: {minutes:D2}:{seconds:D2}";
    }

    void OnDestroy()
    {
        EventManager.StopListening("OnTimeChanged", UpdateClockUI);

        if (ServiceLocator.TryGet(out IProgressionManager progression))
        {
            progression.OnLevelUp -= UpdateLevelUI;
            progression.OnXPChanged -= UpdateXPUi;
        }

        if (ServiceLocator.TryGet(out IEconomySystem economy))
        {
            economy.OnBalanceChanged -= UpdateMoneyUI;
        }

        if (_customerManager != null)
        {
            _customerManager.OnCustomerServiceStarted -= OnServiceStarted;
            _customerManager.OnCustomerLeft -= OnCustomerLeft;
        }
    }

    private void UpdateLevelUI(int newLevel)
    {
        currentLevel.text = "Level   : " + newLevel;
    }

    private void UpdateXPUi(int currentXP, int delta)
    {
        // XP text not yet added to UI, but event is wired for when it is
    }

    private void UpdateMoneyUI(float newBalance, Transaction transaction)
    {
        currentMoney.text = newBalance + " $ ";
    }

    private void UpdateClockUI()
    {
        currentClock.text = $"{TimeManager.Instance.currentHour:D2}:{TimeManager.Instance.currentMinute:D2}";
        currentDay.text = "Day   " + TimeManager.Instance.currentDay.ToString();
    }
    public void ShowInfo(string message)
    {
        infoText.text = message;
        infoText.enabled=true;
    }

    public void HideInfo()
    {
        infoText.enabled=false;
    }

    /// <summary>
    /// Updates the action hints UI based on the highlighted object and current tool.
    /// </summary>
    public void UpdateActionHints(GameObject activeObject, Tool currentTool)
    {
        if (actionHintsText == null) return;

        if (activeObject == null)
        {
            actionHintsText.enabled = false;
            return;
        }

        var lines = new System.Collections.Generic.List<string>();

        // [E] Interact — if object has IInteractable
        if (activeObject.GetComponent<IInteractable>() != null)
        {
            lines.Add("[E] Interact");
        }

        // [R] Read — if object has IReadable
        if (activeObject.GetComponent<IReadable>() != null)
        {
            lines.Add("[R] Read");
        }

        // [LMB] Tool action — check if current tool is compatible with this object
        if (currentTool != Tool.Null && currentTool != Tool.Handle)
        {
            VehiclePart part = activeObject.GetComponent<VehiclePart>();
            if (part == null)
                part = activeObject.GetComponentInParent<VehiclePart>();

            if (part != null && IsToolCompatible(currentTool, part))
            {
                lines.Add("[LMB] Use Tool");
            }
        }

        // Hand tool actions
        if (currentTool == Tool.Handle)
        {
            VehiclePart part = activeObject.GetComponent<VehiclePart>();
            if (part == null)
                part = activeObject.GetComponentInParent<VehiclePart>();

            if (part != null)
            {
                lines.Add("[LMB] Inspect");
            }

            if (activeObject.GetComponent<IGrabbable>() != null)
            {
                lines.Add("[E] Grab");
            }
        }

        if (lines.Count == 0)
        {
            actionHintsText.enabled = false;
        }
        else
        {
            actionHintsText.text = string.Join("\n", lines);
            actionHintsText.enabled = true;
        }
    }

    /// <summary>
    /// Clears the action hints display.
    /// </summary>
    public void ClearActionHints()
    {
        if (actionHintsText != null)
            actionHintsText.enabled = false;
    }

    /// <summary>
    /// Checks if a tool is compatible with a given vehicle part based on the part's interfaces.
    /// </summary>
    private bool IsToolCompatible(Tool tool, VehiclePart part)
    {
        switch (tool)
        {
            case Tool.DigitalPaintThicknessGauge:
            case Tool.MechanicPaintThicknessGauge:
                return part is IExteriorPart;

            case Tool.BatteryTester:
                return part is IVehicleBattery;

            case Tool.TireTreadDepthGauge:
            case Tool.TirePumper:
                return part is IVehicleWheel;

            case Tool.ExhaustGasAnalyser:
                return part is IVehicleExhaust;

            case Tool.ObdScanner:
                return true; // OBD scanner works on any part (scans whole vehicle)

            default:
                return false;
        }
    }

    public void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
}
