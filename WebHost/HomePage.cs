using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Hosting.Aspnet;
using Nancy.Responses;
using Nancy.Responses.Negotiation;

namespace WebHost
{
    //TODO refactor

    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        protected override byte[] FavIcon => null;

        protected override IRootPathProvider RootPathProvider => new AspNetRootPathProvider();

        protected override NancyInternalConfiguration InternalConfiguration => NancyInternalConfiguration.WithOverrides(c =>
        {
            c.ResponseProcessors.Remove(typeof(JsonProcessor));
        });
    }

    public class HomePage : NancyModule
    {
        class Parsed
        {
            public byte[] Json;
            public byte[] File;
        }

        static Dictionary<Guid, Parsed> bytesJson = new Dictionary<Guid, Parsed>();

        static Lazy<Parsed> Example = new Lazy<Parsed>(() => Parse(typeof(AssemblyBytes).Assembly.GetManifestResourceStream("view.Program.dat")));

        public HomePage()
        {
            Get["/"] = _ => Response.AsFile("bin/Content/index.html");
            Get["/favicon.ico"] = _ => Response.AsFile("bin/Content/favicon.ico");

            Get["/{id}/Program.dat"] = _ => LookupParsed(_, ".exe", (Func<Parsed, byte[]>)(r => r.File));
            Get["/{id}/bytes.json"] = _ => LookupParsed(_, ".json", (Func<Parsed, byte[]>)(r => r.Json));

            Get["/{id}/{file}"] = _ => Response.AsFile($"bin/Content/{_.file}");

            Post["/submit.html", true] = async (_, cancel) =>
            {
                var guid = Guid.NewGuid();

                var file = base.Request.Files.FirstOrDefault();
                if (file == null)
                    return new TextResponse(HttpStatusCode.BadRequest, "no file");

                var parsed = await ParseAsync(file.Value, cancel);

                lock (bytesJson)
                {
                    bytesJson[guid] = parsed;
                }

                return Response.AsRedirect($"/{guid.ToString("N")}/view.html");
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

        static dynamic LookupParsed(dynamic _, string filePath, Func<Parsed, byte[]> func)
        {
            string id = _.id;

            Parsed parsed;
            if (id == "example")
            {
                parsed = Example.Value;
            }
            else
            {
                Guid guid;
                if (!Guid.TryParse(id, out guid))
                    return new TextResponse(HttpStatusCode.BadRequest, "Bad Guid");

                lock (bytesJson)
                {
                    bytesJson.TryGetValue(guid, out parsed);
                } 
            }

            if (parsed == null)
                return new TextResponse(HttpStatusCode.NotFound, "Try uploading your file again, sorry");

            return new Response
            {
                ContentType = MimeTypes.GetMimeType(filePath),
                Contents = s => s.Write(func(parsed), 0, func(parsed).Length),
            };
        }
    }
}