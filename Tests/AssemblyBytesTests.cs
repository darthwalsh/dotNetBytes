using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Tests
{
  [TestClass]
  public class AssemblyBytesTests
  {
    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context) {
      while (!Directory.Exists("Samples")) {
        Directory.SetCurrentDirectory("..");
      }
    }

    [TestMethod]
    public void NoConfig() {
      RunCompile(@"Samples\Simple.cs", noconfig: "");
    }

    [TestMethod]
    public void NonOptimized() {
      RunCompile(@"Samples\Simple.cs", optimize: "");
    }

    [TestMethod]
    public void Library() {
      RunCompile(@"Samples\Simple.cs", "/t:library");
    }

    [TestMethod]
    public void Platformx64() {
      RunCompile(@"Samples\Simple.cs", "/platform:x64");
    }

    [TestMethod]
    public void Platformx86() {
      RunCompile(@"Samples\Simple.cs", "/platform:x86");
    }

    [TestMethod]
    public void PlatformAnyCPU32() {
      RunCompile(@"Samples\Simple.cs", "/platform:anycpu32bitpreferred");
    }

    [TestMethod]
    public void EmbeddedResource() {
      RunCompile(@"Samples\Simple.cs", @"/res:Samples\Simple.cs");
    }

    [TestMethod]
    public void EmbeddedResource2() {
      RunCompile(@"Samples\Simple.cs", @"/res:Samples\Simple.cs /res:Samples\Const.cs");
    }


    [TestMethod]
    public void COM() {
      RunCompile(@"Samples\COM.cs");
    }

    [TestMethod]
    public void Const() {
      RunCompile(@"Samples\Const.cs");
    }

    [TestMethod]
    public void Conversion() {
      RunCompile(@"Samples\Conversion.cs");
    }

    [TestMethod]
    public void Delegate() {
      RunCompile(@"Samples\Delegate.cs");
    }

    [TestMethod]
    public void Event() {
      RunCompile(@"Samples\Event.cs");
    }

    [TestMethod]
    public void ExceptionHandling() {
      RunCompile(@"Samples\ExceptionHandling.cs");
    }

    [TestMethod]
    public void Fat() {
      RunCompile(@"Samples\Fat.cs");
    }

    [TestMethod]
    public void FlowControl() {
      RunCompile(@"Samples\FlowControl.cs");
    }

    [TestMethod]
    public void Field() {
      RunCompile(@"Samples\Field.cs");
    }

    [TestMethod]
    public void Generic() {
      RunCompile(@"Samples\Generic.cs");
    }

    [TestMethod]
    public void Inheritance() {
      RunCompile(@"Samples\Inheritance.cs");
    }

    [TestMethod]
    public void Lock() {
      RunCompile(@"Samples\Lock.cs");
    }

    [TestMethod]
    public void Param() {
      RunCompile(@"Samples\Param.cs");
    }

    [TestMethod]
    public void PInvoke() {
      RunCompile(@"Samples\PInvoke.cs");
    }

    [TestMethod]
    public void Property() {
      RunCompile(@"Samples\Property.cs");
    }

    [TestMethod]
    public void ReturnValue() {
      RunCompile(@"Samples\ReturnValue.cs");
    }

    [TestMethod]
    public void Simple() {
      RunCompile(@"Samples\Simple.cs");
    }

    [TestMethod]
    public void TwoMethods() {
      RunCompile(@"Samples\TwoMethods.cs");
    }

    [TestMethod]
    public void TwoSameMethods() {
      RunCompile(@"Samples\TwoSameMethods.cs");
    }

    [TestMethod]
    public void TypeForwarding() {
      RunCompile(@"Samples\TypeForwarding.cs");
    }

    [TestMethod]
    public void Unsafe() {
      RunCompile(@"Samples\Unsafe.cs", "/unsafe");
    }

    [TestMethod]
    public void SimpleIL() {
      RunIL(@"Samples\Simple.il");
    }

    [TestMethod]
    public void Blank() {
      RunIL(@"Samples\Blank.il", "/dll");
    }

    [TestMethod]
    public void Data() {
      RunIL(@"Samples\Data.il");
    }

    [TestMethod]
    public void FileTable() {
      RunIL(@"Samples\FileTable.il");
    }

    //TODO(HACK) create test case that exercises all IL features, check code coverage, then test modifying each byte of the code...
    //    if the exe blows up does it needs to produce error in dotNetBytes (and not an exception)
    //TODO Also test with mono
    //TODO Also test with dotnetcore

    //TODO try out unmanaged exports library? https://sites.google.com/site/robertgiesecke/Home/uploads/unmanagedexports or https://github.com/RealGecko/NppLanguageTool/

    //TODO delete after removing all static fields?
    [TestMethod]
    [Ignore] // Re-enable to test changes to globals
    public void Race() {
      var factory = Task<int>.Factory;
      var tasks = new List<Task<int>>();
      for (var i = 0; i < 8; ++i) {
        var myI = i;
        tasks.Add(factory.StartNew(() => {
          Console.Error.WriteLine($"Starting task {myI}");
          Run(new SlowStream(OpenExampleProgram()));
          Console.Error.WriteLine($"Done with task {myI}");
          return myI;
        }));
      }

      Task.WaitAll(tasks.ToArray());
    }

    static string ilasm = Microsoft.Build.Utilities.ToolLocationHelper.GetPathToDotNetFrameworkFile(
        "ilasm.exe", Microsoft.Build.Utilities.TargetDotNetFrameworkVersion.VersionLatest);
    static string csc = Path.Combine(
        Microsoft.Build.Utilities.ToolLocationHelper.GetFoldersInVSInstalls().First(),
        "MSBuild",
        "Current",
        "Bin",
        "Roslyn",
        "csc.exe");

    static void RunIL(string path, string args = "") {
      var outpath = Path.GetFullPath(path.Replace(".il", $".{CleanFileName(args)}.il.exe"));

      if (!File.Exists(outpath) || File.GetLastWriteTime(path) > File.GetLastWriteTime(outpath)) {
        Console.Error.WriteLine($"Assembling {outpath}");

        RunProcess(ilasm, $@"""{path}"" /OUTPUT=""{outpath}"" {args}");
      } else {
        Console.Error.WriteLine($"Using existing {outpath}");
      }

      Run(File.OpenRead(outpath));
    }

    //TODO either invoke in-memory compiler and assembler, or run compiler as part of build time
    static void RunCompile(string path, string args = "", string optimize = "/optimize", string noconfig = "/noconfig") {
      var allArgs = $"{optimize} {noconfig} {args}";

      var outpath = Path.GetFullPath(path.Replace(".cs", $".{CleanFileName(allArgs)}.exe"));

      if (!File.Exists(outpath) || File.GetLastWriteTime(path) > File.GetLastWriteTime(outpath)) {
        Console.Error.WriteLine($"Compiling {outpath}");

        RunProcess(csc, $@"""{path}"" /out:""{outpath}"" {allArgs}");
      } else {
        Console.Error.WriteLine($"Using existing {outpath}");
      }
      /*//Uncomment this block if you want to see the web version of the parse
                  Program.Run(outpath);
                  Assert.Fail("Oops comment me out");
      /*/
      Run(File.OpenRead(outpath));
      //*/
    }

    static void RunProcess(string filename, string processArgs) {
      using (var p = Process.Start(new ProcessStartInfo {
        FileName = filename,
        Arguments = processArgs,

        WorkingDirectory = Directory.GetCurrentDirectory(),

        CreateNoWindow = true,
        WindowStyle = ProcessWindowStyle.Hidden,
        UseShellExecute = false,

        RedirectStandardOutput = true,
      })) {
        p.WaitForExit();

        var stdout = p.StandardOutput.ReadToEnd();

        Assert.AreEqual(0, p.ExitCode, "exit code. {0}", stdout);
      }
    }

    static string CleanFileName(string fileName) {
      return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, bad) => current.Replace(bad.ToString(), "_"));
    }

    static string FormatJson(string json) {
      dynamic parsedJson = JsonConvert.DeserializeObject(json);
      return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
    }

    [TestMethod]
    public void TestExample() {
      var assm = Run(OpenExampleProgram());
      var expected = FormatJson(assm.Node.ToJson());

      using (var baselineJSON = GetType().Assembly.GetManifestResourceStream("bytes.json"))
      using (var reader = new StreamReader(baselineJSON)) {

        var actual = FormatJson(reader.ReadToEnd());
        if (actual != expected) {
          File.WriteAllText(@"..\..\..\..\view\bytes.json", expected);
          Assert.Fail("Baseline was out of date, but fixed now!");
        }
      }
    }

    static Stream OpenExampleProgram() {
      return typeof(AssemblyBytes).Assembly.GetManifestResourceStream("view.Program.dat");
    }

    static AssemblyBytes Run(Stream s) {
      CodeNode.OnError += error => Assert.Fail(error);

      AssemblyBytes assm;
      try {
        assm = new AssemblyBytes(s);
      } catch {
        s.Dispose();
        throw;
      }

      try {
        assm.Node.CallBack(AssertChildrenDontOverlap);

        assm.Node.CallBack(AssertNoErrors);

        assm.Node.CallBack(AssertUniqueNames);

        assm.Node.CallBack(AssertLinkOrChildren);

        assm.Node.CallBack(AssertParentDifferentSizeThanChild);

        byte[] data;
        using (var memory = new MemoryStream()) {
          s.Position = 0;
          s.CopyTo(memory);
          data = memory.ToArray();
        }
        assm.Node.CallBack(node => AssertInterestingBytesNotIgnored(node, data));

        return assm;
      } catch {
        System.Console.Error.WriteLine(assm.Node.ToString());
        throw;
      } finally {
        s.Dispose();
      }
    }

    static void AssertChildrenDontOverlap(CodeNode node) {
      foreach (var o in node.Children.Zip(node.Children.Skip(1), (last, next) => new { last, next })) {
        Asserts.IsLessThanOrEqual(o.last.End, o.next.Start);
      }
    }

    static void AssertNoErrors(CodeNode node) {
      var error = node.Errors.FirstOrDefault();
      Assert.IsNull(error, error);
    }

    static void AssertUniqueNames(CodeNode node) {
      var name = node.Children.GroupBy(c => c.Name).Where(g => g.Count() > 1).FirstOrDefault()?.Key;
      Assert.IsNull(name, $"duplicate {name} under {node.Name}");
    }

    static void AssertLinkOrChildren(CodeNode node) {
      if (node.LinkPath != null && node.Children.Any()) {
        Assert.Fail(node.Name);
      }
    }

    static IEnumerable<string> exceptions = new[] { "TypeSpecs", "Methods", "GuidHeap", "StandAloneSigs", "ModuleRefs", "CilOps" };
    static void AssertParentDifferentSizeThanChild(CodeNode node) {
      if (node.Children.Count == 1 && node.Start == node.Children.Single().Start && node.End == node.Children.Single().End) {
        if (exceptions.Any(sub => node.Name.Contains(sub))) {
          return;
        }
        Assert.Fail(string.Join("\r\n", node));
      }
    }

    static void AssertInterestingBytesNotIgnored(CodeNode node, byte[] data) {
      if (!node.Children.Any()) {
        return;
      }

      var childIncludes = new HashSet<int>();
      foreach (var child in node.Children) {
        childIncludes.UnionWith(new HashSet<int>(Enumerable.Range(child.Start, child.End - child.Start)));
      }

      foreach (var i in Enumerable.Range(node.Start, node.End - node.Start)) {
        if (data[i] == 0 || childIncludes.Contains(i))
          continue;

        Assert.Fail($"Interesting byte 0x{data[i]:X} at 0x{i:X} was non-zero in node {node.Name}");
      }
    }
  }

  class SlowStream : DelegatingStream
  {
    public SlowStream(Stream stream)
        : base(stream) { }

    void Delay() {
      Thread.Sleep(2);
    }

    public override int Read(byte[] buffer, int offset, int count) {
      Delay();
      return base.Read(buffer, offset, count);
    }

    public override int ReadByte() {
      Delay();
      return base.ReadByte();
    }
  }
}
