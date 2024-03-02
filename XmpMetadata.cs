using FreeImageAPI;
using System.Xml.Linq;
using FrooxEngine;
using FreeImageAPI.Metadata;
using System.Linq;
using Elements.Core;

namespace ResoniteScreenshotExtensions;

public static class XmpMetadata
{
    internal const string rdfPrefixNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    internal const string thisModNamespace = "http://ns.baru.dev/resonite-ss-ext/1.0/";
    internal const string defaultXmpStr = $"<rdf:RDF xmlns:rdf=\"{rdfPrefixNamespace}\"></rdf:RDF>";

    public static void UpsertPhotoMetadata(FreeImageBitmap bmp, PhotoMetadata photoMetadata)
    {
        var fiMeta = bmp.Metadata;
        fiMeta.HideEmptyModels = false;
        var xmpMeta = (MDM_XMP)fiMeta[FREE_IMAGE_MDMODEL.FIMD_XMP];

        var xmpRoot = ParseOrDefaultXmp(xmpMeta.Xml);
        var content = DataTreeUtils.ToJson(DataTreeUtils.SaveComponent(photoMetadata));
        AddRdfDescription(xmpRoot, content);

        xmpMeta.Xml = xmpRoot.ToString(SaveOptions.DisableFormatting);
    }

    public static bool TryLoadPhotoMetadataSavedGraph(string filePath, out SavedGraph? savedGraph)
    {
        try
        {
            using (var bmp = new FreeImageBitmap(filePath))
            {
                return TryLoadPhotoMetadataSavedGraph(bmp, out savedGraph);
            }
        }
        catch
        {
            savedGraph = null;
        }
        return false;
    }

    public static bool TryLoadPhotoMetadataSavedGraph(FreeImageBitmap bmp, out SavedGraph? savedGraph)
    {
        try
        {
            var xmpMeta = (MDM_XMP)bmp.Metadata[FREE_IMAGE_MDMODEL.FIMD_XMP];
            var rawXml = xmpMeta.Xml;
            if (!string.IsNullOrEmpty(rawXml))
            {
                if (TryLoadContentJson(XElement.Parse(rawXml), out var json))
                {
                    var graph = DataTreeUtils.ToSavedGraph(json);
                    if (graph != null)
                    {
                        savedGraph = graph;
                        return true;
                    }
                }
            }
            savedGraph = null;
            return false;
        }
        catch
        {
            savedGraph = null;
        }
        return false;
    }

    static void AddRdfDescription(XElement xmpRoot, string contentJson)
    {
        var rdfNS = XNamespace.Get(rdfPrefixNamespace);
        var rdfName = rdfNS + "RDF";
        var rdf = xmpRoot.Elements().Count(e => e.Name == rdfName) == 1 ? xmpRoot.Element(rdfName) : xmpRoot;

        var description = new XElement(rdfNS + "Description");
        description.SetAttributeValue(rdfNS + "about", "FrooxEngine PhotoMetadata");
        var ns = XNamespace.Get(thisModNamespace);
        description.SetAttributeValue(XNamespace.Xmlns + "resonite-ss-ext", ns);
        description.SetAttributeValue(ns + "PhotoMetadataJson", contentJson);

        rdf.Add(description);
    }

    static bool TryLoadContentJson(XElement xmpRoot, out string json)
    {
        var rdfNS = XNamespace.Get(rdfPrefixNamespace);
        var rdfName = rdfNS + "RDF";
        var rdf = xmpRoot.Elements().Count(e => e.Name == rdfName) == 1 ? xmpRoot.Element(rdfName) : xmpRoot;
        if (rdf != null)
        {
            var metadataAttrName = XNamespace.Get(thisModNamespace) + "PhotoMetadataJson";
            var targetDescription = rdf.Elements(rdfNS + "Description").First((e) => e.Attributes().Count(a => a.Name == metadataAttrName) == 1);
            if (targetDescription != null)
            {
                json = targetDescription.Attributes().First(a => a.Name == metadataAttrName).Value;
                return true;
            }
        }
        json = "";
        return false;
    }

    static XElement ParseOrDefaultXmp(string xmlStr)
    {
        if (string.IsNullOrEmpty(xmlStr))
        {
            return XElement.Parse(defaultXmpStr);
        }
        return XElement.Parse(xmlStr);
    }
}
