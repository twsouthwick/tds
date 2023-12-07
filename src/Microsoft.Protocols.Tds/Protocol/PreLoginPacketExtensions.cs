using Microsoft.Protocols.Tds.Features;
using Microsoft.Protocols.Tds.Packets;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Protocol;

public static class PreLoginPacketExtensions
{
    public static void AddPreLogin(this IPacketCollectionBuilder builder)
        => builder.AddPacket(TdsType.PreLogin)
            .AddOption(new VersionOption())
            .AddOption(new EncryptOption())
            .AddOption(new InstanceOption())
            .AddOption(new ThreadIdOption())
            .AddOption(new MarsOption())
            .AddOption(new TraceIdOption())
            .AddOption(new FedAuthRequiredOption())
            .AddHandler(builder => builder
                .Use((ctx, next) =>
                {
                    return next(ctx);
                }));

    private sealed class VersionOption : IPacketOption
    {
        private static readonly Version _emptyVersion = new(0, 1, 0, 0);

        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
        {
            var version = context.Features.Get<IEnvironmentFeature>()?.Version ?? _emptyVersion;

            writer.Write(version);
        }
    }

    private sealed class EncryptOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.Write((byte)0);
    }

    private sealed class InstanceOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.WriteNullTerminated(string.Empty);
    }

    private sealed class ThreadIdOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
        {
            if (context.Features.Get<IEnvironmentFeature>() is { } feature)
            {
                writer.Write(feature.ThreadId);
            }
            else
            {
                writer.Write(0);
            }
        }
    }

    private sealed class MarsOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
            var reader = new SequenceReader<byte>(data);
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.Write(true);
    }

    private sealed class TraceIdOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
        {
            if (context.TraceId is { } traceId && context.Features.Get<ICorrelationFeature>() is { } feature)
            {
                writer.Write(traceId);
                writer.Write(feature.ActivityId);
                writer.Write(feature.SequenceId);
            }
        }
    }

    private sealed class FedAuthRequiredOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.Write(true);
    }
}
