using UnityEngine;
using System.IO;
using System;
public static class GameLogger
{
    private static string _logFilePath = Application.persistentDataPath + "/gameinformations.log";

    public static void Log(string message)
    {
        string timeStampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Debug.Log(timeStampedMessage);
        File.AppendAllText(_logFilePath, timeStampedMessage + Environment.NewLine);
    }

    public static void LogWarning(string message)
    {
        string timeStampedMessage = $"[WARNING {DateTime.Now:HH:mm:ss}] {message}";
        Debug.LogWarning(timeStampedMessage);
        File.AppendAllText(_logFilePath, timeStampedMessage + Environment.NewLine);
    }

    public static void LogError(string message)
    {
        string timeStampedMessage = $"[ERROR {DateTime.Now:HH:mm:ss}] {message}";
        Debug.LogError(timeStampedMessage);
        File.AppendAllText(_logFilePath, timeStampedMessage + Environment.NewLine);
    }
}
