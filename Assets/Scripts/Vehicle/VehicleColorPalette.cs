using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;
public class VehicleColor
{
    public string Name;
    public string Color;
    public VehicleColor(string name, string hexCode)
    {
        Name = name;
        Color = hexCode;
    }
}
public class VehicleColorPalette
{
    private static Random _random = new Random();
    public static readonly List<VehicleColor> Colors = new List<VehicleColor>
    {
        new VehicleColor("Jet Black", "#0A0A0A"),
        new VehicleColor("Phantom Black", "#161616"),
        new VehicleColor("Deep Charcoal", "#2B2B2B"),
        new VehicleColor("Metallic Gray", "#4B4B4B"),
        new VehicleColor("Steel Gray", "#6E7B8B"),
        new VehicleColor("Silver Mist", "#C0C0C0"),
        new VehicleColor("Platinum", "#E5E4E2"),
        new VehicleColor("Arctic White", "#F5F5F5"),
        new VehicleColor("Pearl White", "#F0EDEE"),
        new VehicleColor("Navy Blue", "#1D2D44"),
        new VehicleColor("Midnight Blue", "#003366"),
        new VehicleColor("Steel Blue", "#4682B4"),
        new VehicleColor("Ocean Blue", "#3B5998"),
        new VehicleColor("Slate Blue", "#6A5ACD"),
        new VehicleColor("Burgundy", "#800020"),
        new VehicleColor("Maroon", "#800000"),
        new VehicleColor("Ruby Red", "#9B111E"),
        new VehicleColor("Crimson", "#DC143C"),
        new VehicleColor("Classic Red", "#B22222"),
        new VehicleColor("Firebrick", "#B22222"),
        new VehicleColor("Copper", "#B87333"),
        new VehicleColor("Bronze", "#CD7F32"),
        new VehicleColor("Burnt Orange", "#CC5500"),
        new VehicleColor("Champagne", "#F7E7CE"),
        new VehicleColor("Desert Sand", "#EDC9AF"),
        new VehicleColor("Olive Green", "#708238"),
        new VehicleColor("Forest Green", "#228B22"),
        new VehicleColor("Hunter Green", "#355E3B"),
        new VehicleColor("Metallic Green", "#4E5D4E"),
        new VehicleColor("Graphite", "#474A51"),
        new VehicleColor("Champagne Gold", "#D4AF37"),
        new VehicleColor("Classic Beige", "#D2B48C")
    };
    public static VehicleColor GetRandomColor()
    {
        int index = _random.Next(Colors.Count);
        return Colors[index];
    }
}

