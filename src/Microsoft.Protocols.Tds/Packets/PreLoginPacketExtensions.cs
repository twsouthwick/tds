using Microsoft.Protocols.Tds.Features;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

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
        private static readonly Version _version = typeof(IPacketCollectionBuilder).Assembly.GetName().Version ?? Version.Parse("0.0.0");

        public void Read(TdsConnectionContext context, in ReadOnlySequence<byte> data)
        {
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.Write(_version);
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
            => writer.Write(Environment.CurrentManagedThreadId);
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
