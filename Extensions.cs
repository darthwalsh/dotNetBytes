using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

interface IVisitChildren
{
    void VisitFields(int start, CodeNode parent);
}

interface ICanRead
{
    void Read(Stream s);
}

interface IHasLocation
{
    int Start { get; }
    int End { get; }
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

    public static object Read(this Stream stream, Type type)
    {
        if (type.IsClass)
        {
            var t = Activator.CreateInstance(type);
            var orderedCustom = t as OrderedCustom;
            if (orderedCustom != null)
            {
                orderedCustom.Read(stream);
                return orderedCustom;
            }

            var custom = t as Custom;
            if (custom != null)
            {
                custom.Read(stream);
                return custom;
            }

            throw new InvalidOperationException(type.Name);
        }

        return typeof(StreamExtensions).GetMethods()
                .Where(m => m.Name == "Read" && m.GetParameters().Length == 1)
                .Single().MakeGenericMethod(type)
                .Invoke(null, new object[] { stream });
    }

    public static T ReadClass<T>(this Stream stream) where T : class
    {
        return (T)stream.Read(typeof(T));
    }

    // http://stackoverflow.com/a/4159279/771768
    public static T Read<T>(this Stream stream) where T : struct
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

        var custom = o as OrderedCustom;
        if (custom != null)
        {
            return o.GetType().GetFields().Select(info => info.GetValue(o)).Sum(GetSize);
        }

        return Marshal.SizeOf(o);
    }

    public static int GetOffset(this object o, string name, int rawAddress)
    {
        var custom = o as OrderedCustom;
        if (custom != null)
        {
            return custom.GetOffset(name, rawAddress);
        }

        return Marshal.OffsetOf(o.GetType(), name).ToInt32();
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

        var method = o.GetType().GetMethod("ToString", new[] { typeof(string) });
        if (method != null)
        {
            return "0x" + (string)method.Invoke(o, new object[] { "X" });
        }

        return o.ToString();
    }

    public static void VisitFields(this object ans, int start, CodeNode parent)
    {
        var visitor = ans as IVisitChildren;
        if (visitor != null)
        {
            visitor.VisitFields(start, parent);
            return;
        }

        var type = ans.GetType();

        if (type.Assembly != typeof(AssemblyBytes).Assembly)
            return;

        if (type.IsArray)
        {
            var arr = (Array)ans;
            for (int i = 0; i < arr.Length; ++i)
            {
                var actual = arr.GetValue(i);
                var name = string.Format("[{0}]", i);
                var size = actual.GetSize();
                var offset = i * size;
                var nextStart = start + offset;

                CodeNode current = new CodeNode
                {
                    Name = name,
                    Description = "",
                    Value = actual.GetString(),

                    Start = nextStart,
                    End = nextStart + size,
                };

                actual.VisitFields(nextStart, current);

                parent.Children.Add(current);
            }

            return;
        }

        foreach (var field in type.GetFields())
        {
            var actual = field.GetValue(ans);
            var name = field.Name;
            var desc = field.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;

            CodeNode current = new CodeNode
            {
                Name = name,
                Description = desc != null ? desc.Description : "",
                Value = actual.GetString(),
            };

            var hasLocation = actual as IHasLocation;
            if (hasLocation != null)
            {
                current.Start = hasLocation.Start;
                current.End = hasLocation.End;
            }
            else
            {
                var offset = ans.GetOffset(name, start);
                var nextStart = start + offset;

                current.Start = nextStart;
                current.End = nextStart + actual.GetSize();
            }

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