# ResoniteScreenshotExtensions

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that extensions of screenshot.

Warn: Conflicts with mods that patch WindowsPlatformConnector such as [SaveExif](https://github.com/rassi0429/SaveExif)

注意: [SaveExif](https://github.com/rassi0429/SaveExif) 等のWindowsPlatformConnectorにパッチを当てるModとは競合するのでどちらかにしてください！

## Current features
- Save FrooxEngine.PhotoMetadata to screenshot files (XMP)
- Restore FrooxEngine.PhotoMetadata from the screenshot file that saved by this mod
    - It will be restored by importing it as an image/texture as usual
- Dig the folder by month and date (yyyy-MM) when saving screenshot
- File name date and time becomes local one
- Selectable image formats: JPEG, WEBP, PNG
- Save webp as lossy (default is lossless. By saving as lossy, you can get a smaller file size and alpha than jpeg with the same image quality)
- Discord webhook integration (you can find in settings)
![Discord webhook integration](https://github.com/user-attachments/assets/ca3c2996-0259-4ee1-a4f6-82496aabd892)

XMP format
```xml
<rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
    <rdf:Description rdf:about="FrooxEngine PhotoMetadata" xmlns:resonite-ss-ext="http://ns.baru.dev/resonite-ss-ext/1.0/" resonite-ss-ext:PhotoMetadataJson="{&quot;VersionNumber&quot;:&quot;2024.3.1.1178&quot;,...}" />
</rdf:RDF>
```

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place [ResoniteScreenshotExtensions.dll](https://github.com/hantabaru1014/ResoniteScreenshotExtensions/releases/latest/download/ResoniteScreenshotExtensions.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
3. Start the game. If you want to verify that the mod is working you can check your Resonite logs.
