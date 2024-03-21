using ComponentAce.Compression.Libs.zlib;
using System;
using System.IO;
using System.Text;

namespace SIT.Manager.Avalonia.Classes;

public static class SimpleZlib
{
    public const int BUFFER_SIZE = 4096;

    public static byte[] CompressToBytes(string text, int compressLevel, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        byte[] numArray = encoding.GetBytes(text);
        return CompressToBytes(numArray, numArray.Length, compressLevel);
    }

    public static byte[] CompressToBytes(byte[] bytes, int length, int compressLevel)
    {
        //Why the fuck are we allocating 30 extra bytes BSG?
        //This literally works perfectly fine without this but for smaller data loads this seems to yield different bytes even though it decodes fine
        //I've given up trying to understand BSG's shit code, I just know this is needed
        byte[] numArray = new byte[length + 30];
        ZStream zstream = new();
        int length1 = CompressWithZStream(ref zstream, bytes, 0, length, numArray, compressLevel);
        byte[] destinationArray = new byte[length1];
        Array.Copy(numArray, 0, destinationArray, 0, length1);
        return destinationArray;
    }

    public static int CompressWithZStream(
      ref ZStream zstream,
      byte[] bytes,
      int startIndex,
      int length,
      byte[] compressedBytes,
      int compressLevel)
    {
        zstream.deflateInit(compressLevel);
        zstream.next_in = bytes;
        zstream.next_in_index = startIndex;
        zstream.avail_in = length;
        zstream.next_out_index = 0;
        zstream.avail_out = compressedBytes.Length;
        zstream.next_out = compressedBytes;
        zstream.deflate(4);
        zstream.free();
        return zstream.next_out_index;
    }

    public static string Decompress(byte[] bytes, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        int length = bytes.Length;
        ZStream zstream = new();
        zstream.inflateInit();
        using MemoryStream memoryStream = new();
        byte[] array = new byte[BUFFER_SIZE];
        zstream.next_in = bytes;
        zstream.next_in_index = 0;
        zstream.avail_in = length;
        int num;
        do
        {
            zstream.avail_out = array.Length;
            zstream.next_out = array;
            zstream.next_out_index = 0;
            num = zstream.inflate(0);
            if (zstream.inflate(0) > -1)
                memoryStream.Write(zstream.next_out, 0, zstream.next_out_index);
            else
                break;
        }
        while (zstream.avail_in > 0 && num != 1);
        memoryStream.Flush();
        string str = encoding.GetString(memoryStream.GetBuffer(), 0, (int) memoryStream.Position);
        zstream.free();
        return str;
    }

    public static byte[] DecompressToBytes(byte[] compressedBytes)
    {
        int length = compressedBytes.Length;
        ZStream zstream = new();
        using MemoryStream memoryStream = new();
        byte[] array = new byte[BUFFER_SIZE];

        zstream.next_in = compressedBytes;
        zstream.next_in_index = 0;
        zstream.avail_in = length;

        while (zstream.avail_in > 0 || zstream.avail_out == 0)
        {
            zstream.avail_out = array.Length;
            zstream.next_out = array;
            zstream.next_out_index = 0;
            int num = zstream.inflate(0);

            if (num > -1)
                memoryStream.Write(zstream.next_out, 0, zstream.next_out_index);
            else
                break;
        }

        byte[] decompressedBytes = memoryStream.ToArray();
        zstream.free();
        return decompressedBytes;
    }
}
