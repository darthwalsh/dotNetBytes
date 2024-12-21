using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => {
  options.AddDefaultPolicy(builder => {
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
  });
});

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.BadRequest("Only POST allowed"));

app.MapPost("/", async (HttpContext context) => {
  using var buffer = new MemoryStream();
  await context.Request.Body.CopyToAsync(buffer, 4096);
  buffer.Position = 0;

  var assm = new AssemblyBytes(buffer);

  var options = context.RequestServices.GetService<IOptions<JsonOptions>>();
  var json = assm.Node.ToJson(options?.Value.JsonSerializerOptions);
  await context.Response.WriteAsync(json);
});

app.Run();
