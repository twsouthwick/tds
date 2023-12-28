using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Protocols.Tds;

public static class SslStreamExtensions
{
    public static ITdsConnectionBuilder UseSslStream(this ITdsConnectionBuilder builder)
    {
        return builder.Use(async (ctx, next) =>
        {
            if (ctx.Features.GetRequiredFeature<IConnectionStringFeature>() is not { Endpoint: { } endpoint })
            {
                throw new InvalidOperationException("No IPAddress available");
            }

            using var feature = new StreamFeatures(endpoint);

            ctx.Features.Set<ITdsConnectionFeature>(feature);
            ctx.Features.Set<IAbortFeature>(feature);

            await feature.AuthenticateAsync();

            await next(ctx);
        });
    }

    private sealed class StreamFeatures : ITdsConnectionFeature, IAbortFeature, IDisposable
    {
        private readonly Pipe _pipe;
        private readonly CancellationTokenSource _cts;
        private readonly TcpClient _client;
        private readonly SslStream _ssl;

        public StreamFeatures(EndPoint endpoint)
        {
            _pipe = new Pipe();
            _cts = new CancellationTokenSource();
            _client = CreateClient(endpoint);
            _ssl = new SslStream(_client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertifiacte), null);
        }

        private static TcpClient CreateClient(EndPoint endpoint) => endpoint switch
        {
            DnsEndPoint dns => new TcpClient(dns.Host, dns.Port),
            IPEndPoint ip => new TcpClient(ip),
            _ => throw new NotImplementedException()
        };

        public async ValueTask AuthenticateAsync()
        {
            var options = new SslClientAuthenticationOptions
            {
                TargetHost = "sql.docker.internal",
            };

            await _ssl.AuthenticateAsClientAsync(options, _cts.Token);
        }

        ValueTask ITdsConnectionFeature.ReadPacketAsync(ITdsPacket packet)
        {
            throw new NotImplementedException();
        }

        ValueTask ITdsConnectionFeature.WritePacket(ITdsPacket packet)
        {
            throw new NotImplementedException();
        }

        private bool ValidateServerCertifiacte(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public void Dispose()
        {
            _ssl.Dispose();
            _client.Dispose();
        }

        CancellationToken IAbortFeature.Token => _cts.Token;

        void IAbortFeature.Abort() => _cts.Cancel();
    }
}
