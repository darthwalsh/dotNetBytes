using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class AssemblyBytesTests
    {
        [TestMethod]
        public void Simple()
        {
            RunCompile(@"Samples\Simple.cs");
        }

        [TestMethod]
        public void NoConfig()
        {
            RunCompile(@"Samples\Simple.cs", noconfig: "");
        }

        [TestMethod]
        public void NonOptimized()
        {
            RunCompile(@"Samples\Simple.cs", optimize: "");
        }

        [TestMethod]
        public void Library()
        {
            RunCompile(@"Samples\Simple.cs", "/t:library");
        }
       
        [TestMethod]
        public void Platformx64()
        {
            RunCompile(@"Samples\Simple.cs", "/platform:x64");
        }

        [TestMethod]
        public void Platformx86()
        {
            RunCompile(@"Samples\Simple.cs", "/platform:x86");
        }

        [TestMethod]
        public void PlatformAnyCPU32()
        {
            RunCompile(@"Samples\Simple.cs", "/platform:anycpu32bitpreferred");
        }

        [TestMethod]
        public void TwoMethods()
        {
            RunCompile(@"Samples\TwoMethods.cs");
        }

        [TestMethod]
        public void TwoSameMethods()
        {
            RunCompile(@"Samples\TwoSameMethods.cs");
        }

        [TestMethod]
        public void Param()
        {
            RunCompile(@"Samples\Param.cs");
        }

        // TODO test parameters, return values
        // TODO Add test to exercise the various NotImplementedExceptions

        static void RunCompile(string path, string args = "", string optimize = "/optimize", string noconfig = "/noconfig")
        {
            var allArgs = $"{optimize} {noconfig} {args}";

            string outpath = Path.GetFullPath(path.Replace(".cs", "." + CleanFileName(allArgs) + ".exe"));

            if (!File.Exists(outpath))
            {
                Console.Error.WriteLine($"Compiling {outpath}");

                RunProcess("csc.exe", $@"""{path}"" /out:""{outpath}"" {allArgs}");
            }
            else
            {
                Console.Error.WriteLine($"Using existing {outpath}");
            }

            Run(File.OpenRead(outpath));
        }

        static void RunProcess(string filename, string processArgs)
        {
            using (var p = Process.Start(new ProcessStartInfo
            {
                FileName = filename,
                Arguments = processArgs,

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
        }

        static string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, bad) => current.Replace(bad.ToString(), "_"));
        }

        [TestMethod]
        public void TestExample()
        {
            Run(File.OpenRead(@"C:\code\dotNetBytes\view\Program.dat"));
        }

        static void Run(Stream s)
        {
            CodeNode.OnError += error => Assert.Fail(error);

            //TODO delete s = new ForwardOnlyStream(s);
            AssemblyBytes assm;
            try
            {
                assm = new AssemblyBytes(s);
            }
            catch
            {
                s.Dispose();
                throw;
            }


            assm.Node.CallBack(AssertChildrenDontOverlap);

            assm.Node.CallBack(AssertNoErrors);

            assm.Node.CallBack(AssertUniqueNames);

            assm.Node.CallBack(AssertLinkOrChildren);

            assm.Node.CallBack(AssertParentDifferentSizeThanChild);

            byte[] data;
            using (var memory = new MemoryStream())
            {
                s.Position = 0;
                s.CopyTo(memory);
                data = memory.ToArray();
            }
            assm.Node.CallBack(node => AssertInterestingBytesNotIgnored(node, data));
        }

        static void AssertChildrenDontOverlap(CodeNode node)
        {
            foreach (var o in node.Children.Zip(node.Children.Skip(1), (last, next) => new { last, next }))
            {
                Asserts.IsLessThanOrEqual(o.last.End, o.next.Start);
            }
        }

        static void AssertNoErrors(CodeNode node)
        {
            string error = node.Errors.FirstOrDefault();
            Assert.IsNull(error, error);
        }

        static void AssertUniqueNames(CodeNode node)
        {
            string name = node.Children.GroupBy(c => c.Name).Where(g => g.Count() > 1).FirstOrDefault()?.Key;
            Assert.IsNull(name, $"multiple {name} under {node.Name}");
        }

        static void AssertLinkOrChildren(CodeNode node)
        {
            if (node.LinkPath != null && node.Children.Any())
            {
                Assert.Fail(node.Name);
            }
        }

        static IEnumerable<string> exceptions = new [] { "TypeSpecs", "Methods" };
        static void AssertParentDifferentSizeThanChild(CodeNode node)
        {
            if (node.Children.Count == 1 && node.Start == node.Children.Single().Start && node.End == node.Children.Single().End)
            {
                if (exceptions.Any(sub => node.Name.Contains(sub)))
                {
                    return;
                }
                Assert.Fail(string.Join("\r\n", node));
            }
        }

        static void AssertInterestingBytesNotIgnored(CodeNode node, byte[] data)
        {
            if (!node.Children.Any())
            {
                return;
            }

            HashSet<int> childIncludes = new HashSet<int>();
            foreach (var child in node.Children)
            {
                childIncludes.UnionWith(new HashSet<int>(Enumerable.Range(child.Start, child.End - child.Start)));
            }

            foreach (var i in Enumerable.Range(node.Start, node.End - node.Start))
            {
                if (data[i] == 0 || childIncludes.Contains(i))
                    continue;

                Assert.Fail($"Interested byte 0x{data[i]:X} at 0x{i:X} was non-zero in node {node.Name}");
            }
        }
    }

    class ForwardOnlyStream : DelegatingStream
    {
        public ForwardOnlyStream(Stream stream)
            : base(stream)
        { }

        public override long Position
        {
            get
            {
                return base.Position;
            }

            set
            {
                if (value < Position)
                    Assert.Fail($"Not allowed: set from {value} to {Position}");

                base.Position = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long relative;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    relative = 0;
                    break;
                case SeekOrigin.Current:
                    relative = Position;
                    break;
                case SeekOrigin.End:
                    relative = Length;
                    break;
                default:
                    throw new InvalidOperationException(origin.ToString());
            }

            long newPos = offset + relative;

            if (offset < Position)
                Assert.Fail($"Not allowed: ${offset} < ${Position}");

            return base.Seek(offset, origin);
        }
    }
}
