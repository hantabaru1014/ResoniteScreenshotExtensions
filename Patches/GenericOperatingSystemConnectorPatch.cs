using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;

namespace ResoniteScreenshotExtensions;

public partial class ResoniteScreenshotExtensions : ResoniteMod
{
    [HarmonyPatch(typeof(GenericOperatingSystemConnector))]
    class GenericOperatingSystemConnector_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GenericOperatingSystemConnector.NotifyOfScreenshot))]
        static bool NotifyOfScreenshot_Prefix(GenericOperatingSystemConnector __instance, World world, string file, ScreenshotType type, DateTime time)
        {
            try
            {
                // PhotoMetadataPatch.RunExport_Postfix で保存を引き受けたスクショのみ無効化する
                return !TryTakeOverDefaultSave(time, () => __instance.NotifyOfScreenshot(world, file, type, time));
            }
            catch (Exception ex)
            {
                Error(ex);
                return true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(GenericOperatingSystemConnector.RegisterOperatingSystemSettings))]
        static void RegisterOperatingSystemSettings_Postfix()
        {
            try
            {
                Settings.RegisterValueChanges<OperatingSystemSettings>(OnSettingsChanged);
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }

        private static void OnSettingsChanged(OperatingSystemSettings setting)
        {
            _keepOriginalScreenshotFormat = setting.KeepOriginalScreenshotFormat.Value;
        }
    }
}
