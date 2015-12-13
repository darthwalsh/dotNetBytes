using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Helpers;

class AssemblyBytes
{
    Stream s;

    PEHeader PEHeader;
    CodeNode node;

    public AssemblyBytes(string path)
    {
        s = File.OpenRead(path);

        int start = (int)s.Position;

        PEHeader = Read<PEHeader>();

        node = new CodeNode
        {
            Name = "Root",
            Description = "",
            Value = "",

            Start = start,
            End = (int)s.Position,
        };

        VisitFields(PEHeader, start, (int)s.Position, node);

        //Console.Error.WriteLine(string.Join("\r\n", node.Yield()));
    }

    T Read<T>()
    {
        return s.Read<T>();
    }

    public string AsJson { get { return Json.Encode(node); } }

    void VisitFields(object ans, int start, int end, CodeNode parent)
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

            CodeNode current = new CodeNode
            {
                Name = name,
                Description = desc != null ? desc.Description : "",
                Value = actual.GetString(),

                Start = nextStart,
                End = nextStart + size,
            };

            VisitFields(actual, nextStart, nextStart + size, current);

            parent.Children.Add(current);

            var expected = field.GetCustomAttributes(typeof(ExpectedAttribute), false).FirstOrDefault() as ExpectedAttribute;
            if (expected == null)
            {
                continue;
            }
            if (!SmartEquals(expected.Value, actual))
            {
                Fail(current, string.Format("Expected {0} to be {1} but instead found {2} at address {3}",
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

    static void Fail(CodeNode node, string message)
    {
        node.Errors.Add(message);
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
            var assmJson = assm.AsJson;

            string local = SetupFiles(path, assmJson);

            using (var p = RunWebServer(local))
            {
                Console.WriteLine("Running web server, opening in IE");
                OpenInIE();
                Console.WriteLine("IE was closed");

                p.Kill();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    static string SetupFiles(string path, string assmJson)
    {
        var assembly = typeof(Program).Assembly;

        var local = Path.Combine(Path.GetDirectoryName(assembly.Location), "serve");

        Directory.CreateDirectory(local);

        File.WriteAllText(Path.Combine(local, "bytes.json"), assmJson);

        File.Copy(path, Path.Combine(local, "Program.dat"), overwrite: true);

        foreach (var res in assembly.GetManifestResourceNames().Where(res => res.StartsWith("view.")))
        {
            var trimmed = res.Substring("view.".Length);
            var destination = Path.Combine(local, trimmed);
            if (File.Exists(destination))
                File.Delete(destination);

            using (var stream = assembly.GetManifestResourceStream(res))
            using (var file = File.OpenWrite(destination))
            {
                stream.CopyTo(file);
            }
        }

        return local;
    }

    static Process RunWebServer(string local)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = "python",
            Arguments = "-m SimpleHTTPServer 8000",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = local,
        });
    }

    static void OpenInIE()
    {
        using (var exit = new ManualResetEvent(false))
        {
            var ie = new SHDocVw.InternetExplorer();
            ie.Visible = true;
            ie.Navigate("http://127.0.0.1:8000/view.html");

            ie.OnQuit += () =>
            {
                exit.Set();
            };

            exit.WaitOne();

            ie.Quit();
        }
    }
}