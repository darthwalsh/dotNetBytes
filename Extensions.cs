using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

public static class StreamExtensions
{
    // http://jonskeet.uk/csharp/readbinary.html
    static void ReadWholeArray(Stream stream, byte[] data)
    {
        int offset = 0;
        int remaining = data.Length;
        while (remaining > 0)
        {
            int read = stream.Read(data, offset, remaining);
            if (read <= 0)
                throw new EndOfStreamException(string.Format("End of stream reached with {0} bytes left to read", remaining));
            remaining -= read;
            offset += read;
        }
    }

    // http://stackoverflow.com/a/4159279/771768
    public static T ReadStruct<T>(this Stream stream) where T : struct
    {
        var sz = Marshal.SizeOf(typeof(T));
        var buffer = new byte[sz];
        ReadWholeArray(stream, buffer);
        var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        var structure = (T)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(T));
        pinnedBuffer.Free();
        return structure;
    }
}

static class TypeExtensions
{
    public static int GetInt32(this object o)
    {
        var convert = o as IConvertible;

        if (convert != null)
        {
            return convert.ToInt32(CultureInfo.InvariantCulture);
        }

        throw new InvalidOperationException("MakeInt doesn't support: " + o.GetType().Name);
    }

    public static int GetSize(this object o)
    {
        var os = o as IEnumerable;
        if (os != null)
        {
            return os.Cast<object>().Sum(GetSize);
        }

        return Marshal.SizeOf(o);
    }

    public static string GetString(this object o)
    {
        var s = o as string;
        if (s != null)
        {
            return s;
        }

        var cs = o as char[];
        if (cs != null)
        {
            return new string(cs);
        }

        var os = o as IEnumerable;
        if (os != null)
        {
            return "{" + string.Join(", ", os.Cast<object>().Select(GetString)) + "}";
        }

        var method = o.GetType().GetMethod("ToString", new[] { typeof(string) });
        if (method != null)
        {
            return "0x" + (string)method.Invoke(o, new object[] { "X" });
        }

        return o.ToString();
    }
}