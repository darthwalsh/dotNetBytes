using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class CompileTests
    {
        // TODO test platforms

        [ClassInitialize]
        public static void ClassInitialize(TestContext ctx)
        {
            foreach (var path in Directory.GetFiles("Samples", "*.exe"))
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void Simple()
        {
            Verify.Run(File.OpenRead(Compile(@"Samples\Simple.cs")));
        }

        [TestMethod]
        public void NoConfig()
        {
            Verify.Run(File.OpenRead(Compile(@"Samples\Simple.cs", noconfig: "")));
        }

        [TestMethod]
        public void NonOptimized()
        {
            Verify.Run(File.OpenRead(Compile(@"Samples\Simple.cs", optimize: "")));
        }

        [TestMethod]
        public void Library()
        {
            Verify.Run(File.OpenRead(Compile(@"Samples\Simple.cs", "/t:library")));
        }

        static string Compile(string path, string args = "", string optimize = "/o", string noconfig = "/noconfig")
        {
            string output = path.Replace(".cs", "." + Guid.NewGuid().GetHashCode().ToString("X")) + ".exe";

            Console.Error.WriteLine($"Compiled {output}");

            using (var p = Process.Start(new ProcessStartInfo
            {
                FileName = "csc.exe",
                Arguments = $@"""{path}"" /out:""{output}"" {optimize} {noconfig} {args}",

                WorkingDirectory = Directory.GetCurrentDirectory(),

                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,

                RedirectStandardOutput = true,
            }))
            {
                p.WaitForExit();

                string stdout = p.StandardOutput.ReadToEnd();

                Assert.AreEqual(0, p.ExitCode, "exit code. {0}", stdout);
            }

            return output;
        }
    }
}
