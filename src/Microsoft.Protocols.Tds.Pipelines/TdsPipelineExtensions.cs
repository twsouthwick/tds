// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Protocols.Tds;

public static class TdsPipelineExtensions
{
    public static ITdsConnectionBuilder UseSockets(this ITdsConnectionBuilder builder)
        => builder.Use(async (ctx, next) =>
        {
            if (ctx.Features.GetRequiredFeature<IConnectionStringFeature>() is { Endpoint: { } endpoint } && CreateTcpClient(endpoint) is { } client)
            {
                using (client)
                {
                    using var stream = client.GetStream();

                    ctx.AddTdsConnection(new NetworkStreamDuplexPipe(stream));

                    await next(ctx);
                }
            }
            else
            {
                await next(ctx);
            }
            static TcpClient? CreateTcpClient(EndPoint ep) => ep switch
            {
                IPEndPoint ip => new TcpClient(ip),
                DnsEndPoint dns => new TcpClient(dns.Host, dns.Port),
                _ => null
            }; ;
        });

    public static IDuplexPipe AddTdsConnection(this TdsConnectionContext ctx, IDuplexPipe pipe)
    {
        var connectionFeature = new ConnectionPipelineFeature(ctx, pipe);

        ctx.Features.Set<ISslFeature>(connectionFeature);
        ctx.Features.Set<ITdsConnectionFeature>(connectionFeature);

        return connectionFeature;
    }

    private sealed class NetworkStreamDuplexPipe(NetworkStream stream) : IDuplexPipe
    {
        public PipeReader Input { get; } = PipeReader.Create(stream);

        public PipeWriter Output { get; } = PipeWriter.Create(stream);
    }
}
