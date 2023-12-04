using Bedrock.Framework;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using System.IO.Pipelines;
using System.Net;

namespace Microsoft.Protocols.Tds
{
    public static class BedrockExtensions
    {
        public static ITdsConnectionBuilder UseBedrock(this ITdsConnectionBuilder builder, string hostOrAddress, int port = 1433)
            => builder
                .Use(async (ctx, next) =>
                {
                    var addresses = await Dns.GetHostAddressesAsync(hostOrAddress);

                    if (addresses.Length == 0)
                    {
                        throw new InvalidOperationException($"Could not find {hostOrAddress}");
                    }

                    var address = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                    if (address is null)
                    {
                        throw new InvalidOperationException("Could not find Ipv4 address");
                    }

                    ctx.Features.Set<IPEndPoint>(new(address, port));

                    await next(ctx);
                })
                .UseBedrock();

        public static ITdsConnectionBuilder UseBedrock(this ITdsConnectionBuilder builder, IPEndPoint endpoint)
            => builder
                .Use((ctx, next) =>
                {
                    ctx.Features.Set(endpoint);
                    return next(ctx);
                })
                .UseBedrock();

        public static ITdsConnectionBuilder UseBedrock(this ITdsConnectionBuilder builder)
        {
            var client = new ClientBuilder(builder.Services)
                .UseSockets()
                .UseClientTls(options =>
                {
                    options.AllowAnyRemoteCertificate();
                })
                .Build();

            return builder.Use(async (ctx, next) =>
            {
                var connection = await client.ConnectAsync(ctx.Features.GetRequiredFeature<IPEndPoint>());
                var feature = new BedrockFeature(connection);

                ctx.Features.Set<ITdsConnectionFeature>(feature);
                ctx.Features.Set<IAbortFeature>(feature);

                await next(ctx);
            });
        }

        private sealed class BedrockFeature(ConnectionContext connection) : ITdsConnectionFeature, IAbortFeature
        {
            public IDuplexPipe Pipe => connection.Transport;

            public CancellationToken Token => connection.ConnectionClosed;

            public void Abort()
            {
                connection.Abort();
            }
        }
    }
}
