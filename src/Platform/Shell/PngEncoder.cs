using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace Koy.Platform.Shell;

/// <summary>
/// RGBA pikselleri en küçük geçerli PNG'ye yazar. Platform WPF'e bağımlı olmadığı için
/// (AGENTS.md) saydamlığı koruyan bir biçimi burada kendimiz üretiyoruz.
/// </summary>
internal static class PngEncoder
{
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static byte[] Encode(byte[] rgba, int width, int height)
    {
        using var ms = new MemoryStream();
        ms.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; // bit derinliği
        header[9] = 6; // RGBA
        WriteChunk(ms, "IHDR", header);

        using (var raw = new MemoryStream())
        {
            using (var zlib = new ZLibStream(raw, CompressionLevel.Fastest, leaveOpen: true))
            {
                var stride = width * 4;
                for (var y = 0; y < height; y++)
                {
                    zlib.WriteByte(0); // satır filtresi: yok
                    zlib.Write(rgba, y * stride, stride);
                }
            }

            WriteChunk(ms, "IDAT", raw.ToArray());
        }

        WriteChunk(ms, "IEND", []);
        return ms.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, data.Length);
        stream.Write(buffer);

        var typeBytes = new[] { (byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3] };
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = Crc(Crc(0xFFFFFFFF, typeBytes), data) ^ 0xFFFFFFFF;
        BinaryPrimitives.WriteUInt32BigEndian(buffer, crc);
        stream.Write(buffer);
    }

    private static uint Crc(uint crc, byte[] data)
    {
        foreach (var b in data)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }
}
