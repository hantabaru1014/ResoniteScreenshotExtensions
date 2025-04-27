using FreeImageAPI;
using System.Xml.Linq;
using FrooxEngine;
using FreeImageAPI.Metadata;
using System.Linq;
using Elements.Core;
using System.IO;
using System.Text;
using System;

namespace ResoniteScreenshotExtensions;

public static class XmpMetadata
{
    internal const string rdfPrefixNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    internal const string thisModNamespace = "http://ns.baru.dev/resonite-ss-ext/1.0/";
    internal const string defaultXmpStr = $"<rdf:RDF xmlns:rdf=\"{rdfPrefixNamespace}\"></rdf:RDF>";

    // MetadataModel.GetTagArray だとunicodeで書き込んだものは取得できないので直接FreeImageAPIを使う
    public static string? LoadXmp(FIBITMAP dib)
    {
        FITAG tag;
        var mdhandle = FreeImage.FindFirstMetadata(FREE_IMAGE_MDMODEL.FIMD_XMP, dib, out tag);
        if (mdhandle.IsNull)
        {
            return null;
        }
        var metaTag = new MetadataTag(tag, dib);
        if (metaTag == null)
        {
            return null;
        }
        return Encoding.UTF8.GetString(metaTag.Value as byte[]);
    }

    public static bool SetXmp(FIBITMAP dib, string xml)
    {
        var tag = new MetadataTag(FREE_IMAGE_MDMODEL.FIMD_XMP);
        if (!tag.SetValue(Encoding.UTF8.GetBytes(xml), FREE_IMAGE_MDTYPE.FIDT_BYTE)) return false;
        return FreeImage.SetMetadata(FREE_IMAGE_MDMODEL.FIMD_XMP, dib, "XMLPacket", tag);
    }

    /// <summary>
    /// MDM_XMP だと非ASCII文字が化けるので、無理やりutf8で書き込むバージョンのMDM_XMP
    /// </summary>
    public class MDM_XMP_Unicode : MetadataModel
    {
        public override FREE_IMAGE_MDMODEL Model => FREE_IMAGE_MDMODEL.FIMD_XMP;

        public string Xml
        {
            get
            {
                return LoadXmp(dib) ?? "";
            }
            set
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                SetTagValue("XMLPacket", bytes);
            }
        }

        public MDM_XMP_Unicode(FIBITMAP dib)
        : base(dib)
        {
        }
    }

    public static void UpsertPhotoMetadata(FIBITMAP dib, PhotoMetadata photoMetadata)
    {
        var xmpMeta = new MDM_XMP_Unicode(dib);

        var xmpRoot = ParseOrDefaultXmp(xmpMeta.Xml);
        var content = DataTreeUtils.ToJson(DataTreeUtils.SaveComponent(photoMetadata));
        AddRdfDescription(xmpRoot, content);

        var result = xmpRoot.ToString(SaveOptions.DisableFormatting);
        xmpMeta.Xml = result;
        ResoniteScreenshotExtensions.Msg(result);
    }

    public static void SetXMP(FIBITMAP dib, string xml)
    {
        var xmpMeta = new MDM_XMP_Unicode(dib);
        xmpMeta.Xml = xml;
    }

    public static bool TryLoadPhotoMetadataSavedGraph(string filePath, out SavedGraph? savedGraph)
    {
        try
        {
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var dib = FreeImage.LoadFromStream(stream);
            stream.Close();

            return TryLoadPhotoMetadataSavedGraph(dib, out savedGraph);
        }
        catch
        {
            savedGraph = null;
        }
        return false;
    }

    public static bool TryLoadPhotoMetadataSavedGraph(FIBITMAP dib, out SavedGraph? savedGraph)
    {
        try
        {
            var xmpMeta = new MDM_XMP_Unicode(dib);
            var rawXml = xmpMeta.Xml;
            ResoniteScreenshotExtensions.Msg($"rawXml: '{rawXml}'");
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
        catch (Exception ex)
        {
            savedGraph = null;
            throw ex;
        }
        return false;
    }

    public static void AddRdfDescription(XElement xmpRoot, string contentJson)
    {
        var rdfNS = XNamespace.Get(rdfPrefixNamespace);
        var rdfName = rdfNS + "RDF";
        var rdf = xmpRoot.Elements().Count(e => e.Name == rdfName) == 1 ? xmpRoot.Element(rdfName) : xmpRoot;

        var description = new XElement(rdfNS + "Description");
        description.SetAttributeValue(rdfNS + "about", "ResoniteScreenshotExtensions");
        var ns = XNamespace.Get(thisModNamespace);
        description.SetAttributeValue(XNamespace.Xmlns + "resonite-ss-ext", ns);

        description.SetAttributeValue(ns + "PhotoMetadataJson", contentJson);
        description.SetAttributeValue(ns + "WorldName", "日本語テスト！ABC");

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

    public static XElement ParseOrDefaultXmp(string xmlStr)
    {
        if (string.IsNullOrEmpty(xmlStr))
        {
            return XElement.Parse(defaultXmpStr);
        }
        return XElement.Parse(xmlStr);
    }
}
