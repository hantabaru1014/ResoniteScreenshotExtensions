﻿using FreeImageAPI;
using FrooxEngine;
using HarmonyLib;
using System.IO;
using MimeDetective;
using ResoniteModLoader;
using System;
using Elements.Core;
using static ResoniteScreenshotExtensions.DiscordWebhookClient;
using System.Collections.Generic;
using System.Linq;

namespace ResoniteScreenshotExtensions;

public partial class ResoniteScreenshotExtensions : ResoniteMod
{
    [HarmonyPatch(typeof(PhotoMetadata))]
    class PhotoMetadata_Patch
    {
        static void SaveImage(PhotoMetadata photoMetadata, string srcPath, string dstPath, ImageFormat format)
        {
            using (var bmp = new FreeImageBitmap(srcPath))
            {
                if (_config?.GetValue(SavePhotoMetadataToFileKey) ?? false)
                {
                    XmpMetadata.UpsertPhotoMetadata(bmp, photoMetadata);
                }

                // EnsureNonHDR
                var imgType = bmp.ImageType;
                if (imgType == FREE_IMAGE_TYPE.FIT_RGBF || imgType == FREE_IMAGE_TYPE.FIT_RGBAF)
                {
                    bmp.TmoDrago03(0, 0);
                }

                // TextureEncoder.ConvertToJPG と同じ処理
                if (format == ImageFormat.JPEG)
                {
                    // Ensure24BPP
                    if (bmp.IsTransparent || bmp.ColorDepth > 24)
                    {
                        bmp.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_24_BPP);
                    }
                }

                switch (format)
                {
                    case ImageFormat.JPEG:
                        bmp.Save(dstPath, FREE_IMAGE_FORMAT.FIF_JPEG, FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYSUPERB);
                        break;
                    case ImageFormat.WEBP:
                        FREE_IMAGE_SAVE_FLAGS quality = (_config?.GetValue(LossyWebpKey) ?? false) ? (FREE_IMAGE_SAVE_FLAGS)_config.GetValue(LossyWebpQualityKey) : FREE_IMAGE_SAVE_FLAGS.WEBP_LOSSLESS;
                        bmp.Save(dstPath, FreeImage.GetFIFFromFormat("webp"), quality);
                        break;
                    case ImageFormat.PNG:
                        bmp.Save(dstPath, FREE_IMAGE_FORMAT.FIF_PNG, FREE_IMAGE_SAVE_FLAGS.PNG_Z_BEST_COMPRESSION);
                        break;
                }
            }

            if (_config != null && _config.GetValue(DiscordWebhookAutoUploadKey) && (_config.GetValue(DiscordWebhookUrlKey) ?? "").Length > 0)
            {
                var fields = new List<EmbedField>();
                if (_config.GetValue(DiscordWebhookEmbedLocationNameKey))
                {
                    fields.Add(new EmbedField("LocationName", photoMetadata.LocationName));
                }
                if (_config.GetValue(DiscordWebhookEmbedLocationHostKey))
                {
                    fields.Add(new EmbedField("LocationHost", photoMetadata.LocationHost.Target.UserName));
                }
                fields.Add(new EmbedField("TakenBy", photoMetadata.TakenBy.Target.UserName));
                var unixTimestamp = (int)(photoMetadata.TimeTaken.Value.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                fields.Add(new EmbedField("TimeTaken", $"<t:{unixTimestamp}>"));
                if (_config.GetValue(DiscordWebhookEmbedUsersKey))
                {
                    fields.Add(new EmbedField("Users", photoMetadata.UserInfos.Select(u => u.User.Target.UserName).Aggregate((acc, curr) => $"{acc}, {curr}")));
                }

                var client = new DiscordWebhookClient(_config?.GetValue(DiscordWebhookUrlKey) ?? "");
                try
                {
                    _ = client.SendImageWithMetadata(srcPath, fields);
                }
                catch (Exception ex)
                {
                    Error(ex);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PhotoMetadata.NotifyOfScreenshot))]
        static void NotifyOfScreenshot_Postfix(PhotoMetadata __instance)
        {
            if (!(_config?.GetValue(EnabledKey) ?? false)) return;

            // PhotoMetadata を WindowsPlatformConnector.NotifyOfScreenshot に確実に渡すのが面倒なのでここで代替する
            __instance.StartGlobalTask(async () =>
            {
                try
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
                    var format = _config?.GetValue(ImageFormatKey) ?? ImageFormat.JPEG;
                    string extension = _keepOriginalScreenshotFormat ? Path.GetExtension(tmpPath) : format switch
                    {
                        ImageFormat.JPEG => ".jpg",
                        ImageFormat.WEBP => ".webp",
                        ImageFormat.PNG => ".png",
                        _ => ".jpg"
                    };
                    if (string.IsNullOrWhiteSpace(extension))
                    {
                        FileType fileType = new FileInfo(tmpPath).GetFileType();
                        if (fileType != null)
                            extension = "." + fileType.Extension;
                    }
                    extension = extension.ToLower();
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

                        if (_keepOriginalScreenshotFormat)
                        {
                            if (extension != ".jpg" && extension != ".webp" && extension != ".png")
                            {
                                File.Copy(tmpPath, str1);
                                File.SetAttributes(str1, FileAttributes.Normal);
                                Msg($"{str1} is an unsupported format, so metadata was not saved.");
                            }
                            else
                            {
                                var extFormat = extension switch
                                {
                                    ".jpg" => ImageFormat.JPEG,
                                    ".webp" => ImageFormat.WEBP,
                                    ".png" => ImageFormat.PNG,
                                    _ => ImageFormat.JPEG
                                };
                                SaveImage(__instance, tmpPath, str1, extFormat);
                            }
                        }
                        else
                        {
                            SaveImage(__instance, tmpPath, str1, format);
                        }
                    }
                    catch (Exception ex)
                    {
                        Error("Exception saving screenshot to Windows:\n" + ex);
                        _config?.Set(EnabledKey, false);
                        NotificationMessage.SpawnTextMessage("[ScreenshotExtensions] Failed saving screenshot!", colorX.Red);
                    }
                    finally
                    {
                        WindowsPlatformConnector.ScreenshotSemaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    Error("Exception saving screenshot to Windows:\n" + ex);
                    _config.Set(EnabledKey, false);
                    NotificationMessage.SpawnTextMessage("[ScreenshotExtensions] Failed saving screenshot!", colorX.Red);
                }
            });
        }
    }
}
