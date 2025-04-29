using Elements.Core;
using FrooxEngine;
using SkyFrost.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ResoniteScreenshotExtensions;

public record MetadataUser(
    string Id,
    string? Name,
    string MachineId
)
{
    public MetadataUser(UserRef user) : this(
        user.LinkedCloudId,
        user.Target?.UserName,
        user.LinkedMachineId
    )
    { }

    public void SetTo(ref UserRef userRef)
    {
        userRef.SetFromCloudId(Id);
        userRef.SetFromMachineId(MachineId);
    }
}

public record MetadataUserInfo(
    MetadataUser User,
    bool IsInVR,
    bool IsPresent,
    float3 HeadPosition,
    floatQ HeadOrientation,
    DateTime SessionJoinTimestamp
)
{
    public MetadataUserInfo(AssetMetadata.UserInfo userInfo) : this(
        new MetadataUser(userInfo.User),
        userInfo.IsInVR,
        userInfo.IsPresent,
        userInfo.HeadPosition,
        userInfo.HeadOrientation,
        userInfo.SessionJoinTimestamp
    )
    { }

    public void SetTo(ref AssetMetadata.UserInfo userInfo)
    {
        // TODO: 対象Userがワールドにいないときに設定できない
        var user = userInfo.User;
        User.SetTo(ref user);
        userInfo.IsInVR.Value = IsInVR;
        userInfo.IsPresent.Value = IsPresent;
        userInfo.HeadPosition.Value = HeadPosition;
        userInfo.HeadOrientation.Value = HeadOrientation;
        userInfo.SessionJoinTimestamp.Value = SessionJoinTimestamp;
    }
}

public record Metadata
(
   string LocationName,
   string? LocationURL,
   MetadataUser LocationHost,
   SessionAccessLevel? LocationAccessLevel,
   bool? LocationHiddenFromListing,
   DateTime TimeTaken,
   MetadataUser TakenBy,
   float3 TakenGlobalPosition,
   floatQ TakenGlobalRotation,
   float3 TakenGlobalScale,
   string AppVersion,
   IEnumerable<MetadataUserInfo> UserInfos,
   string CameraManufacturer,
   string CameraModel,
   float CameraFOV,
   bool Is360,
   StereoLayout StereoLayout
)
{
    public Metadata(PhotoMetadata photoMetadata) : this(
        photoMetadata.LocationName,
        photoMetadata.LocationURL.Value?.ToString(),
        new MetadataUser(photoMetadata.LocationHost),
        photoMetadata.LocationAccessLevel,
        photoMetadata.LocationHiddenFromListing,
        photoMetadata.TimeTaken.Value,
        new MetadataUser(photoMetadata.TakenBy),
        photoMetadata.TakenGlobalPosition,
        photoMetadata.TakenGlobalRotation,
        photoMetadata.TakenGlobalScale,
        photoMetadata.AppVersion,
        photoMetadata.UserInfos.Select(userInfo => new MetadataUserInfo(userInfo)),
        photoMetadata.CameraManufacturer,
        photoMetadata.CameraModel,
        photoMetadata.CameraFOV,
        photoMetadata.Is360,
        photoMetadata.StereoLayout
    )
    { }

    public void SetTo(ref PhotoMetadata photoMetadata)
    {
        photoMetadata.LocationName.Value = LocationName;
#pragma warning disable CS8601 // Null 参照代入の可能性があります。
        photoMetadata.LocationURL.Value = LocationURL != null ? new Uri(LocationURL) : null;
#pragma warning restore CS8601 // Null 参照代入の可能性があります。
        var locationHost = photoMetadata.LocationHost;
        LocationHost.SetTo(ref locationHost);
        photoMetadata.LocationAccessLevel.Value = LocationAccessLevel;
        photoMetadata.LocationHiddenFromListing.Value = LocationHiddenFromListing;
        photoMetadata.TimeTaken.Value = TimeTaken;
        var takenBy = photoMetadata.TakenBy;
        TakenBy.SetTo(ref takenBy);
        photoMetadata.TakenGlobalPosition.Value = TakenGlobalPosition;
        photoMetadata.TakenGlobalRotation.Value = TakenGlobalRotation;
        photoMetadata.TakenGlobalScale.Value = TakenGlobalScale;
        photoMetadata.AppVersion.Value = AppVersion;
        photoMetadata.CameraManufacturer.Value = CameraManufacturer;
        photoMetadata.CameraModel.Value = CameraModel;
        photoMetadata.CameraFOV.Value = CameraFOV;
        photoMetadata.Is360.Value = Is360;
        photoMetadata.StereoLayout.Value = StereoLayout;

        foreach (var metaInfo in UserInfos)
        {
            var info = photoMetadata.UserInfos.Add();
            metaInfo.SetTo(ref info);
        }
    }
}
