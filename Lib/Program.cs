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

    Console.WriteLine(assm.Node.ToString());
  }
}