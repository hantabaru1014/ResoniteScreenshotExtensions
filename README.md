# ResoniteScreenshotExtensions

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that extensions of screenshot.

## Current features
- Save FrooxEngine.PhotoMetadata to screenshot files (XMP)
    - Saved as XMP format in the image file. You can check the embedded metadata at [https://rse.baru.dev/](https://rse.baru.dev/).
- Restore FrooxEngine.PhotoMetadata from the screenshot file that saved by this mod
    - It will be restored by importing it as an image/texture as usual
- Dig the folder by month and date (yyyy-MM) when saving screenshot
- Selectable image formats: JPEG, WEBP, PNG
- Save webp as lossy (default is lossless. By saving as lossy, you can get a smaller file size and alpha than jpeg with the same image quality)
- Speed up PNG and improve JPEG quality
- Discord webhook integration (you can find in settings)
![Discord webhook integration](https://github.com/user-attachments/assets/ca3c2996-0259-4ee1-a4f6-82496aabd892)

XMP sample (Unicode characters will be escaped)
```xml
<rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
    <rdf:Description rdf:about="" xmlns:rse="http://ns.baru.dev/resonite-ss-ext/2.0/"
        rse:LocationName="baru Home" rse:LocationURL="resrec:///U-hantabaru1014/R-Home"
        rse:LocationAccessLevel="Private" rse:LocationHiddenFromListing="false"
        rse:TimeTaken="2025-04-29T12:17:32.8670361Z"
        rse:TakenGlobalPosition="[-0.473039; 0.1036173; 0.6591971]"
        rse:TakenGlobalRotation="[0; 0.9999999; 0; -0.0005215902]"
        rse:TakenGlobalScale="[0.75; 0.75; 0.75]"
        rse:AppVersion="2025.4.10.1305+ResoniteModLoader.dll" rse:CameraManufacturer="Resonite"
        rse:CameraModel="PhotoCaptureManager" rse:CameraFOV="60" rse:Is360="false"
        rse:StereoLayout="None">
        <rse:LocationHost rse:U-Id="U-hantabaru1014" rse:U-Name="baru"
            rse:U-MachineId="wofiumn6rddzk9qwfmb3ui6c4w9emkd5ri1my9934k8k6415f3so" />
        <rse:TakenBy rse:U-Id="U-hantabaru1014" rse:U-Name="baru"
            rse:U-MachineId="wofiumn6rddzk9qwfmb3ui6c4w9emkd5ri1my9934k8k6415f3so" />
        <rse:UserInfos>
            <rse:UserInfo rse:U-Id="U-hantabaru1014" rse:U-Name="baru"
                rse:U-MachineId="wofiumn6rddzk9qwfmb3ui6c4w9emkd5ri1my9934k8k6415f3so"
                rse:UI-IsInVR="false" rse:UI-IsPresent="true"
                rse:UI-HeadPosition="[-0.4432995; 1.324758; 0.5819932]"
                rse:UI-HeadOrientation="[-0.01243596; 0.978942; 0.0007779225; 0.2037579]"
                rse:UI-SessionJoinTimestamp="2025-04-29T11:58:03.5926304Z" />
        </rse:UserInfos>
    </rdf:Description>
</rdf:RDF>
```

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place [ResoniteScreenshotExtensions.dll](https://github.com/hantabaru1014/ResoniteScreenshotExtensions/releases/latest/download/ResoniteScreenshotExtensions.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
3. Start the game. If you want to verify that the mod is working you can check your Resonite logs.
