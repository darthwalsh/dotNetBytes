using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
  class PlatformTools
  {
    public static IEnumerable<BuildTools> All() {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        yield return new FrameworkTools();
      } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
        throw new NotImplementedException();
      } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
        throw new NotImplementedException();
      } else {
        throw new NotImplementedException();
      }
      yield return new CoreTools();
      yield return new MonoTools();
    }
  }

  interface BuildTools
  {
    string Id { get; }
    void Compile(string args);
    void Assemble(string args);
  }

  class FrameworkTools : BuildTools
  {
    public string Id => "framework";

    static string csc;
    public void Compile(string args) {
      if (csc is null) {
        csc = FindCsc();
      }
      ProcessUtil.Run(csc, args);
    }
    public void Assemble(string args) {
      var ilasm = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\ilasm.exe";
      ProcessUtil.Run(ilasm, args);
    }

    static string FindCsc() {
      var years = new[] { "2022", "2019", "2017" };
      var editions = new[] { "Enterprise", "Professional", "Community" };
      foreach (var year in years) {
        foreach (var edition in editions) {
          var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
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
      throw new FileNotFoundException("csc.exe");
      // Doesn't work when running tests in dotnet core:
      //   ToolLocationHelper.GetFoldersInVSInstalls() + @"\MSBuild\Current\Bin\Roslyn\csc.exe";
    }
  }

  class CoreTools : BuildTools {
    public string Id => "core";

    static string csc;
    public void Compile(string args) {
      if (csc is null) {
        csc = FindCsc();
        Console.Error.WriteLine($"Compiling with {csc}");
      }

      var refs = string.Join(' ',
        Directory.GetFiles(RefDir(), "*.dll").Select(dll => $"\"/reference:{dll}\""));
      ProcessUtil.Run("dotnet", $"\"{csc}\" {refs} {args}");
    }
    public void Assemble(string args) {
      var ilasm = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ilasm.exe" : "ilasm";
      ilasm = Path.Join(AppContext.BaseDirectory, "runtimes", rid(), "native", ilasm);

      if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        args = string.Join(' ', args.Split(' ').Select(arg => arg.StartsWith('/') ? "-" + arg.Substring(1) : arg));
      }
      ProcessUtil.Run(ilasm, args);
    }

    static string FindCsc() {
      var lines = ProcessUtil.Run("dotnet", "--list-sdks").Split(Environment.NewLine);
      var v3line = lines.Single(l => l.StartsWith("3.")).Split(' ', 2);
      var version = v3line[0];
      var path = v3line[1].Trim('[', ']');
      return Path.Join(path, version, "Roslyn", "bincore", "csc.dll");
    }

    static string RefDir() {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        return @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\";
      } else
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
        return "/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/3.1.0/ref/netcoreapp3.1/";
      } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
        return "/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref/3.1.0/ref/netcoreapp3.1/";
      } else {
        throw new NotImplementedException();
      }
    }

    static string rid() {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        return "win-x86";
      } else
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
        return "osx-x64";
      } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
        throw new NotImplementedException();
      } else {
        throw new NotImplementedException();
      }
    }
  }

  class MonoTools : BuildTools
  {
    public string Id => "mono";

    static string csc;
    public void Compile(string args) {
      if (csc is null) {
        csc = "mcs";
        // csc = FindCsc();
        Console.Error.WriteLine($"Compiling with {csc}");
      }
      ProcessUtil.Run(csc, args);
    }
    public void Assemble(string args) {
      var ilasm = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ilasm.exe" : "ilasm"; // TODO

      // if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
      //   args = string.Join(' ', args.Split(' ').Select(arg => arg.StartsWith('/') ? "-" + arg.Substring(1) : arg));
      // }
      ProcessUtil.Run("ilasm.bat", args);
    }
  }

  class ProcessUtil {
    public static string Run(string filename, string processArgs) {
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
  }
}
