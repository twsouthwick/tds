using Microsoft.Protocols.Tds.Features;
using System.Net;

namespace Microsoft.Protocols.Tds;

public static class TdsParserBuilderExtensions
{
    public static ITdsConnectionBuilder Use(this ITdsConnectionBuilder builder, Func<TdsConnectionContext, TdsConnectionDelegate, ValueTask> middleware)
        => builder.Use(next => context => middleware(context, next));

    public static ITdsConnectionBuilder UseHostResolution(this ITdsConnectionBuilder builder)
           => builder.Use(async (ctx, next) =>
           {
               if (ctx.Features.Get<IConnectionStringFeature>() is { IPAddress: null, Host: { } host, Port: { } port } feature)
               {
                   if (IPAddress.TryParse(host, out var parsed))
                   {
                       feature.IPAddress = parsed;
                   }
                   else
                   {
                       var addresses = await Dns.GetHostAddressesAsync(host);

                       if (addresses.Length == 0)
                       {
                           throw new InvalidOperationException($"Could not find {host}");
                       }

                       var address = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                       if (address is null)
                       {
                           throw new InvalidOperationException("Could not find Ipv4 address");
                       }

                       feature.IPAddress = address;
                   }
               }

               await next(ctx);
           });

    public static ITdsConnectionBuilder UseDebugging(this ITdsConnectionBuilder builder)
        => builder.Use((ctx, next) =>
        {
            return next(ctx);
        });
}
