using PlayerScripts;
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
        currentLevel.text = "Level   : " + PlayerDataManager.Instance.playerData.level;
        currentMoney.text = PlayerDataManager.Instance.playerData.money+" $ ";
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
