using System;
using System.Runtime.InteropServices;

namespace Server
{
  [Guid("DBE0E8C4-1C61-41f3-B6A4-4E2F353D3D05")]
  public interface IManagedInterface
  {
    int PrintHi(string name);
  }

  [Guid("C6659361-1625-4746-931C-36014B146679")]
  public class InterfaceImplementation : IManagedInterface
  {
    public int PrintHi(string name) {
      Console.WriteLine("Hello, {0}!", name);
      return 33;
    }
  }
}

namespace Client
{
  [Guid("56A868B1-0AD4-11CE-B03A-0020AF0BA770"),
  InterfaceType(ComInterfaceType.InterfaceIsDual)]
  public interface IMediaControl
  {
    void Run();
    void AddSourceFilter(
          [In, MarshalAs(UnmanagedType.BStr)] string strFilename,
          [Out, MarshalAs(UnmanagedType.Interface)]
                  out object ppUnk);

    [return: MarshalAs(UnmanagedType.Interface)]
    object FilterCollection();
  }

  [ComImport, Guid("E436EBB3-524F-11CE-9F53-0020AF0BA770")]
  public class FilgraphManager
  {
  }

  public static class COM
  {
    static void Main(string[] args) {
      var graphManager = new FilgraphManager();

      var mc = (IMediaControl)graphManager;
    }
  }
}



