using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

namespace ResoniteScreenshotExtensions;

public partial class ResoniteScreenshotExtensions : ResoniteMod
{
    [HarmonyPatch(typeof(WindowsPlatformConnector))]
    class WindowsPlatformConnector_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(WindowsPlatformConnector.NotifyOfScreenshot))]
        static bool NotifyOfScreenshot_Prefix()
        {
            // NotifyOfScreenshot_Postfix で代替しているのでこっちは無効化
            return !(_config?.GetValue(EnabledKey) ?? false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(WindowsPlatformConnector.Initialize))]
        static void Initialize_Postfix()
        {
            Settings.RegisterValueChanges<WindowsSettings>(OnSettingsChanged);
        }

        private static void OnSettingsChanged(WindowsSettings setting)
        {
            _keepOriginalScreenshotFormat = setting.KeepOriginalScreenshotFormat.Value;
        }
    }
}