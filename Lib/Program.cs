using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

public static class Program
{
  static void Main(string[] args) {
    try {
      var path = args.FirstOrDefault() ?? Path.Join("..", "view", "Program.dat");

      Run(path);
    } catch (Exception e) {
      Console.WriteLine(e);
    }
  }

  public static void Run(string path) {
    AssemblyBytes assm;
    using (var fileStream = File.OpenRead(path)) {
      assm = new AssemblyBytes(fileStream);
    }

    Console.WriteLine(assm.Node.ToString());
  }
}
