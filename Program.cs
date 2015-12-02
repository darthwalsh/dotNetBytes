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

class CodeNode
{
    public string Name;
    public string Description;
    public string Value;

    public int Start;
    public int End;
    public List<CodeNode> Children = new List<CodeNode>();

    public IEnumerable<string> Yield(int indent = 0)
    {
        yield return new string(' ', indent) + string.Join(" ", new[]
        {
            Start.ToString("X").PadRight(4), End.ToString("X").PadRight(4), Name.PadRight(24), Value.PadRight(10), Description.Substring(0, Math.Min(Description.Length, 97))
        });

        foreach (var c in Children)
        {
            foreach (var s in c.Yield(indent + 2))
            {
                yield return s;
            }
        }
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

    T Read<T>() where T : struct
    {
        int pos = (int)s.Position;

        T ans = s.ReadStruct<T>();

        CodeNode node = new CodeNode
        {
            Name = "Root",
            Description = "",
            Value = "",

            Start = pos,
            End = (int)s.Position,
        };

        VisitFields(ans, pos, (int)s.Position, node.Children.Add);

        Console.WriteLine(string.Join("\r\n", node.Yield()));

        return ans;
    }

    void VisitFields(object ans, int start, int end, Action<CodeNode> callback)
    {
        var type = ans.GetType();

        if (type.Assembly != typeof(AssemblyBytes).Assembly)
            return;

        foreach (var field in type.GetFields())
        {
            var actual = field.GetValue(ans);
            var name = field.Name;
            var offset = Marshal.OffsetOf(type, name);
            var size = actual.GetSize();
            var nextStart = start + offset.ToInt32();

            var desc = field.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;

            CodeNode node = new CodeNode
            {
                Name = name,
                Description = desc != null ? desc.Description : "",
                Value = actual.GetString(),

                Start = nextStart,
                End = nextStart + size,
            };

            VisitFields(actual, nextStart, nextStart + size, node.Children.Add);

            callback(node);

            var expected = field.GetCustomAttributes(typeof(ExpectedAttribute), false).FirstOrDefault() as ExpectedAttribute;
            if (expected == null)
            {
                continue;
            }
            if (!SmartEquals(expected.Value, actual))
            {
                Fail(string.Format("Expected {0} to be {1} but instead found {2} at address {3}",
                    name, expected.Value, actual, offset));
            }
        }
    }

    static bool SmartEquals(object expected, object actual)
    {
        if (object.Equals(expected, actual))
        {
            return true;
        }

        if (expected is int && !(actual is int))
        {
            return ((int)expected).Equals(actual.GetInt32());
        }

        var expecteds = expected as IEnumerable;
        var actuals = actual as IEnumerable;

        if (expecteds != null && actuals != null)
        {
            return expecteds.Cast<object>().SequenceEqual(actuals.Cast<object>());
        }

        return false;
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