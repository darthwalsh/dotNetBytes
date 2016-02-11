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

interface IHaveAName
{
    string Name { get; }
}

interface IHaveValue
{
    object Value { get; }
}

interface IHaveValueNode : IHaveValue
{
    CodeNode Node { get; }
}

sealed class DefaultValueNode : IHaveValueNode
{
    public DefaultValueNode(object value, CodeNode node)
    {
        Value = value;
        Node = node;
    }

    public object Value { get; private set; }
    public CodeNode Node { get; private set; }
}

static class StreamExtensions
{
    // http://jonskeet.uk/csharp/readbinary.html
    public static void ReadWholeArray(this Stream stream, byte[] data)
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

    public static Func<Stream, byte[]> ReadByteArray(int length)
    {
        return stream =>
        {
            var data = new byte[length];
            stream.ReadWholeArray(data);
            return data;
        };
    }

    public static Func<Stream, string> ReadNullTerminated(Encoding encoding, int byteBoundary)
    {
        var builder = new List<byte>();

        var buffer = new byte[byteBoundary];

        return stream =>
        {
            while (true)
            {
                stream.ReadWholeArray(buffer);
                builder.AddRange(buffer);
                if (buffer.Contains((byte)'\0'))
                    return encoding.GetString(builder.TakeWhile(b => b != (byte)'\0').ToArray());
            }
        };
    }

    public static CodeNode ReadStruct<FromT, ToT>(this Stream stream, out ToT t, string name, Func<FromT, ToT> trans) 
        where FromT : struct where ToT : struct
    {
        CodeNode node = new CodeNode();
        node.Name = name;
        node.Start = (int)stream.Position;

        // http://stackoverflow.com/a/4159279/771768
        var sz = typeof(FromT).GetSize();
        var buffer = new byte[sz];
        stream.ReadWholeArray(buffer);
        var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        FromT from = (FromT)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(FromT));
        pinnedBuffer.Free();

        node.End = (int)stream.Position;

        from.VisitFields(node.Start, node);

        t = trans(from);

        if (typeof(ToT).Assembly != typeof(AssemblyBytes).Assembly)
            node.Value = t.GetString();

        return node;
    }

    public static CodeNode ReadStruct<T>(this Stream stream, out T? opt, string name = null) where T : struct
    {
        T t;
        var node = stream.ReadStruct(out t, name);
        opt = t;
        return node;
    }

    public static CodeNode ReadStruct<T>(this Stream stream, out T t, string name = null) where T : struct
    {
        return stream.ReadStruct(out t, name ?? typeof(T).Name, (T f) => f);
    }

    public static IEnumerable<CodeNode> ReadStructs<T>(this Stream stream, out T[] ts, int n, string name = null) where T : struct
    {
        name = name ?? typeof(T).Name + "s";

        var nodes = new List<CodeNode>();

        ts = new T[n];
        for (int i = 0; i < n; ++i)
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

        var haveName = t as IHaveAName;
        if (haveName != null)
        {
            name = name ?? haveName.Name;
        }

        node.Name = name ?? typeof(T).Name;
        node.Start = start;

        node.End = (int)stream.Position;

        try
        {
            if (t is IHaveValue)
                node.Value = t.GetString();
        }
        catch (Exception e)
        {
            node.AddError(e.ToString());
        }

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

        return o.GetType().GetSize();
    }

    public static int GetSize(this Type type)
    {
        if (type.IsEnum)
            return Enum.GetUnderlyingType(type).GetSize();

        if (!type.IsConstructedGenericType)
            return Marshal.SizeOf(type);

        var generics = type.GetGenericArguments();

        return type.GetFields(BindingFlags.Public | BindingFlags.Instance).Sum(field =>
        {
            return field.FieldType.GetSize();
        });
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

        var guid = o as Guid?;
        if (guid != null)
        {
            return guid.ToString();
        }

        var hasValue = o as IHaveValue;
        if (hasValue != null)
        {
            return hasValue.Value.GetString();
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

        var privateField = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
        if (privateField != null)
        {
            throw new InvalidOperationException($"No private fields allowed ! {type.FullName}.{privateField.Name}");
        }

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
            try
            {
                if (!SmartEquals(expected.Value, actual))
                {
                    Fail(current, $"Expected {name} to be {expected.Value.GetString()} but instead found {actual.GetString()} at address 0x{current.Start:X}");
                }
            }
            catch (Exception e)
            {
                Fail(current, $"Expected {name} to be {expected.Value.GetString()} but instead found {actual.GetString()} at address 0x{current.Start:X} {e}");
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
            if (actual is ulong)
            {
                return ((ulong)(int)expected) == ((ulong)actual);
            }

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
        node.AddError(message);
    }
}