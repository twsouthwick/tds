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
    {
        string[] keys = ["Data Source", "Server", "Address", "Addr", "Network Address"];

        bool TryGetFirst(TdsConnectionContext context, out ReadOnlyMemory<char> datasource, out bool trust)
        {
            const bool DefaultTrust = false;

            var c = context.Features.GetRequiredFeature<IConnectionStringFeature>();

            trust = c.TryGetValue("TrustServerCertificate", out var trustServerValue)
                ? (!bool.TryParse(trustServerValue.Span.ToStringIfNotCore(), out var result) || result)
                : DefaultTrust;

            foreach (var key in keys)
            {
                if (c.TryGetValue(key, out datasource))
                {
                    return true;
                }
            }

            datasource = default;
            return false;
        }

        return builder.Use(async (ctx, next) =>
        {
            if (TryGetFirst(ctx, out var datasource, out var trust) && CreateTcpClient(datasource, out var endpoint) is { } client)
            {
                ctx.Features.Set<IConnectionFeature>(new SocketConnectionFeature(endpoint) { TrustServerCertificate = trust });

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
        });
    }

#if NET
    private static ReadOnlySpan<char> ToStringIfNotCore(this ReadOnlySpan<char> r) => r;
#else
    private static string ToStringIfNotCore(this ReadOnlySpan<char> r) => r.ToString();
#endif

    private sealed class SocketConnectionFeature(EndPoint endpoint) : IConnectionFeature
    {
        private string? _hostname;

        public EndPoint? Endpoint { get; set; } = endpoint;

        public string Database { get; set; } = string.Empty;

        public string HostName
        {
            get => _hostname ?? (Endpoint is DnsEndPoint { Host: { } host } ? host : string.Empty);
            set => _hostname = value;
        }
        public bool TrustServerCertificate { get; set; }
    }

    private static TcpClient CreateTcpClient(ReadOnlyMemory<char> datasource, out EndPoint endpoint)
    {
        var hostname = GetPort(datasource.Span, out var port).ToStringIfNotCore();

        if (IPAddress.TryParse(hostname, out var address))
        {
            var ipEndpoint = new IPEndPoint(address, port);
            endpoint = ipEndpoint;
            return new TcpClient(ipEndpoint);
        }
        else
        {
            endpoint = new DnsEndPoint(hostname.ToString(), port);
            return new TcpClient(hostname.ToString(), port);
        }

        static ReadOnlySpan<char> GetPort(ReadOnlySpan<char> datasource, out int port)
        {
            var portIndex = datasource.IndexOf(',');

            if (portIndex == -1)
            {
                port = 1433;
                return datasource;
            }
            else
            {
                var portSpan = datasource.Slice(portIndex).ToStringIfNotCore();

                if (!int.TryParse(portSpan, out port))
                {
                    throw new InvalidOperationException("Invalid port for connection string");
                }

                return datasource.Slice(0, portIndex);
            }
        }
    }

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
