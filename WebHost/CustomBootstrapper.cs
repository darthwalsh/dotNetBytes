using Nancy;
using Nancy.Bootstrapper;
using Nancy.Hosting.Aspnet;
using Nancy.Responses.Negotiation;

namespace WebHost
{
  public class CustomBootstrapper : DefaultNancyBootstrapper
  {
    protected override byte[] FavIcon => null;

    protected override IRootPathProvider RootPathProvider => new AspNetRootPathProvider();

    protected override NancyInternalConfiguration InternalConfiguration => NancyInternalConfiguration.WithOverrides(c =>
    {
      c.ResponseProcessors.Remove(typeof(JsonProcessor));
    });
  }
}