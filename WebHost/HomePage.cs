using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Hosting.Aspnet;
using Nancy.Responses;
using Nancy.Responses.Negotiation;

namespace WebHost
{
    //TODO async, refactor, favicon

    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        protected override IRootPathProvider RootPathProvider
        {
            get { return new AspNetRootPathProvider(); }
        }

        protected override NancyInternalConfiguration InternalConfiguration
        {
            get
            {
                return NancyInternalConfiguration.WithOverrides(
                    (c) =>
                    {
                        c.ResponseProcessors.Remove(typeof(JsonProcessor));
                    });
            }
        }
    }

    public class HomePage : NancyModule
    {
        class Parsed
        {
            public byte[] Json;
            public byte[] File;
        }

        static Dictionary<Guid, Parsed> bytesJson = new Dictionary<Guid, Parsed>();


        public HomePage()
        {
            Get["/"] = _ => Response.AsFile("bin/Content/index.html");

            Get["/{id}/Program.dat"] = _ => LookupParsed(_, ".exe", (Func<Parsed, byte[]>)(r => r.File));
            Get["/{id}/bytes.json"] = _ => LookupParsed(_, ".json", (Func<Parsed, byte[]>)(r => r.Json));

            Get["/{id}/{file}"] = _ => Response.AsFile($"bin/Content/{_.file}");

            Post["/submit.html"] = _ =>
            {
                var guid = Guid.NewGuid();

                using (var buffer = new MemoryStream())
                {
                    var file = base.Request.Files.FirstOrDefault();
                    if (file == null)
                        return new TextResponse(HttpStatusCode.BadRequest, "no file");

                    file.Value.CopyTo(buffer); //TODO async
                    buffer.Position = 0;

                    AssemblyBytes assm = new AssemblyBytes(buffer);

                    var parsed = new Parsed
                    {
                        File = buffer.ToArray(),
                        Json = Encoding.UTF8.GetBytes(assm.Node.ToJson())
                    };

                    lock (bytesJson)
                    {
                        bytesJson[guid] = parsed;
                    }
                }

                return Response.AsRedirect($"/{guid.ToString("N")}/view.html");
            };
        }

        private static dynamic LookupParsed(dynamic _, string filePath, Func<Parsed, byte[]> func)
        {
            Guid guid;
            if (!Guid.TryParse(_.id, out guid))
                return new TextResponse(HttpStatusCode.BadRequest, "Bad Guid");

            Parsed parsed;
            lock (bytesJson)
            {
                bytesJson.TryGetValue(guid, out parsed);
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