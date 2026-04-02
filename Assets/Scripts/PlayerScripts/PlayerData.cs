using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerData
{
    [Header("Basic Info")]
    public string name;
    public int level;
    public int point;
    public int experience;
    public int reliability;

    [Header("Position & Inventory")]
    public Vector3 position;
    public List<MarketItem> inventory;
    public float money;
    public MarketItem selectedItem;

    [Header("Progression Stats")]
    public int totalInspections;
    public int successfulInspections;
    public int perfectInspections;
    public float totalAccuracy;
    public float totalPlayTimeSeconds;
    public int highestLevelReached;
    public int totalIssuesFound;
    public int totalIssuesMissed;
    public float totalMoneyEarned;

    [Header("Achievements")]
    public List<string> unlockedAchievementIds;

    // Serialize et
    //string json = JsonUtility.ToJson(playerData);

    // Deserialize et
    //PlayerData loadedData = JsonUtility.FromJson<PlayerData>(json);

    //kaydetmek için kullanılabilir.
    public PlayerData(string name)
    {
        this.name = name;
        level = 1; // Changed from 25 to 1 for proper progression
        experience = 0;
        reliability = 0;
        position = Vector3.zero;
        inventory = new List<MarketItem>();
        money = 500;

        // Initialize progression stats
        totalInspections = 0;
        successfulInspections = 0;
        perfectInspections = 0;
        totalAccuracy = 0f;
        totalPlayTimeSeconds = 0f;
        highestLevelReached = 1;
        totalIssuesFound = 0;
        totalIssuesMissed = 0;
        totalMoneyEarned = 0f;

        // Initialize achievements
        unlockedAchievementIds = new List<string>();
    }

    public bool PurchaseItem(MarketItem item)
    {
        if (money >= item.price)
        {
            money -= item.price;
            inventory.Add(item);
            return true;
        }

        return false;
    }

    #region Progression Helper Methods

    /// <summary>
    /// Records an inspection result and updates related stats.
    /// </summary>
    public void RecordInspection(float accuracy, int issuesFound, int totalIssues, float earnedMoney)
    {
        totalInspections++;
        totalAccuracy += accuracy;

        if (accuracy >= 0.7f)
        {
            successfulInspections++;
        }

        if (accuracy >= 1.0f && issuesFound == totalIssues)
        {
            perfectInspections++;
        }

        totalIssuesFound += issuesFound;
        totalIssuesMissed += (totalIssues - issuesFound);
        totalMoneyEarned += earnedMoney;
    }

    /// <summary>
    /// Gets the average accuracy across all inspections.
    /// </summary>
    public float GetAverageAccuracy()
    {
        return totalInspections > 0 ? totalAccuracy / totalInspections : 0f;
    }

    /// <summary>
    /// Updates the highest level reached if the new level is higher.
    /// </summary>
    public void UpdateHighestLevel(int newLevel)
    {
        if (newLevel > highestLevelReached)
        {
            highestLevelReached = newLevel;
        }
    }

    /// <summary>
    /// Adds play time in seconds.
    /// </summary>
    public void AddPlayTime(float seconds)
    {
        totalPlayTimeSeconds += seconds;
    }

    /// <summary>
    /// Gets the total play time as a TimeSpan.
    /// </summary>
    public TimeSpan GetTotalPlayTime()
    {
        return TimeSpan.FromSeconds(totalPlayTimeSeconds);
    }

    /// <summary>
    /// Unlocks an achievement by ID.
    /// </summary>
    public bool UnlockAchievement(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId) || unlockedAchievementIds.Contains(achievementId))
        {
            return false;
        }

        unlockedAchievementIds.Add(achievementId);
        return true;
    }

    /// <summary>
    /// Checks if an achievement is unlocked.
    /// </summary>
    public bool IsAchievementUnlocked(string achievementId)
    {
        return !string.IsNullOrEmpty(achievementId) && unlockedAchievementIds.Contains(achievementId);
    }

    #endregion
}
