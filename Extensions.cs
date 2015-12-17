using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

interface ICanRead
{
    CodeNode Read(Stream stream);
}

static class StreamExtensions
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

    public static Func<Stream, string> ReadNullTerminated(Encoding encoding, int byteBoundary)
    {
        var builder = new List<byte>();

        var buffer = new byte[byteBoundary];

        return stream =>
        {
            while (true)
            {
                ReadWholeArray(stream, buffer);
                builder.AddRange(buffer);
                if (buffer.Contains((byte)'\0'))
                    return encoding.GetString(builder.TakeWhile(b => b != (byte)'\0').ToArray());
            }
        };
    }

    // http://stackoverflow.com/a/4159279/771768
    public static CodeNode ReadStruct<T>(this Stream stream, out T t, string name = null) where T : struct
    {
        CodeNode node = new CodeNode();
        node.Name = name ?? typeof(T).Name;
        node.Start = (int)stream.Position;

        var sz = Marshal.SizeOf(typeof(T));
        var buffer = new byte[sz];
        ReadWholeArray(stream, buffer);
        var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        t = (T)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(T));
        pinnedBuffer.Free();

        node.End = (int)stream.Position;

        t.VisitFields(node.Start, node);

        if (typeof(T).Assembly != typeof(AssemblyBytes).Assembly)
            node.Value = t.GetString();

        return node;
    }
    public static IEnumerable<CodeNode> ReadStructs<T>(this Stream stream, out T[] ts, int n, string name = null) where T : struct
    {
        name = name ?? typeof(T).Name + "s";

        var nodes = new List<CodeNode>();

        ts = new T[n];
        for(int i = 0; i < n; ++i)
        {
            nodes.Add(stream.ReadStruct(out ts[i], name + "[" + i + "]"));
        }

        return nodes;
    }

    public static IEnumerable<CodeNode> ReadClasses<T>(this Stream stream, ref T[] ts, int n = -1, string name = null) where T : class, ICanRead
    {
        name = name ?? typeof(T).Name + "s";

        var nodes = new List<CodeNode>();

        ts = ts ?? new T[n];
        for (int i = 0; i < ts.Length; ++i)
        {
            nodes.Add(stream.ReadClass(ref ts[i], name + "[" + i + "]"));
        }

        return nodes;
    }

    public static CodeNode ReadClass<T>(this Stream stream, ref T t, string name = null) where T : class, ICanRead
    {
        var start = (int)stream.Position;

        t = t ?? Activator.CreateInstance<T>();
        CodeNode node = t.Read(stream);
        node.Name = name ?? typeof(T).Name;
        node.Start = start;

        node.End = (int)stream.Position;

        return node;
    }

    public static CodeNode ReadAnything<T>(this Stream stream, out T t, Func<Stream, T> callback, string name = null)
    {
        CodeNode node = new CodeNode();
        node.Name = name ?? typeof(T).Name;
        node.Start = (int)stream.Position;

        t = callback(stream);

        if (typeof(T).Assembly != typeof(AssemblyBytes).Assembly)
            node.Value = t.GetString();

        node.End = (int)stream.Position;

        return node;
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

        var type = o.GetType();

        if (type.IsEnum)
        {
            type = Enum.GetUnderlyingType(type);
        }

        return Marshal.SizeOf(type);
    }

    static Dictionary<char, string> escapes = new Dictionary<char, string>
    {
        { '\0', @"\0" },
        { '\t', @"\t" },
        { '\r', @"\r" },
        { '\n', @"\n" },
        { '\v', @"\v" },
    };

    public static string EscapeControl(this string s)
    {
        return string.Concat(s.Select(c =>
        {
            if (char.IsControl(c))
            {
                string ans;
                if (escapes.TryGetValue(c, out ans))
                {
                    return ans;
                }

                return @"\" + ((int)c).ToString("X");
            }

            return "" + c;
        }));
    }

    public static int CountSetBits(this ulong n)
    {
        ulong count = 0;
        for (; n != 0; n >>= 1)
            count += n & 0x1;
        return (int)count;
    }

    public static string GetString(this object o)
    {
        var s = o as string;
        if (s != null)
        {
            return '"' + s.EscapeControl() + '"';
        }

        var cs = o as char[];
        if (cs != null)
        {
            return new string(cs).GetString();
        }

        var os = o as IEnumerable;
        if (os != null)
        {
            return "{" + string.Join(", ", os.Cast<object>().Select(GetString)) + "}";
        }

        if (o is Enum)
        {
            Enum en = (Enum)o;
            return "0x" + en.ToString("X") + " " + en.ToString(); ;
        }

        var method = o.GetType().GetMethod("ToString", new[] { typeof(string) });
        if (method != null)
        {
            return "0x" + (string)method.Invoke(o, new object[] { "X" });
        }

        return o.ToString();
    }

    public static void VisitFields(this object ans, int start, CodeNode parent)
    {
        var type = ans.GetType();

        if (type.Assembly != typeof(AssemblyBytes).Assembly)
            return;

        if (type.IsEnum)
            return;

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var actual = field.GetValue(ans);
            var name = field.Name;
            var desc = field.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;

            var nextStart = start + Marshal.OffsetOf(ans.GetType(), name).ToInt32();

            CodeNode current = new CodeNode
            {
                Name = name,
                Description = desc != null ? desc.Description : "",
                Value = actual.GetString(),

                Start = nextStart,
                End = nextStart + actual.GetSize(),
            };

            actual.VisitFields(current.Start, current);

            parent.Children.Add(current);

            var expected = field.GetCustomAttributes(typeof(ExpectedAttribute), false).FirstOrDefault() as ExpectedAttribute;
            if (expected == null)
            {
                continue;
            }
            if (!SmartEquals(expected.Value, actual))
            {
                Fail(current, string.Format("Expected {0} to be {1} but instead found {2} at address {3}",
                    name, expected.Value, actual, current.Start));
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

    static void Fail(CodeNode node, string message)
    {
        node.Errors.Add(message);
    }
}