using HarmonyLib;
using ResoniteModLoader;

namespace ResoniteScreenshotExtensions;

public partial class ResoniteScreenshotExtensions : ResoniteMod
{
    public override string Name => "ResoniteScreenshotExtensions";
    public override string Author => "hantabaru1014";
    public override string Version => "1.1.3";
    public override string Link => "https://github.com/hantabaru1014/ResoniteScreenshotExtensions";

    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<bool> EnabledKey =
        new ModConfigurationKey<bool>("Enabled", "Mod enabled", () => true);
    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<ImageFormat> ImageFormatKey =
        new ModConfigurationKey<ImageFormat>("ImageFormat", "Image format of screenshot to save", () => ImageFormat.JPEG);
    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<bool> LossyWebpKey =
        new ModConfigurationKey<bool>("LossyWebp", "Save in Lossy when saving in Webp", () => false);
    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<int> LossyWebpQualityKey =
        new ModConfigurationKey<int>("LossyWebpQuality", "└ Lossy webp quality", () => 80);
    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<bool> SavePhotoMetadataToFileKey =
        new ModConfigurationKey<bool>("SavePhotoMetadataToFile", "Save PhotoMetadata to file", () => true);
    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<bool> LoadPhotoMetadataFromFileKey =
        new ModConfigurationKey<bool>("LoadPhotoMetadataFromFile", "Load PhotoMetadata from file", () => true);
    [AutoRegisterConfigKey]
    public static readonly ModConfigurationKey<bool> DigFolderWhenSavingKey =
        new ModConfigurationKey<bool>("DigFolderWhenSaving", "Dig the folder by month and date when saving", () => true);

    private static bool _keepOriginalScreenshotFormat = false;
    private static ModConfiguration? _config;

    public enum ImageFormat
    {
        JPEG, WEBP, PNG
    }

    public override void OnEngineInit()
    {
        _config = GetConfiguration();

        Harmony harmony = new Harmony("dev.baru.resonite.ResoniteScreenshotExtensions");
        harmony.PatchAll();
    }
}
