using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Tests
{
  [TestClass]
  public class AssemblyBytesTests
  {
    [AssemblyInitialize]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "required")]
    public static void AssemblyInit(TestContext context) {
      while (!Directory.Exists("Samples")) {
        Directory.SetCurrentDirectory("..");
      }
    }

    [TestMethod]
    public void NoConfig() => RunCompile("Simple.cs", noconfig: "");

    [TestMethod]
    public void NonOptimized() => RunCompile("Simple.cs", optimize: "");

    [TestMethod]
    public void Library() => RunCompile("Simple.cs", "/t:library");

    [TestMethod]
    public void Platformx64() => RunCompile("Simple.cs", "/platform:x64");

    [TestMethod]
    public void Platformx86() => RunCompile("Simple.cs", "/platform:x86");

    [TestMethod]
    public void PlatformAnyCPU32() => RunCompile("Simple.cs", "/platform:anycpu32bitpreferred");

    [TestMethod]
    public void EmbeddedResource() => RunCompile("Simple.cs", @"/res:Simple.cs");

    [TestMethod]
    public void EmbeddedResource2() => RunCompile("Simple.cs", @"/res:Simple.cs /res:Const.cs");


    [TestMethod]
    public void COM() => RunCompile("COM.cs");

    [TestMethod]
    public void Const() => RunCompile("Const.cs");

    [TestMethod]
    public void Conversion() => RunCompile("Conversion.cs");

    [TestMethod]
    public void Delegate() => RunCompile("Delegate.cs");

    [TestMethod]
    public void Event() => RunCompile("Event.cs");

    [TestMethod]
    public void ExceptionHandling() => RunCompile("ExceptionHandling.cs");

    [TestMethod]
    public void Fat() => RunCompile("Fat.cs");

    [TestMethod]
    public void FlowControl() => RunCompile("FlowControl.cs");

    [TestMethod]
    public void Field() => RunCompile("Field.cs");

    [TestMethod]
    public void Generic() => RunCompile("Generic.cs");

    [TestMethod]
    public void Inheritance() => RunCompile("Inheritance.cs");

    [TestMethod]
    public void Lock() => RunCompile("Lock.cs");

    [TestMethod]
    public void Param() => RunCompile("Param.cs");

    [TestMethod]
    public void PInvoke() => RunCompile("PInvoke.cs");

    [TestMethod]
    public void Property() => RunCompile("Property.cs");

    [TestMethod]
    public void ReturnValue() => RunCompile("ReturnValue.cs");

    [TestMethod]
    public void Simple() => RunCompile("Simple.cs");

    [TestMethod]
    public void TwoMethods() => RunCompile("TwoMethods.cs");

    [TestMethod]
    public void TwoSameMethods() => RunCompile("TwoSameMethods.cs");

    [TestMethod]
    public void TypeForwarding() => RunCompile("TypeForwarding.cs");

    [TestMethod]
    public void Unsafe() => RunCompile("Unsafe.cs", "/unsafe");

    [TestMethod]
    public void SimpleIL() => RunIL("Simple.il");

    [TestMethod]
    public void Blank() => RunIL("Blank.il", "/dll");

    [TestMethod]
    public void Data() => RunIL("Data.il");

    [TestMethod]
    public void FileTable() => RunIL("FileTable.il");

    [TestMethod]
    public void Opcodes() => RunIL("Opcodes.il");

    [TestMethod]
    public void Signatures() => RunIL("Signatures.il");

    //MAYBE create test case that exercises all IL features, check code coverage, then test modifying each byte of the code...
    //    if the exe blows up does it needs to produce error in dotNetBytes (and not an exception)

    //TODO(fixme) Managed pointers, Span, ref struct, C#11 ref fields https://blog.ndepend.com/managed-pointers-span-ref-struct-c11-ref-fields-and-the-scoped-keyword/
    //TODO(fixme) look through all new C# fatures that required runtime changes, i.e. https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/generic-attributes

    //MAYBE Test Embedded PDB support. In rosyln command line compile with, /debug:embedded

    //MAYBE Validate ilasm output using [Peverify.exe](https://learn.microsoft.com/en-us/dotnet/framework/tools/peverify-exe-peverify-tool) to avoid testing with bad binaries

    //MAYBE try out unmanaged exports library? https://sites.google.com/site/robertgiesecke/Home/uploads/unmanagedexports with https://www.nuget.org/packages/UnmanagedExports
    // or maybe CIL can export unmanaged functions

    /* Testing, for compiling C# and IL:
      - OSX use dotnet core
      - MAYBE Linux use mono
      - Windows use .NET Framework
    */

    // MAYBE, use DataTestMethod (could that simplify above? ) https://www.meziantou.net/mstest-v2-data-tests.htm

    // MAYBE implement tests for the ECMA sample programs, and check the results are reasonable: https://github.com/stakx/ecma-335/blob/master/docs/vi.b-sample-programs.md

    static string ilasm {
      get {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
          return Path.Join(AppContext.BaseDirectory, "runtimes", "osx-x64", "native", "ilasm");
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
          return @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\ilasm.exe";
          // Doesn't work in dotnet core
          // ToolLocationHelper.GetPathToDotNetFrameworkFile(ilasm.exe, VersionLatest);
        } else {
          throw new NotImplementedException();
        }
      }
    }

    static string csc;
    static void RunCsc(string args) {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
        if (csc is null) {
          var lines = RunProcess("dotnet", "--list-sdks").Split('\n');
          var v3line = lines.Single(l => l.StartsWith("6.")).Split(' ');
          var version = v3line[0];
          var path = v3line[1].Trim('[', ']');
          csc = Path.Join(path, version, "Roslyn", "bincore", "csc.dll");
          Console.Error.WriteLine($"Compiling with {csc}");
        }
        var refs = string.Join(' ',
          Directory.GetFiles("/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/6.0.29/ref/net6.0/", "*.dll")
          .Select(dll => "/reference:" + dll));
        RunProcess("dotnet", $"{csc} {refs} {args}");
      } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        if (csc is null) {
          csc = FindCsc();
        }
        RunProcess(csc, args);
      } else {
        throw new NotImplementedException();
      }

      static string FindCsc() {
        // Doesn't work in dotnet core
        // ToolLocationHelper.GetFoldersInVSInstalls() + @"\MSBuild\Current\Bin\Roslyn\csc.exe";
        var programFiles = new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 };
        var years = new[] { "2022", "2019", "2017" };
        var editions = new[] { "Enterprise", "Professional", "Community" };
        foreach (var programFile in programFiles) {
          foreach (var year in years) {
            foreach (var edition in editions) {
              var path = Path.Combine(
                Environment.GetFolderPath(programFile),
                "Microsoft Visual Studio",
                year,
                edition,
                "MSBuild",
                "Current",
                "Bin",
                "Roslyn",
                "csc.exe");
              if (File.Exists(path)) return path;
            }
          }
        }
        throw new FileNotFoundException("csc.exe");
      }
    }

    static bool ShouldCompile(string path, string outpath) => !File.Exists(outpath) || File.GetLastWriteTime(Path.Join("Samples", path)) > File.GetLastWriteTime(outpath);

    public static string Assemble(string path, string args = "") {
      var outpath = Path.GetFullPath(path.Replace(".il", $".{CleanFileName(args)}.il.exe"));

      if (ShouldCompile(path, outpath)) {
        Console.Error.WriteLine($"Assembling {outpath}");

        var switchchar = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/" : "-";
        args = string.Join(' ', args.Split(' ').Select(arg => arg.StartsWith('/') ? switchchar + arg.Substring(1) : arg));
        RunProcess(ilasm, $@"""{path}"" {switchchar}OUTPUT=""{outpath}"" {args}");
      } else {
        Console.Error.WriteLine($"Using existing {outpath}");
      }

      return outpath;
    }

    static void RunIL(string path, string args = "") => Run(Assemble(path, args));

    //MAYBE either invoke in-memory compiler and assembler, or run compiler as part of build time
    static void RunCompile(string path, string args = "", string optimize = "/optimize", string noconfig = "/noconfig") {
      var allArgs = $"{optimize} {noconfig} {args}";

      var outpath = Path.GetFullPath(path.Replace(".cs", $".{CleanFileName(allArgs)}.exe"));

      if (ShouldCompile(path, outpath)) {
        Console.Error.WriteLine($"Compiling {outpath}");

        RunCsc($@"""{path}"" /out:""{outpath}"" {allArgs}");
      } else {
        Console.Error.WriteLine($"Using existing {outpath}");
      }
      /*//Uncomment this block if you want to see the web version of the parse
                  Program.Run(outpath);
                  Assert.Fail("Oops comment me out");
      /*/
      Run(outpath);
      //*/
    }

    static string RunProcess(string filename, string processArgs) {
      using var p = Process.Start(new ProcessStartInfo {
        FileName = filename,
        Arguments = processArgs,

        WorkingDirectory = Path.Join(Directory.GetCurrentDirectory(), "Samples"),

        CreateNoWindow = true,
        WindowStyle = ProcessWindowStyle.Hidden,
        UseShellExecute = false,

        RedirectStandardOutput = true,
        RedirectStandardError = true,
      });
      p.WaitForExit();

      var stdout = p.StandardOutput.ReadToEnd();
      Console.Error.WriteLine(stdout);

      var stderr = p.StandardError.ReadToEnd();
      Console.Error.WriteLine(stderr);

      Assert.AreEqual(0, p.ExitCode, "exit code. {0} from running {1} {2}", stdout, filename, processArgs);

      return stdout;
    }

    static string CleanFileName(string fileName) => Path.GetInvalidFileNameChars().Concat(" :").Aggregate(fileName, (current, bad) => current.Replace(bad.ToString(), "_"));

    static string FormatJson(string json) {
      var parsedJson = JsonConvert.DeserializeObject(json);
      return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
    }

    static Dictionary<string, object> Rewrite(CodeNode node, AssemblyBytes assm) {
      var dict = new Dictionary<string, object>();
      // Skip name
      if (node.Description != "") dict[nameof(node.Description)] = node.Description;
      dict[nameof(node.NodeValue)] = node.NodeValue;
      dict[nameof(node.Start)] = "0x" + node.Start.ToString("X");
      dict[nameof(node.End)] = "0x" + node.End.ToString("X");
      if (node.Link != null) dict["LinkPath"] = node.Link?.SelfPath;
      if (node.EcmaSection != null) dict["Ecma"] = node.EcmaSection;
      if (node.Errors.Any()) dict[nameof(node.Errors)] = node.Errors;

      foreach (var ch in node.Children) {
        var name = ch.NodeName;
        while (dict.ContainsKey(name)) {
          name = "_" + name;
        }
        dict[name] = Rewrite(ch, assm);
      }
      if (node.NodeName.Contains("BlobHeap[")) {
        var bytes = new byte[node.End - node.Start];
        using (assm.TempReposition(node.Start)) {
          assm.Stream.ReadWholeArray(bytes);
        }
        dict["RawBytes"] = string.Join(' ', bytes.Select(b => b.ToString("X2")));
      }
      return dict;
    }

    public static void DumpJson(string path, AssemblyBytes assm) {
      var json = JsonConvert.SerializeObject(Rewrite(assm.Node, assm), Formatting.Indented);
      var jsonPath = Path.Join(AppContext.BaseDirectory, Path.GetFileNameWithoutExtension(path) + ".json");
      File.WriteAllText(jsonPath, json);
      Console.Error.WriteLine($"jsonPath: {jsonPath}");
    }

    [TestMethod]
    public void ValidateExample() {
      Run(view("Program.dat"));
    }

    [TestMethod]
    public void BaselineExample() {
      using var s = File.OpenRead(view("Program.dat"));
      var assm = new AssemblyBytes(s);

      var expected = FormatJson(assm.Node.ToJson());

      var actual = FormatJson(File.ReadAllText(view("bytes.json")));
      if (actual != expected) {
        File.WriteAllText(view("bytes.json"), expected);
        Assert.Fail("Baseline was out of date, but fixed now!");
      }
    }

    static string view(string file) {
      var d = new DirectoryInfo(Directory.GetCurrentDirectory());
      for (; d != null; d = d.Parent) {
        var v = d.GetDirectories("view").SingleOrDefault();
        if (v != null) return v.GetFiles(file).Single().FullName;
      }
      throw new Exception("no view");
    }


    static AssemblyBytes Run(string path) {
      using var s = File.OpenRead(path);

      AssemblyBytes assm = new AssemblyBytes(s);

      DumpJson(path, assm);

      try {
        assm.Node.CallBack(AssertNoErrors);

        assm.Node.CallBack(AssertSized);

        assm.Node.CallBack(AssertChildrenDontOverlap);

        assm.Node.CallBack(AssertUniqueNames);

        assm.Node.CallBack(AssertLinkOrChildren);

        assm.Node.CallBack(AssertEcma);

        assm.Node.CallBack(AssertNamed);

        assm.Node.CallBack(AssertParentDifferentSizeThanChild);

        byte[] data;
        using var memory = new MemoryStream();
        s.Position = 0;
        s.CopyTo(memory);
        data = memory.ToArray();
        assm.Node.CallBack(node => AssertInterestingBytesNotIgnored(node, data));

        return assm;
      } catch {
        System.Console.Error.WriteLine(assm.Node.ToString());
        throw;
      }
    }

    static void AssertSized(CodeNode node) {
      Asserts.IsLessThan(node.Start, node.End, node.SelfPath);
    }

    static void AssertChildrenDontOverlap(CodeNode node) {
      foreach (var o in node.Children.Zip(node.Children.Skip(1), (last, next) => new { last, next })) {
        Asserts.IsLessThanOrEqual(o.last.End, o.next.Start, $"{o.last.SelfPath} overlaps {o.next.SelfPath}");
      }
    }

    static void AssertNoErrors(CodeNode node) {
      var error = node.Errors.FirstOrDefault();
      Assert.IsNull(error, $"{node.SelfPath}: {error}");
    }

    static void AssertUniqueNames(CodeNode node) {
      var name = node.Children.GroupBy(c => c.NodeName).Where(g => g.Count() > 1).FirstOrDefault()?.Key;
      Assert.IsNull(name, $"{node.SelfPath}: duplicate /{name}");
    }

    static void AssertLinkOrChildren(CodeNode node) {
      if (node.Link?.SelfPath != null && node.Children.Any()) {
        Assert.Fail($"{node.NodeName} has link {node.SelfPath} and {node.Children.Count} children");
      }
    }

    static void AssertEcma(CodeNode node) {
      if (node.GetType().IsGenericType && node.GetType().GetGenericTypeDefinition() == typeof(EnumNode<>)) {
        Assert.IsNotNull(node.EcmaSection, $"{node.SelfPath} is an enum but has no EcmaSection");
      }

      if (node.Description != null) {
        Assert.IsFalse(node.Description.Contains("§"), $"{node.NodeName} description {node.Description} should not contain the '§' character");
      }

      if (node.Children.Any()) {
        // if (node.EcmaSection == null) Console.Error.WriteLine(node.SelfPath.Replace("/", " / "));
        Assert.IsNotNull(node.EcmaSection, $"{node.SelfPath} has children but no EcmaSection");
      }
    }

    static void AssertNamed(CodeNode node) {
      Assert.AreNotEqual(CodeNode.OOPS_NAME, node.NodeName, node.SelfPath);
    }

    static void AssertParentDifferentSizeThanChild(CodeNode node) {
      if (node.Children.Count == 1 && node.Start == node.Children.Single().Start && node.End == node.Children.Single().End) {
        var exceptions = new[] {
          nameof(Section.Methods),
          nameof(Section.GuidHeap),
          nameof(TildeStream.TypeSpecs),
          nameof(TildeStream.StandAloneSigs),
          nameof(TildeStream.ModuleRefs),
          nameof(Method.CilOps),
          nameof(TypeSpecSig),
          nameof(MethodDefRefSig.RetType),
          nameof(PropertySig.Param),
          nameof(TypeSig.ArrayElementType),
          nameof(TypeSig.GenArgTypes),
        };
        if (exceptions.Any(sub => node.NodeName.Contains(sub))) {
          return;
        }
        if (node.GetType().GetGenericTypeDefinition() == typeof(TableRun<>)) {
          return;
        }
        Assert.Fail($"{node.SelfPath} at {node.Start} is same size as child {node.Children.Single().NodeName}");
      }
    }

    static void AssertInterestingBytesNotIgnored(CodeNode node, byte[] data) {
      if (!node.Children.Any()) {
        if (node.NodeValue != "") return;

        if (node.NodeName == nameof(Section.UserStringHeap))
          return; //TODO(fixme) user strings are not parsed

        if (node.NodeName.Contains("Sections["))
          return; //TODO(link) parse .sdata and all other PE sections

        foreach (var i in Enumerable.Range(node.Start, node.End - node.Start)) {
          if (data[i] != 0) {
            Assert.Fail($"Interesting byte 0x{data[i]:X} at 0x{i:X} was labeled \"\" in node {node.SelfPath}");
          }
        }
        return;
      }

      var childIncludes = new HashSet<int>();
      foreach (var child in node.Children) {
        childIncludes.UnionWith(new HashSet<int>(Enumerable.Range(child.Start, child.End - child.Start)));
      }

      foreach (var i in Enumerable.Range(node.Start, node.End - node.Start)) {
        if (data[i] == 0 || childIncludes.Contains(i))
          continue;

        Assert.Fail($"Interesting byte 0x{data[i]:X} at 0x{i:X} was non-zero in node {node.NodeName}");
      }
    }
  }
}
