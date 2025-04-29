using FreeImageAPI;
using System.Xml.Linq;
using FrooxEngine;
using FreeImageAPI.Metadata;
using System.Linq;
using Elements.Core;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using SkyFrost.Base;

namespace ResoniteScreenshotExtensions;

public static class XmpMetadata
{
    internal const string rdfPrefixNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    internal const string thisModNamespaceV1 = "http://ns.baru.dev/resonite-ss-ext/1.0/";
    internal const string thisModNamespaceV2 = "http://ns.baru.dev/resonite-ss-ext/2.0/";
    internal const string defaultXmpStr = $"<rdf:RDF xmlns:rdf=\"{rdfPrefixNamespace}\"></rdf:RDF>";

    static string EscapeUnicode(string str)
    {
        return Regex.Replace(str, @"\\u", "\\\\u").Select(c => {
            if (c > 0x7F)
            {
                return "\\u" + ((int)c).ToString("X4");
            }
            return c.ToString();
        }).Aggregate("", (acc, curr) => acc + curr);
    }

    static string UnescapeUnicode(string str)
    {
        var decoded = Regex.Replace(str, @"(?<!\\)\\u([0-9A-F]{4})", m =>
            ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
        return Regex.Replace(decoded, @"\\\\u", "\\u");
    }

    static XElement ParseOrDefaultXmp(string xmlStr)
    {
        if (string.IsNullOrEmpty(xmlStr))
        {
            return XElement.Parse(defaultXmpStr);
        }
        return XElement.Parse(xmlStr);
    }

    static void SerializeMetadataUser(XElement element, MetadataUser user)
    {
        var ns = XNamespace.Get(thisModNamespaceV2);
        void SetAttributeStr(string name, string? value)
        {
            element.SetAttributeValue(ns + name, value != null ? EscapeUnicode(value) : null);
        }

        SetAttributeStr("U-Id", user.Id);
        SetAttributeStr("U-Name", user.Name);
        SetAttributeStr("U-MachineId", user.MachineId);
    }

    static MetadataUser DeserializeMetadataUser(XElement element)
    {
        var ns = XNamespace.Get(thisModNamespaceV2);
        string? GetAttributeStr(string name, bool nullable)
        {
            var attr = element.Attribute(ns + name);
            if (attr != null)
            {
                return UnescapeUnicode(attr.Value);
            }
            if (nullable)
            {
                return null;
            }
            throw new Exception($"Attribute {name} not found in MetadataUser element.");
        }

        return new MetadataUser(
            Id: GetAttributeStr("U-Id", false)!,
            Name: GetAttributeStr("U-Name", true),
            MachineId: GetAttributeStr("U-MachineId", false)!
        );
    }

    static void SerializeMetadataUserInfo(XElement element, MetadataUserInfo userInfo)
    {
        var ns = XNamespace.Get(thisModNamespaceV2);
        void SetAttribute(string name, object? value)
        {
            element.SetAttributeValue(ns + name, value);
        }

        SerializeMetadataUser(element, userInfo.User);
        SetAttribute("UI-IsInVR", userInfo.IsInVR);
        SetAttribute("UI-IsPresent", userInfo.IsPresent);
        SetAttribute("UI-HeadPosition", userInfo.HeadPosition);
        SetAttribute("UI-HeadOrientation", userInfo.HeadOrientation);
        SetAttribute("UI-SessionJoinTimestamp", userInfo.SessionJoinTimestamp);
    }

    static MetadataUserInfo DeserializeMetadataUserInfo(XElement element)
    {
        var ns = XNamespace.Get(thisModNamespaceV2);
        string GetAttribute(string name)
        {
            var attr = element.Attribute(ns + name);
            if (attr != null)
            {
                return attr.Value;
            }
            throw new Exception($"Attribute {name} not found in MetadataUserInfo element.");
        }

        var user = DeserializeMetadataUser(element);
        
        return new MetadataUserInfo(
            User: user,
            IsInVR: bool.Parse(GetAttribute("UI-IsInVR")),
            IsPresent: bool.Parse(GetAttribute("UI-IsPresent")),
            HeadPosition: float3.Parse(GetAttribute("UI-HeadPosition")),
            HeadOrientation: floatQ.Parse(GetAttribute("UI-HeadOrientation")),
            SessionJoinTimestamp: DateTime.Parse(GetAttribute("UI-SessionJoinTimestamp"))
        );
    }

    static void SerializeMetadata(XElement element, Metadata metadata)
    {
        var ns = XNamespace.Get(thisModNamespaceV2);
        void SetAttributeStr(string name, string? value)
        {
            element.SetAttributeValue(ns + name, value != null ? EscapeUnicode(value) : null);
        }
        void SetAttribute(string name, object? value)
        {
            element.SetAttributeValue(ns + name, value);
        }

        SetAttributeStr("LocationName", metadata.LocationName);
        SetAttributeStr("LocationURL", metadata.LocationURL);

        var host = new XElement(ns + "LocationHost");
        SerializeMetadataUser(host, metadata.LocationHost);
        element.Add(host);

        SetAttribute("LocationAccessLevel", metadata.LocationAccessLevel);
        SetAttribute("LocationHiddenFromListing", metadata.LocationHiddenFromListing);
        SetAttribute("TimeTaken", metadata.TimeTaken);

        var takenBy = new XElement(ns + "TakenBy");
        SerializeMetadataUser(takenBy, metadata.TakenBy);
        element.Add(takenBy);

        SetAttribute("TakenGlobalPosition", metadata.TakenGlobalPosition);
        SetAttribute("TakenGlobalRotation", metadata.TakenGlobalRotation);
        SetAttribute("TakenGlobalScale", metadata.TakenGlobalScale);
        SetAttributeStr("AppVersion", metadata.AppVersion);

        var infos = new XElement(ns + "UserInfos");
        foreach (var info in metadata.UserInfos)
        {
            var infoElm = new XElement(ns + "UserInfo");
            SerializeMetadataUserInfo(infoElm, info);
            infos.Add(infoElm);
        }
        element.Add(infos);

        SetAttributeStr("CameraManufacturer", metadata.CameraManufacturer);
        SetAttributeStr("CameraModel", metadata.CameraModel);
        SetAttribute("CameraFOV", metadata.CameraFOV);
        SetAttribute("Is360", metadata.Is360);
        SetAttribute("StereoLayout", metadata.StereoLayout);
    }

    static Metadata DeserializeMetadata(XElement element)
    {
        var ns = XNamespace.Get(thisModNamespaceV2);
        string? GetAttributeStr(string name, bool nullable)
        {
            var attr = element.Attribute(ns + name);
            if (attr != null)
            {
                return UnescapeUnicode(attr.Value);
            }
            if (nullable)
            {
                return null;
            }
            throw new Exception($"Attribute {name} not found in MetadataUser element.");
        }
        string? GetAttribute(string name, bool nullable)
        {
            var attr = element.Attribute(ns + name);
            if (attr != null)
            {
                return attr.Value;
            }
            if (nullable)
            {
                return null;
            }
            throw new Exception($"Attribute {name} not found in Metadata element.");
        }

        var locationHostElement = element.Element(ns + "LocationHost");
        if (locationHostElement == null)
            throw new Exception("Required element LocationHost not found in Metadata element.");

        var takenByElement = element.Element(ns + "TakenBy");
        if (takenByElement == null)
            throw new Exception("Required element TakenBy not found in Metadata element.");

        var userInfosElement = element.Element(ns + "UserInfos");
        if (userInfosElement == null)
            throw new Exception("Required element UserInfos not found in Metadata element.");

        var userInfos = new List<MetadataUserInfo>();
        foreach (var info in userInfosElement.Elements(ns + "UserInfo"))
        {
            userInfos.Add(DeserializeMetadataUserInfo(info));
        }

        var accessLevelStr = GetAttribute("LocationAccessLevel", true);
        SessionAccessLevel? accessLevel = accessLevelStr != null ? (SessionAccessLevel)Enum.Parse(typeof(SessionAccessLevel), accessLevelStr) : null;

        var hiddenStr = GetAttribute("LocationHiddenFromListing", true);
        bool hidden = hiddenStr != null ? bool.Parse(hiddenStr) : false;

        return new Metadata(
            LocationName: GetAttributeStr("LocationName", false)!,
            LocationURL: GetAttributeStr("LocationURL", true),
            LocationHost: DeserializeMetadataUser(locationHostElement),
            LocationAccessLevel: accessLevel,
            LocationHiddenFromListing: hidden,
            TimeTaken: DateTime.Parse(GetAttribute("TimeTaken", false)),
            TakenBy: DeserializeMetadataUser(takenByElement),
            TakenGlobalPosition: float3.Parse(GetAttribute("TakenGlobalPosition", false)),
            TakenGlobalRotation: floatQ.Parse(GetAttribute("TakenGlobalRotation", false)),
            TakenGlobalScale: float3.Parse(GetAttribute("TakenGlobalScale", false)),
            AppVersion: GetAttributeStr("AppVersion", false)!,
            UserInfos: userInfos,
            CameraManufacturer: GetAttributeStr("CameraManufacturer", false)!,
            CameraModel: GetAttributeStr("CameraModel", false)!,
            CameraFOV: float.Parse(GetAttribute("CameraFOV", false)),
            Is360: bool.Parse(GetAttribute("Is360", false)),
            StereoLayout: (StereoLayout)Enum.Parse(typeof(StereoLayout), GetAttribute("StereoLayout", false))
        );
    }

    static void AddRdfDescription(XElement xmpRoot, PhotoMetadata photoMetadata)
    {
        var rdfNS = XNamespace.Get(rdfPrefixNamespace);
        var rdfName = rdfNS + "RDF";
        var rdf = xmpRoot.Elements().Count(e => e.Name == rdfName) == 1 ? xmpRoot.Element(rdfName) : xmpRoot;

        var description = new XElement(rdfNS + "Description");
        description.SetAttributeValue(rdfNS + "about", "");
        var ns = XNamespace.Get(thisModNamespaceV2);
        description.SetAttributeValue(XNamespace.Xmlns + "rse", ns);

        var metadata = new Metadata(photoMetadata);
        SerializeMetadata(description, metadata);

        rdf.Add(description);
    }

    public static void UpsertPhotoMetadata(FreeImageBitmap bmp, PhotoMetadata photoMetadata)
    {
        var fiMeta = bmp.Metadata;
        fiMeta.HideEmptyModels = false;
        var xmpMeta = (MDM_XMP)fiMeta[FREE_IMAGE_MDMODEL.FIMD_XMP];

        var xmpRoot = ParseOrDefaultXmp(xmpMeta.Xml);
        AddRdfDescription(xmpRoot, photoMetadata);

        xmpMeta.Xml = xmpRoot.ToString(SaveOptions.DisableFormatting);
    }

    static bool TryLoadV1Json(XElement xmpRoot, out string json)
    {
        var rdfNS = XNamespace.Get(rdfPrefixNamespace);
        var rdfName = rdfNS + "RDF";
        var rdf = xmpRoot.Elements().Count(e => e.Name == rdfName) == 1 ? xmpRoot.Element(rdfName) : xmpRoot;
        if (rdf != null)
        {
            var metadataAttrName = XNamespace.Get(thisModNamespaceV1) + "PhotoMetadataJson";
            var targetDescription = rdf.Elements(rdfNS + "Description").FirstOrDefault((e) => e.Attributes().Count(a => a.Name == metadataAttrName) == 1);
            if (targetDescription != null)
            {
                json = targetDescription.Attributes().First(a => a.Name == metadataAttrName).Value;
                return true;
            }
        }
        json = "";
        return false;
    }

    public static bool TryLoadMetadata(XElement xmpRoot, out Metadata? metadata)
    {
        var rdfNS = XNamespace.Get(rdfPrefixNamespace);
        var rdfName = rdfNS + "RDF";
        var rdf = xmpRoot.Elements().Count(e => e.Name == rdfName) == 1 ? xmpRoot.Element(rdfName) : xmpRoot;
        if (rdf != null)
        {
            var testAttrName = XNamespace.Get(thisModNamespaceV2) + "LocationName";
            var targetDescription = rdf.Elements(rdfNS + "Description").FirstOrDefault((e) => e.Attributes().Count(a => a.Name == testAttrName) == 1);
            if (targetDescription != null)
            {
                try
                {
                    metadata = DeserializeMetadata(targetDescription);
                }
                catch
                {
                    metadata = null;
                    return false;
                }
                return true;
            }
        }
        metadata = null;
        return false;
    }

    public static bool TryLoadPhotoMetadata(string filePath, Slot targetSlot)
    {
        try
        {
            using (var bmp = new FreeImageBitmap(filePath))
            {
                var xmpMeta = (MDM_XMP)bmp.Metadata[FREE_IMAGE_MDMODEL.FIMD_XMP];
                var rawXml = xmpMeta.Xml;
                if (!string.IsNullOrEmpty(rawXml))
                {
                    var xmpRoot = XElement.Parse(rawXml);
                    if (TryLoadMetadata(xmpRoot, out var metadata))
                    {
                        if (metadata == null) return false;
                        targetSlot.RunSynchronously(() =>
                        {
                            var attached = targetSlot.AttachComponent<PhotoMetadata>();
                            metadata.SetTo(ref attached);
                        });
                        return true;
                    }

                    if (TryLoadV1Json(xmpRoot, out var json))
                    {
                        var graph = DataTreeUtils.ToSavedGraph(json);
                        if (graph == null) return false;
                        targetSlot.RunSynchronously(() =>
                        {
                            targetSlot.AttachComponent<PhotoMetadata>(beforeAttach: c =>
                            {
                                DataTreeUtils.LoadComponent(c, graph);
                            });
                        });
                        return true;
                    }
                }
            }
        }
        catch
        {
            return false;
        }
        return false;
    }
}
