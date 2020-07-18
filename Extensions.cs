using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

sealed class ExpectedAttribute : Attribute
{
    public object Value;
    public ExpectedAttribute(object v)
    {
        Value = v;
    }
}

sealed class DescriptionAttribute : Attribute
{
    public string Description;
    public DescriptionAttribute(string d)
    {
        Description = d;
    }
}

// Should implement either ICanRead or ICanBeReadInOrder
interface ICanBeRead
{
}

//TODO(cleanup) split into multiple passes, parsing first, getting CodeNode later once everything is parsed
interface ICanRead : ICanBeRead
{
    CodeNode Read(Stream stream);
}

// Implement this to allow reflection with [OrderedField] 
interface ICanBeReadInOrder : ICanBeRead
{
    CodeNode Node { get; set; }
}

interface IHaveAName
{
    string Name { get; }
}

interface IHaveValue
{
    object Value { get; }
}

interface IHaveLiteralValueNode : IHaveLiteralValue
{
    CodeNode Node { get; }
}

sealed class DefaultValueNode : IHaveLiteralValueNode
{
    public DefaultValueNode(object value, CodeNode node)
    {
        Value = value;
        Node = node;
    }

    public object Value { get; private set; }
    public CodeNode Node { get; private set; }
}

interface IHaveLiteralValue : IHaveValue
{
}

internal interface IHaveIndex
{
    int Index { get; }
}

static class StreamExtensions
{
    // http://jonskeet.uk/csharp/readbinary.html
    public static void ReadWholeArray(this Stream stream, byte[] data)
    {
        string error;
        if (!stream.TryReadWholeArray(data, out error))
            throw new EndOfStreamException(error);
    }

    public static bool TryReadWholeArray(this Stream stream, byte[] data, out string error)
    {
        int offset = 0;
        int remaining = data.Length;
        while (remaining > 0)
        {
            int read = stream.Read(data, offset, remaining);
            if (read <= 0)
            {
                error = $"End of stream reached with {remaining} bytes left to read";
                return false;
            }
            remaining -= read;
            offset += read;
        }
        error = null;
        return true;
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

    //TODO(cleanup) just wrap the input Stream?
    public static byte ReallyReadByte(this Stream stream)
    {
        int read = stream.ReadByte();
        if (read == -1)
            throw new EndOfStreamException("End of stream reached with 1 byte left to read");
        return (byte)read;
    }

    public static CodeNode ReadStruct<T>(this Stream stream, out T t, string name = null) where T : struct
    {
        CodeNode node = new CodeNode();
        node.Name = name ?? typeof(T).Name;
        node.Start = (int)stream.Position;

        // http://stackoverflow.com/a/4159279/771768
        var sz = typeof(T).GetSize();
        var buffer = new byte[sz];
        stream.ReadWholeArray(buffer);
        var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        var ptype = typeof(T);
        if (ptype.IsEnum)
            ptype = ptype.GetEnumUnderlyingType();
        t = (T)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), ptype);
        pinnedBuffer.Free();

        node.End = (int)stream.Position;

        t.VisitFields(node.Start, node);

        if (typeof(T).Assembly != typeof(AssemblyBytes).Assembly)
            node.Value = t.GetString();

        return node;
    }

    public static CodeNode ReadStruct<FromT, ToT>(this Stream stream, out ToT t, string name, Func<FromT, ToT> trans) 
        where FromT : struct where ToT : struct
    {
        FromT from;
        var node = stream.ReadStruct(out from, name);

        t = trans(from);

        if (typeof(ToT).Assembly != typeof(AssemblyBytes).Assembly)
            node.Value = t.GetString();
        else
            node.Value = "";

        return node;
    }

    public static CodeNode ReadStruct<T>(this Stream stream, out T? opt, string name = null) where T : struct
    {
        T t;
        var node = stream.ReadStruct(out t, name);
        opt = t;
        return node;
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

    public static IEnumerable<CodeNode> ReadClasses<T>(this Stream stream, ref T[] ts, int n = -1, string name = null) where T : class, ICanBeRead
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

    public static CodeNode ReadClass<T>(this Stream stream, ref T t, string name = null) where T : class, ICanBeRead
    {
        var start = (int)stream.Position;

        t = t ?? Activator.CreateInstance<T>();

        CodeNode node = null;

        ICanRead iCanRead = t as ICanRead;
        ICanBeReadInOrder iCanBeReadInOrder = t as ICanBeReadInOrder;
        if (iCanRead != null)
        {
            if (iCanBeReadInOrder != null)
                throw new InvalidOperationException();

            node = iCanRead.Read(stream);
        }
        if (iCanBeReadInOrder != null)
        {
            node = iCanBeReadInOrder.Read(stream);
        }
        if (node == null)
        {
            throw new InvalidOperationException();
        }

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
        if (o == null)
        {
            throw new ArgumentNullException();
        }
        
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
            var value = hasValue.Value;
            var literalValue = o as IHaveLiteralValue;
            if (literalValue != null)
            {
                return (string)value;
            }
            return value.GetString();
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

// Used to guarantee reflecting over class fields uses line number
// http://stackoverflow.com/a/17998371/771768
sealed class OrderedFieldAttribute : Attribute
{
    public int Order;
    public OrderedFieldAttribute([CallerLineNumber] int i = -1)
    {
        Order = i;
    }
}

static class OrderedExtensions
{
    public static CodeNode Read(this ICanBeReadInOrder o, Stream stream)
    {
        o.Node = new CodeNode();
        var ordedFields = o.GetType().GetFields()
            .OrderBy(field =>
            {
                OrderedFieldAttribute attr = (OrderedFieldAttribute)field.GetCustomAttributes(typeof(OrderedFieldAttribute), false).SingleOrDefault();
                if (attr == null)
                    throw new InvalidOperationException($"{o.GetType().FullName}.{field.Name} is missing [OrderedField]");
                return attr.Order;
            }).ToList();

        foreach (var field in ordedFields)
        {
            var fieldType = field.FieldType;

            // Invoking a method generically is not simple...

            string readMethodName;
            if (fieldType.IsArray)
                throw new InvalidOperationException(fieldType.Name + " IsArray");
            else if (fieldType.IsClass)
                readMethodName = "ReadClass";
            else if (fieldType.IsValueType)
                readMethodName = "ReadStruct";
            else
                throw new InvalidOperationException(fieldType.Name);

            // Invoking a method generically is not simple...
            MethodInfo readClass = typeof(StreamExtensions).GetMethods()
                .Where(m => m.Name == readMethodName && m.GetParameters().Length == 3 && !(m.GetParameters()[1].ParameterType.Name.Contains("Nullable")))
                .Single()
                .MakeGenericMethod(fieldType);

            var args = new object[] { stream, null, field.Name };
            o.Node.Add((CodeNode)readClass.Invoke(null, args));
            field.SetValue(o, args[1]);
        }

        return o.Node;
    }
}
