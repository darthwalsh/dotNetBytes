using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

public static class Program
{
  static void Main(string[] args) {
    var path = args.FirstOrDefault() ?? Path.Join("..", "view", "Program.dat");

    Run(path);
  }

  public static void Run(string path) {
    AssemblyBytes assm;
    using (var fileStream = File.OpenRead(path)) {
      assm = new AssemblyBytes(fileStream);
    }

    Console.WriteLine(assm.Node.ToJson());
  }
}

/*
Explanations: 

TODO(diff-solonode) - Remove after merging wip-solonode with main. Makes the bytes.json diff much easier to look at.
TODO(fixme)         - Probablty a bug.
TODO(Descriptions)  - Before, reference types with [DescirptionAttribute] didn't show up. After fix TODO(diff-solonode) might need to copy more text from spec.
TODO(link)          - .Link should be implemented.
TODO(method)        - Error validation for method op codes
TODO(pedant)        - Small pedantic wrong behavior
TODO(Sig)           - Using BlobHeapIndex as byte[] but should parse the semantics
*/
