using UnityEngine;

public class PaintZoneThresholds
{
    public readonly int GreenMin = 0;
    public readonly int GreenMax = 100;
    public readonly int YellowMax = 250;
}

public enum PaintZone
{
    Green,
    Yellow,
    Red
}