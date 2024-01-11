using Bedrock.Framework;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;

namespace Microsoft.Protocols.Tds;

public static class BedrockExtensions
{
    public static ITdsConnectionBuilder UseBedrock(this ITdsConnectionBuilder builder)
    {
        var client = new ClientBuilder(builder.Services)
            .UseSockets()
            .UseConnectionLogging()
            .Build();

        return builder.Use(async (ctx, next) =>
        {
            if (ctx.Features.GetRequiredFeature<IConnectionStringFeature>() is not { Endpoint: { } endpoint })
            {
                throw new InvalidOperationException("No IPAddress available");
            }

            var connection = await client.ConnectAsync(endpoint);

            connection.Transport = ctx.AddTdsConnection(connection.Transport);

            var feature = new BedrockFeature(ctx, connection);

            ctx.Features.Set<IAbortFeature>(feature);

            await next(ctx);
        });
    }

    private sealed class BedrockFeature(TdsConnectionContext ctx, ConnectionContext connection) : IAbortFeature
    {
        public CancellationToken Token => connection.ConnectionClosed;

        public void Abort() => connection.Abort();
    }
}
