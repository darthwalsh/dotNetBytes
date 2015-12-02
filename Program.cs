using System;
using System.Collections;
using System.Collections.Generic;
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

class AssemblyBytes
{
    Stream s;

    PEHeader PEHeader;

    public AssemblyBytes(string path)
    {
        s = File.OpenRead(path);

        PEHeader = Read<PEHeader>();
    }

    T Read<T>(int addr) where T : struct
    {
        s.Position = addr;
        return Read<T>();
    }

    T Read<T>() where T : struct
    {
        T ans = s.ReadStruct<T>();

        VerifyFields(ans);

        return ans;
    }

    static void VerifyFields(object ans)
    {
        var type = ans.GetType();
        if (type.Assembly != typeof(AssemblyBytes).Assembly)
            return;

        foreach (var field in type.GetFields())
        {
            var actual = field.GetValue(ans);
            VerifyFields(actual);

            var expected = field.GetCustomAttributes(typeof(ExpectedAttribute), false).FirstOrDefault() as ExpectedAttribute;
            if (expected == null)
            {
                continue;
            }
            if (!SmartEquals(expected.Value, actual))
            {
                Fail(string.Format("Expected {0} to be {1} but instead found {2} at address {3}",
                    field.Name, expected.Value, actual, Marshal.OffsetOf(type, field.Name)));
            }
        }
    }

    private static bool SmartEquals(object expected, object actual)
    {
        if (object.Equals(expected, actual))
        {
            return true;
        }

        if (expected is int && !(actual is int))
        {
            return ((int)expected).Equals(MakeInt(actual));
        }

        var expecteds = expected as IEnumerable;
        var actuals = actual as IEnumerable;

        if (expecteds != null && actuals != null)
        {
            return expecteds.Cast<object>().SequenceEqual(actuals.Cast<object>());
        }

        return false;
    }

    static int MakeInt(object o)
    {
        var convert = o as IConvertible;

        if (convert != null)
        {
            return convert.ToInt32(CultureInfo.InvariantCulture);
        }

        throw new InvalidOperationException("MakeInt doesn't support: " + o.GetType().Name);
    }

    static void Fail(string message)
    {
        //TODO log failure
        throw new InvalidOperationException(message);
    }
}

static class Program
{
    static void Main(string[] args)
    {
        try
        {
            string path = args.FirstOrDefault() ?? @"C:\code\bootstrappingCIL\understandingCIL\AddR.exe";

            var assm = new AssemblyBytes(path);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}