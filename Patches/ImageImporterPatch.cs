using ResoniteModLoader;
using HarmonyLib;
using FrooxEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System;

namespace ResoniteScreenshotExtensions;

public partial class ResoniteScreenshotExtensions : ResoniteMod
{
    static readonly ConditionalWeakTable<Slot, string> _importingImageFiles = new();

    [HarmonyPatch(typeof(ImageImportDialog), nameof(ImageImportDialog.RunImport), new Type[0])]
    class ImageImportDialog_RunImport_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();

            var slotFieldIndex = codes.FindIndex(c =>
                c.opcode == OpCodes.Stfld
                && c.operand is FieldInfo f
                && f.FieldType == typeof(Slot)
                && f.DeclaringType?.DeclaringType == typeof(ImageImportDialog)
                && f.DeclaringType.GetFields().Any(x => x.FieldType == typeof(ImportItem)));
            if (slotFieldIndex < 0)
            {
                throw new InvalidOperationException("ImageImportDialog.RunImport: target slot field not found.");
            }
            var slotField = (FieldInfo)codes[slotFieldIndex].operand;
            var closureType = slotField.DeclaringType!;
            var itemField = closureType.GetFields().First(x => x.FieldType == typeof(ImportItem));

            var ctorIndex = codes.FindLastIndex(slotFieldIndex, c =>
                c.opcode == OpCodes.Newobj && c.operand is ConstructorInfo ctor && ctor.DeclaringType == closureType);
            if (ctorIndex < 0 || ctorIndex + 1 >= codes.Count || !codes[ctorIndex + 1].IsStloc())
            {
                throw new InvalidOperationException("ImageImportDialog.RunImport: closure local not found.");
            }
            var closureLocal = codes[ctorIndex + 1].LocalIndex();

            codes.InsertRange(slotFieldIndex + 1, new[]
            {
                CodeInstruction.LoadLocal(closureLocal),
                new CodeInstruction(OpCodes.Ldfld, itemField),
                CodeInstruction.LoadLocal(closureLocal),
                new CodeInstruction(OpCodes.Ldfld, slotField),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImageImportDialog_RunImport_Patch), nameof(RegisterImportItem))),
            });
            return codes;
        }

        static void RegisterImportItem(ImportItem item, Slot slot)
        {
            try
            {
                if (slot == null || string.IsNullOrEmpty(item.filePath)) return;
                _importingImageFiles.AddOrUpdate(slot, item.filePath);
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }
    }

    [HarmonyPatch(typeof(ImageImporter), nameof(ImageImporter.SetupTextureProxyComponents))]
    class ImageImporter_SetupTextureProxyComponents_Patch
    {
        static void Postfix(Slot targetSlot, bool setupPhotoMetadata)
        {
            try
            {
                if (targetSlot == null || !_importingImageFiles.TryGetValue(targetSlot, out var filePath)) return;
                _importingImageFiles.Remove(targetSlot);
                LoadMetadata(targetSlot, filePath, setupPhotoMetadata);
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }

        static void LoadMetadata(Slot targetSlot, string filePath, bool skip)
        {
            if (skip || !(_config?.GetValue(LoadPhotoMetadataFromFileKey) ?? false) || string.IsNullOrEmpty(filePath)) return;
            if (XmpMetadata.TryLoadPhotoMetadata(filePath, targetSlot))
            {
                Msg("Loaded PhotoMetadata from XMP");
            }
        }
    }
}
