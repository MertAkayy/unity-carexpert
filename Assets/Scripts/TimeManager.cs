using System;
using System.IO;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;
    public int currentDay = 1;
    public int currentHour = 8;
    public int currentMinute = 0;

    [Header("Speed Settings")]
    [Tooltip("How many real seconds equal 1 game minute. Lower = faster time.")]
    public float realSecondsPerGameMinute = 0.5f;

    [Tooltip("Extra multiplier on top of base speed. 1 = normal, 2 = double speed.")]
    public float timeScale = 1f;

    private float minuteTimer = 0f;
    private string _saveFilePath;

    void Awake()
    {
        Instance = this;
        _saveFilePath = Path.Combine(Application.persistentDataPath, "timedata.json");
        LoadTime();
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

        EventManager.TriggerEvent("OnTimeChanged");
    }

    private void OnApplicationQuit()
    {
        SaveTime();
    }

    public void SaveTime()
    {
        TimeData data = new TimeData
        {
            day = currentDay,
            hour = currentHour,
            minute = currentMinute
        };
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(_saveFilePath, json);
        Debug.Log($"[TimeManager] Saved: Day {currentDay}, {currentHour:D2}:{currentMinute:D2}");
    }

    public void LoadTime()
    {
        if (File.Exists(_saveFilePath))
        {
            string json = File.ReadAllText(_saveFilePath);
            TimeData data = JsonUtility.FromJson<TimeData>(json);
            currentDay = data.day;
            currentHour = data.hour;
            currentMinute = data.minute;
            Debug.Log($"[TimeManager] Loaded: Day {currentDay}, {currentHour:D2}:{currentMinute:D2}");
        }
        else
        {
            Debug.Log("[TimeManager] No save found. Starting Day 1, 08:00");
            currentDay = 1;
            currentHour = 8;
            currentMinute = 0;
        }
    }

    /// <summary>
    /// Resets time to Day 1, 08:00 and deletes save file.
    /// </summary>
    [ContextMenu("Debug: Reset Time")]
    public void ResetTime()
    {
        currentDay = 1;
        currentHour = 8;
        currentMinute = 0;
        if (File.Exists(_saveFilePath))
            File.Delete(_saveFilePath);
        Debug.Log("[TimeManager] Time reset to Day 1, 08:00");
    }
}

[Serializable]
public class TimeData
{
    public int day;
    public int hour;
    public int minute;
}
