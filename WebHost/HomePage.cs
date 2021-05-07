using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nancy;

namespace WebHost
{
  public class HomePage : NancyModule
  {
    class Parsed
    {
      public byte[] Json;
      public byte[] File;
    }

    static Lazy<Parsed> example = new Lazy<Parsed>(() => Parse(typeof(AssemblyBytes).Assembly.GetManifestResourceStream("view.Program.dat")));

    public HomePage()
    {
      Get["/"] = _ => Response.AsFile("bin/Content/view.html");

      // Example data
      Get["/Program.dat"] = _ => new Response
      {
        ContentType = MimeTypes.GetMimeType(".exe"),
        Contents = s => s.Write(example.Value.File),
      };
      Get["/bytes.json"] = _ => new Response
      {
        ContentType = MimeTypes.GetMimeType(".json"),
        Contents = s => s.Write(example.Value.Json),
      };

      Get["/{file}"] = _ => Response.AsFile($"bin/Content/{_.file}");

      Post["/parse", runAsync: true] = async (_, cancel) =>
      {
        try
        {
          var parsed = await ParseAsync(base.Request.Body, cancel);
          return new Response
          {
            ContentType = MimeTypes.GetMimeType(".json"),
            Contents = s => s.Write(parsed.Json),
          };
        }
        catch (Exception e)
        {
          return new Response
          {
            StatusCode = HttpStatusCode.InternalServerError,
            ContentType = MimeTypes.GetMimeType(".json"),
            Contents = s =>
                  {
                    using (var writer = new StreamWriter(s))
                    {
                      writer.Write(e.ToString());
                    }
                  }
          };
        }
      };
    }

    static Parsed Parse(Stream stream) => ParseAsync(stream, CancellationToken.None).Result;

    static async Task<Parsed> ParseAsync(Stream stream, CancellationToken cancel)
    {
      using (stream)
      using (var buffer = new MemoryStream())
      {
        await stream.CopyToAsync(buffer, 4096, cancel);
        buffer.Position = 0;

        var assm = new AssemblyBytes(buffer);

        return new Parsed
        {
          File = buffer.ToArray(),
          Json = Encoding.UTF8.GetBytes(assm.Node.ToJson())
        };
      }
    }
  }

  static class StreamExtensions
  {
    public static void Write(this Stream stream, byte[] buffer) => stream.Write(buffer, 0, buffer.Length);
  }
}