﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    public void SimpleIL() => RunIL(@"Simple.il");

    [TestMethod]
    public void Blank() => RunIL(@"Blank.il", "/dll");

    [TestMethod]
    public void Data() => RunIL(@"Data.il");

    [TestMethod]
    public void FileTable() => RunIL(@"FileTable.il");

    // TODO compare local Lib diff vs online CloudFunction; Example doesn't have all features

    //MAYBE create test case that exercises all IL features, check code coverage, then test modifying each byte of the code...
    //    if the exe blows up does it needs to produce error in dotNetBytes (and not an exception)
    //MAYBE Also test with mono

    //TODO try out unmanaged exports library? https://sites.google.com/site/robertgiesecke/Home/uploads/unmanagedexports or https://github.com/RealGecko/NppLanguageTool/

    static string ilasm {
      get {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
          return Path.Join(AppContext.BaseDirectory, "runtimes", "osx-x64", "native", "ilasm");
        } else {
          // windows
          // Microsoft.Build.Utilities.ToolLocationHelper.GetPathToDotNetFrameworkFile(
          //     "ilasm.exe", Microsoft.Build.Utilities.TargetDotNetFrameworkVersion.VersionLatest);

          throw new NotImplementedException();
        }
      }
    }

    static string csc {
      get {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
          var references = Directory.GetFiles("/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/3.1.0/ref/netcoreapp3.1/", "*.dll").Select(dll => "/reference:" + dll);
          return string.Join(' ', new [] { "/usr/local/share/dotnet/sdk/3.1.413/Roslyn/bincore/csc.dll" }.Concat(references));
        } else {
          // windows
          // Path.Combine(
          //     Microsoft.Build.Utilities.ToolLocationHelper.GetFoldersInVSInstalls().First(),
          //     "MSBuild",
          //     "Current",
          //     "Bin",
          //     "Roslyn",
          //     "csc.exe");

          throw new NotImplementedException();
        }
      }
    }

    static void RunIL(string path, string args = "") {
      var outpath = Path.GetFullPath(path.Replace(".il", $".{CleanFileName(args)}.il.exe"));

      if (!File.Exists(outpath) || File.GetLastWriteTime(path) > File.GetLastWriteTime(outpath)) {
        Console.Error.WriteLine($"Assembling {outpath}");

        var switchchar = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/" : "-";
        args = string.Join(' ', args.Split(' ').Select(arg => arg.StartsWith('/') ? switchchar + arg.Substring(1) : arg));
        RunProcess(ilasm, $@"""{path}"" {switchchar}OUTPUT=""{outpath}"" {args}");
      } else {
        Console.Error.WriteLine($"Using existing {outpath}");
      }

      Run(outpath);
    }

    //TODO either invoke in-memory compiler and assembler, or run compiler as part of build time
    static void RunCompile(string path, string args = "", string optimize = "/optimize", string noconfig = "/noconfig") {
      var allArgs = $"{optimize} {noconfig} {args}";

      var outpath = Path.GetFullPath(path.Replace(".cs", $".{CleanFileName(allArgs)}.exe"));

      if (!File.Exists(outpath) || File.GetLastWriteTime(path) > File.GetLastWriteTime(outpath)) {
        Console.Error.WriteLine($"Compiling {outpath}");

        RunProcess("dotnet", $@"{csc} ""{path}"" /out:""{outpath}"" {allArgs}");
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

    static void RunProcess(string filename, string processArgs) {
      using (var p = Process.Start(new ProcessStartInfo {
        FileName = filename,
        Arguments = processArgs,

        WorkingDirectory = Path.Join(Directory.GetCurrentDirectory(), "Samples"),

        CreateNoWindow = true,
        WindowStyle = ProcessWindowStyle.Hidden,
        UseShellExecute = false,

        RedirectStandardOutput = true,
      })) {
        p.WaitForExit();

        var stdout = p.StandardOutput.ReadToEnd();
        Console.Error.WriteLine(stdout);

        Assert.AreEqual(0, p.ExitCode, "exit code. {0}", stdout);
      }
    }

    static string CleanFileName(string fileName) => Path.GetInvalidFileNameChars().Concat(" :").Aggregate(fileName, (current, bad) => current.Replace(bad.ToString(), "_"));

    static string FormatJson(string json) {
      dynamic parsedJson = JsonConvert.DeserializeObject(json);
      return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
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

      using var baselineJSON = File.OpenRead(view("bytes.json"));
      using var reader = new StreamReader(baselineJSON);

      var actual = FormatJson(reader.ReadToEnd());
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

      try {
        assm.Node.CallBack(AssertSized);

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
      }
    }

    static void AssertSized(CodeNode node) {
      Asserts.IsLessThan(node.Start, node.End);
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
      var name = node.Children.GroupBy(c => c.NodeName).Where(g => g.Count() > 1).FirstOrDefault()?.Key;
      Assert.IsNull(name, $"duplicate {name} under {node.NodeName}");
    }

    static void AssertLinkOrChildren(CodeNode node) {
      if (node.Link?.SelfPath != null && node.Children.Any()) {
        Assert.Fail($"{node.NodeName} has link {node.SelfPath} and {node.Children.Count} children");
      }
    }

    static void AssertParentDifferentSizeThanChild(CodeNode node) {
      if (node.Children.Count == 1 && node.Start == node.Children.Single().Start && node.End == node.Children.Single().End) {
        var exceptions = new[] { "TypeSpecs", "Methods", "GuidHeap", "StandAloneSigs", "ModuleRefs", "CilOps" };
        if (exceptions.Any(sub => node.NodeName.Contains(sub))) {
          return;
        }
        Assert.Fail($"{node.NodeName} at {node.Start}");
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

        Assert.Fail($"Interesting byte 0x{data[i]:X} at 0x{i:X} was non-zero in node {node.NodeName}");
      }
    }
  }
}
