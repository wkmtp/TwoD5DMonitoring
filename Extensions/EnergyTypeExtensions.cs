using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Extensions
{
    public static class EnergyTypeExtensions
    {
        public static string GetUnit(this EnergyType energyType)
        {
            return energyType switch
            {
                EnergyType.Electricity => "kWh",
                EnergyType.Gas => "Nm³",
                EnergyType.Water => "m³",
                EnergyType.Steam => "kg",
                EnergyType.CompressedAir => "Nm³",
                _ => ""
            };
        }
    }
}