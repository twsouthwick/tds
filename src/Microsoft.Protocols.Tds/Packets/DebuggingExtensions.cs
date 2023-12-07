using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Protocols.Tds.Features;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

public static class DebuggingExtensions
{
    public static ITdsConnectionBuilder UseLogging(this ITdsConnectionBuilder builder)
        => builder.Use((ctx, next) =>
        {
            if (ctx.Features.Get<IPacketCollectionFeature>() is { } collection && builder.Services.GetService<ILoggerFactory>() is { } factory)
            {
                ctx.Features.Set<IPacketCollectionFeature>(new DebugFeatures(collection, factory.CreateLogger(nameof(IPacketCollectionFeature))));
            }

            return next(ctx);
        });

    private sealed class DebugFeatures(IPacketCollectionFeature other, ILogger logger) : IPacketCollectionFeature
    {
        public ITdsPacket? Get(TdsType type)
        {
            if (other.Get(type) is { } packet)
            {
                return new DebuggingPacket(packet, logger);
            }

            return null;
        }

        private sealed class DebuggingPacket(ITdsPacket other, ILogger logger) : ITdsPacket
        {
            public TdsType Type => other.Type;

            public async ValueTask OnReadCompleteAsync(TdsConnectionContext context)
            {
                await other.OnReadCompleteAsync(context);
                logger.LogTrace("Completed reading {Type}", Type);
            }

            public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
            {
                logger.LogTrace("Reading {Type}", Type);
                other.Read(context, data);
            }

            public string ToString(ReadOnlyMemory<byte> data, TdsPacketFormattingOptions options)
            {
                return other.ToString(data, options);
            }

            public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            {
                var otherWriter = new ArrayBufferWriter<byte>();
                other.Write(context, otherWriter);
                logger.LogTrace("{Contents}", ToString(otherWriter.WrittenMemory, TdsPacketFormattingOptions.Default));
                writer.Write(otherWriter.WrittenSpan);
            }
        }
    }
}
