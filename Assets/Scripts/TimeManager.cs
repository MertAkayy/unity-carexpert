using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;
    public int currentDay = 1;
    public int currentHour = 8;
    public int currentMinute = 0;
    public float timeScale = 1f;
    private float minuteTimer = 0f;
    public float realSecondsPerGameMinute = 1f;

    void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        minuteTimer += Time.deltaTime * timeScale;
        if (minuteTimer >= realSecondsPerGameMinute)
        {
            AdvanceMinute();
            minuteTimer = 0f;
        }
    }
    private void AdvanceMinute()
    {
        currentMinute++;
        if (currentMinute >= 60)
        {
            currentMinute = 0;
            currentHour++;

            if (currentHour >= 24)
            {
                currentHour = 0;
                currentDay++;
            }
        }

       // GameDataManager.Instance.GameStateData.currentDay = currentDay;
      //  GameDataManager.Instance.GameStateData.currentTime = currentHour + (currentMinute / 60f);
        
        // Event tetikle (örneğin saat başı, gün başı)
        EventManager.TriggerEvent("OnTimeChanged");
    }
}
