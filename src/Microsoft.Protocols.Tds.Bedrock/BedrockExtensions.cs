using Bedrock.Framework;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Buffers;
using System.IO.Pipelines;

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
            var tdsPacketTransport = new TdsPacketAdapter(connection.Transport);
            var sslFeature = new SslDuplexPipeFeature(ctx, tdsPacketTransport);

            connection.Transport = sslFeature;

            var feature = new BedrockFeature(ctx, connection);

            ctx.Features.Set<ITdsConnectionFeature>(feature);
            ctx.Features.Set<IAbortFeature>(feature);
            ctx.Features.Set<IPacketFeature>(tdsPacketTransport);
            ctx.Features.Set<ISslFeature>(sslFeature);

            await next(ctx);
        });
    }

    private sealed class BedrockFeature(TdsConnectionContext ctx, ConnectionContext connection) : ITdsConnectionFeature, IAbortFeature, IMessageWriter<ITdsPacket>
    {
        private IPacketFeature PacketFeature => ctx.Features.GetRequiredFeature<IPacketFeature>();

        public CancellationToken Token => connection.ConnectionClosed;

        public async ValueTask WritePacket(ITdsPacket packet)
        {
            PacketFeature.Type = packet.Type;
            packet.Write(ctx, connection.Transport.Output);
            await connection.Transport.Output.FlushAsync(Token);
        }

        public async ValueTask ReadPacketAsync(ITdsPacket packet)
        {
            var result = await connection.Transport.Input.ReadAsync(Token);

            packet.Read(ctx, result.Buffer);

            connection.Transport.Input.AdvanceTo(result.Buffer.End);
        }

        public void Abort() => connection.Abort();

        void IMessageWriter<ITdsPacket>.WriteMessage(ITdsPacket message, IBufferWriter<byte> output)
            => message.Write(ctx, output);

        private sealed class PacketReader(TdsConnectionContext ctx, PipeReader reader, ITdsPacket packet) : IMessageReader<object>
        {
            bool IMessageReader<object>.TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out object message)
            {
                message = null!;

                if (input.Length == 0)
                {
                    return false;
                }

                packet.Read(ctx, input);
                consumed = input.End;
                examined = input.End;
                reader.AdvanceTo(consumed);
                return true;
            }
        }
    }
}
