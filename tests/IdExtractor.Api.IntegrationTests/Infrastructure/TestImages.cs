using System.Buffers.Binary;
using System.IO.Compression;

namespace IdExtractor.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Builds a genuinely spec-compliant PNG for tests. A hand-truncated "minimal" PNG
/// can pass casual viewers but gets rejected by PdfSharp's stricter decoder (this
/// bit us once already — see the Documents module report generator tests), so this
/// always emits a real, fully-valid image via .NET's own zlib implementation.
/// </summary>
public static class TestImages
{
    public static byte[] CreateValidPng(int width = 16, int height = 16)
    {
        var raw = new byte[height * (1 + (width * 3))];
        var offset = 0;
        for (var y = 0; y < height; y++)
        {
            raw[offset++] = 0; // no filter
            for (var x = 0; x < width; x++)
            {
                raw[offset++] = 255;
                raw[offset++] = 255;
                raw[offset++] = 255;
            }
        }

        using var idatStream = new MemoryStream();
        using (var zlib = new ZLibStream(idatStream, CompressionLevel.Fastest, leaveOpen: true))
        {
            zlib.Write(raw);
        }

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4, 4), height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 2; // color type: truecolor
        WriteChunk(png, "IHDR"u8, ihdr);
        WriteChunk(png, "IDAT"u8, idatStream.ToArray());
        WriteChunk(png, "IEND"u8, []);

        return png.ToArray();
    }

    private static void WriteChunk(Stream output, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> lengthBuffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, data.Length);
        output.Write(lengthBuffer);

        var crcInput = new byte[type.Length + data.Length];
        type.CopyTo(crcInput);
        data.CopyTo(crcInput.AsSpan(type.Length));
        output.Write(crcInput);

        Span<byte> crcBuffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBuffer, Crc32(crcInput));
        output.Write(crcBuffer);
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        const uint polynomial = 0xEDB88320;
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ polynomial : crc >> 1;
            }
        }

        return crc ^ 0xFFFFFFFFu;
    }
}
