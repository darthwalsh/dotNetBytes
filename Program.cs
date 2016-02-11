using System;
using System.IO;
using System.Linq;
using System.Threading;
using Nancy.Hosting.Self;

static class Program
{
    static void Main(string[] args)
    {
        try
        {
            string path = args.FirstOrDefault() ?? @"C:\code\dotNetBytes\view\Program.dat";

            AssemblyBytes assm;
            using (var fileStream = File.OpenRead(path))
            {
                assm = new AssemblyBytes(fileStream); 
            }

            var assmJson = assm.Node.ToJson();

            string local = SetupFiles(path, assmJson);

            using (NancyHost host = new NancyHost( new HostConfiguration { RewriteLocalhost = false }, new Uri("http://127.0.0.1:8000")))
            {
                host.Start();

                OpenInIE();
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

        var local = Path.Combine(Path.GetDirectoryName(assembly.Location), "Content");

        try
        {
            Directory.Delete(local, recursive: true);
        }
        catch
        { }
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

    static void OpenInIE()
    {
        using (var exit = new ManualResetEvent(false))
        {
            var ie = new SHDocVw.InternetExplorer();
            ie.Visible = true;

            ie.Left = 381;
            ie.Top = 0;

            ie.Width = 1546;
            ie.Height = 1057;

            ie.Navigate("http://127.0.0.1:8000/Content/view.html");

            ie.OnQuit += () =>
            {
                exit.Set();
            };

            Console.WriteLine("Waiting for IE to quit");
            exit.WaitOne();

            ie.Quit();
        }
    }
}