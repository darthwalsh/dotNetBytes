using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

[assembly: InternalsVisibleToAttribute("Tests")]

public static class Program
{
  static void Main(string[] args) {
    var path = args.FirstOrDefault() ?? Path.Join("..", "view", "Program.dat");

    Run(path);
  }

  public static void Run(string path) {
    using var fileStream = File.OpenRead(path);
    AssemblyBytes assm = new AssemblyBytes(fileStream);

    Console.WriteLine(assm.Node.ToJson());
  }

  static void Diff(string path1, string path2) {
    var j1 = Serialize(path1);
    var j2 = Serialize(path2);


    var start = new ProcessStartInfo {
      FileName = "code",
      Arguments = $"--diff {j1} {j2}",
      UseShellExecute = true,
    };
    using var proc = Process.Start(start);
    proc.WaitForExit();

    File.Delete(j1);
    File.Delete(j2);

    static string Serialize(string path) {
      using var fileStream = File.OpenRead(path);
      AssemblyBytes assm = new AssemblyBytes(fileStream);

      var jsonPath = Path.Join(Path.GetTempPath(), Path.GetFileNameWithoutExtension(path) + ".json");

      var json = JsonSerializer.Serialize(LengthRewrite(assm.Node, assm), new JsonSerializerOptions { WriteIndented = true });
      File.WriteAllText(jsonPath, json);
      return jsonPath;
    }
  }

  static Dictionary<string, object> LengthRewrite(CodeNode node, AssemblyBytes assm) {
    var dict = new Dictionary<string, object>();
    // Skip name
    if (node.Description != "") dict[nameof(node.Description)] = node.Description;
    dict[nameof(node.NodeValue)] = node.NodeValue;
    dict["Length"] = "0x" + (node.End - node.Start).ToString("X");
    if (node.Link != null) dict["LinkPath"] = node.Link?.SelfPath;
    if (node.Errors.Any()) dict[nameof(node.Errors)] = node.Errors;

    foreach (var ch in node.Children) {
      var name = ch.NodeName;
      while (dict.ContainsKey(name)) {
        name = "_" + name;
      }
      dict[name] = LengthRewrite(ch, assm);
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
}

/*
Explanations: 

TODO(fixme)         - Probablty a bug.
TODO(Descriptions)  - Use [DescriptionAttribute] and copy text from spec.
TODO(Index4Bytes)   - heap/table index can be 2 or 4 bytes wide, but assuming short instead of int
TODO(link)          - .Link should be implemented.
TODO(method)        - Error validation for method op codes
TODO(pedant)        - Small errors aren't caught
TODO(SpecViolation) - Seems to be a spec violation -- could try to resolve.
TODO(PERF)          - Idea to speed up performance; not needed if app is "fast enough"
*/
