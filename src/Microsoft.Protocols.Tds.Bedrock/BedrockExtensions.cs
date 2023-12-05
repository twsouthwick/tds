using Bedrock.Framework;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Protocols.Tds
{
    public static class BedrockExtensions
    {
        public static ITdsConnectionBuilder UseBedrock(this ITdsConnectionBuilder builder)
        {
            var client = new ClientBuilder(builder.Services)
                .UseSockets()
                .UseConnectionLogging()
                //.UseClientTls(options =>
                //{
                //    options.AllowAnyRemoteCertificate();
                //    options.RemoteCertificateValidation = Test;
                //})
                .Build();

            return builder.Use(async (ctx, next) =>
            {
                if (ctx.Features.GetRequiredFeature<IConnectionStringFeature>() is not { IPAddress: { } ip, Port: { } port })
                {
                    throw new InvalidOperationException("No IPAddress available");
                }

                var connection = await client.ConnectAsync(new IPEndPoint(ip, port));
                var feature = new BedrockFeature(ctx, connection);

                ctx.Features.Set<ITdsConnectionFeature>(feature);
                ctx.Features.Set<IAbortFeature>(feature);

                await next(ctx);
            });
        }

        private static bool Test(X509Certificate2 certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }

        private sealed class BedrockFeature(TdsConnectionContext ctx, ConnectionContext connection) : ITdsConnectionFeature, IAbortFeature, IMessageWriter<ITdsPacket>, IMessageReader<ITdsPacket>
        {
            public ProtocolWriter Writer { get; } = connection.CreateWriter();

            public ProtocolReader Reader { get; } = connection.CreateReader();

            public CancellationToken Token => connection.ConnectionClosed;

            public ValueTask WritePacket(ITdsPacket packet)
                => Writer.WriteAsync(this, packet, Token);

            public async ValueTask<ITdsPacket> ReadPacketAsync()
            {
                var result = await Reader.ReadAsync(this, Token);

                return result.Message;
            }

            public void Abort() => connection.Abort();

            void IMessageWriter<ITdsPacket>.WriteMessage(ITdsPacket message, IBufferWriter<byte> output)
                => message.Write(ctx, output);

            bool IMessageReader<ITdsPacket>.TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out ITdsPacket message)
            {
                message = default!;
                return false;
            }
        }
    }
}
