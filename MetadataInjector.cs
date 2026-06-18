using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FrooxEngine;

namespace ResoniteScreenshotExtensions;

public static class MetadataInjector
{
    public static byte[] InjectXmp(byte[] originalBytes, ResoniteScreenshotExtensions.ImageFormat format, Metadata metadata, int width, int height, bool isTransparent)
    {
        try
        {
            string xmpXml = XmpMetadata.GetXmpXmlString(metadata);
            switch (format)
            {
                case ResoniteScreenshotExtensions.ImageFormat.JPEG:
                    return InjectIntoJpeg(originalBytes, xmpXml);
                case ResoniteScreenshotExtensions.ImageFormat.PNG:
                    return InjectIntoPng(originalBytes, xmpXml);
                case ResoniteScreenshotExtensions.ImageFormat.WEBP:
                    return InjectIntoWebp(originalBytes, xmpXml, width, height, isTransparent);
            }
        }
        catch (Exception ex)
        {
            ResoniteScreenshotExtensions.Error($"Failed to inject XMP metadata: {ex}");
        }
        return originalBytes;
    }

    private static byte[] InjectIntoJpeg(byte[] originalBytes, string xmpXml)
    {
        if (originalBytes.Length < 4 || originalBytes[0] != 0xFF || originalBytes[1] != 0xD8)
        {
            throw new InvalidDataException("Invalid JPEG SOI marker.");
        }

        byte[] xmpBytes = Encoding.UTF8.GetBytes(xmpXml);
        byte[] namespaceBytes = Encoding.UTF8.GetBytes("http://ns.adobe.com/xap/1.0/\0");
        int payloadLength = namespaceBytes.Length + xmpBytes.Length;
        int segmentLength = payloadLength + 2;

        if (segmentLength > 65535)
        {
            throw new InvalidOperationException("XMP metadata is too large for JPEG APP1 segment.");
        }

        using (var ms = new MemoryStream())
        {
            ms.WriteByte(0xFF);
            ms.WriteByte(0xD8);

            ms.WriteByte(0xFF);
            ms.WriteByte(0xE1);
            ms.WriteByte((byte)((segmentLength >> 8) & 0xFF));
            ms.WriteByte((byte)(segmentLength & 0xFF));
            ms.Write(namespaceBytes, 0, namespaceBytes.Length);
            ms.Write(xmpBytes, 0, xmpBytes.Length);

            ms.Write(originalBytes, 2, originalBytes.Length - 2);

            return ms.ToArray();
        }
    }

    private static byte[] InjectIntoPng(byte[] originalBytes, string xmpXml)
    {
        byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        for (int i = 0; i < signature.Length; i++)
        {
            if (originalBytes[i] != signature[i])
                throw new InvalidDataException("Invalid PNG signature.");
        }

        byte[] keywordBytes = Encoding.UTF8.GetBytes("XML:com.adobe.xmp\0");
        byte[] xmpBytes = Encoding.UTF8.GetBytes(xmpXml);
        byte[] chunkData = new byte[keywordBytes.Length + 4 + xmpBytes.Length];

        Buffer.BlockCopy(keywordBytes, 0, chunkData, 0, keywordBytes.Length);
        chunkData[keywordBytes.Length] = 0;     // Compression flag
        chunkData[keywordBytes.Length + 1] = 0; // Compression method
        chunkData[keywordBytes.Length + 2] = 0; // Language tag
        chunkData[keywordBytes.Length + 3] = 0; // Translated keyword
        Buffer.BlockCopy(xmpBytes, 0, chunkData, keywordBytes.Length + 4, xmpBytes.Length);

        byte[] typeBytes = Encoding.UTF8.GetBytes("iTXt");
        uint crc = CalculateCrc32(typeBytes, chunkData);

        using (var ms = new MemoryStream())
        {
            ms.Write(originalBytes, 0, 33); // Signature (8 bytes) + IHDR (25 bytes)

            byte[] lenBytes = {
                (byte)((chunkData.Length >> 24) & 0xFF),
                (byte)((chunkData.Length >> 16) & 0xFF),
                (byte)((chunkData.Length >> 8) & 0xFF),
                (byte)(chunkData.Length & 0xFF)
            };
            ms.Write(lenBytes, 0, lenBytes.Length);
            ms.Write(typeBytes, 0, typeBytes.Length);
            ms.Write(chunkData, 0, chunkData.Length);

            byte[] crcBytes = {
                (byte)((crc >> 24) & 0xFF),
                (byte)((crc >> 16) & 0xFF),
                (byte)((crc >> 8) & 0xFF),
                (byte)(crc & 0xFF)
            };
            ms.Write(crcBytes, 0, crcBytes.Length);

            ms.Write(originalBytes, 33, originalBytes.Length - 33);

            return ms.ToArray();
        }
    }

    private static uint CalculateCrc32(byte[] typeBytes, byte[] dataBytes)
    {
        uint[] table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint entry = i;
            for (int j = 0; j < 8; j++)
            {
                if ((entry & 1) == 1)
                    entry = (entry >> 1) ^ 0xEDB88320;
                else
                    entry >>= 1;
            }
            table[i] = entry;
        }

        uint crc = 0xFFFFFFFF;
        foreach (byte b in typeBytes)
        {
            crc = (crc >> 8) ^ table[(crc ^ b) & 0xFF];
        }
        foreach (byte b in dataBytes)
        {
            crc = (crc >> 8) ^ table[(crc ^ b) & 0xFF];
        }
        return crc ^ 0xFFFFFFFF;
    }

    private class WebpChunk
    {
        public string FourCC;
        public byte[] Data;

        public WebpChunk(string fourCC, byte[] data)
        {
            FourCC = fourCC;
            Data = data;
        }
    }

    private static byte[] InjectIntoWebp(byte[] originalBytes, string xmpXml, int width, int height, bool isTransparent)
    {
        if (originalBytes.Length < 12 ||
            originalBytes[0] != 'R' || originalBytes[1] != 'I' || originalBytes[2] != 'F' || originalBytes[3] != 'F' ||
            originalBytes[8] != 'W' || originalBytes[9] != 'E' || originalBytes[10] != 'B' || originalBytes[11] != 'P')
        {
            throw new InvalidDataException("Invalid WebP container.");
        }

        var chunks = new List<WebpChunk>();
        int offset = 12;
        while (offset < originalBytes.Length)
        {
            if (offset + 8 > originalBytes.Length)
            {
                break;
            }

            string fourCC = Encoding.ASCII.GetString(originalBytes, offset, 4);
            uint size = BitConverter.ToUInt32(originalBytes, offset + 4);
            offset += 8;

            if (offset + size > originalBytes.Length)
            {
                break;
            }

            byte[] chunkData = new byte[size];
            Buffer.BlockCopy(originalBytes, offset, chunkData, 0, (int)size);
            chunks.Add(new WebpChunk(fourCC, chunkData));

            offset += (int)((size + 1) & ~1);
        }

        var vp8xChunk = chunks.Find(c => c.FourCC == "VP8X");
        if (vp8xChunk != null)
        {
            vp8xChunk.Data[0] |= 0x04; // XMP flag (0x04)
        }
        else
        {
            byte[] vp8xData = new byte[10];
            vp8xData[0] = (byte)(0x04 | (isTransparent ? 0x10 : 0x00)); // XMP_FLAG (0x04) | (ALPHA_FLAG (0x10) if transparent)
            int wMinusOne = width - 1;
            int hMinusOne = height - 1;
            vp8xData[4] = (byte)(wMinusOne & 0xFF);
            vp8xData[5] = (byte)((wMinusOne >> 8) & 0xFF);
            vp8xData[6] = (byte)((wMinusOne >> 16) & 0xFF);
            vp8xData[7] = (byte)(hMinusOne & 0xFF);
            vp8xData[8] = (byte)((hMinusOne >> 8) & 0xFF);
            vp8xData[9] = (byte)((hMinusOne >> 16) & 0xFF);

            chunks.Insert(0, new WebpChunk("VP8X", vp8xData));
        }

        chunks.RemoveAll(c => c.FourCC == "XMP ");
        chunks.Add(new WebpChunk("XMP ", Encoding.UTF8.GetBytes(xmpXml)));

        using (var ms = new MemoryStream())
        {
            ms.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
            ms.Write(new byte[4], 0, 4); // File size placeholder
            ms.Write(Encoding.ASCII.GetBytes("WEBP"), 0, 4);

            foreach (var chunk in chunks)
            {
                byte[] fccBytes = Encoding.ASCII.GetBytes(chunk.FourCC);
                byte[] lenBytes = BitConverter.GetBytes((uint)chunk.Data.Length);
                ms.Write(fccBytes, 0, 4);
                ms.Write(lenBytes, 0, 4);
                ms.Write(chunk.Data, 0, chunk.Data.Length);
                if ((chunk.Data.Length & 1) != 0)
                {
                    ms.WriteByte(0);
                }
            }

            byte[] finalBytes = ms.ToArray();
            uint finalSize = (uint)finalBytes.Length - 8;
            byte[] sizeBytes = BitConverter.GetBytes(finalSize);
            Buffer.BlockCopy(sizeBytes, 0, finalBytes, 4, 4);

            return finalBytes;
        }
    }
}
