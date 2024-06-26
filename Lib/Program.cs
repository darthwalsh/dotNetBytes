using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleToAttribute("Tests")]

public static class Program
{
  static int Main(string[] args) {
    var path = args.FirstOrDefault() ?? Path.Join("..", "view", "Program.dat");

    return Run(path);
  }

  public static int Run(string path) {
    AssemblyBytes assm;
    using var fileStream = File.OpenRead(path);
    assm = new AssemblyBytes(fileStream);

    Console.WriteLine(assm.Node.ToJson());

    int errors = 0;
    assm.Node.CallBack(n => {
      errors += n.Errors.Count;
      foreach (var error in n.Errors) {
        Console.Error.WriteLine(error);
      }
    });
    return errors;
  }
}

/*
Explanations: 

TODO(fixme)         - Probablty a bug.
TODO(Descriptions)  - Use [DescriptionAttribute] and copy text from spec.
TODO(Index4Bytes)   - heap/table index can be 2 or 4 bytes wide, but assuming short instead of int
TODO(link)          - .Link should be implemented.
TODO(size)          - set .Link, but also check link End-Start and create Error if size doesn't match
TODO(method)        - Error validation for method op codes
TODO(pedant)        - Small errors aren't caught
TODO(SpecViolation) - Seems to be a spec violation -- could try to resolve. Check https://github.com/dotnet/runtime/blob/main/docs/design/specs/Ecma-335-Augments.md or https://github.com/dotnet/runtime/blob/master/docs/project/dotnet-standards.md Microsoft-specific implementation notes
TODO(PERF)          - Idea to speed up performance; not needed if app is "fast enough"
*/
