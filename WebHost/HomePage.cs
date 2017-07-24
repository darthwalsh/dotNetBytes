using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using Nancy.Responses;

namespace WebHost
{
    public class HomePage : NancyModule
    {
        class Parsed
        {
            public byte[] Json;
            public byte[] File;
        }

        static Lazy<Parsed> Example = new Lazy<Parsed>(() => Parse(typeof(AssemblyBytes).Assembly.GetManifestResourceStream("view.Program.dat")));

        public HomePage()
        {
            Get["/"] = _ => Response.AsFile("bin/Content/view.html");
            Get["/favicon.ico"] = _ => Response.AsFile("bin/Content/favicon.ico");

            Get["/{file}"] = _ => Response.AsFile($"bin/Content/{_.file}");

            Post["/parse", runAsync: true] = async (_, cancel) =>
            {
                var parsed = await ParseAsync(base.Request.Body, cancel);

                return new Response
                {
                    ContentType = MimeTypes.GetMimeType(".json"),
                    Contents = s => s.Write(parsed.Json, 0, parsed.Json.Length),
                };
            };
        }

        static Parsed Parse(Stream stream)
        {
            return ParseAsync(stream, CancellationToken.None).Result;
        }

        static async Task<Parsed> ParseAsync(Stream stream, CancellationToken cancel)
        {
            using (stream)
            using (var buffer = new MemoryStream())
            {
                await stream.CopyToAsync(buffer, 4096, cancel);
                buffer.Position = 0;

                AssemblyBytes assm = new AssemblyBytes(buffer);

                return new Parsed
                {
                    File = buffer.ToArray(),
                    Json = Encoding.UTF8.GetBytes(assm.Node.ToJson())
                };
            }
        }
    }
}