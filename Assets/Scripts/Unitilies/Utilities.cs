using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class Utilities
{
    private static readonly System.Random _random = new System.Random();
    public static T GetRandomEnumValue<T>() where T : Enum
    {
        Array values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(_random.Next(values.Length));
    }
    public static string EnumToString<T>(T enumValue) where T : Enum
    {
        return enumValue.ToString();
    }
    public static T StringToEnum<T>(string value) where T : struct, Enum
    {
        if (Enum.TryParse(value, out T result))
        {
            return result;
        }
        else
        {
            Debug.LogWarning($"Invalid enum string: {value}");
            return default;
        }
    }
    public static string AddSpacesBeforeCapitals(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return Regex.Replace(input, "(?<!^)([A-Z])", " $1");
    }
    public static string RemoveSpaces(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.Replace(" ", "");
    }
    public static DateTime GenerateRandomDate(DateTime start, DateTime end)
    {
        int range = (end - start).Days;
        return start.AddDays(_random.Next(range));
    }
    public static string GenerateRandomAlphaNumeric(int length)
    {
        const string allowedChars = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(allowedChars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
    public static string GenerateRandomNumeric(int length)
    {
        const string allowedChars = "0123456789";
        return new string(Enumerable.Repeat(allowedChars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
    public static string GenerateRandomAlphabetic(int length)
    {
        const string allowedChars = "ABCDEFGHJKLMNPRSTUVWXYZ";
        return new string(Enumerable.Repeat(allowedChars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
    public static bool GenerateRandomBool()
    {
        return _random.Next(0, 2) == 1;
    }
    public static Color GenerateRandomColor()
    {
        return new Color(
            _random.NextFloat(0f, 1f),
            _random.NextFloat(0f, 1f),
            _random.NextFloat(0f, 1f)
        );
    }
    public static Vector3 GenerateRandomVector3(float min, float max)
    {
        return new Vector3(
            _random.NextFloat(min, max),
            _random.NextFloat(min, max),
            _random.NextFloat(min, max)
        );
    }
    public static float NextFloat(this System.Random random, float min, float max)
    {
        return (float)(random.NextDouble() * (max - min) + min);
    }
    public static T WeightedRandom<T>(IList<T> items, Func<T, int> weightSelector)
    {
        if (items == null || items.Count == 0) 
            return default;

        int totalWeight = 0;
        foreach (var item in items)
        {
            int w = Mathf.Max(0, weightSelector(item));
            totalWeight += w;
        }

        if (totalWeight <= 0)
            return default;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var item in items)
        {
            cumulative += Mathf.Max(0, weightSelector(item));
            if (roll < cumulative)
                return item;
        }

        return default; // fallback
    }
}
