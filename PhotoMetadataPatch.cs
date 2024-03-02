using FreeImageAPI;
using FrooxEngine;
using HarmonyLib;
using System.IO;
using MimeDetective;
using ResoniteModLoader;
using System;

namespace ResoniteScreenshotExtensions;

public partial class ResoniteScreenshotExtensions : ResoniteMod
{
    [HarmonyPatch(typeof(PhotoMetadata))]
    class PhotoMetadata_Patch
    {
        static void SaveImage(PhotoMetadata photoMetadata, bool convertToJPG, string srcPath, string dstPath)
        {
            using (var bmp = new FreeImageBitmap(srcPath))
            {
                // TextureEncoder.ConvertToJPG と同じ処理
                if (convertToJPG)
                {
                    // EnsureNonHDR
                    var imgType = bmp.ImageType;
                    if (imgType == FREE_IMAGE_TYPE.FIT_RGBF || imgType == FREE_IMAGE_TYPE.FIT_RGBAF)
                    {
                        bmp.TmoDrago03(0, 0);
                    }

                    // Ensure24BPP
                    if (bmp.IsTransparent || bmp.ColorDepth > 24)
                    {
                        bmp.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_24_BPP);
                    }
                }

                if (_config?.GetValue(SavePhotoMetadataToFileKey) ?? false)
                {
                    XmpMetadata.UpsertPhotoMetadata(bmp, photoMetadata);
                }

                bmp.Save(dstPath);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PhotoMetadata.NotifyOfScreenshot))]
        static void NotifyOfScreenshot_Postfix(PhotoMetadata __instance)
        {
            // PhotoMetadata を WindowsPlatformConnector.NotifyOfScreenshot に確実に渡すのが面倒なのでここで代替する
            __instance.StartGlobalTask(async () =>
            {
                var tex = __instance.Slot.GetComponent<StaticTexture2D>();
                var url = tex?.URL.Value;
                if (url is null) return;

                await new ToBackground();
                // キャッシュが効いてるはずなので重複して実行しても大してコストはかからない認識
                var tmpPath = await __instance.Engine.AssetManager.GatherAssetFile(url, 100f);
                if (tmpPath is null) return;

                var timeTaken = __instance.TimeTaken.Value.ToLocalTime();
                string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                pictures = Path.Combine(pictures, __instance.Engine.Cloud.Platform.Name);
                if (_config?.GetValue(DigFolderWhenSavingKey) ?? false)
                {
                    pictures = Path.Combine(pictures, timeTaken.ToString("yyyy-MM"));
                }
                Directory.CreateDirectory(pictures);

                string filename = timeTaken.ToString("yyyy-MM-dd HH.mm.ss");
                string extension = _keepOriginalScreenshotFormat ? Path.GetExtension(tmpPath) : ".jpg";
                if (string.IsNullOrWhiteSpace(extension))
                {
                    FileType fileType = new FileInfo(tmpPath).GetFileType();
                    if (fileType != null)
                        extension = "." + fileType.Extension;
                }
                await WindowsPlatformConnector.ScreenshotSemaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    int num = 1;
                    string str1;
                    do
                    {
                        string str2 = filename;
                        if (num > 1)
                            str2 += string.Format(" ({0})", num);
                        str1 = Path.Combine(pictures, str2 + extension);
                        num++;
                    }
                    while (File.Exists(str1));

                    SaveImage(__instance, !_keepOriginalScreenshotFormat, tmpPath, str1);
                }
                catch (Exception ex)
                {
                    Error("Exception saving screenshot to Windows:\n" + ex);
                }
                finally
                {
                    WindowsPlatformConnector.ScreenshotSemaphore.Release();
                }
            });
        }
    }
}
