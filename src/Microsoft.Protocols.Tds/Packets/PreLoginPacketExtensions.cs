using Microsoft.Protocols.Tds.Features;
using System.Buffers;

namespace Microsoft.Protocols.Tds.Packets;

public static class PreLoginPacketExtensions
{
    public static void AddPreLogin(this IPacketProcessorBuilder builder)
        => builder.AddPacket(TdsType.PreLogin, builder =>
        {
            builder.Add(new VersionOption());
            builder.Add(new EncryptOption());
            builder.Add(new InstanceOption());
            builder.Add(new ThreadIdOption());
            builder.Add(new MarsOption());
            builder.Add(new TraceIdOption());
            builder.Add(new FedAuthRequiredOption());
        });

    private sealed class VersionOption : IPacketOption
    {
        private static readonly Version _version = typeof(IPacketProcessorBuilder).Assembly.GetName().Version ?? Version.Parse("0.0.0");

        public void Read(TdsConnectionContext context, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.Write(_version);
    }

    private sealed class EncryptOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.Write((byte)0);
    }

    private sealed class InstanceOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.WriteNullTerminated(string.Empty);
    }

    private sealed class ThreadIdOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.Write((Int32)Environment.CurrentManagedThreadId);
    }

    private sealed class MarsOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.Write(true);
    }

    private sealed class TraceIdOption : IPacketOption
    {
        public void Read(TdsConnectionContext context, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
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
        public void Read(TdsConnectionContext context, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        public void Write(TdsConnectionContext context, IBufferWriter<byte> writer)
            => writer.Write(true);
    }

}
