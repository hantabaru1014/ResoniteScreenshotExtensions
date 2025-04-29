using ResoniteModLoader;
using HarmonyLib;
using FrooxEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;

namespace ResoniteScreenshotExtensions;

public partial class ResoniteScreenshotExtensions : ResoniteMod
{
    [HarmonyPatch]
    class ImageImporter_Patch
    {
        static Type TargetClass()
        {
            return AccessTools.FirstInner(typeof(ImageImporter), t => t.Name.Contains(nameof(ImageImporter.ImportImage)));
        }

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(TargetClass(), "MoveNext");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var targetClass = TargetClass();
            var setupMethod = AccessTools.Method(typeof(ImageImporter), nameof(ImageImporter.SetupTextureProxyComponents));
            var loadThis = new CodeInstruction(OpCodes.Ldarg_0);

            return new CodeMatcher(instructions, generator)
                .SearchForward(code => code.Calls(setupMethod))
                .Advance(1)
                .Insert(
                    loadThis,
                    CodeInstruction.LoadField(targetClass, "targetSlot"),
                    loadThis,
                    CodeInstruction.LoadField(targetClass, "item"),
                    loadThis,
                    CodeInstruction.LoadField(targetClass, "setupScreenshotMetadata"),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImageImporter_Patch), nameof(LoadMetadata)))
                )
                .InstructionEnumeration();
        }

        static void LoadMetadata(Slot targetSlot, ImportItem item, bool skip)
        {
            if (skip || !(_config?.GetValue(LoadPhotoMetadataFromFileKey) ?? false) || string.IsNullOrEmpty(item.filePath)) return;
            if (XmpMetadata.TryLoadPhotoMetadata(item.filePath, targetSlot))
            {
                Msg("Loaded PhotoMetadata from XMP");
            }
        }
    }
}
