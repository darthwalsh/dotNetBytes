using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Nancy.Hosting.Self;

public static class Program
{
    static void Main(string[] args)
    {
        try
        {
            string path = args.FirstOrDefault() ?? @"C:\code\dotNetBytes\view\Program.dat";

            Run(path);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public static void Run(string path)
    {
        AssemblyBytes assm;
        using (var fileStream = File.OpenRead(path))
        {
            assm = new AssemblyBytes(fileStream);
        }

        Console.WriteLine(assm.Node.ToString());

        var assmJson = assm.Node.ToJson();

        string local = SetupFiles(path, assmJson);

        GC.KeepAlive(ForceNancyDllToBeCopied);
        using (NancyHost host = new NancyHost(new HostConfiguration { RewriteLocalhost = false }, new Uri("http://127.0.0.1:8000")))
        {
            host.Start();

            RunBrowserAndWebServer();
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
            if (res.Contains("Program.dat"))
                continue;

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

    static void RunBrowserAndWebServer()
    {
        string url = "http://127.0.0.1:8000/Content/view.html";
        string timeout = "20";

        Console.WriteLine();
        Console.WriteLine($"Running web server at ${url} for {timeout} seconds...");

        Process.Start(url); // open the URL in default browser

        using (var p = Process.Start(new ProcessStartInfo
        {
            FileName = "timeout.exe",
            Arguments = $"/t {timeout}",
            UseShellExecute = false,
        }))
        {
            p.WaitForExit();
        }
    }

    static Nancy.NancyModule ForceNancyDllToBeCopied = null;
}