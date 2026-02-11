using System;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class VehicleRegistration
{
      // Temel ruhsat bilgileri
      public string PlateNumber;             // Plaka
      public string ChassisNumber;           // Şase no (VIN)
    public string EngineNumber ;               // Motor no
    public string RegistrationNumber ;         // Ruhsat belge no
    public DateTime FirstRegistrationDate ;    // Trafiğe çıkış tarihi

    // Araç bilgileri
    public string Brand ;                       // Marka (Toyota, Ford)
    public string Model ;                      // Model (Corolla, Focus)
    public DateTime ModelDateTime ;                     // Model yılı
    public VehicleColor Color ;                      // Renk
    public FuelType FuelType ;                   // Benzin, dizel, elektrik
    public TransmissionType Transmission ;               // Manuel, otomatik
    public double EngineCapacity ;              // cm³ cinsinden (örn: 1598)
    public int MaxHorsePower ;                 // Beygir gücü
    private static readonly double[] EngineCapacities = {
        1.0, 1.2, 1.4, 1.5, 1.6, 1.8, 2.0, 2.2, 2.4, 2.5, 2.7, 3.0, 3.5, 4.0
    };
    private static Random _random = new Random();
    public VehicleRegistration()
    {
        EngineNumber =Utilities.GenerateRandomAlphaNumeric(10);
        RegistrationNumber = Utilities.GenerateRandomAlphaNumeric(15);
        PlateNumber = GeneratePlateNumber();
        ChassisNumber = Utilities.GenerateRandomAlphaNumeric(17);
        ModelDateTime = Utilities.GenerateRandomDate(new DateTime(1980, 1, 1), DateTime.Today);
        DateTime regStart = ModelDateTime;
        DateTime regEnd = ModelDateTime.AddYears(2) < DateTime.Today ? ModelDateTime.AddYears(2) : DateTime.Today;
        FirstRegistrationDate= Utilities.GenerateRandomDate(regStart, regEnd);
        Brand = GenerateBrandName(2);
        Model = Utilities.GenerateRandomAlphaNumeric(3);
        Color = VehicleColorPalette.GetRandomColor();
        FuelType = Utilities.GetRandomEnumValue<FuelType>();
        Transmission = Utilities.GetRandomEnumValue<TransmissionType>();
        EngineCapacity = GetRandomEngineCapacity();
        MaxHorsePower = EstimateHorsePower(EngineCapacity);
    }
    public static int EstimateHorsePower(double engineCapacityLiters)
    {
        int min = (int)(engineCapacityLiters * 60);
        int max = (int)(engineCapacityLiters * 120);
        return _random.Next(min, max + 1);
    }
    public static double GetRandomEngineCapacity()
    {
        return EngineCapacities[_random.Next(EngineCapacities.Length)];
    }
    public static string GenerateBrandName(int syllableCount = 2)
    {
    string[] syllables = {
            "Auto", "Car", "Moto", "Lux", "Nova", "Max", "Pro", "Vel", "Xen", "Tor", "Zen", "Cor", "Dyn"
        };
        return string.Concat(Enumerable.Range(0, syllableCount)
            .Select(_ => syllables[_random.Next(syllables.Length)]));
    }
    private string GeneratePlateNumber()
    {
        int provinceCode = _random.Next(1, 82);
        int letterCount = _random.Next(1, 5);
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string randomLetters = new string(Enumerable.Repeat(letters, letterCount)
            .Select(s => s[_random.Next(s.Length)]).ToArray());


        int digitCount = _random.Next(1, 7);
        string digits = "";
        for (int i = 0; i < digitCount; i++)
        {
            digits += _random.Next(0, 10).ToString();
        }
        return $"{provinceCode:D2} {randomLetters} {digits}";
    }
}

public enum FuelType
{
    Petrol,
    Diesel,
    Electric,
    Hybrid
}

public enum TransmissionType
{
    Manuel,
    Automatic,
    SemiAutomatic,
}
