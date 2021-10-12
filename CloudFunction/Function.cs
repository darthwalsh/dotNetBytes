using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading.Tasks;

namespace CloudFunction
{
  public class Startup : FunctionsStartup
  {
    public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services) {
      services.AddCors(options => options.AddPolicy("CorsApi", builder => builder
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()));
    }
    
    public override void Configure(WebHostBuilderContext context, IApplicationBuilder app) {
      app.UseCors("CorsApi");
    }
  }

  [FunctionsStartup(typeof(Startup))]
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
