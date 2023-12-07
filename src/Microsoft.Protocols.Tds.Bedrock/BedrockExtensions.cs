using Bedrock.Framework;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Buffers;
using System.Net;

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

        private sealed class BedrockFeature(TdsConnectionContext ctx, ConnectionContext connection) : ITdsConnectionFeature, IAbortFeature, IMessageWriter<ITdsPacket>
        {
            public ProtocolWriter Writer { get; } = connection.CreateWriter();

            public ProtocolReader Reader { get; } = connection.CreateReader();

            public CancellationToken Token => connection.ConnectionClosed;

            public ValueTask WritePacket(ITdsPacket packet)
                => Writer.WriteAsync(this, packet, Token);

            public async ValueTask ReadPacketAsync(ITdsPacket packet)
            {
                await Reader.ReadAsync(new PacketReader(ctx, packet), Token);
            }

            public void Abort() => connection.Abort();

            void IMessageWriter<ITdsPacket>.WriteMessage(ITdsPacket message, IBufferWriter<byte> output)
                => message.Write(ctx, output);

            private sealed class PacketReader(TdsConnectionContext ctx, ITdsPacket packet) : IMessageReader<object>
            {
                bool IMessageReader<object>.TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out object message)
                {
                    message = null!;
                    packet.Read(ctx, input);
                    return true;
                }
            }
        }
    }
}
