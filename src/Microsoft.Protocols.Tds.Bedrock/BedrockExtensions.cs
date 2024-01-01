using Bedrock.Framework;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Security;

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
            var feature = new BedrockFeature(ctx, connection);

            ctx.Features.Set<ITdsConnectionFeature>(feature);
            ctx.Features.Set<IAbortFeature>(feature);
            ctx.Features.Set<ISslFeature>(feature);

            await next(ctx);
        });
    }

    private sealed class BedrockFeature(TdsConnectionContext ctx, ConnectionContext connection) : ITdsConnectionFeature, IAbortFeature, IMessageWriter<ITdsPacket>, ISslFeature
    {
        private bool _isSslEnabled;
        private ProtocolWriter? _writer;
        private ProtocolReader? _reader;

        public ProtocolWriter Writer => _writer ??= connection.CreateWriter();

        public ProtocolReader Reader => _reader ??= connection.CreateReader();

        public CancellationToken Token => connection.ConnectionClosed;

        bool ISslFeature.IsEnabled => connection.Features.Get<ITlsConnectionFeature>() is not null || _isSslEnabled;

        public ValueTask WritePacket(ITdsPacket packet)
            => Writer.WriteAsync(this, packet, Token);

        public async ValueTask ReadPacketAsync(ITdsPacket packet)
        {
            var response = await Reader.ReadAsync(new PacketReader(ctx, connection.Transport.Input, packet), Token);
            Reader.Advance();
        }

        public void Abort() => connection.Abort();

        void IMessageWriter<ITdsPacket>.WriteMessage(ITdsPacket message, IBufferWriter<byte> output)
            => message.Write(ctx, output);

        async ValueTask ISslFeature.EnableAsync()
        {
            if (_isSslEnabled)
            {
                return;
            }

            _writer = null;
            _reader = null;

            var memoryPool = MemoryPool<byte>.Shared;

            var inputPipeOptions = new StreamPipeReaderOptions
            (
                pool: memoryPool,
                bufferSize: memoryPool.GetMinimumSegmentSize(),
                minimumReadSize: memoryPool.GetMinimumAllocSize(),
                leaveOpen: true,
                useZeroByteReads: true
            );

            var outputPipeOptions = new StreamPipeWriterOptions
            (
                pool: memoryPool,
                leaveOpen: true
            );

            //var ssl = new TdsSslDuplexPipe(connection.Transport, inputPipeOptions, outputPipeOptions);
            var t = connection.Transport;
            var ssl = new SslDuplexAdapter(connection.Transport);

            connection.Transport = ssl;

            var options = new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
            };

            await ssl.AuthenticateAsync(options, Token);
            _isSslEnabled = true;
        }

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
