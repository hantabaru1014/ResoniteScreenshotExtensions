using HarmonyLib;
using ResoniteModLoader;
using System.Threading.Tasks;

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
        static void Initialize_Postfix(WindowsPlatformConnector __instance)
        {
            var updateAction = () =>
            {
                Task.Run(async () =>
                {
                    var result = await __instance.Engine.LocalDB.TryReadVariableAsync<bool>(WindowsPlatformConnector.SCREENSHOT_FORMAT_SETTING);
                    if (result.hasValue)
                    {
                        _keepOriginalScreenshotFormat = result.value;
                    }
                });
            };
            updateAction();
            __instance.Engine.LocalDB.RegisterVariableListener(WindowsPlatformConnector.SCREENSHOT_FORMAT_SETTING, updateAction);
        }
    }
}