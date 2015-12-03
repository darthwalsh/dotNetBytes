using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

class AssemblyBytes
{
    Stream s;

    PEHeader PEHeader;

    public AssemblyBytes(string path)
    {
        s = File.OpenRead(path);

        PEHeader = Read<PEHeader>();
    }

    T Read<T>()
    {
        int pos = (int)s.Position;

        T ans = s.Read<T>();

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