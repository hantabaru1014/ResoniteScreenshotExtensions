using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

namespace ResoniteScreenshotExtensions;

public partial class ResoniteScreenshotExtensions : ResoniteMod
{
    [HarmonyPatch(typeof(GenericOperatingSystemConnector))]
    class GenericOperatingSystemConnector_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GenericOperatingSystemConnector.NotifyOfScreenshot))]
        static bool NotifyOfScreenshot_Prefix()
        {
            // PhotoMetadataPatch.NotifyOfScreenshot_Postfix で代替しているのでこっちは無効化
            return !(_config?.GetValue(EnabledKey) ?? false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(GenericOperatingSystemConnector.Initialize))]
        static void Initialize_Postfix()
        {
            Settings.RegisterValueChanges<OperatingSystemSettings>(OnSettingsChanged);
        }

        private static void OnSettingsChanged(OperatingSystemSettings setting)
        {
            _keepOriginalScreenshotFormat = setting.KeepOriginalScreenshotFormat.Value;
        }
    }
}