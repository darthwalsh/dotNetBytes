using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

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