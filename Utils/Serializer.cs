using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace EnoMod.Utils;

public static class Serializer
{
    public static string Serialize<T>(T data)
    {
        return ToB64(JsonSerializer.Serialize(data));
    }

    public static T Deserialize<T>(string data)
    {
        return JsonSerializer.Deserialize<T>(FromB64(data)) ??
               throw new EnoModException("Deserialization error");
    }

    private static string ToB64(string data)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
    }

    private static string FromB64(string data)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(data));
    }

    private static void CopyTo(Stream src, Stream dest)
    {
        var bytes = new byte[4096];
        int cnt;
        while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
        {
            dest.Write(bytes, 0, cnt);
        }
    }

    private static byte[] Zip(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);

        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using var gs = new GZipStream(mso, CompressionMode.Compress);
        msi.CopyTo(gs);
        return mso.ToArray();
    }

    private static string Unzip(byte[] bytes)
    {
        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using var gs = new GZipStream(msi, CompressionMode.Decompress);
        gs.CopyTo(mso);
        return Encoding.UTF8.GetString(mso.ToArray());
    }
}
