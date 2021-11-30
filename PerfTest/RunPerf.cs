using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

namespace PerfTest
{
  [SimpleJob(RunStrategy.ColdStart, launchCount: 2)]
  public class FunctionInvoker
  {
    static HttpClient client = new HttpClient();

    static string view(string file) {
      var d = new DirectoryInfo(Directory.GetCurrentDirectory());
      for (; d != null; d = d.Parent) {
        var v = d.GetDirectories("view").SingleOrDefault();
        if (v != null) return v.GetFiles(file).Single().FullName;
      }
      throw new Exception("no view");
    }


    List<MemoryStream> streams;
    public FunctionInvoker() {
      streams = Directory.GetFiles("/Users/walshca/code/dotNetBytes/Tests/", "*.exe")
        .Select(file => new MemoryStream(File.ReadAllBytes(file)))
        .ToList();

      streams.Add(new MemoryStream(File.ReadAllBytes(view("Program.dat"))));
      Console.WriteLine("streams: " + streams.Count);
    }

    public async Task<int> Run(int port) {
      int len = 0;
      foreach (var stream in streams) {
        stream.Position = 0;

        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-msdownload");
        var response = await client.PostAsync("http://127.0.0.1:" + port, content);
        var str = await response.Content.ReadAsStringAsync();
        // Console.WriteLine(str);
        len += str.Length;
      }
      return len;
    }

    [Benchmark]
    public Task<int> WIPproj() => Run(8080);

    [Benchmark]
    public Task<int> Mainproj() => Run(7777);
  }

  public class Program
  {
    public static void Main(string[] args) {
      var summary = BenchmarkRunner.Run<FunctionInvoker>();

      // new FunctionInvoker().Run(8080).Wait();
    }
  }
}
