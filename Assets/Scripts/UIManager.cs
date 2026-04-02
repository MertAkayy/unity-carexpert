using Core;
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
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        //else
          //  Destroy(gameObject);


    }

    void Start()
    {
        EventManager.StartListening("OnTimeChanged",UpdateClockUI);
        infoText.enabled = false;

        // Subscribe to system events (these persist even if systems aren't initialized yet)
        ServiceLocator.OnAllSystemsInitialized += OnSystemsReady;
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

    public void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
}
