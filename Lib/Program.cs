using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleToAttribute("Tests")]

public static class Program
{
  static void Main(string[] args) {
    var path = args.FirstOrDefault() ?? Path.Join("..", "view", "Program.dat");

    Run(path);
  }

  public static void Run(string path) {
    AssemblyBytes assm;
    using var fileStream = File.OpenRead(path);
    assm = new AssemblyBytes(fileStream);

    Console.WriteLine(assm.Node.ToJson());
  }
}

/*
Explanations: 

TODO(fixme)         - Probablty a bug.
TODO(Descriptions)  - Use [DescriptionAttribute] and copy text from spec.
TODO(link)          - .Link should be implemented.
TODO(method)        - Error validation for method op codes
TODO(pedant)        - Small pedantic wrong behavior
TODO(Sig)           - Using BlobHeapIndex as byte[] but should parse the semantics
*/
