using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CloudFunction
{
  public class Function : IHttpFunction
  {
    [HttpPost]
    public async Task HandleAsync(HttpContext context) {
      if (context.Request.Method != "POST") {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Only POST allowed");
        return;
      }

      using var buffer = new MemoryStream();
      await context.Request.Body.CopyToAsync(buffer, 4096);
      buffer.Position = 0;

      var assm = new AssemblyBytes(buffer);

      var options = context.RequestServices.GetService<IOptions<JsonOptions>>();
      var json = assm.Node.ToJson(options?.Value.JsonSerializerOptions);
      await context.Response.WriteAsync(json);
    }
  }
}
