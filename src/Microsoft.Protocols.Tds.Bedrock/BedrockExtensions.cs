using Bedrock.Framework;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Buffers;
using System.Diagnostics;
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

        private sealed class BedrockFeature(TdsConnectionContext ctx, ConnectionContext connection) : ITdsConnectionFeature, IAbortFeature, IMessageWriter<ITdsPacket>, IMessageReader<object>
        {
            public ProtocolWriter Writer { get; } = connection.CreateWriter();

            public ProtocolReader Reader { get; } = connection.CreateReader();

            public CancellationToken Token => connection.ConnectionClosed;

            public ValueTask WritePacket(ITdsPacket packet)
                => Writer.WriteAsync(this, packet, Token);

            private ITdsPacket? _currentPacket;

            public async ValueTask ReadPacketAsync(ITdsPacket packet)
            {
                Debug.Assert(_currentPacket is null);
                _currentPacket = packet;
                await Reader.ReadAsync(this, Token);
            }

            public void Abort() => connection.Abort();

            void IMessageWriter<ITdsPacket>.WriteMessage(ITdsPacket message, IBufferWriter<byte> output)
                => message.Write(ctx, output);

            bool IMessageReader<object>.TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out object message)
            {
                message = null!;

                if (_currentPacket is { } current)
                {
                    _currentPacket = null;
                    current.Read(ctx, input);
                    return true;
                }

                return false;
            }
        }
    }
}
