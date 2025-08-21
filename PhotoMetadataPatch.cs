using Elements.Assets;
using Elements.Core;
using FreeImageAPI;
using FrooxEngine;
using HarmonyLib;
using MimeDetective;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ResoniteScreenshotExtensions.DiscordWebhookClient;

namespace ResoniteScreenshotExtensions;

public partial class ResoniteScreenshotExtensions : ResoniteMod
{
    [HarmonyPatch(typeof(PhotoMetadata))]
    class PhotoMetadata_Patch
    {
        const string MENU_ITEM_TAG = "RSE_POST_TO_DISCORD";
        static readonly Uri DISCORD_ICON_URI = new Uri("resdb:///f6c1a66250d366d213789faefab59a9ec6ac6334e6c4be0af254680c148810dc.png");

        static void PostToDiscord(Metadata metadata, string filePath)
        {
            if (_config == null || (_config.GetValue(DiscordWebhookUrlKey) ?? "").Length == 0) return;

            var fields = new List<EmbedField>();
            if (_config.GetValue(DiscordWebhookEmbedLocationNameKey))
            {
                fields.Add(new EmbedField("LocationName", SanitizeText(metadata.LocationName)));
            }
            if (_config.GetValue(DiscordWebhookEmbedLocationHostKey))
            {
                fields.Add(new EmbedField("LocationHost", SanitizeText(metadata.LocationHost.Name ?? metadata.LocationHost.Id)));
            }
            fields.Add(new EmbedField("TakenBy", SanitizeText(metadata.TakenBy.Name ?? metadata.TakenBy.Id)));
            var unixTimestamp = (int)metadata.TimeTaken.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            fields.Add(new EmbedField("TimeTaken", $"<t:{unixTimestamp}>"));
            if (_config.GetValue(DiscordWebhookEmbedUsersKey))
            {
                fields.Add(new EmbedField("Users", metadata.UserInfos.Select(u => SanitizeText(u.User.Name ?? u.User.Id)).Aggregate((acc, curr) => $"{acc}, {curr}")));
            }

            var client = new DiscordWebhookClient(_config?.GetValue(DiscordWebhookUrlKey) ?? "");
            try
            {
                _ = client.SendImageWithMetadata(filePath, fields).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Error("Failed to send image to Discord: " + t.Exception);
                    }
                    else
                    {
                        if (t.Result)
                        {
                            Msg("Image sent to Discord successfully.");
                        }
                        else
                        {
                            Error("Failed to send image to Discord");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }

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
                        bmp.Save(dstPath, FREE_IMAGE_FORMAT.FIF_JPEG, (FREE_IMAGE_SAVE_FLAGS)95 | FREE_IMAGE_SAVE_FLAGS.JPEG_SUBSAMPLING_444 | FREE_IMAGE_SAVE_FLAGS.JPEG_PROGRESSIVE);
                        break;
                    case ImageFormat.WEBP:
                        FREE_IMAGE_SAVE_FLAGS quality = (_config?.GetValue(LossyWebpKey) ?? false) ? (FREE_IMAGE_SAVE_FLAGS)_config.GetValue(LossyWebpQualityKey) : FREE_IMAGE_SAVE_FLAGS.WEBP_LOSSLESS;
                        bmp.Save(dstPath, FreeImage.GetFIFFromFormat("webp"), quality);
                        break;
                    case ImageFormat.PNG:
                        bmp.Save(dstPath, FREE_IMAGE_FORMAT.FIF_PNG, (FREE_IMAGE_SAVE_FLAGS)4);
                        break;
                }
            }

            if (_config != null && _config.GetValue(DiscordWebhookAutoUploadKey) && (_config.GetValue(DiscordWebhookUrlKey) ?? "").Length > 0)
            {
                PostToDiscord(new Metadata(photoMetadata), srcPath);
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

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PhotoMetadata.GenerateMenuItems))]
        static void GenerateMenuItems_Postfix(PhotoMetadata __instance, ContextMenu menu)
        {
            if (!(_config?.GetValue(DiscordWebhookGenerateMenuKey) ?? false) || (_config.GetValue(DiscordWebhookUrlKey) ?? "").Length == 0) return;
            if (!__instance.Enabled) return;

            var item = menu.Slot.GetComponentInChildren<ContextMenuItem>((i) => i.Slot.Tag == MENU_ITEM_TAG);
            if (item == null)
            {
                item = menu.AddItem("Post to Discord", DISCORD_ICON_URI, null);
                item.Slot.Tag = MENU_ITEM_TAG;
            }
            item.Button.LocalPressed += (button, eventData) =>
            {
                __instance.LocalUser.CloseContextMenu(null);
                Msg("Posting to Discord...");
                __instance.StartGlobalTask(async () =>
                {
                    var tex = __instance.Slot.GetComponent<StaticTexture2D>();
                    var url = tex?.URL.Value;
                    if (url is null) return;

                    await new ToBackground();
                    var tmpPath = await __instance.Engine.AssetManager.GatherAssetFile(url, 100f);
                    if (tmpPath is null) return;

                    PostToDiscord(new Metadata(__instance), tmpPath);
                });
            };
        }

        private static string SanitizeText(string text)
        {
            return new StringRenderTree(text).GetRawString().Replace("\\", "");
        }
    }
}
