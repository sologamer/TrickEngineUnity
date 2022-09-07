using System;
using System.Collections.Generic;

#if ENABLE_LZ4
using K4os.Compression.LZ4;
#endif

#if ENABLE_ZSTD
using Zstandard.Net;
#endif

namespace TrickCore
{
    public static class ByteArrayExtensions
    {   
        //https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/24343727#24343727
        private static readonly uint[] Lookup32 = CreateLookup32();

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s=i.ToString("X2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }

        public static string ByteArrayToHexViaLookup32(this IReadOnlyList<byte> bytes)
        {
            var lookup32 = Lookup32;
            var result = new char[bytes.Count * 2];
            for (int i = 0; i < bytes.Count; i++)
            {
                var val = lookup32[bytes[i]];
                result[2*i] = (char)val;
                result[2*i + 1] = (char) (val >> 16);
            }
            return new string(result);
        }
        
        
#if ENABLE_LZ4
    // https://github.com/AArnott/MessagePack-CSharp/tree/master/src/MessagePack.UnityClient

    public static string LZ4EncodeBase64(this byte[] value, LZ4Level compressionLevel)
    {
        return Convert.ToBase64String(LZ4Encode(value, compressionLevel));
    }
    
    public static byte[] LZ4Encode(this byte[] value, LZ4Level compressionLevel)
    {
        return LZ4PicklerTrickSafe.Pickle(value, compressionLevel);
    }

    public static byte[] LZ4DecodeBase64(this string value)
    {
        return LZ4Decode(Convert.FromBase64String(value));
    }
    
    public static byte[] LZ4Decode(this byte[] value)
    {
        return LZ4PicklerTrickSafe.Unpickle(value);
    }
#endif

#if ENABLE_ZLIB
    public static Func<byte[], int, byte[]> ZLibEncodeFunc;
    public static Func<byte[], byte[]> ZLibDecodeFunc;
        
    // https://assetstore.unity.com/packages/tools/input-management/7zip-lzma-lz4-fastlz-zip-gzip-brotli-multiplatform-plugins-12674
    /// <summary>
    /// (0-10) recommended 9 for maximum (10 is highest but slower and not zlib compatible)
    /// </summary>
    /// <param name="value"></param>
    /// <param name="compressionLevel"></param>
    /// <returns></returns>
    public static string ZLibEncodeBase64(this byte[] value, int compressionLevel)
    {
        return Convert.ToBase64String(ZLibEncode(value, compressionLevel));
    }
    
    /// <summary>
    /// (0-10) recommended 9 for maximum (10 is highest but slower and not zlib compatible)
    /// </summary>
    /// <param name="value"></param>
    /// <param name="compressionLevel"></param>
    /// <returns></returns>
    public static byte[] ZLibEncode(this byte[] value, int compressionLevel)
    {
        return ZLibEncodeFunc?.Invoke(value, compressionLevel);
        //return lzip.compressBuffer(value, compressionLevel);
    }

    public static byte[] ZLibDecodeBase64(this string value)
    {
        return ZLibDecode(Convert.FromBase64String(value));
    }
    
    public static byte[] ZLibDecode(this byte[] value)
    {
        return ZLibDecodeFunc?.Invoke(value);
        //return lzip.decompressBuffer(value);
    }
#endif
    
#if ENABLE_ZSTD
    public static string ZstdEncodeBase64(this byte[] value, int compressionLevel)
    {
        return Convert.ToBase64String(ZstdEncode(value, compressionLevel));
    }
    
    public static byte[] ZstdEncode(this byte[] value, int compressionLevel)
    {
        using (var memoryStream = new MemoryStream())
        using (var compressionStream = new ZstandardStream(memoryStream, CompressionMode.Compress))
        {
            compressionStream.CompressionLevel = compressionLevel;
            compressionStream.Write(value, 0, value.Length);
            compressionStream.Close();
            return memoryStream.ToArray();
        }
    }

    public static byte[] ZstdDecodeBase64(this string value)
    {
        return ZstdDecode(Convert.FromBase64String(value));
    }
    
    public static byte[] ZstdDecode(this byte[] value)
    {
        using (var memoryStream = new MemoryStream(value))
        using (var compressionStream = new ZstandardStream(memoryStream, CompressionMode.Decompress))
        using (var temp = new MemoryStream())
        {
            compressionStream.CopyTo(temp);
            return temp.ToArray();
        }
    }
#endif
    }
}