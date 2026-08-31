using System.Diagnostics;
using System.Text;
using InGame.Jewelry.Common;
using September.InGame.Jewelry.Drop.Strategies.Amounts;
using September.InGame.Jewelry.Drop.Strategies.Chances;
using Debug = UnityEngine.Debug;

namespace September.InGame.Jewelry.Drop
{
    public static class JewelryDropLogger
    {
        private static readonly StringBuilder SettingsSb = new();
        private static readonly StringBuilder StrategySb = new();

        [Conditional("UNITY_EDITOR"), Conditional("DEBUG")]
        public static void JoinSettingsLog(JewelryType jewelryType, int dropAmount)
        {
            if (SettingsSb.Length == 0)
            {
                SettingsSb.AppendLine("[JewelryDropLogger] log:");
            }
            SettingsSb.AppendLine($"jewelryType:{jewelryType}, resultAmount:{dropAmount}");
            SettingsSb.Append(StrategySb.ToString());
            StrategySb.Clear();
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEBUG")]
        public static void AppendLog(IJewelryDropChance chance, float chanceAmount)
        {
            StrategySb.AppendLine($"- (chance) class:{chance.GetType().Name}, amount:{chanceAmount}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEBUG")]
        public static void AppendLog(IJewelryDropAmount drop, int dropAmount)
        {
            StrategySb.AppendLine($"- (amount) class:{drop.GetType().Name}, amount:{dropAmount}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEBUG")]
        public static void OutputLog()
        {
            Debug.Log(SettingsSb.ToString());
            SettingsSb.Clear();
            StrategySb.Clear();
        }
    }
}
